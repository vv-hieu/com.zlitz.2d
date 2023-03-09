using UnityEngine;
using UnityEditor;

namespace Zlitz.Tiles
{
    [CustomEditor(typeof(ConnectedTile))]
    public class ConnectedTileEditor : Editor
    {
        private SerializedProperty m_groupProperty;
        private SerializedProperty m_colliderTypeProperty;
        private SerializedProperty m_rulesProperty;

        private bool m_rulesFoldout = false;

        private void OnEnable()
        {
            m_groupProperty         = serializedObject.FindProperty("m_group");
            m_colliderTypeProperty  = serializedObject.FindProperty("m_colliderType");
            m_rulesProperty         = serializedObject.FindProperty("m_rules");
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

            m_rulesFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_rulesFoldout, new GUIContent("Rules"));
            if (m_rulesFoldout)
            {
                m_rulesProperty.serializedObject.Update();
                int rulesPerRow = Mathf.Max(1, Mathf.FloorToInt(EditorGUIUtility.currentViewWidth / (9.0f * EditorGUIUtility.singleLineHeight)));
                int idx = 0;
                while (idx < m_rulesProperty.arraySize)
                {
                    EditorGUILayout.BeginHorizontal();
                    for (int i = 0; i < rulesPerRow; i++)
                    {
                        SerializedProperty entryProperty = m_rulesProperty.GetArrayElementAtIndex(idx);
                        entryProperty.serializedObject.Update();
                        EditorGUILayout.PropertyField(entryProperty, GUIContent.none, GUILayout.Width(9.0f * EditorGUIUtility.singleLineHeight), GUILayout.Height(4.5f * EditorGUIUtility.singleLineHeight));
                        idx++;
                        if (idx >= m_rulesProperty.arraySize)
                        {
                            break;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space();
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomPropertyDrawer(typeof(ConnectedTile.Configuration))]
    public class ConnectedTileConfigurationPropertyDrawer : PropertyDrawer
    {
        private static Color s_color1 = new Color(0.880f, 0.767f, 0.458f);
        private static Color s_color2 = new Color(0.285f, 0.708f, 0.890f);

        private Texture2D m_texture;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 4.5f * EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (m_texture == null)
            {
                m_texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);

                m_texture.filterMode = FilterMode.Point;

                m_texture.SetPixel(0, 0, s_color2);
                m_texture.SetPixel(3, 0, s_color2);
                m_texture.SetPixel(0, 3, s_color2);
                m_texture.SetPixel(3, 3, s_color2);

                m_texture.SetPixel(1, 0, s_color2 * 0.95f);
                m_texture.SetPixel(2, 0, s_color2 * 0.95f);
                m_texture.SetPixel(0, 1, s_color2 * 0.95f);
                m_texture.SetPixel(0, 2, s_color2 * 0.95f);
                m_texture.SetPixel(1, 3, s_color2 * 0.95f);
                m_texture.SetPixel(2, 3, s_color2 * 0.95f);
                m_texture.SetPixel(3, 1, s_color2 * 0.95f);
                m_texture.SetPixel(3, 2, s_color2 * 0.95f);

                m_texture.SetPixel(1, 1, s_color1);
                m_texture.SetPixel(1, 2, s_color1);
                m_texture.SetPixel(2, 1, s_color1);
                m_texture.SetPixel(2, 2, s_color1);
            }

            property.serializedObject.Update();

            SerializedProperty configurationProperty = property.FindPropertyRelative("m_configuration");
            configurationProperty.serializedObject.Update();

            uint configuration = configurationProperty.uintValue;

            bool top = ((configuration & ConnectedTile.Configuration.TOP) == ConnectedTile.Configuration.TOP);
            m_texture.SetPixel(1, 3, 0.95f * (top ? s_color1 : s_color2));
            m_texture.SetPixel(2, 3, 0.95f * (top ? s_color1 : s_color2));

            bool bottom = ((configuration & ConnectedTile.Configuration.BOTTOM) == ConnectedTile.Configuration.BOTTOM);
            m_texture.SetPixel(1, 0, 0.95f * (bottom ? s_color1 : s_color2));
            m_texture.SetPixel(2, 0, 0.95f * (bottom ? s_color1 : s_color2));

            bool right = ((configuration & ConnectedTile.Configuration.RIGHT) == ConnectedTile.Configuration.RIGHT);
            m_texture.SetPixel(3, 1, 0.95f * (right ? s_color1 : s_color2));
            m_texture.SetPixel(3, 2, 0.95f * (right ? s_color1 : s_color2));

            bool left = ((configuration & ConnectedTile.Configuration.LEFT) == ConnectedTile.Configuration.LEFT);
            m_texture.SetPixel(0, 1, 0.95f * (left ? s_color1 : s_color2));
            m_texture.SetPixel(0, 2, 0.95f * (left ? s_color1 : s_color2));

            bool topRight = ((configuration & ConnectedTile.Configuration.TOP_RIGHT) == ConnectedTile.Configuration.TOP_RIGHT);
            m_texture.SetPixel(3, 3, topRight ? s_color1 : s_color2);
            
            bool topLeft = ((configuration & ConnectedTile.Configuration.TOP_LEFT) == ConnectedTile.Configuration.TOP_LEFT);
            m_texture.SetPixel(0, 3, topLeft ? s_color1 : s_color2);

            bool bottomRight = ((configuration & ConnectedTile.Configuration.BOTTOM_RIGHT) == ConnectedTile.Configuration.BOTTOM_RIGHT);
            m_texture.SetPixel(3, 0, bottomRight ? s_color1 : s_color2);

            bool bottomLeft = ((configuration & ConnectedTile.Configuration.BOTTOM_LEFT) == ConnectedTile.Configuration.BOTTOM_LEFT);
            m_texture.SetPixel(0, 0, bottomLeft ? s_color1 : s_color2);

            m_texture.Apply();
            
            if (label != null && label.text != "")
            {
                float height = position.height;
                position.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.LabelField(position, label);
                position.height = height;
                position.x += position.width - position.height;
            }

            position.x += 0.15f * EditorGUIUtility.singleLineHeight;
            position.y += 0.15f * EditorGUIUtility.singleLineHeight;
            position.width = position.height = position.height - 0.3f * EditorGUIUtility.singleLineHeight;
            EditorGUI.DrawPreviewTexture(position, m_texture);

            property.serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomPropertyDrawer(typeof(ConnectedTile.Rule))]
    public class ConnectedTileRulePropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 4.5f * EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty spriteProperty        = property.FindPropertyRelative("m_sprite");
            SerializedProperty configurationProperty = property.FindPropertyRelative("m_configuration");

            property.serializedObject.Update();

            if (label != null && label.text != "")
            {
                float height = position.height;
                position.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.LabelField(position, label);
                position.height = height;
                position.x += position.width - 2.0f * position.height;
            }

            position.width = position.height;
            spriteProperty.serializedObject.Update();
            EditorGUI.PropertyField(position, spriteProperty, GUIContent.none);
            spriteProperty.serializedObject.ApplyModifiedProperties();

            position.x += position.width;
            configurationProperty.serializedObject.Update();
            EditorGUI.PropertyField(position, configurationProperty, GUIContent.none);
            configurationProperty.serializedObject.ApplyModifiedProperties();

            property.serializedObject.ApplyModifiedProperties();
        }
    }
}