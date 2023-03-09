using UnityEngine;
using UnityEngine.Tilemaps;

using Zlitz.Sprites;

namespace Zlitz.Tiles
{
    public class RuleTile : BaseTile
    {
        [SerializeField]
        private RuleTileGroup m_group;

        [SerializeField]
        private Tile.ColliderType m_colliderType;

        [SerializeField]
        private int m_tileId;

        [SerializeField]
        private bool m_init = false;

        public Tile.ColliderType colliderType => m_colliderType;
        public RuleTileGroup group => m_group;
        public int tileId => m_tileId;

        public void Initialize(RuleTileGroup group, Tile.ColliderType colliderType, int tileId)
        {
            if (m_init)
            {
                return;
            }
            m_init = true;

            m_group        = group;
            m_colliderType = colliderType;
            m_tileId       = tileId;
        }

        public override void RefreshTile(Vector3Int position, ITilemap tilemap)
        {
            base.RefreshTile(position + new Vector3Int(-1, -1,  0), tilemap);
            base.RefreshTile(position + new Vector3Int( 0, -1,  0), tilemap);
            base.RefreshTile(position + new Vector3Int( 1, -1,  0), tilemap);
            base.RefreshTile(position + new Vector3Int(-1,  0,  0), tilemap);
            base.RefreshTile(position + new Vector3Int( 0,  0,  0), tilemap);
            base.RefreshTile(position + new Vector3Int( 1,  0,  0), tilemap);
            base.RefreshTile(position + new Vector3Int(-1,  1,  0), tilemap);
            base.RefreshTile(position + new Vector3Int( 0,  1,  0), tilemap);
            base.RefreshTile(position + new Vector3Int( 1,  1,  0), tilemap);
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
            int minAnyCount = int.MaxValue;
            RuleTileGroup.Rule res = default(RuleTileGroup.Rule);
            bool found = false;
            foreach (RuleTileGroup.Rule rule in m_group.GetRules(m_tileId))
            {
                if (rule.Check(position, tilemap, m_group) && minAnyCount > rule.anyCount)
                {
                    minAnyCount = rule.anyCount;
                    res = rule;
                    found = true;
                }
            }
            return found ? res.sprite : null;
        }
    }
}