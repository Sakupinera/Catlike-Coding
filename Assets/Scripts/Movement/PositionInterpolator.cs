using UnityEngine;

namespace Assets.Scripts.Movement
{
    /// <summary>
    /// 位置插值器
    /// </summary>
    public class PositionInterpolator : MonoBehaviour
    {
        #region 方法

        /// <summary>
        /// 根据时间t进行插值
        /// </summary>
        /// <param name="t"></param>
        public void Interpolate(float t)
        {
            Vector3 p;
            if (m_relativeTo)
            {
                p = Vector3.LerpUnclamped(m_relativeTo.TransformPoint(from), m_relativeTo.TransformPoint(to), t);
            }
            else
            {
                p = Vector3.LerpUnclamped(from, to, t);
            }
            m_rigidbody.MovePosition(p);
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 刚体组件
        /// </summary>
        [SerializeField] 
        private Rigidbody m_rigidbody = default;

        /// <summary>
        /// 初始位置
        /// </summary>
        [SerializeField]
        private Vector3 from = default;

        /// <summary>
        /// 目标位置
        /// </summary>
        [SerializeField]
        private Vector3 to = default;

        /// <summary>
        /// 局部空间选项
        /// </summary>
        [SerializeField] 
        private Transform m_relativeTo = default;

        #endregion
    }
}
