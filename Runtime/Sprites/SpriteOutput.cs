using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

using Zlitz.Utilities;

namespace Zlitz.Sprites
{
    [Serializable]
    public class SpriteOutput
    {
        [SerializeField]
        private Type m_type;

        [SerializeField]
        private Sprite[] m_sprites;

        [SerializeField]
        private SpritePattern[] m_patterns;

        [SerializeField]
        private float[] m_weights;

        [SerializeField]
        private float m_totalWeight;

        [SerializeField]
        private float m_minSpeed;

        [SerializeField]
        private float m_maxSpeed;

        [SerializeField]
        private float m_randomOffset;

        [SerializeField]
        private int m_offset;

        [SerializeField]
        private bool m_vertical;

        private static WhiteNoiseSampler3D s_whiteNoise0;
        private static WhiteNoiseSampler3D s_whiteNoise1;
        private static WhiteNoiseSampler3D s_whiteNoise2;

        public Type type => m_type;
        public Sprite[] sprites => m_sprites;
        public SpritePattern[] patterns => m_patterns;
        public float[] weights => m_weights;
        public float minSpeed => m_minSpeed;
        public float maxSpeed => m_maxSpeed;
        public float randomOffset => m_randomOffset;
        public int offset => m_offset;
        public bool vertical => m_vertical;

        public float averageSpeed => 0.5f * (m_minSpeed + m_maxSpeed);

        public void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            switch (m_type)
            {
                case Type.Single:
                    {
                        tileData.sprite = m_sprites[0];
                        break;
                    }
                case Type.Randomized:
                    {
                        if (s_whiteNoise0 == null)
                        {
                            s_whiteNoise0 = new WhiteNoiseSampler3D(69);
                        }
                        float c = 0.0f;
                        float r = s_whiteNoise0.Sample(position.x, position.y, position.z + tilemap.GetComponent<Transform>().gameObject.GetHashCode()) * m_totalWeight;
                        for (int i = 0; i < m_sprites.Length; i++)
                        {
                            c += m_weights[i];
                            if (r <= c)
                            {
                                tileData.sprite = m_sprites[i];
                                break;
                            }
                        }
                        break;
                    }
                case Type.Animated:
                    {
                        tileData.sprite = (m_sprites.Length > 0 ? m_sprites[0] : null) ;
                        break;
                    }
                case Type.Pattern:
                    {
                        if (m_patterns == null || m_patterns.Length == 0)
                        {
                            return;
                        }

                        int patternX = Mathf.FloorToInt(position.x * 1.0f / m_patterns[0].width);
                        int patternY = Mathf.FloorToInt(position.y * 1.0f / m_patterns[0].height);

                        if (m_vertical)
                        {
                            position.y += m_offset * patternX;
                            patternY = Mathf.FloorToInt(position.y * 1.0f / m_patterns[0].height);
                        }
                        else
                        {
                            position.x += m_offset * patternY;
                            patternX = Mathf.FloorToInt(position.x * 1.0f / m_patterns[0].width);
                        }

                        int localX = position.x - patternX * m_patterns[0].width;
                        int localY = position.y - patternY * m_patterns[0].height;

                        SpriteOutput sprite = m_patterns[0] == null ? null : m_patterns[0].GetSprite(localX, localY);
                        if (sprite != null)
                        {
                            sprite.GetTileData(position, tilemap, ref tileData);
                        }
                        break;
                    }
                case Type.RandomizedPattern:
                    {
                        if (m_patterns == null || m_patterns.Length == 0)
                        {
                            return;
                        }

                        int patternX = Mathf.FloorToInt(position.x * 1.0f / m_patterns[0].width);
                        int patternY = Mathf.FloorToInt(position.y * 1.0f / m_patterns[0].height);

                        if (m_vertical)
                        {
                            position.y += m_offset * patternX;
                            patternY = Mathf.FloorToInt(position.y * 1.0f / m_patterns[0].height);
                        }
                        else
                        {
                            position.x += m_offset * patternY;
                            patternX = Mathf.FloorToInt(position.x * 1.0f / m_patterns[0].width);
                        }

                        int localX = position.x - patternX * m_patterns[0].width;
                        int localY = position.y - patternY * m_patterns[0].height;

                        if (s_whiteNoise2 == null)
                        {
                            s_whiteNoise2 = new WhiteNoiseSampler3D(12321);
                        }
                        float c = 0.0f;
                        float r = s_whiteNoise2.Sample(patternX, patternY, position.z + tilemap.GetComponent<Transform>().gameObject.GetHashCode()) * m_totalWeight;
                        for (int i = 0; i < m_patterns.Length; i++)
                        {
                            c += m_weights[i];
                            if (r <= c)
                            {
                                SpriteOutput sprite = m_patterns[0] == null ? null : m_patterns[i].GetSprite(localX, localY);
                                if (sprite != null)
                                {
                                    sprite.GetTileData(position, tilemap, ref tileData);
                                }
                                break;
                            }
                        }
                        break;
                    }
            }
        }

