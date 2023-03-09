using UnityEngine;
using UnityEngine.Tilemaps;

using Zlitz.Sprites;

namespace Zlitz.Tiles
{
    public abstract class BaseTile : TileBase
    {
        protected abstract Sprite GetDefaultSprite();

        protected abstract SpriteOutput GetSprite(Vector3Int position, ITilemap tilemap);

        protected abstract Tile.ColliderType GetCollider();

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            tileData.sprite       = GetDefaultSprite();
            tileData.colliderType = GetCollider();

            SpriteOutput sprite = GetSprite(position, tilemap);
            if (sprite != null)
            {
                sprite.GetTileData(position, tilemap, ref tileData);
            }
        }

        public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
        {
            SpriteOutput sprite = GetSprite(position, tilemap);
            if (sprite != null)
            {
                return sprite.GetTileAnimationData(position, tilemap, ref tileAnimationData);
            }
            return false;
        }
    }
}
