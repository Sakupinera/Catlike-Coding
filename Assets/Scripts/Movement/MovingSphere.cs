using UnityEngine;

namespace Assets.Scripts.Movement
{
    /// <summary>
    /// 移动的小球
    /// </summary>
    public class MovingSphere : MonoBehaviour
    {
        #region 方法

        /// <summary>
        /// 检测输入并更新小球位置
        /// </summary>
        private void Update()
        {
            Vector2 playerInput;
            playerInput.x = Input.GetAxis("Horizontal");
            playerInput.y = Input.GetAxis("Vertical");
            // 约束输入向量的大小永远小于等于1
            playerInput = Vector2.ClampMagnitude(playerInput, 1f);
            Vector3 desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * m_maxSpeed;
            float maxSpeedChange = m_maxAcceleration * Time.deltaTime;

            // 根据当前值和目标值以及最大允许的速度变化变换速度
            m_velocity.x = Mathf.MoveTowards(m_velocity.x, desiredVelocity.x, maxSpeedChange);
            m_velocity.z = Mathf.MoveTowards(m_velocity.z, desiredVelocity.z, maxSpeedChange);

            // 检查点是位于其内部还是位于其边缘
            Vector3 displacement = m_velocity * Time.deltaTime;
            Vector3 newPosition = transform.localPosition + displacement;
            if (newPosition.x < m_allowedArea.xMin)
            {
                newPosition.x = m_allowedArea.xMin;
                m_velocity.x = -m_velocity.x * m_bounciness;
            }
            else if (newPosition.x > m_allowedArea.xMax)
            {
                newPosition.x = m_allowedArea.xMax;
                m_velocity.x = -m_velocity.x * m_bounciness;
            }
            if (newPosition.z < m_allowedArea.yMin)
            {
                newPosition.z = m_allowedArea.yMin;
                m_velocity.z = -m_velocity.z * m_bounciness;
            }
            else if (newPosition.z > m_allowedArea.yMax)
            {
                newPosition.z = m_allowedArea.yMax;
                m_velocity.z = -m_velocity.z * m_bounciness;
            }
            transform.position = newPosition;
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 最大速度
        /// </summary>
        [SerializeField, Range(0f, 100f)]
        private float m_maxSpeed = 10f;

        /// <summary>
        /// 当前速度
        /// </summary>
        private Vector3 m_velocity;

        /// <summary>
        /// 最大加速度
        /// </summary>
        [SerializeField, Range(0f, 100f)]
        private float m_maxAcceleration = 10f;

        /// <summary>
        /// 使用Rect结构约束球体的运动区域
        /// </summary>
        [SerializeField]
        private Rect m_allowedArea = new Rect(-5f, -5f, 10f, 10f);

        /// <summary>
        /// 设置小球的弹性
        /// </summary>
        [SerializeField, Range(0f, 1f)]
        private float m_bounciness = 0.5f;

        #endregion
    }
}
