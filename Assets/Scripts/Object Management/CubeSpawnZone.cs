using UnityEngine;

namespace Assets.Scripts.Object_Management
{
    /// <summary>
    /// 球形生成区域
    /// </summary>
    public class CubeSpawnZone : SpawnZone
    {
        #region 方法

        /// <summary>
        /// 可视化区域范围
        /// </summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }

        #endregion

        #region 属性

        /// <summary>
        /// 获取一个任意的生成点
        /// </summary>
        public override Vector3 SpawnPoint
        {
            get
            {
                Vector3 p;
                p.x = Random.Range(-0.5f, 0.5f);
                p.y = Random.Range(-0.5f, 0.5f);
                p.z = Random.Range(-0.5f, 0.5f);
                if (m_surfaceOnly)
                {
                    int axis = Random.Range(0, 3);
                    p[axis] = p[axis] < 0f ? -0.5f : 0.5f;
                }
                return transform.TransformPoint(p);
            }
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