#define PhysicsLevelThree

using UnityEngine;
#if SildingASphere
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
#endif

#if PhysicsLevelOne
namespace Assets.Scripts.Movement
{
    /// <summary>
    /// 移动的小球
    /// </summary>
    public class MovingSphere : MonoBehaviour
    {
#region 方法

        /// <summary>
        /// 获取组件
        /// </summary>
        private void Awake()
        {
            m_rigidbody = GetComponent<Rigidbody>();
        }

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
            m_desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * m_maxSpeed;

            m_desiredJump |= Input.GetButtonDown("Jump");
        }

        /// <summary>
        /// 以固定的时间步长调整步速
        /// </summary>
        private void FixedUpdate()
        {
            UpdateState();
            float acceleration = m_onGround ? m_maxAcceleration : m_maxAirAcceleration;
            // 当FixedUpdate被调用时，Time.deltaTime等于Time.fixedDeltaTime
            float maxSpeedChange = acceleration * Time.deltaTime;

            // 根据当前值和目标值以及最大允许的速度变化变换速度
            m_velocity.x = Mathf.MoveTowards(m_velocity.x, m_desiredVelocity.x, maxSpeedChange);
            m_velocity.z = Mathf.MoveTowards(m_velocity.z, m_desiredVelocity.z, maxSpeedChange);

            // 判断之前是否按下过跳跃键
            if (m_desiredJump)
            {
                m_desiredJump = false;
                Jump();
            }

            m_rigidbody.velocity = m_velocity;

            m_onGround = false;
        }

        /// <summary>
        /// 更新小球状态
        /// </summary>
        private void UpdateState()
        {
            m_velocity = m_rigidbody.velocity;
            if (m_onGround)
            {
                m_jumpPhase = 0;
            }
        }

        /// <summary>
        /// 在PhysX检测到新的碰撞后被调用
        /// </summary>
        private void OnCollisionEnter(Collision collision)
        {
            EvaluateCollision(collision);
        }

        /// <summary>
        /// 只要碰撞仍然存在，就可以在每个物理步骤中调用该方法
        /// </summary>
        private void OnCollisionStay(Collision collision)
        {
            EvaluateCollision(collision);
        }

        /// <summary>
        /// 小球跳跃
        /// </summary>
        private void Jump()
        {
            // 检查小球是否在地面上或是否尚未达到允许的最大空中跳跃次数
            if (m_onGround || m_jumpPhase < m_maxAirJumps)
            {
                m_jumpPhase += 1;
                float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * m_jumpHeight);
                // 限制小球的跳跃速度
                if (m_velocity.y > 0f)
                {
                    jumpSpeed = Mathf.Max(jumpSpeed - m_velocity.y, 0f);
                }
                m_velocity.y += jumpSpeed;
            }
        }

        /// <summary>
        /// 评估小球与其他物体发生的碰撞
        /// </summary>
        /// <param name="collision"></param>
        private void EvaluateCollision(Collision collision)
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                Vector3 normal = collision.GetContact(i).normal;
                m_onGround |= normal.y >= 0.9f;
            }
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
        /// 预期速度
        /// </summary>
        private Vector3 m_desiredVelocity;

        /// <summary>
        /// 最大加速度
        /// </summary>
        [SerializeField, Range(0f, 100f)]
        private float m_maxAcceleration = 10f;

        /// <summary>
        /// 空中最大加速度
        /// </summary>
        [SerializeField, Range(0f, 100f)] 
        private float m_maxAirAcceleration = 1f;

        /// <summary>
        /// 刚体组件
        /// </summary>
        private Rigidbody m_rigidbody;

        /// <summary>
        /// 是否执行跳跃
        /// </summary>
        private bool m_desiredJump;

        /// <summary>
        /// 跳跃高度
        /// </summary>
        [SerializeField, Range(0f, 10f)] 
        private float m_jumpHeight = 2f;

        /// <summary>
        /// 是否在地面
        /// </summary>
        private bool m_onGround;

        /// <summary>
        /// 空中允许的最大跳跃次数
        /// </summary>
        [SerializeField, Range(0, 5)] 
        private int m_maxAirJumps = 0;

        /// <summary>
        /// 当前处于第几段跳跃
        /// </summary>
        private int m_jumpPhase;

