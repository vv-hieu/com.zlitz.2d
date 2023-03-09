using UnityEngine;
using UnityEditor;

namespace Zlitz.Tiles
{
    [CustomEditor(typeof(ConnectedTileGroup))]
    public class ConnectedTileGroupEditor : Editor
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
                m_tilesProperty.serializedObject.Update();

                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.indentLevel++;

                SerializedProperty visibleTileProperty = m_tilesProperty.GetArrayElementAtIndex(0);
                visibleTileProperty.serializedObject.Update();
                EditorGUILayout.PropertyField(visibleTileProperty, new GUIContent("Visible Tile"));

                SerializedProperty InvisbleTileProperty = m_tilesProperty.GetArrayElementAtIndex(1);
                InvisbleTileProperty.serializedObject.Update();
                EditorGUILayout.PropertyField(InvisbleTileProperty, new GUIContent("Invisible Tile"));

                EditorGUI.indentLevel--;
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}
