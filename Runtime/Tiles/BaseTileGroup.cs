using UnityEngine;
using UnityEngine.Tilemaps;

namespace Zlitz.Tiles
{
    public interface ITileGroup
    {
        TileBase[] GetTiles();
    }

    public abstract class BaseTileGroup : ScriptableObject, ITileGroup
    {
        public abstract TileBase[] GetTiles();
    }
}