#endregion
    }
}
#endif

#if PhysicsLevelTwo
namespace Assets.Scripts.Movement
{
    /// <summary>
    /// 移动的小球
    /// </summary>
    public class MovingSphere : MonoBehaviour
    {
#region 方法

        /// <summary>
        /// 获取组件
        /// </summary>
        private void Awake()
        {
            m_rigidbody = GetComponent<Rigidbody>();
            OnValidate();
        }

        /// <summary>
        /// 在播放模式下通过检查器更改角度时，更新相应字段值
        /// </summary>
        private void OnValidate()
        {
            m_minGroundDotProduct = Mathf.Cos(m_maxGroundAngle * Mathf.Deg2Rad);
        }

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
            m_desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * m_maxSpeed;

            m_desiredJump |= Input.GetButtonDown("Jump");
        }

        /// <summary>
        /// 以固定的时间步长调整步速
        /// </summary>
        private void FixedUpdate()
        {
            UpdateState();
            AdjustVelocity();

            // 判断之前是否按下过跳跃键
            if (m_desiredJump)
            {
                m_desiredJump = false;
                Jump();
            }

            m_rigidbody.velocity = m_velocity;

            m_onGround = false;
        }

        /// <summary>
        /// 更新小球状态
        /// </summary>
        private void UpdateState()
        {
            m_velocity = m_rigidbody.velocity;
            if (m_onGround)
            {
                m_jumpPhase = 0;
            }
            else
            {
                // 空中跳跃时，小球直线上升
                m_contactNormal = Vector3.up;
            }
        }

        /// <summary>
        /// 在PhysX检测到新的碰撞后被调用
        /// </summary>
        private void OnCollisionEnter(Collision collision)
        {
            EvaluateCollision(collision);
        }

        /// <summary>
        /// 只要碰撞仍然存在，就可以在每个物理步骤中调用该方法
        /// </summary>
        private void OnCollisionStay(Collision collision)
        {
            EvaluateCollision(collision);
        }

        /// <summary>
        /// 小球跳跃
        /// </summary>
        private void Jump()
        {
            // 检查小球是否在地面上或是否尚未达到允许的最大空中跳跃次数
            if (m_onGround || m_jumpPhase < m_maxAirJumps)
            {
                m_jumpPhase += 1;
                float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * m_jumpHeight);
                float alignedSpeed = Vector3.Dot(m_velocity, m_contactNormal);
                // 限制小球的跳跃速度
                if (alignedSpeed > 0f)
                {
                    jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
                }
                m_velocity += m_contactNormal * jumpSpeed;
            }
        }

        /// <summary>
        /// 评估小球与其他物体发生的碰撞
        /// </summary>
        /// <param name="collision"></param>
        private void EvaluateCollision(Collision collision)
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                Vector3 normal = collision.GetContact(i).normal;
                if (normal.y > m_minGroundDotProduct)
                {
                    m_onGround = true;
                    m_contactNormal = normal;
                }
            }
        }

        /// <summary>
        /// 将所需的速度与地面对齐
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        private Vector3 ProjectOnContactPlane(Vector3 vector)
        {
            return vector - m_contactNormal * Vector3.Dot(vector, m_contactNormal);
        }

        /// <summary>
        /// 根据接触平面上的投影来调整小球的速度
        /// </summary>
        private void AdjustVelocity()
        {
            Vector3 xAxis = ProjectOnContactPlane(Vector3.right).normalized;
            Vector3 zAxis = ProjectOnContactPlane(Vector3.forward).normalized;

            float currentX = Vector3.Dot(m_velocity, xAxis);
            float currentZ = Vector3.Dot(m_velocity, zAxis);

            float acceleration = m_onGround ? m_maxAcceleration : m_maxAirAcceleration;
            // 当FixedUpdate被调用时，Time.deltaTime等于Time.fixedDeltaTime
            float maxSpeedChange = acceleration * Time.deltaTime;

            // 根据当前值和目标值以及最大允许的速度变化变换速度
            float newX = Mathf.MoveTowards(m_velocity.x, m_desiredVelocity.x, maxSpeedChange);
            float newZ = Mathf.MoveTowards(m_velocity.z, m_desiredVelocity.z, maxSpeedChange);

            m_velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
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
        /// 预期速度
        /// </summary>
        private Vector3 m_desiredVelocity;

        /// <summary>
        /// 最大加速度
        /// </summary>
        [SerializeField, Range(0f, 100f)]
        private float m_maxAcceleration = 10f;

        /// <summary>
        /// 空中最大加速度
        /// </summary>
        [SerializeField, Range(0f, 100f)]
        private float m_maxAirAcceleration = 1f;

        /// <summary>
        /// 刚体组件
        /// </summary>
        private Rigidbody m_rigidbody;

        /// <summary>
        /// 是否执行跳跃
        /// </summary>
        private bool m_desiredJump;

        /// <summary>
        /// 跳跃高度
        /// </summary>
        [SerializeField, Range(0f, 10f)]
        private float m_jumpHeight = 2f;

        /// <summary>
        /// 是否在地面
        /// </summary>
        private bool m_onGround;

        /// <summary>
        /// 空中允许的最大跳跃次数
        /// </summary>
        [SerializeField, Range(0, 5)]
        private int m_maxAirJumps = 0;

        /// <summary>
        /// 当前处于第几段跳跃
        /// </summary>
        private int m_jumpPhase;

        /// <summary>
        /// 斜坡被判定为地面的最大角度
        /// </summary>
        [SerializeField, Range(0f, 90f)] 
        private float m_maxGroundAngle = 25f;

        /// <summary>
        /// 与斜坡表面法线点乘的最小结果
        /// </summary>
        private float m_minGroundDotProduct;

        /// <summary>
        /// 小球与斜坡接触点的表面法线
        /// </summary>
        private Vector3 m_contactNormal;

#endregion
    }
}
#endif

