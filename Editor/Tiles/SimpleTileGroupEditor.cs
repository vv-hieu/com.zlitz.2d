using UnityEngine;
using UnityEditor;

namespace Zlitz.Tiles
{
    [CustomEditor(typeof(SimpleTileGroup))]
    public class SimpleTileGroupEditor : Editor
    {
        private SerializedProperty m_tilesProperty;

        private bool m_foldout = true;

        private void OnEnable()
        {
            m_tilesProperty = serializedObject.FindProperty("m_tiles");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            m_foldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_foldout, new GUIContent("Tiles"));
            if (m_foldout)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.indentLevel++;
                for (int i = 0; i < m_tilesProperty.arraySize; i++)
                {
                    SerializedProperty tileProperty = m_tilesProperty.GetArrayElementAtIndex(i);
                    tileProperty.serializedObject.Update();
                    EditorGUILayout.PropertyField(tileProperty, new GUIContent("Simple Tile " + i));
                }
                EditorGUI.indentLevel--;
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}
