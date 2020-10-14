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
				report.AddError($"Missing script on GameObject '{gameObject.name}'.", gameObject);
				return;
			}

			Type type = monoBehaviour.GetType();

			if (ShouldValidate(type) == false)
				return;

			var so = new SerializedObject(monoBehaviour);
			var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

			SerializedProperty property = so.GetIterator();
			while (property.NextVisible(true))
			{
				if (property.propertyType == SerializedPropertyType.ObjectReference)
				{
					// A simple object reference on the MonoBehaviour.
					FieldInfo field = FindMatchingField(fields, property.name, 0);
					if (field != null)
					{
						var attributes = field.GetCustomAttributes();

						// Ignore a field from the validation if it has the optional attribute applied.
						if (attributes.Any(x => x.GetType() == typeof(OptionalAttribute)))
							continue;

						if (property.objectReferenceValue == null)
						{
							report.AddErrorFormatted(property.displayName, type.Name, monoBehaviour);
						}
					}
				}
				else if (property.propertyType == SerializedPropertyType.Generic)
				{
					// A serializable collection that has at least some entries.
					if (property.isArray && property.arraySize > 0)
					{
						// Check the first item to identify the item type of the whole array.
						var element = property.GetArrayElementAtIndex(0);
						if (element.propertyType == SerializedPropertyType.ObjectReference)
						{
							FieldInfo field = FindMatchingField(fields, property.name, 0);

							var attributes = field.GetCustomAttributes();

							// Ignore a field from the validation if it has the optional attribute applied.
							if (attributes.Any(x => x.GetType() == typeof(OptionalAttribute)))
								continue;

							for (int i = 0; i < property.arraySize; i++)
							{
								element = property.GetArrayElementAtIndex(i);
								if (element.objectReferenceValue == null)
								{
									report.AddErrorFormatted(
										$"{property.displayName}.{element.displayName}",
										type.Name, monoBehaviour, "Missing required array reference");
								}
							}
						}
					}
				}
			}
			so.Dispose();
		}

		private static bool ShouldValidate(Type type)
		{
			// Check if this script is part of the user defined assemblies.
			// This may be the builtin Assembly-CSharp, custom ASMDEF or any other.
			string typeAssemblyName = type.Assembly.GetName().Name;

			string[] assemblies = ValidationSettings.Assemblies.Value.Split(',');
			for (int i = 0; i < assemblies.Length; i++)
			{
				if (typeAssemblyName == assemblies[i].Trim())
					return true;
			}

			// If no assembly match has been found or no assemblies have been configured in the settings,
			// continue searching for valid types by their namespace. This could be useful if a user
			// keeps code in separate assemblies, but wants to validate all of them (and they all share
			// the same namespace).
			if (type.Namespace == null)
				return false;

			string[] namespaceParts = type.Namespace.Split('.');
			string[] namespacesToBeValidated = ValidationSettings.Namespaces.Value.Split(',');

			// TODO: This is not perfectly robust or may required more user input.
			// Currently, it checks for a match in any part of the type's namespace.
			// So, setting 'UI' as the namespace, would match 'Nementic.UI' and 'UnityEngine.UI'.
			// Maybe this should allow a more elaborate pattern such as 'Nementic.UI' in the settings.
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

		private static FieldInfo FindMatchingField(FieldInfo[] fields, string propertyName, int level)
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

				if (fields[i].Name == propertyName)
					return fields[i];

				// Check nested types.
				if (type.IsClass && ShouldValidate(type))
				{
					var field = FindMatchingField(
						type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
						propertyName, level + 1);

					if (field != null)
						return field;
				}
			}
			return null;
		}
	}
}
#endif
