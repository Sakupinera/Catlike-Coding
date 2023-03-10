using UnityEngine;

namespace Assets.Scripts.Object_Management
{
    /// <summary>
    /// 游戏对象复合生成区域
    /// </summary>
    public class CompositeSpawnZone : SpawnZone
    {
        #region 属性

        /// <summary>
        /// 获取一个任意的生成点
        /// </summary>
        public override Vector3 SpawnPoint
        {
            get
            {
                int index;
                if (m_sequential)
                {
                    index = m_nextSequentialIndex++;
                    if (m_nextSequentialIndex >= m_spawnZones.Length)
                    {
                        m_nextSequentialIndex = 0;
                    }
                }
                else
                {
                    index = Random.Range(0, m_spawnZones.Length);
                }
                return m_spawnZones[index].SpawnPoint;
            }
        }

        /// <summary>
        /// 存档
        /// </summary>
        /// <param name="writer"></param>
        public override void Save(GameDataWriter writer)
        {
            base.Save(writer);
            writer.Write(m_nextSequentialIndex);
        }

        /// <summary>
        /// 读档
        /// </summary>
        /// <param name="reader"></param>
        public override void Load(GameDataReader reader)
        {
            if (reader.Version >= 7)
            {
                base.Load(reader);
            }
            m_nextSequentialIndex = reader.ReadInt();
        }

        /// <summary>
        /// 生成游戏对象
        /// </summary>
        /// <param name="shape"></param>
        public override void SpawnShapes()
        {
            if (m_overrideConfig)
            {
                base.SpawnShapes();
            }
            else
            {
                int index;
                if (m_sequential)
                {
                    index = m_nextSequentialIndex++;
                    if (m_nextSequentialIndex >= m_spawnZones.Length)
                    {
                        m_nextSequentialIndex = 0;
                    }
                }
                else
                {
                    index = Random.Range(0, m_spawnZones.Length);
                }
                m_spawnZones[index].SpawnShapes();
            }
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 生成区域数组
        /// </summary>
        [SerializeField]
        private SpawnZone[] m_spawnZones;

        /// <summary>
        /// 是否按顺序生成
        /// </summary>
        [SerializeField]
        private bool m_sequential;

        /// <summary>
        /// 是否重写配置
        /// </summary>
        [SerializeField]
        private bool m_overrideConfig;

        /// <summary>
        /// 下一个序列下标
        /// </summary>
        private int m_nextSequentialIndex;

        #endregion
    }
}
