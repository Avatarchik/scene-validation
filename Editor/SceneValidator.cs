// Copyright (c) 2019 Nementic Games GmbH. All Rights Reserved.
// Author: Chris Yarbrough 

#if UNITY_EDITOR
namespace Nementic.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEditor.ShortcutManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    /// <summary>
    /// Used to check if there are any designer errors in a given scene,
    /// for example, unassigned references or invalid GameObject configurations.
    /// </summary>
    public class SceneValidator
    {
        [MenuItem("Nementic/Scene Validation/Validate Open Scenes", priority = 10)]
        [Shortcut("Nementic/Scene Validation/Validate Open Scenes", KeyCode.O, ShortcutModifiers.Action | ShortcutModifiers.Alt)]
        public static void ValidateOpenScenes()
        {
            SceneValidator validator = new SceneValidator();
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                Scene scene = EditorSceneManager.GetSceneAt(i);
                validator.CreateValidationReport(scene).LogErrors();
            }
        }

        [MenuItem("Nementic/Scene Validation/Validate Active Scene", priority = 10)]
        [Shortcut("Nementic/Scene Validation/Validate Active Scene", KeyCode.A, ShortcutModifiers.Action | ShortcutModifiers.Alt)]
        public static void ValidateActiveScene()
        {
            Scene activeScene = EditorSceneManager.GetActiveScene();
            new SceneValidator().CreateValidationReport(activeScene).LogErrors();
        }

        private readonly ValidationReport report = new ValidationReport();
        private readonly List<GameObject> rootGameObjects = new List<GameObject>(64);
        private readonly List<MonoBehaviour> monoBehaviours = new List<MonoBehaviour>(8);

        /// <summary>
        /// Validates a given scene and returns the report, which can be used
        /// for logging or writing to file.
        /// </summary>
        public ValidationReport CreateValidationReport(Scene scene)
        {
            report.Clear();

            // Cleares and ensures capacity for us.
            scene.GetRootGameObjects(rootGameObjects);

            bool validateInactiveGameObjects = ValidationSettings.ValidateInactiveGameObjects;

            for (int i = 0; i < rootGameObjects.Count; i++)
            {
                ProcessGameObjectHierarchy(rootGameObjects[i], report, validateInactiveGameObjects);
            }
            return report;
        }

        private void ProcessGameObjectHierarchy(GameObject gameObject, ValidationReport report, bool validateInactiveGameObjects)
        {
            // GameObjects may be destroyed while processing them.
            if (gameObject == null || (validateInactiveGameObjects == false && gameObject.activeInHierarchy == false))
                return;

            ProcessGameObject(gameObject, report);

            for (int i = gameObject.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = gameObject.transform.GetChild(i);
                ProcessGameObjectHierarchy(child.gameObject, report, validateInactiveGameObjects);
            }
        }

        private void ProcessGameObject(GameObject gameObject, ValidationReport report)
        {
            gameObject.GetComponents(monoBehaviours);

            for (int i = 0; i < monoBehaviours.Count; i++)
            {
                ValidateMonoBehaviour(monoBehaviours[i], gameObject, report);
            }
        }

        private void ValidateMonoBehaviour(MonoBehaviour monoBehaviour, GameObject gameObject, ValidationReport report)
        {
            if (monoBehaviour == null)
            {
                report.AddError($"Missing script on {gameObject.name}.", gameObject);
                return;
            }

            Type type = monoBehaviour.GetType();

            // Skip Unity types inheriting from MonoBehaviour or plugin namespaces.
            // This call is pretty slow, but caching results in lookups has proven to be complex
            // and did not provide measurable performance improvements.
            if (ShouldTypeBeValidated(type) == false)
                return;

            var so = new SerializedObject(monoBehaviour);
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            SerializedProperty property = so.GetIterator();
            while (property.NextVisible(true))
            {
                if (property.propertyType == SerializedPropertyType.ObjectReference)
                {
                    FieldInfo field = FindMatchingField(fields, property, 0);
                    if (field != null)
                    {
                        var attributes = field.GetCustomAttributes();

                        // Ignore a field from the validation if it has the optional attribute applied.
                        if (attributes.Any(x => x.GetType() == typeof(OptionalAttribute)))
                            continue;

                        if (property.objectReferenceValue == null)
                        {
                            report.AddError(
                                 $"Missing required reference '{property.displayName}' ({type.Name}) on '{monoBehaviour.name}'.\n" +
                                 $"Scene: {GetSceneNameOrUnknownLabel(monoBehaviour.gameObject.scene)}", monoBehaviour);
                        }
                    }
                }
                else if (property.propertyType == SerializedPropertyType.Generic)
                {
                    // Handle simple arrays.
                    if (property.isArray && property.arraySize > 0)
                    {
                        var element = property.GetArrayElementAtIndex(0);
                        if (element.propertyType == SerializedPropertyType.ObjectReference)
                        {
                            for (int i = 0; i < property.arraySize; i++)
                            {
                                element = property.GetArrayElementAtIndex(i);
                                if (element.objectReferenceValue == null)
                                {
                                    report.AddError(
                                         $"Missing required array reference '{property.displayName}.{element.displayName}' ({type.Name}) on '{monoBehaviour.name}'.\n" +
                                         $"Scene: {GetSceneNameOrUnknownLabel(monoBehaviour.gameObject.scene)}", monoBehaviour);
                                }
                            }

                            // TODO: Arrays do not respect the optional attribute, they are always validated.
                            // To fix this, I need to find a way to retrieve the reflected field info for array elements.
                            // TODO: Handle nested arrays?
                        }
                    }
                }
            }
            so.Dispose();
        }

        private static FieldInfo FindMatchingField(FieldInfo[] fields, SerializedProperty property, int level)
        {
            if (level >= 5)
            {
                // Validation depth limit exceeded. This happens when we have difficult to detect recursion.
                return null;
            }

            // Match the Unity properties with reflected field info to retrieve custom attributes
            // from the field, because Unity doesn't store them.
            for (int i = 0; i < fields.Length; i++)
            {
                Type type = fields[i].FieldType;

                if (fields[i].Name == property.name)
                    return fields[i];

                // Check nested types.
                if (type.IsClass && ShouldTypeBeValidated(type))
                {
                    var field = FindMatchingField(
                        type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
                        property, level + 1);

                    if (field != null)
                        return field;
                }
            }
            return null;
        }

        private static bool ShouldTypeBeValidated(Type type)
        {
            if (type.Namespace == null)
                return false;

            string[] namespaceParts = type.Namespace.Split('.');
            string[] namespacesToBeValidated = ValidationSettings.Namespaces.Value.Split(';');

            for (int i = 0; i < namespacesToBeValidated.Length; i++)
            {
                for (int j = 0; j < namespaceParts.Length; j++)
                {
                    if (namespaceParts[j] == namespacesToBeValidated[i].Trim())
                        return true;
                }
            }

            return false;
        }

        private string GetSceneNameOrUnknownLabel(Scene scene)
        {
            return scene.name.Length > 0 ? scene.name : "Unknown";
        }
    }
}
#endif
