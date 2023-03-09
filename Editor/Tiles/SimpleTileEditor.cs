using UnityEditor;
using UnityEngine;

namespace Zlitz.Tiles
{
    [CustomEditor(typeof(SimpleTile))]
    public class SimpleTileEditor : Editor
    {
        private SerializedProperty m_groupProperty;
        private SerializedProperty m_colliderTypeProperty;
        private SerializedProperty m_spriteProperty;

        private void OnEnable()
        {
            m_groupProperty        = serializedObject.FindProperty("m_group");
            m_colliderTypeProperty = serializedObject.FindProperty("m_colliderType");
            m_spriteProperty       = serializedObject.FindProperty("m_sprite");
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

            m_spriteProperty.serializedObject.Update();
            EditorGUILayout.PropertyField(m_spriteProperty, new GUIContent("Sprite"));
            m_spriteProperty.serializedObject.ApplyModifiedProperties();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
