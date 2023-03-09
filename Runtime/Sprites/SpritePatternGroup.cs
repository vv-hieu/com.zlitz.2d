using System.Collections.Generic;
using UnityEngine;

namespace Zlitz.Sprites
{
    public class SpritePatternGroup : ScriptableObject
    {
        [SerializeField]
        private SpritePattern[] m_patterns;

        [SerializeField]
        private int m_width;

        [SerializeField]
        private int m_height;

        [SerializeField]
        private bool m_init = false;

        public SpritePattern[] patterns => m_patterns;
        public int width => m_width;
        public int height => m_height;

        public void Initialize(IEnumerable<SpritePattern> patterns, int width, int height)
        {
            if (m_init)
            {
                return;
            }
            m_init = true;

            m_patterns = new List<SpritePattern>(patterns).ToArray();
            m_width    = Mathf.Max(1, width);
            m_height   = Mathf.Max(1, height);
        }
    }
}
