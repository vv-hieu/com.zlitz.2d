using UnityEngine;
using UnityEngine.Tilemaps;

using Zlitz.Sprites;

namespace Zlitz.Tiles
{
    public class SimpleTile : BaseTile
    {
        [SerializeField]
        private SimpleTileGroup m_group;

        [SerializeField]
        private Tile.ColliderType m_colliderType;

        [SerializeField]
        private SpriteOutput m_sprite;

        [SerializeField, HideInInspector]
        private bool m_init = false;

        public SimpleTileGroup group => m_group;
        public Tile.ColliderType colliderType => m_colliderType;
        public SpriteOutput sprite => m_sprite;

        public void Initialize(SimpleTileGroup group, Tile.ColliderType colliderType, SpriteOutput sprite)
        {
            if (m_init)
            {
                return;
            }
            m_init = true;

            m_group        = group;
            m_colliderType = colliderType;
            m_sprite       = sprite;
        }

        protected override Tile.ColliderType GetCollider()
        {
            return m_colliderType;
        }

        protected override Sprite GetDefaultSprite()
        {
            return null;
        }

        protected override SpriteOutput GetSprite(Vector3Int position, ITilemap tilemap)
        {
            return m_sprite;
        }
    }
}
