using UnityEngine;
using UnityEditor;

namespace Zlitz.Sprites
{
    [CustomEditor(typeof(SpritePatternGroup))]
    public class SpritePatternGroupEditor : Editor
    {
        private SerializedProperty m_widthProperty;
        private SerializedProperty m_heightProperty;
        private SerializedProperty m_patternsProperty;

        private bool m_foldout = true;

        private void OnEnable()
        {
            m_widthProperty    = serializedObject.FindProperty("m_width");
            m_heightProperty   = serializedObject.FindProperty("m_height");
            m_patternsProperty = serializedObject.FindProperty("m_patterns");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            m_widthProperty.serializedObject.Update();
            EditorGUILayout.PropertyField(m_widthProperty, new GUIContent("Width"));
            m_heightProperty.serializedObject.Update();
            EditorGUILayout.PropertyField(m_heightProperty, new GUIContent("Height"));
            EditorGUI.EndDisabledGroup();

            m_foldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_foldout, new GUIContent("Patterns"));
            if (m_foldout)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.indentLevel++;
                m_patternsProperty.serializedObject.Update();
                for (int i = 0; i < m_patternsProperty.arraySize; i++)
                {
                    SerializedProperty patternProperty = m_patternsProperty.GetArrayElementAtIndex(i);
                    patternProperty.serializedObject.Update();
                    EditorGUILayout.PropertyField(patternProperty, new GUIContent("Pattern " + i));
                }
                EditorGUI.indentLevel--;
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}