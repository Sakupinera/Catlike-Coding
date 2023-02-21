using UnityEngine;

namespace Assets.Scripts.Object_Management
{
    /// <summary>
    /// 旋转的物体
    /// </summary>
    public class RotatingObject :PersistableObject
    {
        #region 方法

        /// <summary>
        /// 逐帧更新
        /// </summary>
        private void FixedUpdate()
        {
            transform.Rotate(m_angularVelocity * Time.deltaTime);
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 角速度
        /// </summary>
        [SerializeField]
        private Vector3 m_angularVelocity;

        #endregion
    }
}
