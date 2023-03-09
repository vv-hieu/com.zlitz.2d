using UnityEngine;
using UnityEditor;

using Zlitz.Utilities;

namespace Zlitz.Sprites
{
    [CustomEditor(typeof(SpritePattern))]
    public class SpritePatternEditor : Editor
    {
        private SerializedProperty m_groupProperty;
        private SerializedProperty m_spritesProperty;

        private bool m_foldout = true;

        private static readonly Color s_borderColor = new Color(0.4f, 0.8f, 0.6f);

        private void OnEnable()
        {
            m_groupProperty   = serializedObject.FindProperty("m_group");
            m_spritesProperty = serializedObject.FindProperty("m_sprites");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            m_groupProperty.serializedObject.Update();
            EditorGUILayout.PropertyField(m_groupProperty, new GUIContent("Sprite Pattern Group"));
            EditorGUI.EndDisabledGroup();

            m_foldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_foldout, new GUIContent("Pattern"));
            if (m_foldout)
            {
                m_spritesProperty.serializedObject.Update();
                SpritePatternGroup group = m_groupProperty.GetValue<SpritePatternGroup>();

                Rect rect = GUILayoutUtility.GetRect(1.0f, group.height * 4.5f * EditorGUIUtility.singleLineHeight + 6.0f);
                rect.width = 4.5f * EditorGUIUtility.singleLineHeight * group.width + 6.0f;
                DrawPattern(rect, (SpritePattern)serializedObject.targetObject);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        public static void DrawPattern(Rect position, SpritePattern pattern)
        {
            EditorGUI.DrawRect(position, s_borderColor);

            if (pattern != null)
            {
                int maxDim = Mathf.Max(pattern.width, pattern.height);
                float gridSize = (position.height - 6.0f) / maxDim;

                Rect rectSprite = new Rect(0.0f, 0.0f, gridSize, gridSize);
                float offsetX = 0.5f * (position.width - 6.0f - gridSize * pattern.width);
                float offsetY = 0.5f * (position.height - 6.0f - gridSize * pattern.height);
                for (int row = 0; row < pattern.height; row++)
                {
                    rectSprite.y = position.y + offsetY + 3.0f + row * gridSize;
                    for (int col = 0; col < pattern.width; col++)
                    {
                        rectSprite.x = position.x + offsetX + 3.0f + col * gridSize;
                        SpriteOutputPropertyDrawer.DrawSpriteOutput(rectSprite, pattern.GetSprite(col, pattern.height - 1 - row));
                    }
                }
            }
        }
    }
}