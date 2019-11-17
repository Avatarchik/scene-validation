// Copyright (c) 2019 Nementic Games GmbH. All Rights Reserved.
// Author: Chris Yarbrough 

#if UNITY_EDITOR
namespace Nementic.Validation
{
    using System.Collections.Generic;
    using UnityEditor;

    public class ValidationSettings : CustomSettingsProvider
    {
        public static readonly Pref<bool> ValidateScenesBeforePlayMode = new BoolPref(
            "ValidateScenesBeforePlay", "Validate Scenes Before Play Mode", false);

        public static readonly Pref<bool> ValidateScenesDuringPlayMode = new BoolPref(
            "ValidateScenesDuringPlay", "Validate Scenes During Play Mode", false);

        public static readonly Pref<bool> ValidateInactiveGameObjects = new BoolPref(
            "ValidateInactiveGameObjects", "Validate Inactive GameObjects", true);

        public static readonly Pref<string> Namespaces = new StringPref(
            "Namespaces", "Validation Namespaces", "Nementic");

        public ValidationSettings() : base("Validation") { }

        [SettingsProvider]
        private static SettingsProvider CreateCustomPreferences()
        {
            return new ValidationSettings()
            {
                keywords = new HashSet<string>(new[] { "Validation", "Validate", "Scene", "Scenes" })
            };
        }
    }
}
#endif
