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
                int index = Random.Range(0, m_spawnZones.Length);
                return m_spawnZones[index].SpawnPoint;
            }
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 生成区域数组
        /// </summary>
        [SerializeField]
        private SpawnZone[] m_spawnZones;

        #endregion
    }
}
