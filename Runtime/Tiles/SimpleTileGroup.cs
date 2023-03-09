using UnityEngine;
using UnityEngine.Tilemaps;

namespace Zlitz.Tiles
{
    public class SimpleTileGroup : BaseTileGroup
    {
        [SerializeField]
        private SimpleTile[] m_tiles;

        [SerializeField, HideInInspector]
        private bool m_init = false;

        public int tileCount { get; private set; } = -1;

        public override TileBase[] GetTiles()
        {
            return m_tiles;
        }

        public void Initialize(int tileCount)
        {
            if (m_init)
            {
                return;
            }

            m_init  = true;
            m_tiles = new SimpleTile[tileCount];
            this.tileCount = tileCount;
        }

        public SimpleTile GetTile(int tileId)
        {
            if (tileId >= 0 && tileId < m_tiles.Length)
            {
                return m_tiles[tileId];
            }
            return null;
        }

        public void SetTile(int tileId, SimpleTile tile)
        {
            if (tileId >= 0 && tileId < m_tiles.Length)
            {
                m_tiles[tileId] = tile;
            }
        }
    }
}
