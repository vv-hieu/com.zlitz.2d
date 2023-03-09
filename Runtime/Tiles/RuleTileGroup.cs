using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

using Zlitz.Sprites;

namespace Zlitz.Tiles
{
    public class RuleTileGroup : BaseTileGroup
    {
        [SerializeField]
        private Entry[] m_entries;

        [SerializeField]
        private bool m_init = false;

        public override TileBase[] GetTiles()
        {
            if (m_entries == null)
            {
                return null;
            }
            return m_entries.Select(e => e.tile).ToArray();
        }

        public void Initialize(int tileCount)
        {
            if (m_init)
            {
                return;
            }
            m_init = true;

            m_entries = new Entry[tileCount];
        }

        public RuleTile GetTile(int tileId)
        {
            if (m_entries != null && m_entries.Length > 0)
            {
                return m_entries[tileId].tile;
            }
            return null;
        }

        public Rule[] GetRules(int tileId)
        {
            if (m_entries != null && m_entries.Length > 0)
            {
                if (m_entries[tileId].rules != null)
                {
                    return m_entries[tileId].rules;
                }
            }
            return new Rule[] { };
        }

        public void SetTile(int tileId, RuleTile tile, IEnumerable<Rule> rules)
        {
            if (tileId >= 0 && tileId < m_entries.Length)
            {
                m_entries[tileId] = new Entry(tile, rules);
            }
        }

        [Serializable]
        public struct Rule : ISerializationCallbackReceiver
        {
            [SerializeField]
            private SpriteOutput m_sprite;

            [SerializeField]
            private int[] m_rules;

            [HideInInspector]
            public RuleTileGroup m_group;

            public SpriteOutput sprite => m_sprite;

            public int anyCount { get; private set; }

            public const int NONE = -2;
            public const int ANY  = -1;

            public bool Check(Vector3Int position, ITilemap tilemap, RuleTileGroup group)
            {
                return (
                    (m_rules[0] == ANY ? true : tilemap.GetTile(position + new Vector3Int( 0,  1,  0)) == (m_rules[0] == NONE ? null : group.GetTile(m_rules[0]))) &&
                    (m_rules[1] == ANY ? true : tilemap.GetTile(position + new Vector3Int( 1,  1,  0)) == (m_rules[1] == NONE ? null : group.GetTile(m_rules[1]))) &&
                    (m_rules[2] == ANY ? true : tilemap.GetTile(position + new Vector3Int( 1,  0,  0)) == (m_rules[2] == NONE ? null : group.GetTile(m_rules[2]))) &&
                    (m_rules[3] == ANY ? true : tilemap.GetTile(position + new Vector3Int( 1, -1,  0)) == (m_rules[3] == NONE ? null : group.GetTile(m_rules[3]))) &&
                    (m_rules[4] == ANY ? true : tilemap.GetTile(position + new Vector3Int( 0, -1,  0)) == (m_rules[4] == NONE ? null : group.GetTile(m_rules[4]))) &&
                    (m_rules[5] == ANY ? true : tilemap.GetTile(position + new Vector3Int(-1, -1,  0)) == (m_rules[5] == NONE ? null : group.GetTile(m_rules[5]))) &&
                    (m_rules[6] == ANY ? true : tilemap.GetTile(position + new Vector3Int(-1,  0,  0)) == (m_rules[6] == NONE ? null : group.GetTile(m_rules[6]))) &&
                    (m_rules[7] == ANY ? true : tilemap.GetTile(position + new Vector3Int(-1,  1,  0)) == (m_rules[7] == NONE ? null : group.GetTile(m_rules[7])))
                );
            }

            public Rule(SpriteOutput sprite, IEnumerable<int> rules)
            {
                m_sprite = sprite;
                m_rules  = new int[8];
                m_group  = null;

                int idx = 0;
                foreach (int rule in rules)
                {
                    m_rules[idx++] = rule;
                }
                while (idx < 8)
                {
                    m_rules[idx++] = NONE;
                }

                anyCount = 0;
                if (m_rules[0] == ANY) anyCount++;
                if (m_rules[1] == ANY) anyCount++;
                if (m_rules[2] == ANY) anyCount++;
                if (m_rules[3] == ANY) anyCount++;
                if (m_rules[4] == ANY) anyCount++;
                if (m_rules[5] == ANY) anyCount++;
                if (m_rules[6] == ANY) anyCount++;
                if (m_rules[7] == ANY) anyCount++;
            }

            private void p_Validate()
            {
                if (m_rules == null || m_rules.Length != 8)
                {
                    m_rules = new int[8];
                }

                anyCount = 0;
                if (m_rules[0] == ANY) anyCount++;
                if (m_rules[1] == ANY) anyCount++;
                if (m_rules[2] == ANY) anyCount++;
                if (m_rules[3] == ANY) anyCount++;
                if (m_rules[4] == ANY) anyCount++;
                if (m_rules[5] == ANY) anyCount++;
                if (m_rules[6] == ANY) anyCount++;
                if (m_rules[7] == ANY) anyCount++;
            }

            void ISerializationCallbackReceiver.OnAfterDeserialize() => p_Validate();
            void ISerializationCallbackReceiver.OnBeforeSerialize() => p_Validate();
        }

        [Serializable]
        private struct Entry
        {
            [SerializeField]
            private RuleTile m_tile;

            [SerializeField]
            private Rule[] m_rules;

            public RuleTile tile => m_tile;
            public Rule[] rules => m_rules;

            public Entry(RuleTile tile, IEnumerable<Rule> rules)
            {
                m_tile  = tile;
                m_rules = rules.ToArray();
            }
        }
    }
}
