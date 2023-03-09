using System.Collections.Generic;
using UnityEngine;

namespace Zlitz.Tiles
{
    [CreateAssetMenu(menuName = "Zlitz/Tiles/Rule Template")]
    public class RuleTemplate : ScriptableObject
    {
        [SerializeField]
        private int m_width;

        [SerializeField]
        private int m_height;

        [SerializeField]
        private int m_count;

        [SerializeField]
        private Color[] m_colors;

        [SerializeField]
        private bool[] m_generateRules;

        [SerializeField]
        private int[] m_elements;

        [SerializeField]
        private int[] m_indices;

        [SerializeField]
        private Vector2Int[] m_positions;

        [SerializeField]
        private int m_ruleSetsCount;

        public int width => m_width;
        public int height => m_height;
        public int count => m_count;
        public Color[] colors => m_colors;
        public bool[] generatRules => m_generateRules;
        public int[] elements => m_elements;
        public int[] indices => m_indices;
        public Vector2Int[] position => m_positions;
        public int ruleSetsCount => m_ruleSetsCount;

        public const int NONE = -2;
        public const int ANY  = -1;

        private void OnEnable()
        {
            m_width  = Mathf.Max(1, m_width);
            m_height = Mathf.Max(1, m_height);
            m_count  = Mathf.Max(1, m_count);

            if (m_colors == null || m_count != m_colors.Length)
            {
                m_colors = new Color[m_count];
            }
            if (m_generateRules == null || m_count != m_generateRules.Length)
            {
                m_generateRules = new bool[m_count];
                for (int i = 0; i < m_count; i++)
                {
                    m_generateRules[i] = true;
                }
            }
            if (m_elements == null || m_width * m_height != m_elements.Length)
            {
                m_elements = new int[m_width * m_height];
                for (int i = 0; i < m_width * m_height; i++)
                {
                    m_elements[i] = NONE;
                }
            }

            List<Vector2Int> positions = new List<Vector2Int>();
            m_indices = new int[m_elements.Length];
            int current = 0;
            for (int i = 0; i < m_elements.Length; i++)
            {
                if (m_elements[i] < 0 || !m_generateRules[m_elements[i]])
                {
                    m_indices[i] = -1;
                }
                else
                {
                    m_indices[i] = current++;
                    positions.Add(new Vector2Int(i % m_width, i / m_width));
                }
            }
            m_ruleSetsCount = current;
            m_positions = positions.ToArray();
        }

        private void OnValidate()
        {
            m_width  = Mathf.Max(1, m_width);
            m_height = Mathf.Max(1, m_height);
            m_count  = Mathf.Max(1, m_count);

            if (m_colors == null || m_count != m_colors.Length)
            {
                m_colors = new Color[m_count];
            }
            if (m_generateRules == null || m_count != m_generateRules.Length)
            {
                m_generateRules = new bool[m_count];
                for (int i = 0; i < m_count; i++)
                {
                    m_generateRules[i] = true;
                }
            }
            if (m_elements == null || m_width * m_height != m_elements.Length)
            {
                m_elements = new int[m_width * m_height];
                for (int i = 0; i < m_width * m_height; i++)
                {
                    m_elements[i] = NONE;
                }   
            }

            List<Vector2Int> positions = new List<Vector2Int>();
            m_indices = new int[m_elements.Length];
            int current = 0;
            for (int i = 0; i < m_elements.Length; i++)
            {
                if (m_elements[i] < 0 || !m_generateRules[m_elements[i]])
                {
                    m_indices[i] = -1;
                }
                else
                {
                    m_indices[i] = current++;
                    positions.Add(new Vector2Int(i % m_width, i / m_width));
                }
            }
            m_ruleSetsCount = current;
            m_positions = positions.ToArray();
        }
    }
}