        public bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
        {
            switch (m_type)
            {
                case Type.Single:
                    {
                        return false;
                    }
                case Type.Randomized:
                    {
                        return false;
                    }
                case Type.Animated:
                    {
                        if (m_sprites == null || m_sprites.Length == 0)
                        {
                            return false;
                        }

                        if (s_whiteNoise0 == null)
                        {
                            s_whiteNoise0 = new WhiteNoiseSampler3D(69);
                        }
                        if (s_whiteNoise1 == null)
                        {
                            s_whiteNoise1 = new WhiteNoiseSampler3D(420);
                        }
                        tileAnimationData.animatedSprites    = m_sprites;
                        tileAnimationData.animationSpeed     = (s_whiteNoise0.Sample(position.x, position.y, position.z + tilemap.GetComponent<Transform>().gameObject.GetHashCode()) * (m_maxSpeed - m_minSpeed) + m_minSpeed);
                        tileAnimationData.animationStartTime = (s_whiteNoise1.Sample(position.x, position.y, position.z + tilemap.GetComponent<Transform>().gameObject.GetHashCode()) * 2.0f - 1.0f) * m_randomOffset;
                        return true;
                    }
                case Type.Pattern:
                    {
                        if (m_patterns == null || m_patterns.Length == 0)
                        {
                            return false;
                        }

                        int patternX = Mathf.FloorToInt(position.x * 1.0f / m_patterns[0].width);
                        int patternY = Mathf.FloorToInt(position.y * 1.0f / m_patterns[0].height);

                        if (m_vertical)
                        {
                            position.y += m_offset * patternX;
                            patternY = Mathf.FloorToInt(position.y * 1.0f / m_patterns[0].height);
                        }
                        else
                        {
                            position.x += m_offset * patternY;
                            patternX = Mathf.FloorToInt(position.x * 1.0f / m_patterns[0].width);
                        }

                        int localX = position.x - patternX * m_patterns[0].width;
                        int localY = position.y - patternY * m_patterns[0].height;

                        SpriteOutput sprite = m_patterns[0].GetSprite(localX, localY);
                        if (sprite == null)
                        {
                            return false;
                        }
                        return sprite.GetTileAnimationData(position, tilemap, ref tileAnimationData);
                    }
                case Type.RandomizedPattern:
                    {
                        if (m_patterns == null || m_patterns.Length == 0)
                        {
                            return false;
                        }

                        int patternX = Mathf.FloorToInt(position.x * 1.0f / m_patterns[0].width);
                        int patternY = Mathf.FloorToInt(position.y * 1.0f / m_patterns[0].height);

                        if (m_vertical)
                        {
                            position.y += m_offset * patternX;
                            patternY = Mathf.FloorToInt(position.y * 1.0f / m_patterns[0].height);
                        }
                        else
                        {
                            position.x += m_offset * patternY;
                            patternX = Mathf.FloorToInt(position.x * 1.0f / m_patterns[0].width);
                        }

                        int localX = position.x - patternX * m_patterns[0].width;
                        int localY = position.y - patternY * m_patterns[0].height;

                        if (s_whiteNoise2 == null)
                        {
                            s_whiteNoise2 = new WhiteNoiseSampler3D(12321);
                        }
                        float c = 0.0f;
                        float r = s_whiteNoise2.Sample(patternX, patternY, position.z + tilemap.GetComponent<Transform>().gameObject.GetHashCode()) * m_totalWeight;
                        for (int i = 0; i < m_patterns.Length; i++)
                        {
                            c += m_weights[i];
                            if (r <= c)
                            {
                                SpriteOutput sprite = m_patterns[0] == null ? null : m_patterns[i].GetSprite(localX, localY);
                                if (sprite != null)
                                {
                                    return sprite.GetTileAnimationData(position, tilemap, ref tileAnimationData);
                                }
                            }
                        }
                        return false;
                    }
            }
            return false;
        }

