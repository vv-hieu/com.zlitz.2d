using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

using Zlitz.Sprites;

namespace Zlitz.Tiles
{
    public class TileGeneratorEditorWindow : EditorWindow
    {
        private Vector2 m_scrollPos = Vector2.zero;
        private bool    m_queueRepaint = false;

        private Mode              m_mode;
        private EditMode          m_editMode;
        private Tile.ColliderType m_globalColliderType = Tile.ColliderType.Sprite;
        private SpriteOutput.Type m_globalOutputType   = SpriteOutput.Type.Single;
        private bool              m_addingOrReplacing = false;
        private float             m_globalWeight;
        private float             m_globalMinSpeed;
        private float             m_globalMaxSpeed;
        private float             m_globalRandomOffset;
        private int               m_globalOffset;
        private bool              m_globalVertical;

        [SerializeField]
        private Sprite[]           m_sprites;
        private SerializedProperty m_spritesProperty;

        [SerializeField]
        private Sprite[]           m_newSprites;
        private SerializedProperty m_newSpritesProperty;

        [SerializeField]
        private SpritePattern[]    m_newPatterns;
        private SerializedProperty m_newPatternsProperty;

        [SerializeField]
        private SpriteOutput[]     m_spriteOutputs;
        private SerializedProperty m_spriteOutputsProperty;

        private int                 m_selectedSpriteOutput        = -1;
        private int                 m_editingSpriteOutput         = -1;
        private int                 m_selectedTile                = -1;
        private int                 m_editingTile                 = -1;
        private bool                m_selectedSpriteOutputFoldout = true;
        private bool                m_selectedTileFoldout         = true;
        private string[]            m_tileNames;
        private Tile.ColliderType[] m_colliderTypes;
        private Sprite[]            m_outputSprites;
        private SpritePattern[]     m_outputPatterns;
        private float[]             m_weights;
        private float               m_minSpeed;
        private float               m_maxSpeed;
        private float               m_randomOffset;
        private int                 m_offset;
        private bool                m_vertical;
        private SpriteOutput        m_clipboard = null;

        private ConnectedTemplate m_connectedTemplate;
        private RuleTemplate      m_ruleTemplate;
        private Texture2D         m_templateTexture;

        private static Material s_solidColor;
        private static Color    s_color1 = new Color(0.880f, 0.767f, 0.458f);
        private static Color    s_color2 = new Color(0.285f, 0.708f, 0.890f);
        private static Color    s_color3 = s_color1 * 0.95f;
        private static Color    s_color4 = s_color2 * 0.95f;

        [MenuItem("Zlitz/Tools/Tile Generator")]
        public static void ShowWindow()
        {
            GetWindow<TileGeneratorEditorWindow>("Tile Generator"); 
        }

