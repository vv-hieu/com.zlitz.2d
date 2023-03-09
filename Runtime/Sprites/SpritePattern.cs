using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Zlitz.Sprites
{
    public class SpritePattern : ScriptableObject
    {
        [SerializeField]
        private SpritePatternGroup m_group;

        [SerializeField]
        private SpriteOutput[] m_sprites;

        [SerializeField]
        private bool m_init = false;

        public SpritePatternGroup group => m_group;
        public SpriteOutput[] sprites => m_sprites;
        public int width => m_group == null ? 1 : m_group.width;
        public int height => m_group == null ? 1 : m_group.height;

        public void Initialize(SpritePatternGroup group, IEnumerable<SpriteOutput> sprites)
        {
            if (m_init)
            {
                return;
            }
            m_init = true;

            m_group   = group;
            m_sprites = sprites.ToArray();
        }

        public SpriteOutput GetSprite(int x, int y)
        {
            if (m_group != null)
            {
                y = height - 1 - y;
                int index = y * width + x;
                
                if (m_sprites != null)
                {
                    return m_sprites.ElementAtOrDefault(index);
                }
            }
            return null;
        }
    }
}
