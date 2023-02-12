using UnityEngine;

namespace Assets.Scripts.Movement
{
    /// <summary>
    /// 重力球体
    /// </summary>
    public class GravitySphere : GravitySource
    {
        #region 方法

        /// <summary>
        /// 初始化
        /// </summary>
        private void Awake()
        {
            OnValidate();
        }

        /// <summary>
        /// 约束在检查器中数值调整的范围并调整相关参数
        /// </summary>
        private void OnValidate()
        {
            m_innerFalloffRadius = Mathf.Max(m_innerFalloffRadius, 0f);
            m_innerRadius = Mathf.Max(m_innerRadius, m_innerFalloffRadius);
            m_outerRadius = Mathf.Max(m_outerRadius, m_innerRadius);
            m_outerFalloffRadius = Mathf.Max(m_outerFalloffRadius, m_outerRadius);

            m_innerFalloffFactor = 1f / (m_innerRadius - m_innerFalloffRadius);
            m_outerFalloffFactor = 1f / (m_outerFalloffRadius - m_outerRadius);
        }

        /// <summary>
        /// 根据位置获取重力源的重力
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public override Vector3 GetGravity(Vector3 position)
        {
            Vector3 vector = transform.position - position;
            float distance = vector.magnitude;
            // 当距离大于外部衰减半径或者小于内部衰减半径时，可以忽略重力效果
            if (distance > m_outerFalloffRadius || distance < m_innerFalloffRadius)
            {
                return Vector3.zero;
            }

            float g = m_gravity / distance;
            if (distance > m_outerRadius)
            {
                g *= 1f - (distance - m_outerRadius) * m_outerFalloffFactor;
            }
            else if (distance < m_innerRadius)
            {
                g *= 1f - (m_innerRadius - distance) * m_innerFalloffFactor;
            }
            return g * vector;
        }

        /// <summary>
        /// 可视化半径和衰减半径
        /// </summary>
        private void OnDrawGizmos()
        {
            Vector3 p = transform.position;
            if (m_innerFalloffRadius > 0f && m_innerFalloffRadius < m_innerRadius)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(p, m_innerFalloffRadius);
            }
            Gizmos.color = Color.yellow;
            if (m_innerRadius > 0f && m_innerRadius < m_outerRadius)
            {
                Gizmos.DrawWireSphere(p, m_innerRadius);
            }
            Gizmos.DrawWireSphere(p, m_outerRadius);
            if (m_outerFalloffRadius > m_outerRadius)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(p, m_outerFalloffRadius);
            }
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 重力
        /// </summary>
        [SerializeField]
        private float m_gravity = 9.81f;

        /// <summary>
        /// 球体的内部衰减半径
        /// </summary>
        [SerializeField, Min(0f)]
        private float m_innerFalloffRadius = 1f;

        /// <summary>
        /// 球体的内部半径
        /// </summary>
        [SerializeField, Min(0f)]
        private float m_innerRadius = 5f;

        /// <summary>
        /// 球体的外半径
        /// </summary>
        [SerializeField, Min(0f)]
        private float m_outerRadius = 10f;

        /// <summary>
        /// 球体的外衰减半径
        /// </summary>
        [SerializeField, Min(0f)]
        private float m_outerFalloffRadius = 15f;

        /// <summary>
        /// 内部重力衰减因子
        /// </summary>
        private float m_innerFalloffFactor;

        /// <summary>
        /// 外部重力衰减因子
        /// </summary>
        private float m_outerFalloffFactor;

        #endregion
    }
}
