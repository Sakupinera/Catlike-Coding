using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Object_Management.Editor
{
    /// <summary>
    /// 浮点数范围UI
    /// </summary>
    [CustomPropertyDrawer(typeof(FloatRange))]
    public class FloatRangeDrawer : PropertyDrawer
    {
        /// <summary>
        /// 绘制UI
        /// </summary>
        /// <param name="position"></param>
        /// <param name="property"></param>
        /// <param name="label"></param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            int originalIndentLevel = EditorGUI.indentLevel;
            float originalLabelWidth = EditorGUIUtility.labelWidth;

            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            position.width = position.width / 2f;
            EditorGUIUtility.labelWidth = position.width / 2f;
            EditorGUI.indentLevel = 1;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("min"));
            position.x += position.width;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("max"));
            EditorGUI.EndProperty();

            EditorGUI.indentLevel = originalIndentLevel;
            EditorGUIUtility.labelWidth = originalLabelWidth;
        }
    }
}
