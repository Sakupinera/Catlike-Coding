#define Swimming
using UnityEngine;

#if CustomGravity
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
#endif

#if Swimming
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

            m_gravity = CustomGravity.GetGravity(m_rigidbody.position);
            // 计算物体在水中受到的力
            if (m_submergence > 0f)
            {
                float drag = Mathf.Max(0f, 1f - m_waterDrag * m_submergence * Time.deltaTime);
                m_rigidbody.velocity *= drag;
                m_rigidbody.angularVelocity *= drag;
                // 使物体朝着特定方向漂浮
                m_rigidbody.AddForceAtPosition(m_gravity * -(m_buoyancy * m_submergence), 
                    transform.TransformPoint(m_buoyancyOffset), ForceMode.Acceleration);
                m_submergence = 0f;
            }
            m_rigidbody.AddForce(m_gravity, ForceMode.Acceleration);
        }

        /// <summary>
        /// 检测物体与水的接触
        /// </summary>
        /// <param name="other"></param>
        private void OnTriggerEnter(Collider other)
        {
            if ((m_waterMask & (1 << other.gameObject.layer)) != 0)
            {
                EvaluateSubmergence();
            }
        }

        /// <summary>
        /// 检测物体与水的接触
        /// </summary>
        /// <param name="other"></param>
        private void OnTriggerStay(Collider other)
        {
            if (!m_rigidbody.IsSleeping() && (m_waterMask & (1 << other.gameObject.layer)) != 0)
            {
                EvaluateSubmergence();
            }
        }

        /// <summary>
        /// 计算物体浸入的深度
        /// </summary>
        private void EvaluateSubmergence()
        {
            Vector3 upAxis = -m_gravity.normalized;
            if (Physics.Raycast(m_rigidbody.position + upAxis * m_submergenceOffset, -upAxis, out RaycastHit hit,
                    m_submergenceRange + 1f, m_waterMask, QueryTriggerInteraction.Collide))
            {
                m_submergence = 1f - hit.distance / m_submergenceRange;
            }
            else
            {
                m_submergence = 1f;
            }
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

        /// <summary>
        /// 中心点距离最上方的偏移距离
        /// </summary>
        [SerializeField]
        private float m_submergenceOffset = 0.5f;

        /// <summary>
        /// 完全浸水深度
        /// </summary>
        [SerializeField, Min(0.1f)]
        private float m_submergenceRange = 1f;

        /// <summary>
        /// 浮力
        /// </summary>
        [SerializeField, Min(0f)]
        private float m_buoyancy = 1f;

        /// <summary>
        /// 水的阻力
        /// </summary>
        [SerializeField, Range(0f, 10f)]
        private float m_waterDrag = 1f;

        /// <summary>
        /// 水遮罩
        /// </summary>
        [SerializeField]
        private LayerMask m_waterMask = 0;

        /// <summary>
        /// 浸水程度
        /// </summary>
        private float m_submergence;

        /// <summary>
        /// 重力
        /// </summary>
        private Vector3 m_gravity;

        /// <summary>
        /// 浮力偏移向量
        /// </summary>
        [SerializeField]
        private Vector3 m_buoyancyOffset = Vector3.zero;

        #endregion
    }
}
#endif