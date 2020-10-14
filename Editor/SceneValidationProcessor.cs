// Copyright (c) 2019 Nementic Games GmbH. All Rights Reserved.
// Author: Chris Yarbrough 

#if UNITY_EDITOR
namespace Nementic.Validation
{
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;
    using UnityEditor.SceneManagement;
    using UnityEngine.SceneManagement;

    /// <summary>
    /// Runs during a build or before play mode is entered and
    /// processes involved scenes with e.g. validation.
    /// </summary>
    public class SceneValidationProcessor : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        private readonly SceneValidator sceneValidator = new SceneValidator();

        public void OnProcessScene(Scene scene, BuildReport buildReport)
        {
            if (ValidationSettings.ValidateScenesDuringPlayMode)
            {
                var validationReport = sceneValidator.CreateValidationReport(scene);
                validationReport.LogErrors();
            }
        }

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (ValidationSettings.ValidateScenesBeforePlayMode && change == PlayModeStateChange.ExitingEditMode)
            {
                var validator = new SceneValidator();
                bool hasErrors = false;

                for (int i = 0; i < EditorSceneManager.sceneCount; i++)
                {
                    var scene = EditorSceneManager.GetSceneAt(i);
                    if (scene.isLoaded)
                    {
                        var validationReport = validator.CreateValidationReport(scene);
                        validationReport.LogErrors();

                        if (validationReport.HasErrors)
                            hasErrors = true;
                    }
                }

                // Abort entering play mode.
                if (hasErrors)
                {
                    EditorApplication.isPlaying = false;

                    var sceneView = SceneView.lastActiveSceneView;
                    if (sceneView != null)
                        sceneView.ShowNotification(new UnityEngine.GUIContent("- Play Mode Validation Failed -\nSee the console window for details."), 3.0);
                }
            }
        }
    }
}
#endif