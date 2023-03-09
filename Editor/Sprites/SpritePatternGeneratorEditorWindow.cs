using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Zlitz.Sprites
{
    public class SpritePatternGeneratorEditorWindow : EditorWindow
    {
        private Vector2 m_scrollPos;
        private bool    m_queueRepaint;

        private int      m_count     = 0;
        private int      m_width     = 0;
        private int      m_height    = 0;
        private EditMode m_editMode  = EditMode.Individual;
        private float    m_viewScale = 1.0f;

        private int             m_selectedSpriteOutput        = -1;
        private int             m_editingSpriteOutput         = -1;
        private bool            m_selectedSpriteOutputFoldout = true;
        private Sprite[]        m_outputSprites;
        private SpritePattern[] m_outputPatterns;
        private float[]         m_weights;
        private float           m_minSpeed;
        private float           m_maxSpeed;
        private float           m_randomOffset;
        private int             m_offset;
        private bool            m_vertical;
        private SpriteOutput    m_clipboard = null;

        private SpriteOutput.Type[] m_globalOutputTypes;
        private bool                m_addingOrReplacing = false;
        private float               m_globalWeight;
        private float               m_globalMinSpeed;
        private float               m_globalMaxSpeed;
        private float               m_globalRandomOffset;
        private int                 m_globalOffset;
        private bool                m_globalVertical;

        [SerializeField]
        private SpriteOutput[]     m_spriteOutputs;
        private SerializedProperty m_spriteOutputsProperty;

        [SerializeField]
        private Sprite[]           m_newSprites;
        private SerializedProperty m_newSpritesProperty;

        [SerializeField]
        private SpritePattern[]    m_newPatterns;
        private SerializedProperty m_newPatternsProperty;

        [MenuItem("Zlitz/Tools/Sprite Pattern Generator")]
        public static void ShowWindow()
        {
            GetWindow<SpritePatternGeneratorEditorWindow>("Sprite Pattern Generator");
        }

        private void OnEnable()
        {
            m_newSpritesProperty    = new SerializedObject(this).FindProperty("m_newSprites");
            m_newPatternsProperty   = new SerializedObject(this).FindProperty("m_newPatterns");
            m_spriteOutputsProperty = new SerializedObject(this).FindProperty("m_spriteOutputs");
        }

        private void Update()
        {
            if (m_queueRepaint)
            {
                m_queueRepaint = false;
                Repaint();
            }
        }

        private void OnGUI()
        {
            m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos);

            // Pattern count and size
            int newCount  = Mathf.Max(1, EditorGUILayout.IntField(new GUIContent("Count"), m_count));
            int newWidth  = Mathf.Max(1, EditorGUILayout.IntField(new GUIContent("Width"), m_width));
            int newHeight = Mathf.Max(1, EditorGUILayout.IntField(new GUIContent("Height"), m_height));
            if (m_count != newCount || m_width != newWidth || m_height != newHeight)
            {
                p_OnCountOrSizeChanged(newCount, newWidth, newHeight);
                m_count  = newCount;
                m_width  = newWidth;
                m_height = newHeight;
            }

            if (m_spriteOutputs != null && m_spriteOutputs.Length > 0)
            {
                // Edit mode
                EditMode newEditMode = (EditMode)EditorGUILayout.EnumPopup(new GUIContent("Edit Mode"), m_editMode);
                if (m_editMode != newEditMode)
                {
                    m_editMode = newEditMode;
                    p_OnEditModeChanged();
                }

                // Multiple editing
                if (m_editMode == EditMode.Multiple && m_editingSpriteOutput >= 0)
                {
                    // Sprite output type
                    SpriteOutput.Type newGlobalOutputType = (SpriteOutput.Type)EditorGUILayout.EnumPopup(new GUIContent("Output Type"), m_globalOutputTypes[m_editingSpriteOutput % (m_width * m_height)]);
                    if (m_globalOutputTypes[m_editingSpriteOutput % (m_width * m_height)] != newGlobalOutputType)
                    {
                        m_globalOutputTypes[m_editingSpriteOutput % (m_width * m_height)] = newGlobalOutputType;
                        p_OnGlobalOutputTypeChanged(m_editingSpriteOutput % (m_width * m_height));
                    }

                    // Handle multiple editing for each output type
                    if (m_editingSpriteOutput >= 0)
                    {
                        switch (m_globalOutputTypes[m_editingSpriteOutput % (m_width * m_height)])
                        {
                            case SpriteOutput.Type.Single:
                                {
                                    p_MultipleEdit_Single();
                                    break;
                                }
                            case SpriteOutput.Type.Randomized:
                                {
                                    p_MultipleEdit_Randomized();
                                    break;
                                }
                            case SpriteOutput.Type.Animated:
                                {
                                    p_MultipleEdit_Animated();
                                    break;
                                }
                            case SpriteOutput.Type.Pattern:
                                {
                                    p_MultipleEdit_Pattern();
                                    break;
                                }
                            case SpriteOutput.Type.RandomizedPattern:
                                {
                                    p_MultipleEdit_RandomizedPattern();
                                    break;
                                }
                        }
                    }
                }

                // Repaint when there is at least one animated sprite output
                if (m_spriteOutputs != null)
                {
                    foreach (SpriteOutput spriteOutput in m_spriteOutputs)
                    {
                        if (spriteOutput.type == SpriteOutput.Type.Animated || spriteOutput.type == SpriteOutput.Type.Pattern || spriteOutput.type == SpriteOutput.Type.RandomizedPattern)
                        {
                            m_queueRepaint = true;
                            break;
                        }
                    }
                }

                // Main
                m_spriteOutputsProperty.serializedObject.Update();

                m_viewScale = EditorGUILayout.Slider(new GUIContent("View Scale"), m_viewScale, 0.0f, 1.0f);
                float gridSize = Mathf.Max(0.01f, m_viewScale) * 4.5f * EditorGUIUtility.singleLineHeight;

                Vector2 patternSize = new Vector2(m_width, m_height) * gridSize;

                int patternsPerRow = Mathf.Max(Mathf.FloorToInt(position.width / (patternSize.x + 12.0f)));
                int patternRows   = (m_count - 1) / patternsPerRow + 1;

                Rect rect = GUILayoutUtility.GetRect(1.0f, patternRows * (patternSize.y + 12.0f));

                Rect rectPattern = new Rect(0.0f, .0f, patternSize.x + 3.0f, patternSize.y + 3.0f);
                for (int row = 0; row < patternRows; row++)
                {
                    rectPattern.y = rect.y + (patternSize.y + 12.0f) * row;
                    for (int col = 0; col < patternsPerRow; col++)
                    {
                        rectPattern.x = rect.x + (patternSize.x + 12.0f) * col;
                        int index = row * patternsPerRow + col;
                        if (index < m_count)
                        {
                            EditorGUI.DrawRect(rectPattern, Color.white);

                            Rect rectSprite = new Rect(0.0f, 0.0f, gridSize, gridSize);
                            for (int row2 = 0; row2 < m_height; row2++)
                            {
                                rectSprite.y = rectPattern.y + 1.5f + gridSize * row2;
                                for (int col2 = 0; col2 < m_width; col2++)
                                {
                                    rectSprite.x = rectPattern.x + 1.5f + gridSize * col2;
                                    int index2 = (index * m_height + row2) * m_width + col2;
                                    SerializedProperty spriteProperty = m_spriteOutputsProperty.GetArrayElementAtIndex(index2);
                                    EditorGUI.PropertyField(rectSprite, spriteProperty, GUIContent.none);

                                    if (Event.current.type == EventType.MouseDown &&
                                        Event.current.mousePosition.x >= rectSprite.x &&
                                        Event.current.mousePosition.x <= rectSprite.x + rectSprite.width &&
                                        Event.current.mousePosition.y >= rectSprite.y &&
                                        Event.current.mousePosition.y <= rectSprite.y + rectSprite.height
                                    )
                                    {
                                        if (m_editingSpriteOutput == index2)
                                        {
                                            if (Event.current.button == 1)
                                            {
                                                p_Select(-1);
                                            }
                                        }
                                        else
                                        {
                                            if (Event.current.button == 0)
                                            {
                                                p_Select(index2);
                                            }
                                        }
                                    }

                                    if (m_editingSpriteOutput >= 0 && index2 % (m_width * m_height) == m_editingSpriteOutput % (m_width * m_height))
                                    {
                                        if (index2 == m_editingSpriteOutput)
                                        {
                                            EditorGUI.DrawRect(rectSprite, new Color(0.0f, 1.0f, 1.0f, 0.5f));
                                        }
                                        else if (m_addingOrReplacing)
                                        {
                                            EditorGUI.DrawRect(rectSprite, new Color(0.5f, 1.0f, 0.5f, 0.5f));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                p_EditCurrentSpriteOutput();

                if (GUILayout.Button("Generate"))
                {
                    string path = EditorUtility.SaveFilePanelInProject("Select Folder", "", "asset", "Please enter a file name");
                    if (path != "")
                    {
                        string filename = Path.GetFileNameWithoutExtension(path);

                        SpritePatternGroup group = (SpritePatternGroup)CreateInstance(typeof(SpritePatternGroup));
                        AssetDatabase.CreateAsset(group, path);
                        AssetDatabase.SaveAssets();

                        SpritePattern[] patterns = new SpritePattern[m_count];

                        for (int i = 0; i < m_count; i++)
                        {
                            SpriteOutput[] sprites = new SpriteOutput[m_width * m_height];
                            for (int j = 0; j < m_width * m_height; j++)
                            {
                                sprites[j] = m_spriteOutputs[i * m_width * m_height + j];
                            }

                            patterns[i] = (SpritePattern)CreateInstance(typeof(SpritePattern));
                            patterns[i].Initialize(group, sprites);
                            patterns[i].name = filename + "_" + i;
                            AssetDatabase.AddObjectToAsset(patterns[i], group);
                        }

                        group.Initialize(patterns, m_width, m_height);

                        EditorUtility.SetDirty(group);
                        AssetDatabase.SaveAssets();

                        Close();
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void p_OnCountOrSizeChanged(int newCount, int newWidth, int newHeight)
        {
            SpriteOutput[] newSprites = new SpriteOutput[newCount * newWidth * newHeight];
            for (int i = 0; i < newCount; i++)
                for (int row = 0; row < newHeight; row++)
                    for (int col = 0; col < newWidth; col++)
                    {
                        if (m_spriteOutputs == null || m_spriteOutputs.Length == 0 || i >= m_count || row >= m_height || col >= m_width)
                        {
                            newSprites[(i * newHeight + row) * newWidth + col] = SpriteOutput.Single(null);
                        }
                        else
                        {
                            newSprites[(i * newHeight + row) * newWidth + col] = m_spriteOutputs[(i * m_height + row) * m_width + col];
                        }
                    }
            m_spriteOutputs = newSprites;

            SpriteOutput.Type[] newGlobalOutputTypes = new SpriteOutput.Type[newWidth * newHeight];
            for (int row = 0; row < newHeight; row++)
                for (int col = 0; col < newWidth; col++)
                {
                    if (m_globalOutputTypes == null || m_globalOutputTypes.Length == 0 || row >= m_height || col >= m_width)
                    {
                        newGlobalOutputTypes[row * newWidth + col] = SpriteOutput.Type.Single;
                    }
                    else
                    {
                        newGlobalOutputTypes[row * newWidth + col] = m_globalOutputTypes[row * m_width + col];
                    }
                }
            m_globalOutputTypes = newGlobalOutputTypes;
            if (m_editMode == EditMode.Multiple)
            {
                for (int i = 0; i < newWidth * newHeight; i++)
                {
                    p_OnGlobalOutputTypeChanged(i);
                }
            }

            m_selectedSpriteOutput = -1;
            m_editingSpriteOutput  = -1;
        }

        private void p_OnEditModeChanged()
        {
            // Reset global editing variables
            m_addingOrReplacing  = false;
            m_globalWeight       = 1.0f;
            m_globalMinSpeed     = 1.0f;
            m_globalMaxSpeed     = 1.0f;
            m_globalRandomOffset = 0.0f;
            m_globalOffset       = 0;
            m_globalVertical     = false;

            if (m_editMode == EditMode.Multiple)
            {
                for (int i = 0; i < m_width * m_height; i++)
                {
                    p_OnGlobalOutputTypeChanged(i);
                }
            }
        }

        private void p_OnGlobalOutputTypeChanged(int index)
        {
            if (m_spriteOutputs != null && index >= 0 && index < m_width * m_height)
            {
                // Set all current sprite output to coresponding type
                switch (m_globalOutputTypes[index])
                {
                    case SpriteOutput.Type.Single:
                        {
                            for (int i = 0; i < m_count; i++)
                            {
                                m_spriteOutputs[i * m_width * m_height + index] = SpriteOutput.Single((m_spriteOutputs[i * m_width * m_height + index].sprites == null || m_spriteOutputs[i * m_width * m_height + index].sprites.Length == 0) ? null : m_spriteOutputs[i * m_width * m_height + index].sprites[0]);
                            }
                            break;
                        }
                    case SpriteOutput.Type.Randomized:
                        {
                            for (int i = 0; i < m_count; i++)
                            {
                                float[] newWeights = new float[m_spriteOutputs[i * m_width * m_height + index].sprites == null ? 0 : m_spriteOutputs[i * m_width * m_height + index].sprites.Length];
                                for (int j = 0; j < newWeights.Length; j++)
                                {
                                    newWeights[j] = 1.0f;
                                }
                                m_spriteOutputs[i * m_width * m_height + index] = SpriteOutput.Randomized(m_spriteOutputs[i * m_width * m_height + index].sprites, newWeights);
                            }
                            break;
                        }
                    case SpriteOutput.Type.Animated:
                        {
                            for (int i = 0; i < m_count; i++)
                            {
                                m_spriteOutputs[i * m_width * m_height + index] = SpriteOutput.Animated(m_spriteOutputs[i * m_width * m_height + index].sprites, 1.0f, 1.0f, 0.0f);
                            }
                            break;
                        }
                    case SpriteOutput.Type.Pattern:
                        {
                            for (int i = 0; i < m_count; i++)
                            {
                                m_spriteOutputs[i * m_width * m_height + index] = SpriteOutput.Pattern((m_spriteOutputs[i * m_width * m_height + index].patterns == null || m_spriteOutputs[i * m_width * m_height + index].patterns.Length == 0) ? null : m_spriteOutputs[i * m_width * m_height + index].patterns[0], 0, false);
                            }
                            break;
                        }
                    case SpriteOutput.Type.RandomizedPattern:
                        {
                            for (int i = 0; i < m_count; i++)
                            {
                                float[] newWeights = new float[m_spriteOutputs[i * m_width * m_height + index].patterns == null ? 0 : m_spriteOutputs[i * m_width * m_height + index].patterns.Length];
                                for (int j = 0; j < newWeights.Length; j++)
                                {
                                    newWeights[j] = 1.0f;
                                }
                                m_spriteOutputs[i * m_width * m_height + index] = SpriteOutput.RandomizedPattern(m_spriteOutputs[i * m_width * m_height + index].patterns, newWeights, 0, false);
                            }
                            break;
                        }
                }
                m_addingOrReplacing = false;
                m_newSprites        = null;
                m_newPatterns       = null;

                // Reload selected sprite output
                m_editingSpriteOutput = -1;
            }
        }

        private void p_Select(int index)
        {
            m_selectedSpriteOutput = index;
            m_queueRepaint = true;
        }

        private void p_EditCurrentSpriteOutput()
        {
            if (m_spriteOutputs != null)
            {
                // Handle selection changed
                if (m_editingSpriteOutput != m_selectedSpriteOutput)
                {
                    m_editingSpriteOutput = m_selectedSpriteOutput;
                    p_EditCurrentSpriteOutput_OnSelectedIndexChanged();
                }

                if (m_editingSpriteOutput < 0 || m_editingSpriteOutput >= m_spriteOutputs.Length)
                {
                    return;
                }

                // Sprite output properties
                m_selectedSpriteOutputFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_selectedSpriteOutputFoldout, new GUIContent("Sprite Output Properties"));
                if (m_selectedSpriteOutputFoldout)
                {
                    EditorGUI.BeginDisabledGroup(m_editMode != EditMode.Individual);
                    EditorGUILayout.BeginHorizontal();
                    SpriteOutput.Type newType = (SpriteOutput.Type)EditorGUILayout.EnumPopup(new GUIContent("Output Type"), m_spriteOutputs[m_editingSpriteOutput].type);
                    if (newType != m_spriteOutputs[m_editingSpriteOutput].type)
                    {
                        p_EditCurrentSpriteOutput_OnOutputTypeChanged(newType);
                    }
                    if (GUILayout.Button("Copy", GUILayout.Width(60.0f)))
                    {
                        m_clipboard = m_spriteOutputs[m_selectedSpriteOutput];
                    }
                    EditorGUI.BeginDisabledGroup(m_clipboard == null);
                    if (GUILayout.Button("Paste", GUILayout.Width(60.0f)))
                    {
                        m_spriteOutputs[m_selectedSpriteOutput] = m_clipboard;
                        p_EditCurrentSpriteOutput_OnSelectedIndexChanged();
                    }
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.EndHorizontal();

                    switch (m_spriteOutputs[m_editingSpriteOutput].type)
                    {
                        case SpriteOutput.Type.Single:
                            {
                                EditorGUILayout.LabelField("Sprite");
                                Sprite newSprite = (Sprite)EditorGUILayout.ObjectField(GUIContent.none, m_outputSprites[0], typeof(Sprite), true, GUILayout.Width(4.5f * EditorGUIUtility.singleLineHeight), GUILayout.Height(4.5f * EditorGUIUtility.singleLineHeight));
                                if (m_outputSprites[0] != newSprite)
                                {
                                    m_outputSprites[0] = newSprite;

                                    m_spriteOutputs[m_editingSpriteOutput] = SpriteOutput.Single(newSprite);
                                }
                                break;
                            }
                        case SpriteOutput.Type.Randomized:
                            {
                                bool changed = false;
                                int remove = -1;
                                int add = -1;
                                EditorGUILayout.LabelField("Sprites");
                                for (int i = 0; i < m_outputSprites.Length; i++)
                                {
                                    EditorGUILayout.BeginHorizontal();
                                    Sprite newSprite = (Sprite)EditorGUILayout.ObjectField(GUIContent.none, m_outputSprites[i], typeof(Sprite), true, GUILayout.Width(4.5f * EditorGUIUtility.singleLineHeight), GUILayout.Height(4.5f * EditorGUIUtility.singleLineHeight));
                                    EditorGUILayout.BeginVertical();
                                    EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
                                    EditorGUILayout.LabelField("Weight");
                                    float newWeight = Mathf.Max(0.0f, EditorGUILayout.FloatField(m_weights[i]));
                                    EditorGUILayout.BeginHorizontal();
                                    if (GUILayout.Button("Add"))
                                    {
                                        add = i;
                                    }
                                    EditorGUI.BeginDisabledGroup(m_outputSprites.Length <= 1);
                                    if (GUILayout.Button("Remove"))
                                    {
                                        remove = i;
                                    }
                                    EditorGUI.EndDisabledGroup();
                                    EditorGUILayout.EndHorizontal();
                                    EditorGUILayout.EndVertical();
                                    EditorGUILayout.EndHorizontal();
                                    if (newSprite != m_outputSprites[i] || newWeight != m_weights[i])
                                    {
                                        m_outputSprites[i] = newSprite;
                                        m_weights[i] = newWeight;

                                        changed = true;
                                    }
                                }
                                if (remove >= 0)
                                {
                                    List<Sprite> newSprites = new List<Sprite>(m_outputSprites);
                                    List<float> newWeights = new List<float>(m_weights);

                                    newSprites.RemoveAt(remove);
                                    newWeights.RemoveAt(remove);

                                    m_outputSprites = newSprites.ToArray();
                                    m_weights = newWeights.ToArray();

                                    changed = true;
                                }
                                if (add >= 0)
                                {
                                    List<Sprite> newSprites = new List<Sprite>(m_outputSprites);
                                    List<float> newWeights = new List<float>(m_weights);

                                    newSprites.Insert(add, null);
                                    newWeights.Insert(add, 1.0f);

                                    m_outputSprites = newSprites.ToArray();
                                    m_weights = newWeights.ToArray();

                                    changed = true;
                                }
                                if (GUILayout.Button("Add"))
                                {
                                    List<Sprite> newSprites = new List<Sprite>(m_outputSprites);
                                    List<float> newWeights = new List<float>(m_weights);

                                    newSprites.Add(null);
                                    newWeights.Add(1.0f);

                                    m_outputSprites = newSprites.ToArray();
                                    m_weights = newWeights.ToArray();

                                    changed = true;
                                }
                                if (changed)
                                {
                                    m_spriteOutputs[m_editingSpriteOutput] = SpriteOutput.Randomized(m_outputSprites, m_weights);
                                }
                                break;
                            }
                        case SpriteOutput.Type.Animated:
                            {
                                bool changed = false;
                                int remove = -1;
                                int add = -1;
                                float newMinSpeed = EditorGUILayout.FloatField(new GUIContent("Min Speed"), m_minSpeed);
                                if (newMinSpeed != m_minSpeed)
                                {
                                    m_minSpeed = newMinSpeed;
                                    changed = true;
                                }
                                float newMaxSpeed = EditorGUILayout.FloatField(new GUIContent("Max Speed"), m_maxSpeed);
                                if (newMaxSpeed != m_maxSpeed)
                                {
                                    m_maxSpeed = newMaxSpeed;
                                    changed = true;
                                }
                                float newRandomOffset = EditorGUILayout.FloatField(new GUIContent("Random Offset"), m_randomOffset);
                                if (newRandomOffset != m_randomOffset)
                                {
                                    m_randomOffset = newRandomOffset;
                                    changed = true;
                                }
                                EditorGUILayout.LabelField("Sprites");
                                for (int i = 0; i < m_outputSprites.Length; i++)
                                {
                                    EditorGUILayout.BeginHorizontal();
                                    Sprite newSprite = (Sprite)EditorGUILayout.ObjectField(GUIContent.none, m_outputSprites[i], typeof(Sprite), true, GUILayout.Width(4.5f * EditorGUIUtility.singleLineHeight), GUILayout.Height(4.5f * EditorGUIUtility.singleLineHeight));
                                    EditorGUILayout.BeginVertical();
                                    EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
                                    if (GUILayout.Button("Add"))
                                    {
                                        add = i;
                                    }
                                    EditorGUI.BeginDisabledGroup(m_outputSprites.Length <= 1);
                                    if (GUILayout.Button("Remove"))
                                    {
                                        remove = i;
                                    }
                                    EditorGUI.EndDisabledGroup();
                                    EditorGUILayout.EndVertical();
                                    EditorGUILayout.EndHorizontal();
                                    if (newSprite != m_outputSprites[i])
                                    {
                                        m_outputSprites[i] = newSprite;

                                        changed = true;
                                    }
                                }
                                if (remove >= 0)
                                {
                                    List<Sprite> newSprites = new List<Sprite>(m_outputSprites);

                                    newSprites.RemoveAt(remove);

                                    m_outputSprites = newSprites.ToArray();

                                    changed = true;
                                }
                                if (add >= 0)
                                {
                                    List<Sprite> newSprites = new List<Sprite>(m_outputSprites);

                                    newSprites.Insert(add, null);

                                    m_outputSprites = newSprites.ToArray();

                                    changed = true;
                                }
                                if (GUILayout.Button("Add"))
                                {
                                    List<Sprite> newSprites = new List<Sprite>(m_outputSprites);
                                    List<float> newWeights = new List<float>(m_weights);

                                    newSprites.Add(null);
                                    newWeights.Add(1.0f);

                                    m_outputSprites = newSprites.ToArray();
                                    m_weights = newWeights.ToArray();

                                    changed = true;
                                }
                                if (changed)
                                {
                                    m_spriteOutputs[m_editingSpriteOutput] = SpriteOutput.Animated(m_outputSprites, m_minSpeed, m_maxSpeed, m_randomOffset);
                                }
                                break;
                            }
                        case SpriteOutput.Type.Pattern:
                            {
                                bool changed = false;
                                int newOffset = EditorGUILayout.IntField(new GUIContent("Offset"), m_offset);
                                if (m_offset != newOffset)
                                {
                                    m_offset = newOffset;
                                    changed = true;
                                }
                                bool newVertical = EditorGUILayout.Toggle(new GUIContent("Vertical"), m_vertical);
                                if (m_vertical != newVertical)
                                {
                                    m_vertical = newVertical;
                                    changed = true;
                                }
                                SpritePattern newPattern = (SpritePattern)EditorGUILayout.ObjectField(new GUIContent("Pattern"), m_outputPatterns[0], typeof(SpritePattern), true);
                                if (m_outputPatterns[0] != newPattern)
                                {
                                    m_outputPatterns[0] = newPattern;
                                    changed = true;
                                }
                                EditorGUILayout.BeginHorizontal();
                                EditorGUILayout.Space();
                                Rect rectPattern = GUILayoutUtility.GetRect(1.0f, 4.5f * EditorGUIUtility.singleLineHeight + 6.0f);
                                GUILayout.FlexibleSpace();
                                EditorGUILayout.EndHorizontal();
                                rectPattern.width = 4.5f * EditorGUIUtility.singleLineHeight + 6.0f;
                                SpritePatternEditor.DrawPattern(rectPattern, (m_spriteOutputs[m_editingSpriteOutput].patterns == null || m_spriteOutputs[m_editingSpriteOutput].patterns.Length == 0) ? null : m_spriteOutputs[m_editingSpriteOutput].patterns[0]);
                                if (changed)
                                {
                                    m_spriteOutputs[m_editingSpriteOutput] = SpriteOutput.Pattern(m_outputPatterns[0], m_offset, m_vertical);
                                }
                                break;
                            }
                        case SpriteOutput.Type.RandomizedPattern:
                            {
                                bool changed = false;
                                int remove = -1;
                                int add = -1;
                                int newOffset = EditorGUILayout.IntField(new GUIContent("Offset"), m_offset);
                                if (m_offset != newOffset)
                                {
                                    m_offset = newOffset;
                                    changed = true;
                                }
                                bool newVertical = EditorGUILayout.Toggle(new GUIContent("Vertical"), m_vertical);
                                if (m_vertical != newVertical)
                                {
                                    m_vertical = newVertical;
                                    changed = true;
                                }
                                EditorGUILayout.LabelField("Patterns");
                                for (int i = 0; i < m_outputPatterns.Length; i++)
                                {
                                    EditorGUILayout.BeginHorizontal();
                                    Rect rectPattern = GUILayoutUtility.GetRect(4.5f * EditorGUIUtility.singleLineHeight + 12.0f, 4.5f * EditorGUIUtility.singleLineHeight + 6.0f, GUILayout.Width(4.5f * EditorGUIUtility.singleLineHeight + 12.0f));
                                    rectPattern.x += 6.0f;
                                    rectPattern.width -= 6.0f;
                                    SpritePatternEditor.DrawPattern(rectPattern, (m_outputPatterns == null || m_outputPatterns.Length == 0) ? null : m_outputPatterns[i]);
                                    EditorGUILayout.BeginVertical();
                                    SpritePattern newPattern = (SpritePattern)EditorGUILayout.ObjectField(GUIContent.none, m_outputPatterns[i], typeof(SpritePattern), true);
                                    if (newPattern != null && m_outputPatterns != null)
                                    {
                                        for (int j = 0; j < m_outputPatterns.Length; j++)
                                        {
                                            if (j == i || m_outputPatterns[j] == null)
                                            {
                                                continue;
                                            }
                                            if (m_outputPatterns[j].width != newPattern.width || m_outputPatterns[j].height != newPattern.height)
                                            {
                                                Debug.LogError("Pattern size mismatched");
                                                newPattern = m_outputPatterns[i];
                                                break;
                                            }
                                        }
                                    }
                                    EditorGUILayout.LabelField("Weight");
                                    float newWeight = Mathf.Max(0.0f, EditorGUILayout.FloatField(m_weights[i]));
                                    EditorGUILayout.BeginHorizontal();
                                    if (GUILayout.Button("Add"))
                                    {
                                        add = i;
                                    }
                                    EditorGUI.BeginDisabledGroup(m_outputPatterns.Length <= 1);
                                    if (GUILayout.Button("Remove"))
                                    {
                                        remove = i;
                                    }
                                    EditorGUI.EndDisabledGroup();
                                    EditorGUILayout.EndHorizontal();
                                    EditorGUILayout.EndVertical();
                                    EditorGUILayout.EndHorizontal();
                                    if (newPattern != m_outputPatterns[i] || newWeight != m_weights[i])
                                    {
                                        m_outputPatterns[i] = newPattern;
                                        m_weights[i] = newWeight;

                                        changed = true;
                                    }
                                }
                                if (remove >= 0)
                                {
                                    List<SpritePattern> newPatterns = new List<SpritePattern>(m_outputPatterns);
                                    List<float> newWeights = new List<float>(m_weights);

                                    newPatterns.RemoveAt(remove);
                                    newWeights.RemoveAt(remove);

                                    m_outputPatterns = newPatterns.ToArray();
                                    m_weights = newWeights.ToArray();

                                    changed = true;
                                }
                                if (add >= 0)
                                {
                                    List<SpritePattern> newPatterns = new List<SpritePattern>(m_outputPatterns);
                                    List<float> newWeights = new List<float>(m_weights);

                                    newPatterns.Insert(add, null);
                                    newWeights.Insert(add, 1.0f);

                                    m_outputPatterns = newPatterns.ToArray();
                                    m_weights = newWeights.ToArray();

                                    changed = true;
                                }
                                if (GUILayout.Button("Add"))
                                {
                                    List<SpritePattern> newPatterns = new List<SpritePattern>(m_outputPatterns);
                                    List<float> newWeights = new List<float>(m_weights);

                                    newPatterns.Add(null);
                                    newWeights.Add(1.0f);

                                    m_outputPatterns = newPatterns.ToArray();
                                    m_weights = newWeights.ToArray();

                                    changed = true;
                                }
                                if (changed)
                                {
                                    m_spriteOutputs[m_editingSpriteOutput] = SpriteOutput.RandomizedPattern(m_outputPatterns, m_weights, m_offset, m_vertical);
                                }
                                break;
                            }
                    }
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
        }

        private void p_EditCurrentSpriteOutput_OnSelectedIndexChanged()
        {
            if (m_editingSpriteOutput < 0)
            {
                return;
            }
            m_outputSprites  = m_spriteOutputs[m_editingSpriteOutput].sprites;
            m_outputPatterns = m_spriteOutputs[m_editingSpriteOutput].patterns;
            switch (m_spriteOutputs[m_editingSpriteOutput].type)
            {
                case SpriteOutput.Type.Single:
                    {
                        break;
                    }
                case SpriteOutput.Type.Randomized:
                    {
                        m_weights = m_spriteOutputs[m_editingSpriteOutput].weights;
                        break;
                    }
                case SpriteOutput.Type.Animated:
                    {
                        m_minSpeed     = m_spriteOutputs[m_editingSpriteOutput].minSpeed;
                        m_maxSpeed     = m_spriteOutputs[m_editingSpriteOutput].maxSpeed;
                        m_randomOffset = m_spriteOutputs[m_editingSpriteOutput].randomOffset;
                        break;
                    }
                case SpriteOutput.Type.Pattern:
                    {
                        m_offset   = m_spriteOutputs[m_editingSpriteOutput].offset;
                        m_vertical = m_spriteOutputs[m_editingSpriteOutput].vertical;
                        break;
                    }
                case SpriteOutput.Type.RandomizedPattern:
                    {
                        m_weights  = m_spriteOutputs[m_editingSpriteOutput].weights;
                        m_offset   = m_spriteOutputs[m_editingSpriteOutput].offset;
                        m_vertical = m_spriteOutputs[m_editingSpriteOutput].vertical;
                        break;
                    }
            }
        }

        private void p_EditCurrentSpriteOutput_OnOutputTypeChanged(SpriteOutput.Type newType)
        {
            switch (newType)
            {
                case SpriteOutput.Type.Single:
                    {
                        m_spriteOutputs[m_editingSpriteOutput] = SpriteOutput.Single(m_spriteOutputs[m_editingSpriteOutput].sprites[0]);

                        m_outputSprites = m_spriteOutputs[m_editingSpriteOutput].sprites;

                        break;
                    }
                case SpriteOutput.Type.Randomized:
                    {
                        float[] newWeights = new float[m_spriteOutputs[m_editingSpriteOutput].sprites == null ? 0 : m_spriteOutputs[m_editingSpriteOutput].sprites.Length];
                        for (int j = 0; j < newWeights.Length; j++)
                        {
                            newWeights[j] = 1.0f;
                        }

                        m_spriteOutputs[m_editingSpriteOutput] = SpriteOutput.Randomized(m_spriteOutputs[m_editingSpriteOutput].sprites, newWeights);

                        m_weights = newWeights;
                        m_outputSprites = m_spriteOutputs[m_editingSpriteOutput].sprites;

                        break;
                    }
                case SpriteOutput.Type.Animated:
                    {
                        m_spriteOutputs[m_editingSpriteOutput] = SpriteOutput.Animated(m_spriteOutputs[m_editingSpriteOutput].sprites, 1.0f, 1.0f, 0.0f);

                        m_minSpeed = 1.0f;
                        m_maxSpeed = 1.0f;
                        m_randomOffset = 0.0f;
                        m_outputSprites = m_spriteOutputs[m_editingSpriteOutput].sprites;

                        break;
                    }
                case SpriteOutput.Type.Pattern:
                    {
                        m_spriteOutputs[m_editingSpriteOutput] = SpriteOutput.Pattern((m_spriteOutputs[m_editingSpriteOutput].patterns == null || m_spriteOutputs[m_editingSpriteOutput].patterns.Length == 0) ? null : m_spriteOutputs[m_editingSpriteOutput].patterns[0], 0, false);

                        m_offset         = 0;
                        m_vertical       = false;
                        m_outputPatterns = m_spriteOutputs[m_editingSpriteOutput].patterns;

                        break;
                    }
                case SpriteOutput.Type.RandomizedPattern:
                    {
                        float[] newWeights = new float[m_spriteOutputs[m_editingSpriteOutput].patterns == null ? 0 : m_spriteOutputs[m_editingSpriteOutput].patterns.Length];
                        for (int j = 0; j < newWeights.Length; j++)
                        {
                            newWeights[j] = 1.0f;
                        }

                        m_spriteOutputs[m_editingSpriteOutput] = SpriteOutput.RandomizedPattern(m_spriteOutputs[m_editingSpriteOutput].patterns, newWeights, 0, false);

                        m_outputPatterns = m_spriteOutputs[m_editingSpriteOutput].patterns;
                        m_weights  = newWeights;
                        m_offset   = 0;
                        m_vertical = false;

                        break;
                    }
            }
        }

        private void p_MultipleEdit_Single()
        {
            if (!m_addingOrReplacing)
            {
                if (GUILayout.Button("Replace Sprites"))
                {
                    m_addingOrReplacing = true;
                }
            }
            else
            {
                m_newSpritesProperty.serializedObject.Update();
                EditorGUILayout.PropertyField(m_newSpritesProperty, new GUIContent("Sprites"));
                m_newSpritesProperty.serializedObject.ApplyModifiedProperties();

                int length = m_newSprites == null ? 0 : m_newSprites.Length;
                EditorGUI.BeginDisabledGroup(length < m_count || m_editingSpriteOutput < 0);
                if (GUILayout.Button("Replace"))
                {
                    if (m_spriteOutputs != null)
                    {
                        for (int i = 0; i < m_count; i++)
                        {
                            m_spriteOutputs[i * m_width * m_height + m_editingSpriteOutput % (m_width * m_height)] = SpriteOutput.Single(m_newSprites[i]);
                        }
                        m_editingSpriteOutput = -1;
                    }
                    m_newSprites        = null;
                    m_addingOrReplacing = false;

                }
                EditorGUI.EndDisabledGroup();
            }
        }

        private void p_MultipleEdit_Randomized()
        {
            if (!m_addingOrReplacing)
            {
                if (GUILayout.Button("Add Variation"))
                {
                    m_addingOrReplacing = true;
                }
            }
            else
            {
                m_newSpritesProperty.serializedObject.Update();
                EditorGUILayout.PropertyField(m_newSpritesProperty, new GUIContent("Sprites"));
                m_newSpritesProperty.serializedObject.ApplyModifiedProperties();

                m_globalWeight = Mathf.Max(0.0f, EditorGUILayout.FloatField(new GUIContent("Weight"), m_globalWeight));

                int length = m_newSprites == null ? 0 : m_newSprites.Length;
                EditorGUI.BeginDisabledGroup(length < m_count || m_editingSpriteOutput < 0);
                if (GUILayout.Button("Add"))
                {
                    if (m_spriteOutputs != null)
                    {
                        for (int i = 0; i < m_count; i++)
                        {
                            List<Sprite> sprites = new List<Sprite>(m_spriteOutputs[i * m_width * m_height + m_editingSpriteOutput % (m_width * m_height)].sprites);
                            List<float>  weights = new List<float>(m_spriteOutputs[i * m_width * m_height + m_editingSpriteOutput % (m_width * m_height)].weights);

                            sprites.Add(m_newSprites[i]);
                            weights.Add(m_globalWeight);

                            m_spriteOutputs[i * m_width * m_height + m_editingSpriteOutput % (m_width * m_height)] = SpriteOutput.Randomized(sprites, weights);
                        }
                        m_editingSpriteOutput = -1;
                    }
                    m_newSprites        = null;
                    m_globalWeight      = 1.0f;
                    m_addingOrReplacing = false;
                }
                EditorGUI.EndDisabledGroup();
            }
        }

        private void p_MultipleEdit_Animated()
        {
            float newGobalMinSpeed     = Mathf.Max(0.0f, EditorGUILayout.FloatField("Min Speed", m_globalMinSpeed));
            float newGobalMaxSpeed     = Mathf.Max(0.0f, EditorGUILayout.FloatField("Max Speed", m_globalMaxSpeed));
            float newGobalRandomOffset = EditorGUILayout.FloatField("Random Offset", m_globalRandomOffset);

            if (newGobalMinSpeed != m_globalMinSpeed || newGobalMaxSpeed != m_globalMaxSpeed || newGobalRandomOffset != m_globalRandomOffset)
            {
                m_globalMinSpeed     = newGobalMinSpeed;
                m_globalMaxSpeed     = newGobalMaxSpeed;
                m_globalRandomOffset = newGobalRandomOffset;

                if (m_spriteOutputs != null && m_editingSpriteOutput >= 0)
                {
                    for (int i = 0; i < m_count; i++)
                    {
                        Sprite[] sprites = m_spriteOutputs[i * m_width * m_height + m_editingSpriteOutput % (m_width * m_height)].sprites;
                        m_spriteOutputs[i * m_width * m_height + m_editingSpriteOutput % (m_width * m_height)] = SpriteOutput.Animated(sprites, m_globalMinSpeed, m_globalMaxSpeed, m_globalRandomOffset);
                    }
                    m_editingSpriteOutput = -1;
                }
            }

            if (!m_addingOrReplacing)
            {
                if (GUILayout.Button("Add Frame"))
                {
                    m_addingOrReplacing = true;
                }
            }
            else
            {
                m_newSpritesProperty.serializedObject.Update();
                EditorGUILayout.PropertyField(m_newSpritesProperty, new GUIContent("Sprites"));
                m_newSpritesProperty.serializedObject.ApplyModifiedProperties();

                int length = m_newSprites == null ? 0 : m_newSprites.Length;
                EditorGUI.BeginDisabledGroup(length < m_count || m_editingSpriteOutput < 0);
                if (GUILayout.Button("Add"))
                {
                    if (m_spriteOutputs != null)
                    {
                        for (int i = 0; i < m_count; i++)
                        {
                            List<Sprite> sprites = new List<Sprite>(m_spriteOutputs[i * m_width * m_height + m_editingSpriteOutput % (m_width * m_height)].sprites);

                            sprites.Add(m_newSprites[i]);

                            float minSpeed     = m_spriteOutputs[i * m_width * m_height + m_editingSpriteOutput % (m_width * m_height)].minSpeed;
                            float maxSpeed     = m_spriteOutputs[i * m_width * m_height + m_editingSpriteOutput % (m_width * m_height)].maxSpeed;
                            float randomOffset = m_spriteOutputs[i * m_width * m_height + m_editingSpriteOutput % (m_width * m_height)].randomOffset;

                            m_spriteOutputs[i * m_width * m_height + m_editingSpriteOutput % (m_width * m_height)] = SpriteOutput.Animated(sprites, minSpeed, maxSpeed, randomOffset);
                        }
                        m_editingSpriteOutput = -1;
                    }
                    m_newSprites = null;
                    m_addingOrReplacing = false;
                }
                EditorGUI.EndDisabledGroup();
            }
        }

        private void p_MultipleEdit_Pattern()
        {
            int newGlobalOffset = EditorGUILayout.IntField("Offset", m_globalOffset);
            bool newGlobalVertical = EditorGUILayout.Toggle("Vertical", m_globalVertical);

            if (newGlobalOffset != m_globalOffset || newGlobalVertical != m_globalVertical)
            {
                m_globalOffset   = newGlobalOffset;
                m_globalVertical = newGlobalVertical;

                if (m_spriteOutputs != null && m_editingSpriteOutput >= 0)
                {
                    for (int i = 0; i < m_count; i++)
                    {
                        m_spriteOutputs[i * m_width * m_height + m_editingSpriteOutput % (m_width * m_height)] = SpriteOutput.Pattern((m_spriteOutputs[i * m_width * m_height + m_editingSpriteOutput % (m_width * m_height)].patterns == null || m_spriteOutputs[i * m_width * m_height + m_editingSpriteOutput % (m_width * m_height)].patterns.Length == 0) ? null : m_spriteOutputs[i * m_width * m_height + m_editingSpriteOutput % (m_width * m_height)].patterns[0], m_globalOffset, m_globalVertical);
                    }
                    m_editingSpriteOutput = -1;
                }
            }

            if (!m_addingOrReplacing)
            {
                if (GUILayout.Button("Replace Patterns"))
                {
                    m_addingOrReplacing = true;
                }
            }
            else
            {
                m_newPatternsProperty.serializedObject.Update();
                EditorGUILayout.PropertyField(m_newPatternsProperty, new GUIContent("Patterns"));
                m_newPatternsProperty.serializedObject.ApplyModifiedProperties();

                int length = m_newPatterns == null ? 0 : m_newPatterns.Length;
                EditorGUI.BeginDisabledGroup(length < m_count || m_editingSpriteOutput < 0);
                if (GUILayout.Button("Replace"))
                {
                    if (m_spriteOutputs != null)
                    {
                        for (int i = 0; i < m_count; i++)
                        {
                            m_spriteOutputs[i * m_width * m_height + m_editingSpriteOutput % (m_width * m_height)] = SpriteOutput.Pattern(m_newPatterns[i], m_globalOffset, m_globalVertical);
                        }
                        m_editingSpriteOutput = -1;
                    }
                    m_newPatterns       = null;
                    m_addingOrReplacing = false;

                }
                EditorGUI.EndDisabledGroup();
            }
        }

        private void p_MultipleEdit_RandomizedPattern()
        {
            int  newGlobalOffset   = EditorGUILayout.IntField("Offset", m_globalOffset);
            bool newGlobalVertical = EditorGUILayout.Toggle("Vertical", m_globalVertical);

            if (newGlobalOffset != m_globalOffset || newGlobalVertical != m_globalVertical)
            {
                m_globalOffset = newGlobalOffset;
                m_globalVertical = newGlobalVertical;

                if (m_spriteOutputs != null && m_editingSpriteOutput >= 0)
                {
                    for (int i = 0; i < m_count; i++)
                    {
                        m_spriteOutputs[i * m_width * m_height + m_editingSpriteOutput % (m_width * m_height)] = SpriteOutput.RandomizedPattern(m_spriteOutputs[i * m_width * m_height + m_editingSpriteOutput % (m_width * m_height)].patterns, m_spriteOutputs[i * m_width * m_height + m_editingSpriteOutput % (m_width * m_height)].weights, m_globalOffset, m_globalVertical);
                    }
                    m_editingSpriteOutput = -1;
                }
            }

            if (!m_addingOrReplacing)
            {
                if (GUILayout.Button("Add Patterns"))
                {
                    m_addingOrReplacing = true;
                }
            }
            else
            {
                m_newPatternsProperty.serializedObject.Update();
                EditorGUILayout.PropertyField(m_newPatternsProperty, new GUIContent("Patterns"));
                m_newPatternsProperty.serializedObject.ApplyModifiedProperties();

                m_globalWeight = Mathf.Max(0.0f, EditorGUILayout.FloatField(new GUIContent("Weight"), m_globalWeight));

                int length = m_newPatterns == null ? 0 : m_newPatterns.Length;
                bool valid = m_newPatterns != null;
                if (m_newPatterns != null && m_spriteOutputs != null)
                {
                    for (int i = 0; i < m_count; i++)
                    {
                        if (m_spriteOutputs[i * m_width * m_height + m_editingSpriteOutput % (m_width * m_height)].patterns != null && m_spriteOutputs[i * m_width * m_height + m_editingSpriteOutput % (m_width * m_height)].patterns.Length > 0)
                        {
                            SpritePattern currentPattern = m_spriteOutputs[i * m_width * m_height + m_editingSpriteOutput % (m_width * m_height)].patterns[0];
                            if (currentPattern != null && m_newPatterns[i] != null && (currentPattern.width != m_newPatterns[i].width || currentPattern.height != m_newPatterns[i].height))
                            {
                                valid = false;
                                break;
                            }
                        }
                    }
                }

                EditorGUI.BeginDisabledGroup(length < m_count || !valid || m_editingSpriteOutput < 0);
                if (GUILayout.Button("Add"))
                {
                    if (m_spriteOutputs != null)
                    {
                        for (int i = 0; i < m_count; i++)
                        {
                            List<SpritePattern> patterns = new List<SpritePattern>(m_spriteOutputs[i * m_width * m_height + m_editingSpriteOutput % (m_width * m_height)].patterns);
                            List<float> weights = new List<float>(m_spriteOutputs[i * m_width * m_height + m_editingSpriteOutput % (m_width * m_height)].weights);

                            patterns.Add(m_newPatterns[i]);
                            weights.Add(m_globalWeight);

                            m_spriteOutputs[i * m_width * m_height + m_editingSpriteOutput % (m_width * m_height)] = SpriteOutput.RandomizedPattern(patterns, weights, m_globalOffset, m_globalVertical);
                        }
                        m_editingSpriteOutput = -1;
                    }
                    m_newPatterns       = null;
                    m_addingOrReplacing = false;

                }
                EditorGUI.EndDisabledGroup();
            }
        }

        private enum EditMode
        {
            Individual,
            Multiple
        }
    }
}
