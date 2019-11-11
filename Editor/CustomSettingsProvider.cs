// Copyright (c) 2019 Nementic Games GmbH. All Rights Reserved.
// Author: Chris Yarbrough

#if UNITY_EDITOR
namespace Nementic.Validation
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// A <see cref="SettingsProvider"/> which adds the default margins
    /// and shared styling to custom preferences.
    /// </summary>
    public abstract class CustomSettingsProvider : SettingsProvider
    {
        // Caution: When this string is changed, all users lose their saved preferences.
        private const string baseKey = "Nementic/";

        private readonly List<IPref> properties;

        public CustomSettingsProvider(string path) : base("Nementic/" + path, SettingsScope.User)
        {
            properties = new List<IPref>();

            var fields = this.GetType().GetFields(BindingFlags.Static | BindingFlags.Public);
            foreach (var iPrefType in fields.Where(f => typeof(IPref).IsAssignableFrom(f.FieldType)))
                properties.Add((IPref)iPrefType.GetValue(this));
        }

        public override void OnGUI(string searchContext)
        {
            // Unity's own preferences have a left margin and max width,
            // but we need to manually recreate this to match the style.
            EditorGUIUtility.labelWidth = 200f;
            EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(380));
            GUILayout.Space(10);
            EditorGUILayout.BeginVertical();
            CustomGUI(searchContext);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            Event e = Event.current;
            if (e.type == EventType.ContextClick)
            {
                HandleContextMenu();
                e.Use();
            }
        }

        private void HandleContextMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Reset to defaults"), false, Reset);
            menu.ShowAsContext();
        }

        protected virtual void Reset()
        {
            for (int i = 0; i < properties.Count; i++)
                properties[i].Reset();
        }

        protected virtual void CustomGUI(string searchContext)
        {
            EditorGUI.BeginChangeCheck();
            for (int i = 0; i < properties.Count; i++)
                Draw(properties[i]);
            if (EditorGUI.EndChangeCheck())
                OnValuesChanged();
        }

        protected virtual void OnValuesChanged()
        {
        }

        protected static void Draw(IPref pref)
        {
            pref.DrawProperty();
        }
    }
}
#endif