#if PhysicsLevelThree
namespace Assets.Scripts.Movement
{
    /// <summary>
    /// 移动的小球
    /// </summary>
    public class MovingSphere : MonoBehaviour
    {
        #region 方法

        /// <summary>
        /// 初始化
        /// </summary>
        private void Awake()
        {
            m_rigidbody = GetComponent<Rigidbody>();
            OnValidate();
        }

        /// <summary>
        /// 在播放模式下通过检查器更改角度时，更新相应字段值
        /// </summary>
        private void OnValidate()
        {
            m_minGroundDotProduct = Mathf.Cos(m_maxGroundAngle * Mathf.Deg2Rad);
        }

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
            m_desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * m_maxSpeed;

            m_desiredJump |= Input.GetButtonDown("Jump");

            // 根据小球与地面的接触点数改变小球的颜色
            GetComponent<Renderer>().material.SetColor("_Color", Color.white * (m_groundContactCount * 0.25f));
        }

        /// <summary>
        /// 以固定的时间步长调整步速
        /// </summary>
        private void FixedUpdate()
        {
            UpdateState();
            AdjustVelocity();

            // 判断之前是否按下过跳跃键
            if (m_desiredJump)
            {
                m_desiredJump = false;
                Jump();
            }

            m_rigidbody.velocity = m_velocity;

            ClearState();
        }

        /// <summary>
        /// 更新小球状态
        /// </summary>
        private void UpdateState()
        {
            m_velocity = m_rigidbody.velocity;
            if (OnGround)
            {
                m_jumpPhase = 0;
                if (m_groundContactCount > 1)
                {
                    m_contactNormal.Normalize();
                }
            }
            else
            {
                // 空中跳跃时，小球直线上升
                m_contactNormal = Vector3.up;
            }
        }

        /// <summary>
        /// 重置小球的状态
        /// </summary>
        private void ClearState()
        {
            m_groundContactCount = 0;
            m_contactNormal = Vector3.zero;
        }

        /// <summary>
        /// 在PhysX检测到新的碰撞后被调用
        /// </summary>
        private void OnCollisionEnter(Collision collision)
        {
            EvaluateCollision(collision);
        }

        /// <summary>
        /// 只要碰撞仍然存在，就可以在每个物理步骤中调用该方法
        /// </summary>
        private void OnCollisionStay(Collision collision)
        {
            EvaluateCollision(collision);
        }

