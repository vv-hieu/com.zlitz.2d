using UnityEngine;

namespace Zlitz.Tiles
{
    [CreateAssetMenu(menuName = "Zlitz/Tiles/Connected Template")]
    public class ConnectedTemplate : ScriptableObject
    {
        [SerializeField] 
        private int m_width;

        [SerializeField]
        private int m_height;

        [SerializeField]
        private ConnectedTile.Configuration[] m_configurations;

        public int width => m_width;
        public int height => m_height;
        public ConnectedTile.Configuration[] configurations => m_configurations;

        private void OnEnable()
        {
            m_width  = Mathf.Max(1, m_width);
            m_height = Mathf.Max(1, m_height);

            if (m_configurations == null || m_width * m_height != m_configurations.Length)
            {
                m_configurations = new ConnectedTile.Configuration[m_width * m_height];
                for (int i = 0; i < m_width * m_height; i++)
                {
                    m_configurations[i] = new ConnectedTile.Configuration();
                }
            }
        }

        private void OnValidate()
        {
            m_width  = Mathf.Max(1, m_width);
            m_height = Mathf.Max(1, m_height);

            if (m_configurations == null || m_width * m_height != m_configurations.Length)
            {
                m_configurations = new ConnectedTile.Configuration[m_width * m_height];
                for (int i = 0; i < m_width * m_height; i++)
                {
                    m_configurations[i] = new ConnectedTile.Configuration();
                }
            }
        }
    }
}
