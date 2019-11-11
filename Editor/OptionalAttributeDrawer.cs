// Copyright (c) 2019 Nementic Games GmbH. All Rights Reserved.
// Author: Chris Yarbrough 

#if UNITY_EDITOR
namespace Nementic.Validation
{
    using System.Linq;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(OptionalAttribute))]
    public class OptionalAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attributes = fieldInfo.GetCustomAttributes();

            if (attributes.Any(x => x.GetType() == typeof(OptionalAttribute)))
            {
                // Indicate that this field is optional.
                label.text += " (Optional)";
            }

            EditorGUI.PropertyField(position, property, label);
        }
    }
}
#endif