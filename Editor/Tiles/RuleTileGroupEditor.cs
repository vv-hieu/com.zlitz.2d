using UnityEngine;
using UnityEditor;

namespace Zlitz.Tiles
{
    [CustomEditor(typeof(RuleTileGroup))]
    public class RuleTileGroupEditor : Editor
    {
        private SerializedProperty m_entriesProperty;

        private bool m_foldout = true;

        private void OnEnable()
        {
            m_entriesProperty = serializedObject.FindProperty("m_entries");   
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            m_foldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_foldout, new GUIContent("Tiles"));
            if (m_foldout)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.indentLevel++;
                for (int i = 0; i < m_entriesProperty.arraySize; i++)
                {
                    SerializedProperty entryProperty = m_entriesProperty.GetArrayElementAtIndex(i);
                    entryProperty.serializedObject.Update();
                    SerializedProperty tileProperty = entryProperty.FindPropertyRelative("m_tile");
                    tileProperty.serializedObject.Update();
                    EditorGUILayout.PropertyField(tileProperty, new GUIContent("Rule Tile " + i));
                }
                EditorGUI.indentLevel--;
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}
