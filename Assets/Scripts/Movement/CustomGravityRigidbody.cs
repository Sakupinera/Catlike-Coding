using UnityEngine;

namespace Assets.Scripts.Movement
{
    /// <summary>
    /// 自定义刚体组件
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CustomGravityRigidbody : MonoBehaviour
    {
        #region 方法

        /// <summary>
        /// 初始化
        /// </summary>
        private void Awake()
        {
            m_rigidbody = GetComponent<Rigidbody>();
            m_rigidbody.useGravity = false;
        }

        /// <summary>
        /// 更新重力
        /// </summary>
        private void FixedUpdate()
        {
            if (m_floatToSleep)
            {
                // 检测刚体是否处于睡眠状态
                if (m_rigidbody.IsSleeping())
                {
                    m_floatDelay = 0f;
                    GetComponent<Renderer>().material.SetColor("_Color", Color.grey);
                    return;
                }

                // 如果刚体速度非常低，我们就认为刚体静止了
                if (m_rigidbody.velocity.sqrMagnitude < 0.0001f)
                {
                    m_floatDelay += Time.deltaTime;
                    GetComponent<Renderer>().material.SetColor("_Color", Color.yellow);
                    if (m_floatDelay >= 1f)
                        return;
                }
                else
                {
                    m_floatDelay = 0f;
                    GetComponent<Renderer>().material.SetColor("_Color", Color.red);
                }
            }

            m_rigidbody.AddForce(CustomGravity.GetGravity(m_rigidbody.position), ForceMode.Acceleration);
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 刚体组件
        /// </summary>
        private Rigidbody m_rigidbody;

        /// <summary>
        /// 停止检测延时
        /// </summary>
        private float m_floatDelay;

        /// <summary>
        /// 物体是否允许进入睡眠状态
        /// </summary>
        [SerializeField]
        private bool m_floatToSleep = false;

        #endregion
    }
}
