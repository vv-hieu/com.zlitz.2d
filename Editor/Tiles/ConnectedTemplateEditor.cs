using UnityEngine;
using UnityEditor;

namespace Zlitz.Tiles
{
    [CustomEditor(typeof(ConnectedTemplate))]
    class ConnectedTemplateEditor : Editor
    {
        private SerializedProperty m_widthProperty;
        private SerializedProperty m_heightProperty;
        private SerializedProperty m_configurationsProperty;
        private Texture2D          m_texture;

        private static Material s_solidColor;
        private static Color    s_color1 = new Color(0.880f, 0.767f, 0.458f);
        private static Color    s_color2 = new Color(0.285f, 0.708f, 0.890f);
        private static Color    s_color3 = s_color1 * 0.95f;
        private static Color    s_color4 = s_color2 * 0.95f;

        private void OnEnable()
        {
            if (s_solidColor == null)
            {
                s_solidColor = new Material(Shader.Find("Hidden/Internal-Colored"));
            }

            m_widthProperty          = serializedObject.FindProperty("m_width");
            m_heightProperty         = serializedObject.FindProperty("m_height");
            m_configurationsProperty = serializedObject.FindProperty("m_configurations");
        }

        private void OnDestroy()
        {
            if (m_texture != null)
            {
                DestroyImmediate(m_texture);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            m_widthProperty.serializedObject.Update();
            m_heightProperty.serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            Vector2Int size = new Vector2Int(m_widthProperty.intValue, m_heightProperty.intValue);
            size = EditorGUILayout.Vector2IntField("Template size", size);
            size.x = Mathf.Max(1, size.x);
            size.y = Mathf.Max(1, size.y);
            m_widthProperty.intValue  = size.x;
            m_heightProperty.intValue = size.y;

            m_widthProperty.serializedObject.ApplyModifiedProperties();
            m_heightProperty.serializedObject.ApplyModifiedProperties();

            serializedObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
            {
                if (m_texture != null)
                {
                    DestroyImmediate(m_texture);
                    m_texture = null;
                }
            }

            serializedObject.Update();

            m_configurationsProperty.serializedObject.Update();

            if (m_texture == null)
            {
                m_texture = new Texture2D(size.x * 4, size.y * 4, TextureFormat.ARGB32, false);
                m_texture.filterMode = FilterMode.Point;

                for (int row = 0; row < size.y; row++)
                    for (int col = 0; col < size.x; col++)
                    {
                        int x = col;
                        int y = size.y - 1 - row;

                        int index = row * size.x + col;
                        SerializedProperty templateElementProperty = m_configurationsProperty.GetArrayElementAtIndex(index).FindPropertyRelative("m_configuration");
                        uint configuration = templateElementProperty.uintValue;

                        bool c0 = (configuration & ConnectedTile.Configuration.TOP)          == ConnectedTile.Configuration.TOP;
                        bool c1 = (configuration & ConnectedTile.Configuration.TOP_RIGHT)    == ConnectedTile.Configuration.TOP_RIGHT;
                        bool c2 = (configuration & ConnectedTile.Configuration.RIGHT)        == ConnectedTile.Configuration.RIGHT;
                        bool c3 = (configuration & ConnectedTile.Configuration.BOTTOM_RIGHT) == ConnectedTile.Configuration.BOTTOM_RIGHT;
                        bool c4 = (configuration & ConnectedTile.Configuration.BOTTOM)       == ConnectedTile.Configuration.BOTTOM;
                        bool c5 = (configuration & ConnectedTile.Configuration.BOTTOM_LEFT)  == ConnectedTile.Configuration.BOTTOM_LEFT;
                        bool c6 = (configuration & ConnectedTile.Configuration.LEFT)         == ConnectedTile.Configuration.LEFT;
                        bool c7 = (configuration & ConnectedTile.Configuration.TOP_LEFT)     == ConnectedTile.Configuration.TOP_LEFT;

                        m_texture.SetPixel(4 * x + 0, 4 * y + 3 - 0, c7 ? s_color1 : s_color2);
                        m_texture.SetPixel(4 * x + 1, 4 * y + 3 - 0, c0 ? s_color3 : s_color4);
                        m_texture.SetPixel(4 * x + 2, 4 * y + 3 - 0, c0 ? s_color3 : s_color4);
                        m_texture.SetPixel(4 * x + 3, 4 * y + 3 - 0, c1 ? s_color1 : s_color2);
                        m_texture.SetPixel(4 * x + 0, 4 * y + 3 - 1, c6 ? s_color3 : s_color4);
                        m_texture.SetPixel(4 * x + 1, 4 * y + 3 - 1, s_color1);
                        m_texture.SetPixel(4 * x + 2, 4 * y + 3 - 1, s_color1);
                        m_texture.SetPixel(4 * x + 3, 4 * y + 3 - 1, c2 ? s_color3 : s_color4);
                        m_texture.SetPixel(4 * x + 0, 4 * y + 3 - 2, c6 ? s_color3 : s_color4);
                        m_texture.SetPixel(4 * x + 1, 4 * y + 3 - 2, s_color1);
                        m_texture.SetPixel(4 * x + 2, 4 * y + 3 - 2, s_color1);
                        m_texture.SetPixel(4 * x + 3, 4 * y + 3 - 2, c2 ? s_color3 : s_color4);
                        m_texture.SetPixel(4 * x + 0, 4 * y + 3 - 3, c5 ? s_color1 : s_color2);
                        m_texture.SetPixel(4 * x + 1, 4 * y + 3 - 3, c4 ? s_color3 : s_color4);
                        m_texture.SetPixel(4 * x + 2, 4 * y + 3 - 3, c4 ? s_color3 : s_color4);
                        m_texture.SetPixel(4 * x + 3, 4 * y + 3 - 3, c3 ? s_color1 : s_color2);
                    }

                m_texture.Apply();
            }

            size = new Vector2Int(m_widthProperty.intValue, m_heightProperty.intValue);

            EditorGUILayout.LabelField("Configurations");

            Rect rectConfiguration = GUILayoutUtility.GetAspectRect(size.x * 1.0f / size.y);
            EditorGUI.DrawPreviewTexture(rectConfiguration, m_texture);
            float gridSize = rectConfiguration.width / size.x;

            bool changed = false;

            if (Event.current.type == EventType.MouseDown)
            {
                Vector2 mousePos = Event.current.mousePosition - rectConfiguration.position;
                if (mousePos.x >= 0.0f && mousePos.x <= rectConfiguration.width && mousePos.y >= 0.0f && mousePos.y <= rectConfiguration.height)
                {
                    int x = Mathf.FloorToInt(mousePos.x / gridSize);
                    int y = Mathf.FloorToInt(mousePos.y / gridSize);
                    mousePos.x -= x * gridSize;
                    mousePos.y -= y * gridSize;
                    mousePos.x /= gridSize;
                    mousePos.y /= gridSize;

                    int index = y * size.x + x;
                    SerializedProperty configurationProperty = m_configurationsProperty.GetArrayElementAtIndex(index).FindPropertyRelative("m_configuration");
                    configurationProperty.serializedObject.Update();
                    uint configuration = configurationProperty.uintValue;

                    bool top         = ((configuration & ConnectedTile.Configuration.TOP)          == ConnectedTile.Configuration.TOP);
                    bool bottom      = ((configuration & ConnectedTile.Configuration.BOTTOM)       == ConnectedTile.Configuration.BOTTOM);
                    bool right       = ((configuration & ConnectedTile.Configuration.RIGHT)        == ConnectedTile.Configuration.RIGHT);
                    bool left        = ((configuration & ConnectedTile.Configuration.LEFT)         == ConnectedTile.Configuration.LEFT);
                    bool topRight    = ((configuration & ConnectedTile.Configuration.TOP_RIGHT)    == ConnectedTile.Configuration.TOP_RIGHT);
                    bool topLeft     = ((configuration & ConnectedTile.Configuration.TOP_LEFT)     == ConnectedTile.Configuration.TOP_LEFT);
                    bool bottomRight = ((configuration & ConnectedTile.Configuration.BOTTOM_RIGHT) == ConnectedTile.Configuration.BOTTOM_RIGHT);
                    bool bottomLeft  = ((configuration & ConnectedTile.Configuration.BOTTOM_LEFT)  == ConnectedTile.Configuration.BOTTOM_LEFT);
                
                    if (mousePos.x >= 0.25f && mousePos.x <= 0.75f && mousePos.y >= 0.0f && mousePos.y <= 0.25f)
                    {
                        if (top)
                        {
                            configuration &= ~ConnectedTile.Configuration.TOP_RIGHT;
                            configuration &= ~ConnectedTile.Configuration.TOP_LEFT;
                            m_texture.SetPixel(x * 4 + 0, size.y * 4 - 1 - (y * 4 + 0), s_color2);
                            m_texture.SetPixel(x * 4 + 3, size.y * 4 - 1 - (y * 4 + 0), s_color2);
                        }
                        configuration ^= ConnectedTile.Configuration.TOP;
                        top = !top;
                        m_texture.SetPixel(x * 4 + 1, size.y * 4 - 1 - (y * 4 + 0), 0.95f * (top ? s_color1 : s_color2));
                        m_texture.SetPixel(x * 4 + 2, size.y * 4 - 1 - (y * 4 + 0), 0.95f * (top ? s_color1 : s_color2));
                        changed = true;
                    }
                    else if (mousePos.x >= 0.25f && mousePos.x <= 0.75f && mousePos.y >= 0.75f && mousePos.y <= 1.0f)
                    {
                        if (bottom)
                        {
                            configuration &= ~ConnectedTile.Configuration.BOTTOM_RIGHT;
                            configuration &= ~ConnectedTile.Configuration.BOTTOM_LEFT;
                            m_texture.SetPixel(x * 4 + 0, size.y * 4 - 1 - (y * 4 + 3), s_color2);
                            m_texture.SetPixel(x * 4 + 3, size.y * 4 - 1 - (y * 4 + 3), s_color2);
                        }
                        configuration ^= ConnectedTile.Configuration.BOTTOM;
                        bottom = !bottom;
                        m_texture.SetPixel(x * 4 + 1, size.y * 4 - 1 - (y * 4 + 3), 0.95f * (bottom ? s_color1 : s_color2));
                        m_texture.SetPixel(x * 4 + 2, size.y * 4 - 1 - (y * 4 + 3), 0.95f * (bottom ? s_color1 : s_color2));
                        changed = true;
                    }
                    else if (mousePos.x >= 0.75f && mousePos.x <= 1.0f && mousePos.y >= 0.25f && mousePos.y <= 0.75f)
                    {
                        if (right)
                        {
                            configuration &= ~ConnectedTile.Configuration.TOP_RIGHT;
                            configuration &= ~ConnectedTile.Configuration.BOTTOM_RIGHT;
                            m_texture.SetPixel(x * 4 + 3, size.y * 4 - 1 - (y * 4 + 0), s_color2);
                            m_texture.SetPixel(x * 4 + 3, size.y * 4 - 1 - (y * 4 + 3), s_color2);
                        }
                        configuration ^= ConnectedTile.Configuration.RIGHT;
                        right = !right;
                        m_texture.SetPixel(x * 4 + 3, size.y * 4 - 1 - (y * 4 + 1), 0.95f * (right ? s_color1 : s_color2));
                        m_texture.SetPixel(x * 4 + 3, size.y * 4 - 1 - (y * 4 + 2), 0.95f * (right ? s_color1 : s_color2));
                        changed = true;
                    }
                    else if (mousePos.x >= 0.0f && mousePos.x <= 0.25f && mousePos.y >= 0.25f && mousePos.y <= 0.75f)
                    {
                        if (left)
                        {
                            configuration &= ~ConnectedTile.Configuration.TOP_LEFT;
                            configuration &= ~ConnectedTile.Configuration.BOTTOM_LEFT;
                            m_texture.SetPixel(x * 4 + 0, size.y * 4 - 1 - (y * 4 + 0), s_color2);
                            m_texture.SetPixel(x * 4 + 0, size.y * 4 - 1 - (y * 4 + 3), s_color2);
                        }
                        configuration ^= ConnectedTile.Configuration.LEFT;
                        left = !left;
                        m_texture.SetPixel(x * 4 + 0, size.y * 4 - 1 - (y * 4 + 1), 0.95f * (left ? s_color1 : s_color2));
                        m_texture.SetPixel(x * 4 + 0, size.y * 4 - 1 - (y * 4 + 2), 0.95f * (left ? s_color1 : s_color2));
                        changed = true;
                    }
                    else if (mousePos.x >= 0.75f && mousePos.x <= 1.0f && mousePos.y >= 0.0f && mousePos.y <= 0.25f)
                    {
                        if (!topRight)
                        {
                            configuration |= ConnectedTile.Configuration.TOP;
                            configuration |= ConnectedTile.Configuration.RIGHT;
                            m_texture.SetPixel(x * 4 + 1, size.y * 4 - 1 - (y * 4 + 0), 0.95f * s_color1);
                            m_texture.SetPixel(x * 4 + 2, size.y * 4 - 1 - (y * 4 + 0), 0.95f * s_color1);
                            m_texture.SetPixel(x * 4 + 3, size.y * 4 - 1 - (y * 4 + 1), 0.95f * s_color1);
                            m_texture.SetPixel(x * 4 + 3, size.y * 4 - 1 - (y * 4 + 2), 0.95f * s_color1);
                        }
                        configuration ^= ConnectedTile.Configuration.TOP_RIGHT;
                        topRight = !topRight;
                        m_texture.SetPixel(x * 4 + 3, size.y * 4 - 1 - (y * 4 + 0), topRight ? s_color1 : s_color2);
                        changed = true;
                    }
                    else if (mousePos.x >= 0.0f && mousePos.x <= 0.25f && mousePos.y >= 0.0f && mousePos.y <= 0.25f)
                    {
                        if (!topLeft)
                        {
                            configuration |= ConnectedTile.Configuration.TOP;
                            configuration |= ConnectedTile.Configuration.LEFT;
                            m_texture.SetPixel(x * 4 + 1, size.y * 4 - 1 - (y * 4 + 0), 0.95f * s_color1);
                            m_texture.SetPixel(x * 4 + 2, size.y * 4 - 1 - (y * 4 + 0), 0.95f * s_color1);
                            m_texture.SetPixel(x * 4 + 0, size.y * 4 - 1 - (y * 4 + 1), 0.95f * s_color1);
                            m_texture.SetPixel(x * 4 + 0, size.y * 4 - 1 - (y * 4 + 2), 0.95f * s_color1);
                        }
                        configuration ^= ConnectedTile.Configuration.TOP_LEFT;
                        topLeft = !topLeft;
                        m_texture.SetPixel(x * 4 + 0, size.y * 4 - 1 - (y * 4 + 0), topLeft ? s_color1 : s_color2);
                        changed = true;
                    }
                    else if (mousePos.x >= 0.75f && mousePos.x <= 1.0f && mousePos.y >= 0.75f && mousePos.y <= 1.0f)
                    {
                        if (!bottomRight)
                        {
                            configuration |= ConnectedTile.Configuration.BOTTOM;
                            configuration |= ConnectedTile.Configuration.RIGHT;
                            m_texture.SetPixel(x * 4 + 1, size.y * 4 - 1 - (y * 4 + 3), 0.95f * s_color1);
                            m_texture.SetPixel(x * 4 + 2, size.y * 4 - 1 - (y * 4 + 3), 0.95f * s_color1);
                            m_texture.SetPixel(x * 4 + 3, size.y * 4 - 1 - (y * 4 + 1), 0.95f * s_color1);
                            m_texture.SetPixel(x * 4 + 3, size.y * 4 - 1 - (y * 4 + 2), 0.95f * s_color1);
                        }
                        configuration ^= ConnectedTile.Configuration.BOTTOM_RIGHT;
                        bottomRight = !bottomRight;
                        m_texture.SetPixel(x * 4 + 3, size.y * 4 - 1 - (y * 4 + 3), bottomRight ? s_color1 : s_color2);
                        changed = true;
                    }
                    else if (mousePos.x >= 0.0f && mousePos.x <= 0.25f && mousePos.y >= 0.75f && mousePos.y <= 1.0f)
                    {
                        if (!bottomLeft)
                        {
                            configuration |= ConnectedTile.Configuration.BOTTOM;
                            configuration |= ConnectedTile.Configuration.LEFT;
                            m_texture.SetPixel(x * 4 + 1, size.y * 4 - 1 - (y * 4 + 3), 0.95f * s_color1);
                            m_texture.SetPixel(x * 4 + 2, size.y * 4 - 1 - (y * 4 + 3), 0.95f * s_color1);
                            m_texture.SetPixel(x * 4 + 0, size.y * 4 - 1 - (y * 4 + 1), 0.95f * s_color1);
                            m_texture.SetPixel(x * 4 + 0, size.y * 4 - 1 - (y * 4 + 2), 0.95f * s_color1);
                        }
                        configuration ^= ConnectedTile.Configuration.BOTTOM_LEFT;
                        bottomLeft = !bottomLeft;
                        m_texture.SetPixel(x * 4 + 0, size.y * 4 - 1 - (y * 4 + 3), bottomLeft ? s_color1 : s_color2);
                        changed = true;
                    }

                    configurationProperty.uintValue = configuration;
                    configurationProperty.serializedObject.ApplyModifiedProperties();
                }
            }

            m_configurationsProperty.serializedObject.ApplyModifiedProperties();

            if (changed)
            {
                m_texture.Apply();
            }

            s_solidColor?.SetPass(0);

            GL.PushMatrix();
            GL.Begin(GL.LINES);
            GL.Color(Color.black);
            for (int row = 0; row <= size.y; row++)
            {
                GL.Vertex(new Vector3(rectConfiguration.x                          , rectConfiguration.y + gridSize * row, 0.0f));
                GL.Vertex(new Vector3(rectConfiguration.x + rectConfiguration.width, rectConfiguration.y + gridSize * row, 0.0f));
            }
            for (int col = 0; col <= size.x; col++)
            {
                GL.Vertex(new Vector3(rectConfiguration.x + gridSize * col, rectConfiguration.y                           , 0.0f));
                GL.Vertex(new Vector3(rectConfiguration.x + gridSize * col, rectConfiguration.y + rectConfiguration.height, 0.0f));
            }
            GL.End();
            GL.PopMatrix();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
