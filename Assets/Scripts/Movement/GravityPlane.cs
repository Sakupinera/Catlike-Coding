using UnityEngine;

namespace Assets.Scripts.Movement
{
    /// <summary>
    /// 重力平面
    /// </summary>
    public class GravityPlane : GravitySource
    {
        #region 方法

        /// <summary>
        /// 根据位置获取重力源的重力
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public override Vector3 GetGravity(Vector3 position)
        {
            Vector3 up = transform.up;
            float distance = Vector3.Dot(up, position - transform.position);
            if (distance > m_range)
            {
                return Vector3.zero;
            }

            // 在重力范围内根据与重力源的远近调整重力的大小
            float g = -m_gravity;
            if (distance > 0f)
            {
                g *= 1f - distance / m_range;
            }

            return g * up;
        }

        /// <summary>
        /// 可视化平面
        /// </summary>
        private void OnDrawGizmos()
        {
            // 使用范围作为偏移量，而不受平面游戏对象的缩放影响
            Vector3 scale = transform.localScale;
            scale.y = m_range;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, scale);
            Vector3 size = new Vector3(1f, 0f, 1f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(Vector3.zero, size);
            if (m_range > 0f)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(Vector3.up, size);
            }
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 重力
        /// </summary>
        private float m_gravity = 9.81f;

        /// <summary>
        /// 重力范围
        /// </summary>
        [SerializeField, Min(0f)] private float m_range = 1f;

        #endregion
    }
}