        private void OnEnable()
        {
            if (s_solidColor == null)
            {
                s_solidColor = new Material(Shader.Find("Hidden/Internal-Colored"));
            }

            m_spritesProperty       = new SerializedObject(this).FindProperty("m_sprites");
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
            
            // Tile generation mode
            Mode newMode = (Mode)EditorGUILayout.EnumPopup(new GUIContent("Select Mode"), m_mode);
            if (newMode != m_mode)
            {
                m_mode = newMode;
                p_OnModeChanged();
            }

            // Edit mode
            if (m_mode != Mode._ && m_spriteOutputs != null && m_spriteOutputs.Length > 0)
            {
                EditMode newEditMode = (EditMode)EditorGUILayout.EnumPopup(new GUIContent("Edit Mode"), m_editMode);
                if (newEditMode != m_editMode)
                {
                    m_editMode = newEditMode;
                    p_OnEditModeChanged();
                }
            }

            // Multiple editing
            if (m_editMode == EditMode.Multiple && m_spriteOutputs != null && m_spriteOutputs.Length > 0)
            {
                // Global collider type
                Tile.ColliderType newGlobalColliderType = (Tile.ColliderType)EditorGUILayout.EnumPopup(new GUIContent("Collider Type"), m_globalColliderType);
                if (newGlobalColliderType != m_globalColliderType)
                {
                    m_globalColliderType = newGlobalColliderType;
                    p_OnGlobalColldierTypeChanged();
                }

                // Global sprite output type
                SpriteOutput.Type newGlobalOutputType = (SpriteOutput.Type)EditorGUILayout.EnumPopup(new GUIContent("Output Type"), m_globalOutputType);
                if (newGlobalOutputType != m_globalOutputType)
                {
                    m_globalOutputType = newGlobalOutputType;
                    p_OnGlobalOutputTypeChanged();
                }

                // Handle multiple editing for each output type
                switch (m_globalOutputType)
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
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
            switch (m_mode)
            {
                case Mode.SimpleTile:
                    {
                        p_OnGUI_SimpleTile();
                        break;
                    }
                case Mode.ConnectedTile:
                    {
                        p_OnGUI_ConnectedTile();
                        break;
                    }
                case Mode.RuleTile:
                    {
                        p_OnGUI_RuleTile();
                        break;
                    }
                default:
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void p_OnModeChanged()
        {
            // Deselect sprite output and tile
            m_selectedTile         = -1;
            m_editingTile          = -1;
            m_selectedSpriteOutput = -1;
            m_editingSpriteOutput  = -1;
            m_selectedSpriteOutputFoldout = true;

            // Clear all current sprite outputs, tile properties and templates
            m_sprites           = null;
            m_spriteOutputs     = null;
            m_tileNames         = null;
            m_colliderTypes     = null;
            m_connectedTemplate = null;
            m_ruleTemplate      = null;

            // Destroy template texture
            if (m_templateTexture != null)
            {
                DestroyImmediate(m_templateTexture);
                m_templateTexture = null;
            }
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
                p_OnGlobalOutputTypeChanged();
            }
        }

        private void p_OnGlobalColldierTypeChanged()
        {
            if (m_colliderTypes != null)
            {
                for (int i = 0; i < m_colliderTypes.Length; i++)
                {
                    m_colliderTypes[i] = m_globalColliderType;
                }
            }
        }

        private void p_OnGlobalOutputTypeChanged()
        {
            if (m_spriteOutputs != null)
            {
                // Set all current sprite output to coresponding type
                switch (m_globalOutputType)
                {
                    case SpriteOutput.Type.Single: 
                        {
                            for (int i = 0; i < m_spriteOutputs.Length; i++)
                            {
                                m_spriteOutputs[i] = SpriteOutput.Single(m_spriteOutputs[i].sprites == null || m_spriteOutputs[i].sprites.Length == 0 ? null : m_spriteOutputs[i].sprites[0]);
                            }
                            break;
                        }
                    case SpriteOutput.Type.Randomized: 
                        {
                            for (int i = 0; i < m_spriteOutputs.Length; i++)
                            {
                                float[] newWeights = new float[m_spriteOutputs[i].sprites == null ? 0 : m_spriteOutputs[i].sprites.Length];
                                for (int j = 0; j < newWeights.Length; j++)
                                {
                                    newWeights[j] = 1.0f;
                                }
                                m_spriteOutputs[i] = SpriteOutput.Randomized(m_spriteOutputs[i].sprites, newWeights);
                            }
                            break;
                        }
                    case SpriteOutput.Type.Animated: 
                        {
                            for (int i = 0; i < m_spriteOutputs.Length; i++)
                            {
                                m_spriteOutputs[i] = SpriteOutput.Animated(m_spriteOutputs[i].sprites, 1.0f, 1.0f, 0.0f);
                            }
                            break;
                        }
                    case SpriteOutput.Type.Pattern:
                        {
                            for (int i = 0; i < m_spriteOutputs.Length; i++)
                            {
                                m_spriteOutputs[i] = SpriteOutput.Pattern((m_spriteOutputs[i].patterns == null || m_spriteOutputs[i].patterns.Length == 0) ? null : m_spriteOutputs[i].patterns[0], 0, false);
                            }
                            break;
                        }
                    case SpriteOutput.Type.RandomizedPattern:
                        {
                            for (int i = 0; i < m_spriteOutputs.Length; i++)
                            {
                                float[] newWeights = new float[m_spriteOutputs[i].patterns == null ? 0 : m_spriteOutputs[i].patterns.Length];
                                for (int j = 0; j < newWeights.Length; j++)
                                {
                                    newWeights[j] = 1.0f;
                                }
                                m_spriteOutputs[i] = SpriteOutput.RandomizedPattern(m_spriteOutputs[i].patterns, newWeights, 0, false);
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
                EditorGUI.BeginDisabledGroup(length < m_spriteOutputs.Length);
                if (GUILayout.Button("Replace"))
                {
                    if (m_spriteOutputs != null)
                    {
                        for (int i = 0; i < m_spriteOutputs.Length; i++)
                        {
                            m_spriteOutputs[i] = SpriteOutput.Single(m_newSprites[i]);
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
                EditorGUI.BeginDisabledGroup(length != m_spriteOutputs.Length);
                if (GUILayout.Button("Add"))
                {
                    if (m_spriteOutputs != null)
                    {
                        for (int i = 0; i < m_spriteOutputs.Length; i++)
                        {
                            List<Sprite> sprites = new List<Sprite>(m_spriteOutputs[i].sprites);
                            List<float> weights = new List<float>(m_spriteOutputs[i].weights);

                            sprites.Add(m_newSprites[i]);
                            weights.Add(m_globalWeight);

                            m_spriteOutputs[i] = SpriteOutput.Randomized(sprites, weights);
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

                if (m_spriteOutputs != null)
                {
                    for (int i = 0; i < m_spriteOutputs.Length; i++)
                    {
                        Sprite[] sprites = m_spriteOutputs[i].sprites;
                        m_spriteOutputs[i] = SpriteOutput.Animated(sprites, m_globalMinSpeed, m_globalMaxSpeed, m_globalRandomOffset);
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
                EditorGUI.BeginDisabledGroup(length != m_spriteOutputs.Length);
                if (GUILayout.Button("Add"))
                {
                    if (m_spriteOutputs != null)
                    {
                        for (int i = 0; i < m_spriteOutputs.Length; i++)
                        {
                            List<Sprite> sprites = new List<Sprite>(m_spriteOutputs[i].sprites);

                            sprites.Add(m_newSprites[i]);

                            float minSpeed     = m_spriteOutputs[i].minSpeed;
                            float maxSpeed     = m_spriteOutputs[i].maxSpeed;
                            float randomOffset = m_spriteOutputs[i].randomOffset;

                            m_spriteOutputs[i] = SpriteOutput.Animated(sprites, minSpeed, maxSpeed, randomOffset);
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
            int  newGlobalOffset   = EditorGUILayout.IntField("Offset", m_globalOffset);
            bool newGlobalVertical = EditorGUILayout.Toggle("Vertical", m_globalVertical);

            if (newGlobalOffset != m_globalOffset || newGlobalVertical != m_globalVertical)
            {
                m_globalOffset   = newGlobalOffset;
                m_globalVertical = newGlobalVertical;

                if (m_spriteOutputs != null)
                {
                    for (int i = 0; i < m_spriteOutputs.Length; i++)
                    {
                        m_spriteOutputs[i] = SpriteOutput.Pattern((m_spriteOutputs[i].patterns == null || m_spriteOutputs[i].patterns.Length == 0) ? null : m_spriteOutputs[i].patterns[0], m_globalOffset, m_globalVertical);
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
                EditorGUI.BeginDisabledGroup(length < m_spriteOutputs.Length);
                if (GUILayout.Button("Replace"))
                {
                    if (m_spriteOutputs != null)
                    {
                        for (int i = 0; i < m_spriteOutputs.Length; i++)
                        {
                            m_spriteOutputs[i] = SpriteOutput.Pattern(m_newPatterns[i], m_globalOffset, m_globalVertical);
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
                m_globalOffset   = newGlobalOffset;
                m_globalVertical = newGlobalVertical;

                if (m_spriteOutputs != null)
                {
                    for (int i = 0; i < m_spriteOutputs.Length; i++)
                    {
                        m_spriteOutputs[i] = SpriteOutput.RandomizedPattern(m_spriteOutputs[i].patterns, m_spriteOutputs[i].weights, m_globalOffset, m_globalVertical);
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
                    for (int i = 0; i < m_newPatterns.Length; i++)
                    {
                        if (m_spriteOutputs[i].patterns != null && m_spriteOutputs[i].patterns.Length > 0)
                        {
                            SpritePattern currentPattern = m_spriteOutputs[i].patterns[0];
                            if (currentPattern != null && m_newPatterns[i] != null && (currentPattern.width != m_newPatterns[i].width || currentPattern.height != m_newPatterns[i].height))
                            {
                                valid = false;
                                break;
                            }
                        }
                    }
                }
                
                EditorGUI.BeginDisabledGroup((length < m_spriteOutputs.Length) || !valid);
                if (GUILayout.Button("Add"))
                {
                    if (m_spriteOutputs != null)
                    {
                        for (int i = 0; i < m_spriteOutputs.Length; i++)
                        {
                            List<SpritePattern> patterns = new List<SpritePattern>(m_spriteOutputs[i].patterns);
                            List<float> weights = new List<float>(m_spriteOutputs[i].weights);

                            patterns.Add(m_newPatterns[i]);
                            weights.Add(m_globalWeight);

                            m_spriteOutputs[i] = SpriteOutput.RandomizedPattern(patterns, weights, m_globalOffset, m_globalVertical);
                        }
                        m_editingSpriteOutput = -1;
                    }
                    m_newPatterns       = null;
                    m_addingOrReplacing = false;

                }
                EditorGUI.EndDisabledGroup();
            }
        }

        private void p_Select(int spriteOutput, int tile)
        {
            m_selectedSpriteOutput = spriteOutput;
            m_selectedTile         = tile;
            m_queueRepaint         = true;
        }

        private void p_Deselect()
        {
            m_selectedSpriteOutput = -1;
            m_selectedTile         = -1;
            m_queueRepaint         = true;
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
                                int  remove = -1;
                                int  add    = -1;
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
                                int  remove  = -1;
                                int  add     = -1;
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
                                int  remove  = -1;
                                int  add     = -1;
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
                        m_spriteOutputs[m_editingSpriteOutput] = SpriteOutput.Single((m_spriteOutputs[m_editingSpriteOutput].sprites == null || m_spriteOutputs[m_editingSpriteOutput].sprites.Length == 0) ? null : m_spriteOutputs[m_editingSpriteOutput].sprites[0]);
                        
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
                        
                        m_outputSprites = m_spriteOutputs[m_editingSpriteOutput].sprites;
                        m_weights       = newWeights;
                        
                        break;
                    }
                case SpriteOutput.Type.Animated:
                    {
                        m_spriteOutputs[m_editingSpriteOutput] = SpriteOutput.Animated(m_spriteOutputs[m_editingSpriteOutput].sprites, 1.0f, 1.0f, 0.0f);
                        
                        m_outputSprites = m_spriteOutputs[m_editingSpriteOutput].sprites;
                        m_minSpeed      = 1.0f;
                        m_maxSpeed      = 1.0f;
                        m_randomOffset  = 0.0f;
                        
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
                        m_weights        = newWeights;
                        m_offset         = 0;
                        m_vertical       = false;

                        break;
                    }
            }
        }
            
        private void p_EditCurrentTile()
        {
            if (m_editingTile != m_selectedTile)
            {
                m_editingTile = m_selectedTile;
            }

            if (m_tileNames != null && m_editingTile >= 0 && m_editingTile < m_tileNames.Length)
            {
                m_selectedTileFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_selectedTileFoldout, new GUIContent("Tile Properties"));
                if (m_selectedTileFoldout)
                {
                    m_tileNames[m_editingTile] = EditorGUILayout.TextField(new GUIContent("Tile Name"), m_tileNames[m_editingTile]);

                    EditorGUI.BeginDisabledGroup(m_editMode == EditMode.Multiple);
                    m_colliderTypes[m_editingTile] = (Tile.ColliderType)EditorGUILayout.EnumPopup(new GUIContent("Collider Type"), m_colliderTypes[m_editingTile]);
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
        }

        private void p_OnGUI_SimpleTile()
        {
            if (m_sprites == null || m_sprites.Length == 0)
            {
                m_spritesProperty.serializedObject.Update();
                EditorGUILayout.PropertyField(m_spritesProperty, new GUIContent("Sprites"));
                m_spritesProperty.serializedObject.ApplyModifiedProperties();
            }

            int length       = m_sprites == null ? 0 : m_sprites.Length;
            int outputLength = m_spriteOutputs == null ? 0 : m_spriteOutputs.Length;
            if (length != outputLength)
            {
                if (length > 0)
                {
                    m_spriteOutputs = new SpriteOutput[length];
                    m_colliderTypes = new Tile.ColliderType[length];
                    m_tileNames     = new string[length];
                    for (int i = 0; i < m_sprites.Length; i++)
                    {
                        m_spriteOutputs[i] = SpriteOutput.Single(m_sprites[i]);
                        m_colliderTypes[i] = Tile.ColliderType.Sprite;
                        m_tileNames[i]     = "" + i;
                    }
                }
                else
                {
                    m_spriteOutputs = null;
                    m_colliderTypes = null;
                    m_tileNames     = null;
                }
            }

            if (m_spriteOutputs != null && m_spriteOutputs.Length > 0)
            {
                m_spriteOutputsProperty.serializedObject.Update();

                int spritesPerRow = Mathf.Max(1, Mathf.FloorToInt(position.width / (6.0f + 4.5f * EditorGUIUtility.singleLineHeight)));
                int rows          = (m_spriteOutputs.Length - 1) / spritesPerRow + 1;
                
                int index = 0;
                for (int row = 0; row < rows; row++)
                {
                    Rect rect = GUILayoutUtility.GetRect(1.0f, 4.5f * EditorGUIUtility.singleLineHeight);

                    Rect rectTile = new Rect();
                    rectTile.size = 4.5f * EditorGUIUtility.singleLineHeight * Vector2.one;
                    rectTile.y = rect.y;

                    for (int i = 0; i < spritesPerRow; i++)
                    {
                        if (index + i >= m_spriteOutputs.Length)
                        {
                            break;
                        }

                        rectTile.x = rect.x + 6.0f + (6.0f + 4.5f * EditorGUIUtility.singleLineHeight) * i;
                        SerializedProperty spriteProperty = m_spriteOutputsProperty.GetArrayElementAtIndex(index + i);
                        spriteProperty.serializedObject.Update();
                        EditorGUI.PropertyField(rectTile, spriteProperty, GUIContent.none);
                        if (Event.current.type == EventType.MouseDown)
                        {
                            if (Event.current.mousePosition.x >= rectTile.x &&
                                Event.current.mousePosition.x <= rectTile.x + rectTile.width &&
                                Event.current.mousePosition.y >= rectTile.y &&
                                Event.current.mousePosition.y <= rectTile.y + rectTile.height
                            )
                            {
                                if (m_editingSpriteOutput == index + i) 
                                {
                                    if (Event.current.button == 1)
                                    {
                                        p_Deselect();
                                    }
                                }
                                else
                                {
                                    if (Event.current.button == 0)
                                    {
                                        p_Select(index + i, index + i);
                                    }
                                }
                            }
                        }

                        if (index + i == m_editingSpriteOutput)
                        {
                            EditorGUI.DrawRect(rectTile, new Color(0.0f, 1.0f, 1.0f, 0.5f));
                        }
                    }
                    EditorGUILayout.Space();
                    index += spritesPerRow;
                }

                p_EditCurrentTile();
                EditorGUILayout.Space();
                p_EditCurrentSpriteOutput();
                EditorGUILayout.Space();

                if (m_spriteOutputs != null && m_spriteOutputs.Length > 0)
                {
                    if (GUILayout.Button("Generate"))
                    {
                        string path = EditorUtility.SaveFilePanelInProject("Select Folder", "", "asset", "Please enter a file name");
                        if (path != "")
                        {
                            string filename = Path.GetFileNameWithoutExtension(path);

                            SimpleTileGroup group = (SimpleTileGroup)CreateInstance(typeof(SimpleTileGroup));
                            group.Initialize(m_tileNames.Length);
                            AssetDatabase.CreateAsset(group, path);
                            AssetDatabase.SaveAssets();

                            for (int i = 0; i < m_tileNames.Length; i++)
                            {
                                SimpleTile simpleTile = (SimpleTile)CreateInstance(typeof(SimpleTile));
                                simpleTile.Initialize(group, m_colliderTypes[i], m_spriteOutputs[i]);
                                simpleTile.name = filename + "_" + m_tileNames[i];
                                group.SetTile(i, simpleTile);
                                AssetDatabase.AddObjectToAsset(simpleTile, group);
                            }

                            EditorUtility.SetDirty(group);
                            AssetDatabase.SaveAssets();

                            Close();
                        }
                    }
                }
            }
        }

        private void p_OnGUI_ConnectedTile()
        {
            ConnectedTemplate newConnectedTemplate = (ConnectedTemplate)EditorGUILayout.ObjectField(new GUIContent("Template"), m_connectedTemplate, typeof(ConnectedTemplate), true);

            if (newConnectedTemplate != m_connectedTemplate)
            {
                m_connectedTemplate = newConnectedTemplate;

                if (m_templateTexture != null)
                {
                    DestroyImmediate(m_templateTexture);
                }

                m_templateTexture = new Texture2D(m_connectedTemplate.width * 4, m_connectedTemplate.height * 4, TextureFormat.RGBA32, false);
                m_templateTexture.filterMode = FilterMode.Point;

                for (int row = 0; row < m_connectedTemplate.height; row++)
                    for (int col = 0; col < m_connectedTemplate.width; col++)
                    {
                        int x = col;
                        int y = m_connectedTemplate.height - 1 - row;

                        uint configuration = m_connectedTemplate.configurations[row * m_connectedTemplate.width + col].configuration;

                        bool c0 = (configuration & ConnectedTile.Configuration.TOP)          == ConnectedTile.Configuration.TOP;
                        bool c1 = (configuration & ConnectedTile.Configuration.TOP_RIGHT)    == ConnectedTile.Configuration.TOP_RIGHT;
                        bool c2 = (configuration & ConnectedTile.Configuration.RIGHT)        == ConnectedTile.Configuration.RIGHT;
                        bool c3 = (configuration & ConnectedTile.Configuration.BOTTOM_RIGHT) == ConnectedTile.Configuration.BOTTOM_RIGHT;
                        bool c4 = (configuration & ConnectedTile.Configuration.BOTTOM)       == ConnectedTile.Configuration.BOTTOM;
                        bool c5 = (configuration & ConnectedTile.Configuration.BOTTOM_LEFT)  == ConnectedTile.Configuration.BOTTOM_LEFT;
                        bool c6 = (configuration & ConnectedTile.Configuration.LEFT)         == ConnectedTile.Configuration.LEFT;
                        bool c7 = (configuration & ConnectedTile.Configuration.TOP_LEFT)     == ConnectedTile.Configuration.TOP_LEFT;

                        m_templateTexture.SetPixel(4 * x + 0, 4 * y + 3 - 0, c7 ? s_color1 : s_color2);
                        m_templateTexture.SetPixel(4 * x + 1, 4 * y + 3 - 0, c0 ? s_color3 : s_color4);
                        m_templateTexture.SetPixel(4 * x + 2, 4 * y + 3 - 0, c0 ? s_color3 : s_color4);
                        m_templateTexture.SetPixel(4 * x + 3, 4 * y + 3 - 0, c1 ? s_color1 : s_color2);
                        m_templateTexture.SetPixel(4 * x + 0, 4 * y + 3 - 1, c6 ? s_color3 : s_color4);
                        m_templateTexture.SetPixel(4 * x + 1, 4 * y + 3 - 1, s_color1);
                        m_templateTexture.SetPixel(4 * x + 2, 4 * y + 3 - 1, s_color1);
                        m_templateTexture.SetPixel(4 * x + 3, 4 * y + 3 - 1, c2 ? s_color3 : s_color4);
                        m_templateTexture.SetPixel(4 * x + 0, 4 * y + 3 - 2, c6 ? s_color3 : s_color4);
                        m_templateTexture.SetPixel(4 * x + 1, 4 * y + 3 - 2, s_color1);
                        m_templateTexture.SetPixel(4 * x + 2, 4 * y + 3 - 2, s_color1);
                        m_templateTexture.SetPixel(4 * x + 3, 4 * y + 3 - 2, c2 ? s_color3 : s_color4);
                        m_templateTexture.SetPixel(4 * x + 0, 4 * y + 3 - 3, c5 ? s_color1 : s_color2);
                        m_templateTexture.SetPixel(4 * x + 1, 4 * y + 3 - 3, c4 ? s_color3 : s_color4);
                        m_templateTexture.SetPixel(4 * x + 2, 4 * y + 3 - 3, c4 ? s_color3 : s_color4);
                        m_templateTexture.SetPixel(4 * x + 3, 4 * y + 3 - 3, c3 ? s_color1 : s_color2);
                    }

                m_templateTexture.Apply();
            }

            if (m_connectedTemplate != null)
            {
                if (m_sprites == null || m_sprites.Length < m_connectedTemplate.width * m_connectedTemplate.height)
                {
                    m_spritesProperty.serializedObject.Update();
                    EditorGUILayout.PropertyField(m_spritesProperty, new GUIContent("Sprites"));
                    m_spritesProperty.serializedObject.ApplyModifiedProperties();
                }

                int length = m_sprites == null ? 0 : m_sprites.Length;
                int outputLength = m_spriteOutputs == null ? 0 : m_spriteOutputs.Length;
                if (length != outputLength)
                {
                    if (length > 0)
                    {
                        m_spriteOutputs = new SpriteOutput[length];
                        m_colliderTypes = new Tile.ColliderType[1] { Tile.ColliderType.Sprite };
                        m_tileNames     = new string[1] { "Visible" };
                        for (int i = 0; i < m_sprites.Length; i++)
                        {
                            m_spriteOutputs[i] = SpriteOutput.Single(m_sprites[i]);
                        }
                    }
                    else
                    {
                        m_spriteOutputs = null;
                        m_colliderTypes = null;
                        m_tileNames     = null;
                    }
                }

                Rect rect = GUILayoutUtility.GetAspectRect(m_connectedTemplate.width * 1.0f / m_connectedTemplate.height);
                float gridSize = rect.width / m_connectedTemplate.width;
                EditorGUI.DrawPreviewTexture(rect, m_templateTexture);

                s_solidColor?.SetPass(0);

                GL.PushMatrix();
                GL.Begin(GL.LINES);
                GL.Color(Color.black);
                for (int row = 0; row <= m_connectedTemplate.height; row++)
                {
                    GL.Vertex(new Vector3(rect.x, rect.y + gridSize * row, 0.0f));
                    GL.Vertex(new Vector3(rect.x + rect.width, rect.y + gridSize * row, 0.0f));
                }
                for (int col = 0; col <= m_connectedTemplate.width; col++)
                {
                    GL.Vertex(new Vector3(rect.x + gridSize * col, rect.y, 0.0f));
                    GL.Vertex(new Vector3(rect.x + gridSize * col, rect.y + rect.height, 0.0f));
                }
                GL.End();
                GL.PopMatrix();

                if (m_spriteOutputs != null && m_spriteOutputs.Length >= m_connectedTemplate.width * m_connectedTemplate.height)
                {
                    m_spriteOutputsProperty.serializedObject.Update();

                    Rect rectTile = new Rect(0.0f, 0.0f, gridSize, gridSize);

                    Color restoreColor = GUI.color;
                    GUI.color = new Color(1.0f, 1.0f, 1.0f, 0.5f);

                    for (int row = 0; row < m_connectedTemplate.height; row++)
                    {
                        rectTile.y = rect.y + row * gridSize;
                        for (int col = 0; col < m_connectedTemplate.width; col++)
                        {
                            rectTile.x = rect.x + col * gridSize;
                            int index = row * m_connectedTemplate.width + col;

                            if (index >= m_spriteOutputs.Length)
                            {
                                break;
                            }
                            SerializedProperty spriteProperty = m_spriteOutputsProperty.GetArrayElementAtIndex(index);
                            spriteProperty.serializedObject.Update();
                            EditorGUI.PropertyField(rectTile, spriteProperty, GUIContent.none);

                            if (Event.current.type == EventType.MouseDown)
                            {
                                if (Event.current.mousePosition.x >= rectTile.x &&
                                    Event.current.mousePosition.x <= rectTile.x + rectTile.width &&
                                    Event.current.mousePosition.y >= rectTile.y &&
                                    Event.current.mousePosition.y <= rectTile.y + rectTile.height
                                )
                                {
                                    if (m_editingSpriteOutput == index)
                                    {
                                        if (Event.current.button == 1)
                                        {
                                            p_Deselect();
                                        }
                                    }
                                    else
                                    {
                                        if (Event.current.button == 0)
                                        {
                                            p_Select(index, 0);
                                        }
                                    }
                                }
                            }

                            if (index == m_editingSpriteOutput)
                            {
                                EditorGUI.DrawRect(rectTile, new Color(0.0f, 1.0f, 1.0f, 0.5f));
                            }
                        }
                    }

                    GUI.color = restoreColor;

                    p_EditCurrentTile();
                    EditorGUILayout.Space();
                    p_EditCurrentSpriteOutput();
                    EditorGUILayout.Space();

                    if (m_spriteOutputs != null && m_spriteOutputs.Length > 0)
                    {
                        if (GUILayout.Button("Generate"))
                        {
                            string path = EditorUtility.SaveFilePanelInProject("Select Folder", "", "asset", "Please enter a file name");
                            if (path != "")
                            {
                                string filename = Path.GetFileNameWithoutExtension(path);

                                ConnectedTile.Rule[] rules = new ConnectedTile.Rule[Mathf.Min(m_spriteOutputs.Length, m_connectedTemplate.width * m_connectedTemplate.height)];
                                for (int i = 0; i < rules.Length; i++)
                                {
                                    rules[i] = new ConnectedTile.Rule(m_spriteOutputs[i], m_connectedTemplate.configurations[i]);
                                }

                                ConnectedTileGroup group = (ConnectedTileGroup)CreateInstance(typeof(ConnectedTileGroup));
                                group.Initialize();
                                AssetDatabase.CreateAsset(group, path);
                                AssetDatabase.SaveAssets();

                                ConnectedTile visibleTile = (ConnectedTile)CreateInstance(typeof(ConnectedTile));
                                visibleTile.Initialize(group, m_colliderTypes[0], rules);
                                visibleTile.name = filename + "_" + m_tileNames[0];

                                ConnectedTile invisibleTile = (ConnectedTile)CreateInstance(typeof(ConnectedTile));
                                invisibleTile.Initialize(group, Tile.ColliderType.None, null);
                                invisibleTile.name = filename + "_Invisible";

                                group.SetTile(visibleTile, invisibleTile);
                                AssetDatabase.AddObjectToAsset(visibleTile, group);
                                AssetDatabase.AddObjectToAsset(invisibleTile, group);

                                EditorUtility.SetDirty(group);
                                AssetDatabase.SaveAssets();

                                Close();
                            }
                        }
                    }
                }
            }
        }

        private void p_OnGUI_RuleTile()
        {
            RuleTemplate newRuleTemplate = (RuleTemplate)EditorGUILayout.ObjectField(new GUIContent("Template"), m_ruleTemplate, typeof(RuleTemplate), true);

            if (m_ruleTemplate != newRuleTemplate)
            {
                m_ruleTemplate = newRuleTemplate;

                if (m_templateTexture != null)
                {
                    DestroyImmediate(m_templateTexture);
                }

                m_templateTexture = new Texture2D(m_ruleTemplate.width, m_ruleTemplate.height, TextureFormat.RGBA32, false);
                m_templateTexture.filterMode = FilterMode.Point;

                for (int row = 0; row < m_ruleTemplate.height; row++)
                    for (int col = 0; col < m_ruleTemplate.width; col++)
                    {
                        int value = m_ruleTemplate.elements[row * m_ruleTemplate.width + col];
                        switch (value)
                        {
                            case RuleTemplate.NONE:
                                m_templateTexture.SetPixel(col, m_templateTexture.height - 1 - row, Color.red);
                                break;
                            case RuleTemplate.ANY:
                                m_templateTexture.SetPixel(col, m_templateTexture.height - 1 - row, Color.green);
                                break;
                            default:
                                m_templateTexture.SetPixel(col, m_templateTexture.height - 1 - row, m_ruleTemplate.colors[value]);
                                break;
                        }
                    }

                m_templateTexture.Apply();
            }

            if (m_ruleTemplate != null)
            {
                if (m_sprites == null || m_sprites.Length < m_ruleTemplate.ruleSetsCount)
                {
                    m_spritesProperty.serializedObject.Update();
                    EditorGUILayout.PropertyField(m_spritesProperty, new GUIContent("Sprites"));
                    m_spritesProperty.serializedObject.ApplyModifiedProperties();
                }

                Rect rect = GUILayoutUtility.GetAspectRect(m_ruleTemplate.width * 1.0f / m_ruleTemplate.height);
                float gridSize = rect.width / m_ruleTemplate.width;

                EditorGUI.DrawPreviewTexture(rect, m_templateTexture);

                s_solidColor?.SetPass(0);

                GL.PushMatrix();
                GL.Begin(GL.LINES);
                GL.Color(Color.black);
                for (int row = 0; row <= m_ruleTemplate.height; row++)
                {
                    GL.Vertex(new Vector3(rect.x, rect.y + gridSize * row, 0.0f));
                    GL.Vertex(new Vector3(rect.x + rect.width, rect.y + gridSize * row, 0.0f));
                }
                for (int col = 0; col <= m_ruleTemplate.width; col++)
                {
                    GL.Vertex(new Vector3(rect.x + gridSize * col, rect.y, 0.0f));
                    GL.Vertex(new Vector3(rect.x + gridSize * col, rect.y + rect.height, 0.0f));
                }
                GL.End();
                GL.PopMatrix();

                int length = m_sprites == null ? 0 : m_sprites.Length;
                int outputLength = m_spriteOutputs == null ? 0 : m_spriteOutputs.Length;
                if (length != outputLength)
                {
                    if (length > 0)
                    {
                        m_spriteOutputs = new SpriteOutput[length];
                        m_colliderTypes = new Tile.ColliderType[m_ruleTemplate.count];
                        m_tileNames = new string[m_ruleTemplate.count];
                        for (int i = 0; i < m_sprites.Length; i++)
                        {
                            m_spriteOutputs[i] = SpriteOutput.Single(m_sprites[i]);
                        }
                        for (int i = 0; i < m_ruleTemplate.count; i++)
                        {
                            m_colliderTypes[i] = Tile.ColliderType.Sprite;
                            m_tileNames[i] = "" + i;
                        }
                    }
                    else
                    {
                        m_spriteOutputs = null;
                        m_colliderTypes = null;
                        m_tileNames     = null;
                    }
                }

                if (m_spriteOutputs != null && m_spriteOutputs.Length > 0)
                {
                    m_spriteOutputsProperty.serializedObject.Update();

                    Rect rectTile = new Rect(0.0f, 0.0f, gridSize, gridSize);

                    Color restoreColor = GUI.color;
                    GUI.color = new Color(1.0f, 1.0f, 1.0f, 0.5f);

                    for (int row = 0; row < m_ruleTemplate.height; row++)
                    {
                        rectTile.y = rect.y + row * gridSize;
                        for (int col = 0; col < m_ruleTemplate.width; col++)
                        {
                            rectTile.x = rect.x + col * gridSize;
                            int index = row * m_ruleTemplate.width + col;
                            int ruleSetIndex = m_ruleTemplate.indices[index];
                            int tileIndex = m_ruleTemplate.elements[index];

                            if (ruleSetIndex >= 0 && ruleSetIndex < m_spriteOutputs.Length)
                            {
                                SerializedProperty spriteProperty = m_spriteOutputsProperty.GetArrayElementAtIndex(ruleSetIndex);
                                spriteProperty.serializedObject.Update();
                                EditorGUI.PropertyField(rectTile, spriteProperty, GUIContent.none);
                            }
                            
                            if (Event.current.type == EventType.MouseDown)
                            {
                                if (Event.current.mousePosition.x >= rectTile.x &&
                                    Event.current.mousePosition.x <= rectTile.x + rectTile.width &&
                                    Event.current.mousePosition.y >= rectTile.y &&
                                    Event.current.mousePosition.y <= rectTile.y + rectTile.height
                                )
                                {
                                    if (Event.current.button == 0)
                                    {
                                        p_Select(ruleSetIndex < 0 ? -1 : ruleSetIndex, tileIndex < 0 ? -1 : tileIndex);
                                    }
                                    if (m_editingSpriteOutput == ruleSetIndex)
                                    {
                                        if (Event.current.button == 1)
                                        {
                                            p_Deselect();
                                        }
                                    }
                                }
                            }

                            if (ruleSetIndex == m_editingSpriteOutput && m_editingTile == tileIndex && tileIndex >= 0)
                            {
                                EditorGUI.DrawRect(rectTile, new Color(0.0f, 1.0f, 1.0f, 0.5f));
                            }
                        }
                    }

                    GUI.color = restoreColor;

                    p_EditCurrentTile();
                    EditorGUILayout.Space();
                    p_EditCurrentSpriteOutput();
                    EditorGUILayout.Space();

                    if (m_spriteOutputs != null && m_spriteOutputs.Length >= m_ruleTemplate.ruleSetsCount)
                    {
                        if (GUILayout.Button("Generate"))
                        {
                            string path = EditorUtility.SaveFilePanelInProject("Select Folder", "", "asset", "Please enter a file name");
                            if (path != "")
                            {
                                string filename = Path.GetFileNameWithoutExtension(path);

                                RuleTileGroup group = (RuleTileGroup)CreateInstance(typeof(RuleTileGroup));
                                group.Initialize(m_ruleTemplate.count);
                                AssetDatabase.CreateAsset(group, path);
                                AssetDatabase.SaveAssets();

                                List<RuleTileGroup.Rule>[] ruleSets = new List<RuleTileGroup.Rule>[m_ruleTemplate.count];
                                for (int i = 0; i < ruleSets.Length; i++)
                                {
                                    ruleSets[i] = new List<RuleTileGroup.Rule>();
                                }

                                for (int row = 1; row < m_ruleTemplate.height - 1; row++)
                                    for (int col = 1; col < m_ruleTemplate.width - 1; col++)
                                    {
                                        int index        = row * m_ruleTemplate.width + col;
                                        int ruleSetIndex = m_ruleTemplate.indices[index];
                                        int tileIndex    = m_ruleTemplate.elements[index];
                                        
                                        if (tileIndex >= 0 && ruleSetIndex >= 0)
                                        {
                                            int[] rule = new int[] {
                                                m_ruleTemplate.elements[(row - 1) * m_ruleTemplate.width + col + 0],
                                                m_ruleTemplate.elements[(row - 1) * m_ruleTemplate.width + col + 1],
                                                m_ruleTemplate.elements[(row + 0) * m_ruleTemplate.width + col + 1],
                                                m_ruleTemplate.elements[(row + 1) * m_ruleTemplate.width + col + 1],
                                                m_ruleTemplate.elements[(row + 1) * m_ruleTemplate.width + col + 0],
                                                m_ruleTemplate.elements[(row + 1) * m_ruleTemplate.width + col - 1],
                                                m_ruleTemplate.elements[(row + 0) * m_ruleTemplate.width + col - 1],
                                                m_ruleTemplate.elements[(row - 1) * m_ruleTemplate.width + col - 1]
                                            };

                                            RuleTileGroup.Rule newRule = new RuleTileGroup.Rule(m_spriteOutputs[ruleSetIndex], rule);
                                            ruleSets[tileIndex].Add(newRule);
                                        }
                                    }

                                for (int i = 0; i < ruleSets.Length; i++)
                                {
                                    RuleTile ruleTile = (RuleTile)CreateInstance(typeof(RuleTile));
                                    ruleTile.Initialize(group, m_colliderTypes[i], i);
                                    ruleTile.name = filename + "_" + m_tileNames[i];
                                    group.SetTile(i, ruleTile, ruleSets[i]);
                                    AssetDatabase.AddObjectToAsset(ruleTile, group);
                                }

                                EditorUtility.SetDirty(group);
                                AssetDatabase.SaveAssets();

                                Close();
                            }
                        }
                    }
                }
            }
        }

        private enum Mode
        {
            _,
            SimpleTile,
            ConnectedTile,
            RuleTile
        }

        private enum EditMode
        {
            Individual,
            Multiple
        }
    }
}