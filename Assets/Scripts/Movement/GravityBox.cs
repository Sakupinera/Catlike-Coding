using UnityEngine;

namespace Assets.Scripts.Movement
{
    /// <summary>
    /// 重力盒子
    /// </summary>
    public class GravityBox : GravitySource
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
        /// 在编辑器中约束各距离的范围并且调整相关参数
        /// </summary>
        private void OnValidate()
        {
            m_boundaryDistance = Vector3.Max(m_boundaryDistance, Vector3.zero);

            float maxInner = Mathf.Min(Mathf.Min(m_boundaryDistance.x, m_boundaryDistance.y), m_boundaryDistance.z);
            m_innerDistance = Mathf.Min(m_innerDistance, maxInner);

            m_innerFalloffDistance = Mathf.Max(Mathf.Min(m_innerFalloffDistance, maxInner), m_innerDistance);
            m_outerFalloffDistance = Mathf.Max(m_outerFalloffDistance, m_outerDistance);

            m_innerFalloffFactor = 1f / (m_innerFalloffDistance - m_innerDistance);
            m_outerFalloffFactor = 1f / (m_outerFalloffDistance - m_outerDistance);
        }

        /// <summary>
        /// 根据位置获取重力
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public override Vector3 GetGravity(Vector3 position)
        {
            // 为了支持任意旋转，需要旋转小球的相对位置来与立方体对齐
            position = transform.InverseTransformDirection(position - transform.position);
            Vector3 vector = Vector3.zero;
            // 检测小球是在（检测）盒子内侧还是外侧，并更新向量值
            int outside = 0;
            if (position.x > m_boundaryDistance.x)
            {
                vector.x = m_boundaryDistance.x - position.x;
                outside = 1;
            }
            else if (position.x < -m_boundaryDistance.x)
            {
                vector.x = -m_boundaryDistance.x - position.x;
                outside = 1;
            }
            if (position.y > m_boundaryDistance.y)
            {
                vector.y = m_boundaryDistance.y - position.y;
                outside += 1;
            }
            else if (position.y < -m_boundaryDistance.y)
            {
                vector.y = -m_boundaryDistance.y - position.y;
                outside += 1;
            }
            if (position.z > m_boundaryDistance.z)
            {
                vector.z = m_boundaryDistance.z - position.z;
                outside += 1;
            }
            else if (position.z < -m_boundaryDistance.z)
            {
                vector.z = -m_boundaryDistance.z - position.z;
                outside += 1;
            }

            // 如果小球至少在一个表面外，则计算（检测）盒子外部重力的方向和大小
            if (outside > 0)
            {
                // 如果小球恰好在一个面外面，则可以简化计算
                float distance = outside == 1 ? Mathf.Abs(vector.x + vector.y + vector.z) : vector.magnitude;
                if (distance > m_outerFalloffDistance)
                {
                    return Vector3.zero;
                }

                float g = m_gravity / distance;
                if (distance > m_outerDistance)
                {
                    g *= 1f - (distance - m_outerDistance) * m_outerFalloffFactor;
                }

                return transform.TransformDirection(g * vector);
            }
            // 执行到这里，则计算（检测）盒子内部重力的方向和大小
            Vector3 distances;
            distances.x = m_boundaryDistance.x - Mathf.Abs(position.x);
            distances.y = m_boundaryDistance.y - Mathf.Abs(position.y);
            distances.z = m_boundaryDistance.z - Mathf.Abs(position.z);
            if (distances.x < distances.y)
            {
                if (distances.x < distances.z)
                {
                    vector.x = GetGravityComponent(position.x, distances.x);
                }
                else
                {
                    vector.z = GetGravityComponent(position.z, distances.z);
                }
            }
            else if (distances.y < distances.z)
            {
                vector.y = GetGravityComponent(position.y, distances.y);
            }
            else
            {
                vector.z = GetGravityComponent(position.z, distances.z);
            }
            return transform.TransformDirection(vector);
        }

        /// <summary>
        /// 获取单一重力分量
        /// </summary>
        /// <param name="coordinate"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        private float GetGravityComponent(float coordinate, float distance)
        {
            // 如果距离大于内部衰减距离，则小球位于零重力区域
            if (distance > m_innerFalloffDistance)
            {
                return 0f;
            }

            // 根据距离调整重力的大小
            float g = m_gravity;
            if (distance > m_innerDistance)
            {
                g *= 1f - (distance - m_innerDistance) * m_innerFalloffFactor;
            }
            // 如果坐标小于零，则必须反转重力，这意味着我们位于中心的另一侧
            return coordinate > 0f ? -g : g;
        }

