using UnityEngine;

namespace Assets.Scripts.Object_Management
{
    /// <summary>
    /// 球形生成区域
    /// </summary>
    public class SphereSpawnZone : SpawnZone
    {
        #region 方法

        /// <summary>
        /// 可视化区域范围
        /// </summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireSphere(Vector3.zero, 1f);
        }

        #endregion

        #region 属性

        /// <summary>
        /// 获取一个任意的生成点
        /// </summary>
        public override Vector3 SpawnPoint
        {
            get { return transform.TransformPoint(m_surfaceOnly ? Random.onUnitSphere : Random.insideUnitSphere); }
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 是否只在表面生成游戏物体
        /// </summary>
        [SerializeField]
        private bool m_surfaceOnly;

        #endregion
    }
}
