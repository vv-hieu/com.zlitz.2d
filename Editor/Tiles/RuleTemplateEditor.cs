using UnityEngine;
using UnityEditor;

namespace Zlitz.Tiles
{
    [CustomEditor(typeof(RuleTemplate))]
    public class RuleTemplateEditor : Editor
    {
        private SerializedProperty m_widthProperty;
        private SerializedProperty m_heightProperty;
        private SerializedProperty m_countProperty;
        private SerializedProperty m_colorsProperty;
        private SerializedProperty m_generateRulesProperty;
        private SerializedProperty m_elementsProperty;
        private Texture2D          m_texture;

        private bool m_foldout = true;

        public static Material s_solidColor;

        private void OnEnable()
        {
            if (s_solidColor == null)
            {
                s_solidColor = new Material(Shader.Find("Hidden/Internal-Colored"));
            }

            m_widthProperty         = serializedObject.FindProperty("m_width");
            m_heightProperty        = serializedObject.FindProperty("m_height");
            m_countProperty         = serializedObject.FindProperty("m_count");
            m_colorsProperty        = serializedObject.FindProperty("m_colors");
            m_generateRulesProperty = serializedObject.FindProperty("m_generateRules");
            m_elementsProperty      = serializedObject.FindProperty("m_elements");
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

            bool recolor = false;

            EditorGUI.BeginChangeCheck();

            m_countProperty.serializedObject.Update();
            EditorGUILayout.PropertyField(m_countProperty, new GUIContent("Tile Type Count"));
            m_countProperty.serializedObject.ApplyModifiedProperties();

            m_foldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_foldout, new GUIContent("Tiles"));
            if (m_foldout)
            {
                m_colorsProperty.serializedObject.Update();
                m_generateRulesProperty.serializedObject.Update();
                EditorGUI.indentLevel++;
                for (int i = 0; i < m_countProperty.intValue; i++)
                {
                    SerializedProperty colorProperty = m_colorsProperty.GetArrayElementAtIndex(i);
                    colorProperty.serializedObject.Update();
                    EditorGUILayout.PropertyField(colorProperty, new GUIContent("Tile " + i));
                    colorProperty.serializedObject.ApplyModifiedProperties();

                    EditorGUI.indentLevel++;
                    SerializedProperty generateRulesProperty = m_generateRulesProperty.GetArrayElementAtIndex(i);
                    generateRulesProperty.serializedObject.Update();
                    EditorGUILayout.PropertyField(generateRulesProperty, new GUIContent("Generate Rules"));
                    generateRulesProperty.serializedObject.ApplyModifiedProperties();
                    EditorGUI.indentLevel--;

                    EditorGUILayout.Space();
                }
                EditorGUI.indentLevel--;
                m_colorsProperty.serializedObject.ApplyModifiedProperties();
                m_generateRulesProperty.serializedObject.ApplyModifiedProperties();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            if (EditorGUI.EndChangeCheck())
            {
                recolor = true;
            }

            m_elementsProperty.serializedObject.Update();

            EditorGUILayout.LabelField("Configurations");

            Rect rectConfiguration = GUILayoutUtility.GetAspectRect(size.x * 1.0f / size.y);
            float gridSize = rectConfiguration.width / size.x;

            if (Event.current.type == EventType.MouseDown)
            {
                Vector2 mousePos = Event.current.mousePosition - rectConfiguration.position;
                if (mousePos.x >= 0.0f && mousePos.y >= 0.0f && mousePos.x <= rectConfiguration.width && mousePos.y <= rectConfiguration.height)
                {
                    int gridX = Mathf.FloorToInt(mousePos.x / gridSize);
                    int gridY = Mathf.FloorToInt(mousePos.y / gridSize);

                    bool edge = (
                        gridX == 0 || 
                        gridX == m_widthProperty.intValue - 1 ||
                        gridY == 0 || 
                        gridY == m_heightProperty.intValue - 1
                    );

                    SerializedProperty elementProperty = m_elementsProperty.GetArrayElementAtIndex(gridY * size.x + gridX);
                    elementProperty.serializedObject.Update();
                    if (edge)
                    {
                        elementProperty.intValue = (elementProperty.intValue + 3) % 2 - 2;
                    }
                    else
                    {
                        elementProperty.intValue = (elementProperty.intValue + 2 + (Event.current.button == 0 ? 1 : m_countProperty.intValue + 1)) % (m_countProperty.intValue + 2) - 2;
                    }
                    elementProperty.serializedObject.ApplyModifiedProperties();
                    recolor = true;
                }
            }

            if (m_texture == null)
            {
                recolor = true;

                m_texture = new Texture2D(size.x, size.y, TextureFormat.ARGB32, false);
                m_texture.filterMode = FilterMode.Point;
            }

            if (recolor)
            {
                for (int row = 0; row < size.y; row++)
                    for (int col = 0; col < size.x; col++)
                    {
                        Color color = Color.magenta;
                        SerializedProperty elementProperty = m_elementsProperty.GetArrayElementAtIndex(row * size.x + col);
                        int elementValue = elementProperty.intValue;
                        switch (elementValue)
                        {
                            case RuleTemplate.ANY:
                                color = Color.green;
                                break;
                            case RuleTemplate.NONE:
                                color = Color.red;
                                break;
                            default:
                                color = m_colorsProperty.GetArrayElementAtIndex(elementValue).colorValue;
                                break;
                        }
                        m_texture.SetPixel(col, size.y - 1 - row, color);
                    }

                m_texture.Apply();
            }

            EditorGUI.DrawPreviewTexture(rectConfiguration, m_texture);

            m_elementsProperty.serializedObject.ApplyModifiedProperties();

            s_solidColor?.SetPass(0);

            GL.PushMatrix();
            GL.Begin(GL.LINES);
            GL.Color(Color.black);
            for (int row = 0; row <= size.y; row++)
            {
                GL.Vertex(new Vector3(rectConfiguration.x, rectConfiguration.y + gridSize * row, 0.0f));
                GL.Vertex(new Vector3(rectConfiguration.x + rectConfiguration.width, rectConfiguration.y + gridSize * row, 0.0f));
            }
            for (int col = 0; col <= size.x; col++)
            {
                GL.Vertex(new Vector3(rectConfiguration.x + gridSize * col, rectConfiguration.y, 0.0f));
                GL.Vertex(new Vector3(rectConfiguration.x + gridSize * col, rectConfiguration.y + rectConfiguration.height, 0.0f));
            }
            GL.End();
            GL.PopMatrix();

            serializedObject.ApplyModifiedProperties();
        }
    }

}