        /// <summary>
        /// 可视化约束边界
        /// </summary>
        private void OnDrawGizmos()
        {
            #region 绘制盒子内侧边界

            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Vector3 size;
            if (m_innerFalloffDistance > m_innerDistance)
            {
                Gizmos.color = Color.cyan;
                size.x = 2f * (m_boundaryDistance.x - m_innerFalloffDistance);
                size.y = 2f * (m_boundaryDistance.y - m_innerFalloffDistance);
                size.z = 2f * (m_boundaryDistance.z - m_innerFalloffDistance);
                Gizmos.DrawWireCube(Vector3.zero, size);
            }

            if (m_innerDistance > 0f)
            {
                Gizmos.color = Color.yellow;
                size.x = 2f * (m_boundaryDistance.x - m_innerDistance);
                size.y = 2f * (m_boundaryDistance.y - m_innerDistance);
                size.z = 2f * (m_boundaryDistance.z - m_innerDistance);
                Gizmos.DrawWireCube(Vector3.zero, size);
            }

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(Vector3.zero, 2f * m_boundaryDistance);

            #endregion

            #region 绘制盒子外侧边界

            if (m_outerDistance > 0f)
            {
                Gizmos.color = Color.yellow;
                DrawGizmosOuterCube(m_outerDistance);
            }
            if (m_outerFalloffDistance > m_outerDistance)
            {
                Gizmos.color = Color.cyan;
                DrawGizmosOuterCube(m_outerFalloffDistance);
            }

            #endregion

        }

        /// <summary>
        /// 绘制圆角立方体形状的外部区域
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        private void DrawGizmosRect(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(b, c);
            Gizmos.DrawLine(c, d);
            Gizmos.DrawLine(d, a);
        }

        /// <summary>
        /// 绘制给定距离的外部立方体
        /// </summary>
        /// <param name="distance"></param>
        private void DrawGizmosOuterCube(float distance)
        {
            Vector3 a, b, c, d;
            a.y = b.y = m_boundaryDistance.y;
            d.y = c.y = -m_boundaryDistance.y;
            b.z = c.z = m_boundaryDistance.z;
            d.z = a.z = -m_boundaryDistance.z;
            a.x = b.x = c.x = d.x = m_boundaryDistance.x + distance;
            DrawGizmosRect(a, b, c, d);
            // 取反这些点的X坐标，以便绘制另一侧
            a.x = b.x = c.x = d.x = -a.x;
            DrawGizmosRect(a, b, c, d);

            a.x = d.x = m_boundaryDistance.x;
            b.x = c.x = -m_boundaryDistance.x;
            a.z = b.z = m_boundaryDistance.z;
            c.z = d.z = -m_boundaryDistance.z;
            a.y = b.y = c.y = d.y = m_boundaryDistance.y + distance;
            DrawGizmosRect(a, b, c, d);
            // 取反这些点的Y坐标，以便绘制另一侧
            a.y = b.y = c.y = d.y = -a.y;
            DrawGizmosRect(a, b, c, d);

            a.x = d.x = m_boundaryDistance.x;
            b.x = c.x = -m_boundaryDistance.x;
            a.y = b.y = m_boundaryDistance.y;
            c.y = d.y = -m_boundaryDistance.y;
            a.z = b.z = c.z = d.z = m_boundaryDistance.z + distance;
            DrawGizmosRect(a, b, c, d);
            // 取反这些点的Z坐标，以便绘制另一侧
            a.z = b.z = c.z = d.z = -a.z;
            DrawGizmosRect(a, b, c, d);

            // 在立方体的圆角点之间添加单独的线框立方体来近似圆角
            distance *= 0.5773502692f;
            Vector3 size = m_boundaryDistance;
            size.x = 2f * (size.x + distance);
            size.y = 2f * (size.y + distance);
            size.z = 2f * (size.z + distance);
            Gizmos.DrawWireCube(Vector3.zero, size);
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 重力
        /// </summary>
        [SerializeField]
        private float m_gravity = 9.81f;

        /// <summary>
        /// 边界距离向量（盒子的大小）
        /// </summary>
        [SerializeField]
        private Vector3 m_boundaryDistance = Vector3.one;

        /// <summary>
        /// 重力影响的内部距离
        /// </summary>
        [SerializeField, Min(0f)]
        private float m_innerDistance = 0f;

        /// <summary>
        /// 重力影响的外部距离
        /// </summary>
        [SerializeField, Min(0f)]
        private float m_outerDistance = 0f;

        /// <summary>
        /// 重力衰减的内部距离
        /// </summary>
        [SerializeField, Min(0f)]
        private float m_innerFalloffDistance = 0f;

        /// <summary>
        /// 重力衰减的外部距离
        /// </summary>
        [SerializeField, Min(0f)]
        private float m_outerFalloffDistance = 0f;

        /// <summary>
        /// 盒子内部的重力衰减系数
        /// </summary>
        private float m_innerFalloffFactor;

        /// <summary>
        /// 盒子外部的重力衰减系数
        /// </summary>
        private float m_outerFalloffFactor;

        #endregion
    }
}
