using UnityEditor;
using UnityEngine;

using Zlitz.Utilities;

namespace Zlitz.Sprites
{
    [CustomPropertyDrawer(typeof(SpriteOutput))]
    public class SpriteOutputPropertyDrawer : PropertyDrawer
    {
        private static readonly float s_borderWidth = 0.15f * EditorGUIUtility.singleLineHeight;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 4.5f * EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            property.serializedObject.Update();

            if (label != null && label.text != "")
            {
                float height = position.height;
                position.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.LabelField(position, label);
                position.height = height;
                position.x += position.width - position.height;
            }

            position.width = position.height;
            
            SpriteOutput spriteOutput = property.GetValue<SpriteOutput>();
            if (spriteOutput.type == SpriteOutput.Type.Animated || spriteOutput.type == SpriteOutput.Type.Pattern || spriteOutput.type == SpriteOutput.Type.RandomizedPattern)
            {
                EditorUtility.SetDirty(property.serializedObject.targetObject);
            }

            DrawSpriteOutput(position, spriteOutput);
        }

        public static void DrawSpriteOutput(Rect position, SpriteOutput spriteOutput)
        {
            EditorGUI.DrawRect(position, Color.black);

            position.x += s_borderWidth;
            position.y += s_borderWidth;

            switch (spriteOutput.type)
            {
                case SpriteOutput.Type.Single:
                    {
                        position.width -= 2.0f * s_borderWidth;
                        position.height -= 2.0f * s_borderWidth;
                        if (spriteOutput.sprites != null && spriteOutput.sprites.Length > 0)
                        {
                            Sprite sprite = spriteOutput.sprites[0];
                            EditorGUI.DrawTextureTransparent(position, sprite == null ? SpriteExtensions.transparent : sprite.GetTexture());
                        }
                        else
                        {
                            EditorGUI.DrawTextureTransparent(position, SpriteExtensions.transparent);
                        }
                        break;
                    }
                case SpriteOutput.Type.Randomized:
                    {
                        int n = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(spriteOutput.sprites == null ? 0 : spriteOutput.sprites.Length)));
                        position.width = (position.width - (n + 1.0f) * s_borderWidth) / n; ;
                        position.height = position.width;
                        Vector2 pos = position.position;
                        if (spriteOutput.sprites != null && spriteOutput.sprites.Length > 0)
                        {
                            for (int row = 0; row < n; row++)
                                for (int col = 0; col < n; col++)
                                {
                                    int index = row * n + col;
                                    if (index < spriteOutput.sprites.Length)
                                    {
                                        position.position = pos + new Vector2(col, row) * (position.width + s_borderWidth);
                                        Sprite sprite = spriteOutput.sprites[index];
                                        EditorGUI.DrawTextureTransparent(position, sprite == null ? SpriteExtensions.transparent : sprite.GetTexture());
                                    }
                                }
                        }
                        else
                        {
                            EditorGUI.DrawTextureTransparent(position, SpriteExtensions.transparent);
                        }
                        break;
                    }
                case SpriteOutput.Type.Animated:
                    {
                        position.width -= 2.0f * s_borderWidth;
                        position.height -= 2.0f * s_borderWidth;
                        if (spriteOutput.sprites != null && spriteOutput.sprites.Length > 0)
                        {
                            float frameTime = 1.0f / spriteOutput.averageSpeed;
                            float totalTime = spriteOutput.sprites.Length * frameTime;

                            float t = (float)EditorApplication.timeSinceStartup - Mathf.FloorToInt((float)EditorApplication.timeSinceStartup / totalTime) * totalTime;
                            int frame = Mathf.Clamp(Mathf.FloorToInt(t / frameTime), 0, spriteOutput.sprites.Length - 1);

                            Sprite sprite = spriteOutput.sprites[frame];
                            EditorGUI.DrawTextureTransparent(position, sprite == null ? SpriteExtensions.transparent : sprite.GetTexture());
                        }
                        else
                        {
                            EditorGUI.DrawTextureTransparent(position, SpriteExtensions.transparent);
                        }
                        break;
                    }
                case SpriteOutput.Type.Pattern:
                    {
                        position.width -= 2.0f * s_borderWidth;
                        position.height -= 2.0f * s_borderWidth;
                        SpritePatternEditor.DrawPattern(position, (spriteOutput.patterns == null || spriteOutput.patterns.Length == 0) ? null : spriteOutput.patterns[0]);
                        break;
                    }
                case SpriteOutput.Type.RandomizedPattern:
                    {
                        int n = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(spriteOutput.patterns == null ? 0 : spriteOutput.patterns.Length)));
                        position.width  = (position.width - (n + 1.0f) * s_borderWidth) / n;
                        position.height = position.width;
                        Vector2 pos = position.position;
                        if (spriteOutput.patterns != null && spriteOutput.patterns.Length > 0)
                        {
                            for (int row = 0; row < n; row++)
                                for (int col = 0; col < n; col++)
                                {
                                    int index = row * n + col;
                                    if (index < spriteOutput.patterns.Length)
                                    {
                                        position.position = pos + new Vector2(col, row) * (position.width + s_borderWidth);
                                        SpritePattern pattern = spriteOutput.patterns[index];
                                        SpritePatternEditor.DrawPattern(position, pattern);
                                    }
                                }
                        }
                        else
                        {
                            EditorGUI.DrawTextureTransparent(position, SpriteExtensions.transparent);
                        }
                        break;
                    }
            }
        }
    }
}