        public static SpriteOutput Single(Sprite sprite)
        {
            SpriteOutput res = new SpriteOutput();
            res.m_type = Type.Single;

            res.m_sprites = new Sprite[1] { sprite };

            res.p_Validate();
            return res;
        }

        public static SpriteOutput Randomized(IEnumerable<Sprite> sprites, IEnumerable<float> weights)
        {
            SpriteOutput res = new SpriteOutput();
            res.m_type = Type.Randomized;

            List<Sprite> spritesList = new List<Sprite>(sprites == null ? new Sprite[] { } : sprites);
            List<float>  weightsList = new List<float>(weights == null ? new float[] { } : weights);

            int entryCount = Mathf.Min(spritesList.Count, weightsList.Count);
            spritesList.RemoveRange(entryCount, spritesList.Count - entryCount);
            weightsList.RemoveRange(entryCount, weightsList.Count - entryCount);

            res.m_sprites = spritesList.ToArray();
            res.m_weights = weightsList.ToArray();

            res.p_Validate();
            return res;
        }

        public static SpriteOutput Animated(IEnumerable<Sprite> sprites, float minSpeed, float maxSpeed, float randomOffset)
        {
            SpriteOutput res = new SpriteOutput();
            res.m_type = Type.Animated;

            res.m_sprites = sprites == null ? new Sprite[] { } : sprites.ToArray();

            res.m_minSpeed     = minSpeed;
            res.m_maxSpeed     = maxSpeed;
            res.m_randomOffset = randomOffset;

            res.p_Validate();
            return res;
        }

        public static SpriteOutput Pattern(SpritePattern pattern, int offset, bool vertical)
        {
            SpriteOutput res = new SpriteOutput();
            res.m_type = Type.Pattern;

            res.m_patterns = new SpritePattern[1] { pattern };
            res.m_offset   = offset;
            res.m_vertical = vertical;

            res.p_Validate();
            return res;
        }

        public static SpriteOutput RandomizedPattern(IEnumerable<SpritePattern> patterns, IEnumerable<float> weights, int offset, bool vertical)
        {
            SpriteOutput res = new SpriteOutput();
            res.m_type = Type.RandomizedPattern;

            List<SpritePattern> patternsList = new List<SpritePattern>(patterns == null ? new SpritePattern[] { } : patterns);
            List<float>         weightsList  = new List<float>(weights == null ? new float[] { } : weights);

            int entryCount = Mathf.Min(patternsList.Count, weightsList.Count);
            patternsList.RemoveRange(entryCount, patternsList.Count - entryCount);
            weightsList.RemoveRange(entryCount, weightsList.Count - entryCount);

            res.m_patterns = patternsList.ToArray();
            res.m_weights  = weightsList.ToArray();
            res.m_offset   = offset;
            res.m_vertical = vertical;

            res.p_Validate();
            return res;
        }

        private SpriteOutput()
        {
        }

        private void p_Validate()
        {
            switch (m_type)
            {
                case Type.Single: 
                    {
                        break;
                    }
                case Type.Randomized: 
                    {
                        m_totalWeight = 0.0f;
                        if (m_weights != null)
                        {
                            for (int i = 0; i < m_weights.Length; i++)
                            {
                                m_weights[i] = Mathf.Max(0.0f, m_weights[i]);
                                m_totalWeight += m_weights[i];
                            }
                        }
                        break;
                    }
                case Type.Animated:
                    {
                        break;
                    }
                case Type.Pattern:
                    {
                        break;
                    }
                case Type.RandomizedPattern:
                    {
                        m_totalWeight = 0.0f;
                        if (m_weights != null)
                        {
                            for (int i = 0; i < m_weights.Length; i++)
                            {
                                m_weights[i] = Mathf.Max(0.0f, m_weights[i]);
                                m_totalWeight += m_weights[i];
                            }
                        }
                        break;
                    }
            }
        }

        public enum Type
        {
            Single,
            Randomized,
            Animated,
            Pattern,
            RandomizedPattern
        }
    }
}