        /// <summary>
        /// 小球跳跃
        /// </summary>
        private void Jump()
        {
            // 检查小球是否在地面上或是否尚未达到允许的最大空中跳跃次数
            if (OnGround || m_jumpPhase < m_maxAirJumps)
            {
                m_jumpPhase += 1;
                float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * m_jumpHeight);
                float alignedSpeed = Vector3.Dot(m_velocity, m_contactNormal);
                // 限制小球的跳跃速度
                if (alignedSpeed > 0f)
                {
                    jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
                }
                m_velocity += m_contactNormal * jumpSpeed;
            }
        }

        /// <summary>
        /// 评估小球与其他物体发生的碰撞
        /// </summary>
        /// <param name="collision"></param>
        private void EvaluateCollision(Collision collision)
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                Vector3 normal = collision.GetContact(i).normal;
                if (normal.y > m_minGroundDotProduct)
                {
                    m_groundContactCount += 1;
                    m_contactNormal += normal;
                }
            }
        }

        /// <summary>
        /// 将所需的速度与地面对齐
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        private Vector3 ProjectOnContactPlane(Vector3 vector)
        {
            return vector - m_contactNormal * Vector3.Dot(vector, m_contactNormal);
        }

        /// <summary>
        /// 根据接触平面上的投影来调整小球的速度
        /// </summary>
        private void AdjustVelocity()
        {
            Vector3 xAxis = ProjectOnContactPlane(Vector3.right).normalized;
            Vector3 zAxis = ProjectOnContactPlane(Vector3.forward).normalized;

            float currentX = Vector3.Dot(m_velocity, xAxis);
            float currentZ = Vector3.Dot(m_velocity, zAxis);

            float acceleration = OnGround ? m_maxAcceleration : m_maxAirAcceleration;
            // 当FixedUpdate被调用时，Time.deltaTime等于Time.fixedDeltaTime
            float maxSpeedChange = acceleration * Time.deltaTime;

            // 根据当前值和目标值以及最大允许的速度变化变换速度
            float newX = Mathf.MoveTowards(m_velocity.x, m_desiredVelocity.x, maxSpeedChange);
            float newZ = Mathf.MoveTowards(m_velocity.z, m_desiredVelocity.z, maxSpeedChange);

            m_velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
        }

        #endregion

        #region 属性

        /// <summary>
        /// 小球是否与地面接触
        /// </summary>
        private bool OnGround => m_groundContactCount > 0;

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
        /// 预期速度
        /// </summary>
        private Vector3 m_desiredVelocity;

        /// <summary>
        /// 最大加速度
        /// </summary>
        [SerializeField, Range(0f, 100f)]
        private float m_maxAcceleration = 10f;

        /// <summary>
        /// 空中最大加速度
        /// </summary>
        [SerializeField, Range(0f, 100f)]
        private float m_maxAirAcceleration = 1f;

        /// <summary>
        /// 刚体组件
        /// </summary>
        private Rigidbody m_rigidbody;

        /// <summary>
        /// 是否执行跳跃
        /// </summary>
        private bool m_desiredJump;

        /// <summary>
        /// 跳跃高度
        /// </summary>
        [SerializeField, Range(0f, 10f)]
        private float m_jumpHeight = 2f;

        /// <summary>
        /// 空中允许的最大跳跃次数
        /// </summary>
        [SerializeField, Range(0, 5)]
        private int m_maxAirJumps = 0;

        /// <summary>
        /// 当前处于第几段跳跃
        /// </summary>
        private int m_jumpPhase;

        /// <summary>
        /// 斜坡被判定为地面的最大角度
        /// </summary>
        [SerializeField, Range(0f, 90f)]
        private float m_maxGroundAngle = 25f;

        /// <summary>
        /// 与斜坡表面法线点乘的最小结果
        /// </summary>
        private float m_minGroundDotProduct;

        /// <summary>
        /// 小球与斜坡接触点的表面法线
        /// </summary>
        private Vector3 m_contactNormal;

        /// <summary>
        /// 小球与地面的接触点数
        /// </summary>
        private int m_groundContactCount;

        #endregion
    }
}
#endif