using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

using Zlitz.Sprites;

namespace Zlitz.Tiles
{
    public class ConnectedTile : BaseTile
    {
        [SerializeField]
        private ConnectedTileGroup m_group;

        [SerializeField]
        private Tile.ColliderType m_colliderType;

        [SerializeField]
        private Rule[] m_rules;

        [SerializeField]
        private bool m_init = false;

        public ConnectedTileGroup group => m_group;
        public Tile.ColliderType colliderType => m_colliderType;
        public Rule[] rules => m_rules;

        public void Initialize(ConnectedTileGroup group, Tile.ColliderType colliderType, IEnumerable<Rule> rules)
        {
            if (m_init)
            {
                return;
            }
            m_init = true;

            m_group         = group;
            m_colliderType  = colliderType;
            m_rules         = rules == null ? new Rule[] { } : rules.ToArray();
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
            bool found = false;
            Rule res = default(Rule);

            uint configuration = 0;

            int minDifferentBitsCount = 8;

            if (tilemap != null)
            {
                if (this == tilemap.GetTile(position + new Vector3Int(0, 1, 0))) configuration |= Configuration.TOP;
                if (this == tilemap.GetTile(position + new Vector3Int(1, 1, 0))) configuration |= Configuration.TOP_RIGHT;
                if (this == tilemap.GetTile(position + new Vector3Int(1, 0, 0))) configuration |= Configuration.RIGHT;
                if (this == tilemap.GetTile(position + new Vector3Int(1, -1, 0))) configuration |= Configuration.BOTTOM_RIGHT;
                if (this == tilemap.GetTile(position + new Vector3Int(0, -1, 0))) configuration |= Configuration.BOTTOM;
                if (this == tilemap.GetTile(position + new Vector3Int(-1, -1, 0))) configuration |= Configuration.BOTTOM_LEFT;
                if (this == tilemap.GetTile(position + new Vector3Int(-1, 0, 0))) configuration |= Configuration.LEFT;
                if (this == tilemap.GetTile(position + new Vector3Int(-1, 1, 0))) configuration |= Configuration.TOP_LEFT;

                if (m_group != null && m_group.invisibleTile != null)
                {
                    if (m_group.invisibleTile == tilemap.GetTile(position + new Vector3Int(0, 1, 0))) configuration |= Configuration.TOP;
                    if (m_group.invisibleTile == tilemap.GetTile(position + new Vector3Int(1, 1, 0))) configuration |= Configuration.TOP_RIGHT;
                    if (m_group.invisibleTile == tilemap.GetTile(position + new Vector3Int(1, 0, 0))) configuration |= Configuration.RIGHT;
                    if (m_group.invisibleTile == tilemap.GetTile(position + new Vector3Int(1, -1, 0))) configuration |= Configuration.BOTTOM_RIGHT;
                    if (m_group.invisibleTile == tilemap.GetTile(position + new Vector3Int(0, -1, 0))) configuration |= Configuration.BOTTOM;
                    if (m_group.invisibleTile == tilemap.GetTile(position + new Vector3Int(-1, -1, 0))) configuration |= Configuration.BOTTOM_LEFT;
                    if (m_group.invisibleTile == tilemap.GetTile(position + new Vector3Int(-1, 0, 0))) configuration |= Configuration.LEFT;
                    if (m_group.invisibleTile == tilemap.GetTile(position + new Vector3Int(-1, 1, 0))) configuration |= Configuration.TOP_LEFT;
                }
            }

            if ((configuration & Configuration.TOP) == 0)
            {
                configuration &= ~Configuration.TOP_LEFT;
                configuration &= ~Configuration.TOP_RIGHT;
            }
            if ((configuration & Configuration.BOTTOM) == 0)
            {
                configuration &= ~Configuration.BOTTOM_LEFT;
                configuration &= ~Configuration.BOTTOM_RIGHT;
            }
            if ((configuration & Configuration.RIGHT) == 0)
            {
                configuration &= ~Configuration.TOP_RIGHT;
                configuration &= ~Configuration.BOTTOM_RIGHT;
            }
            if ((configuration & Configuration.LEFT) == 0)
            {
                configuration &= ~Configuration.TOP_LEFT;
                configuration &= ~Configuration.BOTTOM_LEFT;
            }

            foreach (Rule rule in rules)
            {
                if ((configuration & rule.configuration.configuration) == rule.configuration.configuration)
                {
                    uint differentBits = configuration ^ rule.configuration.configuration;
                    int differentBitsCount = 0;
                    if (((differentBits >> 0) & 1) != 0) differentBitsCount++;
                    if (((differentBits >> 1) & 1) != 0) differentBitsCount++;
                    if (((differentBits >> 2) & 1) != 0) differentBitsCount++;
                    if (((differentBits >> 3) & 1) != 0) differentBitsCount++;
                    if (((differentBits >> 4) & 1) != 0) differentBitsCount++;
                    if (((differentBits >> 5) & 1) != 0) differentBitsCount++;
                    if (((differentBits >> 6) & 1) != 0) differentBitsCount++;
                    if (((differentBits >> 7) & 1) != 0) differentBitsCount++;
                    if (differentBitsCount < minDifferentBitsCount)
                    {
                        minDifferentBitsCount = differentBitsCount;
                        res = rule;
                        found = true;
                    }
                }
            }

            return found ? res.sprite : null;
        }

        [Serializable]
        public struct Configuration : ISerializationCallbackReceiver
        {
            public static uint TOP          = 1 << 0;
            public static uint TOP_RIGHT    = 1 << 1;
            public static uint RIGHT        = 1 << 2;
            public static uint BOTTOM_RIGHT = 1 << 3;
            public static uint BOTTOM       = 1 << 4;
            public static uint BOTTOM_LEFT  = 1 << 5;
            public static uint LEFT         = 1 << 6;
            public static uint TOP_LEFT     = 1 << 7;

            [SerializeField]
            private uint m_configuration;

            public uint configuration => m_configuration;

            public Configuration(uint configuration)
            {
                m_configuration = configuration;
                p_Validate();
            }

            private void p_Validate()
            {
                if ((m_configuration & TOP) == 0)
                {
                    m_configuration &= ~TOP_LEFT;
                    m_configuration &= ~TOP_RIGHT;
                }
                if ((m_configuration & BOTTOM) == 0)
                {
                    m_configuration &= ~BOTTOM_LEFT;
                    m_configuration &= ~BOTTOM_RIGHT;
                }
                if ((m_configuration & RIGHT) == 0)
                {
                    m_configuration &= ~TOP_RIGHT;
                    m_configuration &= ~BOTTOM_RIGHT;
                }
                if ((m_configuration & LEFT) == 0)
                {
                    m_configuration &= ~TOP_LEFT;
                    m_configuration &= ~BOTTOM_LEFT;
                }
            }

            void ISerializationCallbackReceiver.OnBeforeSerialize() => p_Validate();

            void ISerializationCallbackReceiver.OnAfterDeserialize() => p_Validate();
        }

        [Serializable]
        public struct Rule
        {
            [SerializeField]
            private SpriteOutput m_sprite;

            [SerializeField]
            private Configuration m_configuration;

            public SpriteOutput sprite => m_sprite;
            public Configuration configuration => m_configuration;

            public Rule(SpriteOutput sprite, Configuration configuration)
            {
                m_sprite        = sprite;
                m_configuration = configuration;
            }
        }
    }
}
