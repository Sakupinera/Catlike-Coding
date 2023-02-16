using System;
using UnityEngine;

namespace Assets.Scripts.Movement
{
    /// <summary>
    /// 可加速区域
    /// </summary>
    public class AccelerationZone : MonoBehaviour
    {
        #region 方法

        /// <summary>
        /// 检测是否有物体进入加速区域
        /// </summary>
        /// <param name="other"></param>
        private void OnTriggerEnter(Collider other)
        {
            Rigidbody body = other.attachedRigidbody;
            if (body)
            {
                Accelerate(body);
            }
        }

        /// <summary>
        /// 检测是否有物体一直在加速区域
        /// </summary>
        /// <param name="other"></param>
        private void OnTriggerStay(Collider other)
        {
            Rigidbody body  = other.attachedRigidbody;
            if (body)
            {
                Accelerate(body);
            }
        }

        /// <summary>
        /// 计算物体加速之后的速度
        /// </summary>
        /// <param name="body"></param>
        private void Accelerate(Rigidbody body)
        {
            // 将世界空间的速度转换为局部空间的速度
            Vector3 velocity = transform.InverseTransformDirection(body.velocity);
            if (velocity.y >= m_speed)
            {
                return;
            }

            // 为物体持续加速
            if (m_acceleration > 0f)
            {
                velocity.y = Mathf.MoveTowards(
                    velocity.y, m_speed, m_acceleration * Time.deltaTime
                );
            }
            else
            {
                velocity.y = m_speed;
            }

            body.velocity = transform.TransformDirection(velocity);
            if (body.TryGetComponent(out MovingSphere sphere))
            {
                sphere.PreventSnapToGround();
            }
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 可加速区域提供的加速度
        /// </summary>
        [SerializeField, Min(0f)]
        private float m_acceleration = 10f;

        /// <summary>
        /// 可加速区域提供的速度
        /// </summary>
        [SerializeField, Min(0f)]
        private float m_speed = 10f;

        #endregion
    }
}
