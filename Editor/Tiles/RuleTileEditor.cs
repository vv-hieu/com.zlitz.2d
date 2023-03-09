using UnityEditor;
using UnityEngine;

namespace Zlitz.Tiles
{
    [CustomEditor(typeof(RuleTile))]
    public class RuleTileEditor : Editor
    {
        private SerializedProperty m_colliderTypeProperty;
        private SerializedProperty m_groupProperty;
        private SerializedProperty m_tileIdProperty;

        private void OnEnable()
        {
            m_colliderTypeProperty = serializedObject.FindProperty("m_colliderType");
            m_groupProperty        = serializedObject.FindProperty("m_group");
            m_tileIdProperty       = serializedObject.FindProperty("m_tileId");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            m_groupProperty.serializedObject.Update();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(m_groupProperty, new GUIContent("Tile Group"));
            EditorGUI.EndDisabledGroup();

            m_colliderTypeProperty.serializedObject.Update();
            EditorGUILayout.PropertyField(m_colliderTypeProperty, new GUIContent("Collider Type"));
            m_colliderTypeProperty.serializedObject.ApplyModifiedProperties();

            m_tileIdProperty.serializedObject.Update();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(m_tileIdProperty, new GUIContent("Tile ID"));
            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
