using UnityEngine;
using UnityEngine.Tilemaps;

namespace Zlitz.Tiles
{
    public class ConnectedTileGroup : BaseTileGroup
    {
        [SerializeField]
        private ConnectedTile[] m_tiles;

        [SerializeField]
        private bool m_init = false;

        public ConnectedTile visibleTile => m_tiles[0];
        public ConnectedTile invisibleTile => m_tiles[1];

        public override TileBase[] GetTiles()
        {
            return m_tiles;
        }

        public void Initialize()
        {
            if (m_init)
            {
                return;
            }
            m_init = true;

            m_tiles = new ConnectedTile[2];
        }

        public void SetTile(ConnectedTile visibleTile, ConnectedTile invisibleTile)
        {
            m_tiles[0] = visibleTile;
            m_tiles[1] = invisibleTile;
        }
    }
}
