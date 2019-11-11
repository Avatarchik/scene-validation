// Copyright (c) 2019 Nementic Games GmbH. All Rights Reserved.
// Author: Chris Yarbrough

#if UNITY_EDITOR
namespace Nementic.Validation
{
    using System;
    using UnityEditor;
    using UnityEngine;

    public interface IPref
    {
        void DrawProperty();
        void Reset();
    }

    public abstract class Pref<T> : IPref where T : IEquatable<T>
    {
        protected readonly string key;
        protected readonly T defaultValue;
        protected string displayText;

        private T cachedValue = default;
        private bool cacheInitialized = false;
        private GUIContent label;

        public Pref(string key, T defaultValue = default) : this(key, null, defaultValue)
        {
        }

        public Pref(string key, string displayText, T defaultValue = default)
        {
            this.key = key;
            this.defaultValue = defaultValue;
            this.displayText = displayText;
        }

        public static implicit operator T(Pref<T> pref)
        {
            return pref.Value;
        }

        public T Value
        {
            get
            {
                if (cacheInitialized == false)
                {
                    if (HasPrefKey(key))
                        cachedValue = ReadValueFromPrefs();
                    else
                        cachedValue = defaultValue;

                    cacheInitialized = true;
                }

                return cachedValue;
            }
            set
            {
                WriteValueToPrefs(value);
                cachedValue = value;
                cacheInitialized = true;
            }
        }

        protected virtual bool HasPrefKey(string key)
        {
            return EditorPrefs.HasKey(key);
        }

        protected abstract T ReadValueFromPrefs();
        protected abstract void WriteValueToPrefs(T value);

        public void Reset()
        {
            Value = defaultValue;
        }

        public void DrawProperty()
        {
            if (displayText == null)
                displayText = ObjectNames.NicifyVariableName(key);

            DrawProperty(displayText);
        }

        public void DrawProperty(string labelText)
        {
            if (this.label == null)
                this.label = new GUIContent();

            this.label.text = labelText;
            DrawProperty(this.label);
        }

        public void DrawProperty(GUIContent label)
        {
            EditorGUI.BeginChangeCheck();
            T newValue = DrawProperty(label, Value);
            if (EditorGUI.EndChangeCheck())
                Value = newValue;
        }

        protected abstract T DrawProperty(GUIContent label, T value);

        public override string ToString()
        {
            return Value != null ? Value.ToString() : base.ToString();
        }
    }

    public class BoolPref : Pref<bool>
    {
        public BoolPref(string key, bool defaultValue = false) : base(key, defaultValue)
        {
        }

        public BoolPref(string key, string displayText, bool defaultValue = false) : base(key, displayText, defaultValue)
        {
        }

        protected override bool DrawProperty(GUIContent label, bool value)
        {
            return EditorGUILayout.Toggle(label, value);
        }

        protected override bool ReadValueFromPrefs() => EditorPrefs.GetBool(key, defaultValue);

        protected override void WriteValueToPrefs(bool value) => EditorPrefs.SetBool(key, value);
    }

    public class ColorPref : Pref<Color>
    {
        private static readonly string[] suffixes = new string[] { "R", "G", "B", "A" };
        private string[] cachedKeys = new string[4];

        public ColorPref(string key, float r, float g, float b)
            : this(key, new Color(r, g, b, 1f)) { }

        public ColorPref(string key, float r, float g, float b, float a)
            : this(key, new Color(r, g, b, a)) { }

        public ColorPref(string key, Color defaultValue = default)
            : base(key, defaultValue)
        {
            for (int i = 0; i < 4; i++)
                cachedKeys[i] = key + suffixes[i];
        }

        public ColorPref(string key, string displayText, Color defaultValue = default)
            : base(key, displayText, defaultValue)
        {
            for (int i = 0; i < 4; i++)
                cachedKeys[i] = key + suffixes[i];
        }

        protected override Color DrawProperty(GUIContent label, Color value)
        {
            return EditorGUILayout.ColorField(label, value);
        }

        protected override Color ReadValueFromPrefs()
        {
            return new Color(
                EditorPrefs.GetFloat(cachedKeys[0], 1f),
                EditorPrefs.GetFloat(cachedKeys[1], 1f),
                EditorPrefs.GetFloat(cachedKeys[2], 1f),
                EditorPrefs.GetFloat(cachedKeys[3], 1f)
            );
        }

        protected override void WriteValueToPrefs(Color value)
        {
            for (int i = 0; i < 4; i++)
                EditorPrefs.SetFloat(cachedKeys[i], value[i]);
        }

        protected override bool HasPrefKey(string key)
        {
            return EditorPrefs.HasKey(cachedKeys[0]);
        }
    }

    public class FloatPref : Pref<float>
    {
        public FloatPref(string key, float defaultValue = 0) : base(key, defaultValue)
        {
        }

        public FloatPref(string key, string displayText, float defaultValue = 0) : base(key, displayText, defaultValue)
        {
        }

        protected override float DrawProperty(GUIContent label, float value)
        {
            return EditorGUILayout.FloatField(label, value);
        }

        protected override float ReadValueFromPrefs() => EditorPrefs.GetFloat(key);

        protected override void WriteValueToPrefs(float value) => EditorPrefs.SetFloat(key, value);
    }

    public class IntPref : Pref<int>
    {
        public IntPref(string key, int defaultValue = 0) : base(key, defaultValue)
        {
        }

        public IntPref(string key, string displayText, int defaultValue = 0) : base(key, displayText, defaultValue)
        {
        }

        protected override int DrawProperty(GUIContent label, int value)
        {
            return EditorGUILayout.IntField(label, value);
        }

        protected override int ReadValueFromPrefs() => EditorPrefs.GetInt(key);

        protected override void WriteValueToPrefs(int value) => EditorPrefs.SetInt(key, value);

        public void DrawProperty(string label, int minValue)
        {
            EditorGUI.BeginChangeCheck();
            int newValue = EditorGUILayout.IntField(label, Value);
            if (EditorGUI.EndChangeCheck())
            {
                if (newValue < minValue)
                    newValue = minValue;

                Value = newValue;
            }
        }
    }

    public class StringPref : Pref<string>
    {
        public StringPref(string key, string defaultValue = "") : base(key, defaultValue)
        {
        }

        public StringPref(string key, string displayText, string defaultValue = "") : base(key, displayText, defaultValue)
        {
        }

        protected override string DrawProperty(GUIContent label, string value)
        {
            return EditorGUILayout.TextField(label, value);
        }

        protected override string ReadValueFromPrefs() => EditorPrefs.GetString(key);

        protected override void WriteValueToPrefs(string value) => EditorPrefs.SetString(key, value);
    }
}
#endif