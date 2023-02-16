#define ReactiveEnvironment
using UnityEngine;

#if ReactiveEnvironment
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
            m_rigidbody.useGravity = false;
            m_meshRenderer = GetComponent<MeshRenderer>();
            OnValidate();
        }

        /// <summary>
        /// 在播放模式下通过检查器更改角度时，更新相应字段值
        /// </summary>
        private void OnValidate()
        {
            m_minGroundDotProduct = Mathf.Cos(m_maxGroundAngle * Mathf.Deg2Rad);
            m_minStairsDotProduct = Mathf.Cos(m_maxStairsAngle * Mathf.Deg2Rad);
            m_minClimbDotProduct = Mathf.Cos(m_maxClimbAngle * Mathf.Deg2Rad);
        }

        /// <summary>
        /// 检测输入并更新小球位置
        /// </summary>
        private void Update()
        {
            m_playerInput.x = Input.GetAxis("Horizontal");
            m_playerInput.y = Input.GetAxis("Vertical");
            m_playerInput.z = Swimming ? Input.GetAxis("UpDown") : 0f;
            // 约束输入向量的大小永远小于等于1
            m_playerInput = Vector3.ClampMagnitude(m_playerInput, 1f);
            if (m_playerInputSpace)
            {
                m_rightAxis = ProjectDirectionOnPlane(m_playerInputSpace.right, m_upAxis);
                m_forwardAxis = ProjectDirectionOnPlane(m_playerInputSpace.forward, m_upAxis);
            }
            else
            {
                m_rightAxis = ProjectDirectionOnPlane(Vector3.right, m_upAxis);
                m_forwardAxis = ProjectDirectionOnPlane(Vector3.forward, m_upAxis);
            }

            if (Swimming)
            {
                m_desiresClimbing = false;
            }
            else
            {
                m_desiredJump |= Input.GetButtonDown("Jump");
                m_desiresClimbing = Input.GetButton("Climb");
            }

            // 区分攀爬/游泳状态和其他状态小球的颜色
            m_meshRenderer.material = Climbing ? m_climbingMaterial : Swimming ? m_swimmingMaterial : m_normalMaterial;
        }

        /// <summary>
        /// 以固定的时间步长调整步速
        /// </summary>
        private void FixedUpdate()
        {
            Vector3 gravity = CustomGravity.GetGravity(m_rigidbody.position, out m_upAxis);

            UpdateState();
            // 如果小球在水中，则给小球应用阻力
            if (InWater)
            {
                m_velocity *= 1f - m_waterDrag * m_submergence * Time.deltaTime;
            }

            AdjustVelocity();

            // 判断之前是否按下过跳跃键
            if (m_desiredJump)
            {
                m_desiredJump = false;
                Jump(gravity);
            }

            // 为小球攀爬施加抓地力
            if (Climbing)
            {
                m_velocity -= m_contactNormal * (m_maxClimbAcceleration * 0.9f * Time.deltaTime);
            }
            // 如果小球在水中，则为小球施加浮力
            else if (InWater)
            {
                m_velocity += gravity * ((1f - m_buoyancy * m_submergence) * Time.deltaTime);
            }
            else if (OnGround && m_velocity.sqrMagnitude < 0.01f)
            {
                m_velocity += m_contactNormal * (Vector3.Dot(gravity, m_contactNormal) * Time.deltaTime);
            }
            // 球体在到达墙顶后仍想继续攀爬，则同时施加抓地力和重力
            else if (m_desiresClimbing && OnGround)
            {
                m_velocity += (gravity - m_contactNormal * (m_maxClimbAcceleration * 0.9f)) * Time.deltaTime;
            }
            // 只有在非攀爬状态下，才应用重力
            else
            {
                m_velocity += gravity * Time.deltaTime;
            }

            m_rigidbody.velocity = m_velocity;

            ClearState();
        }

        /// <summary>
        /// 更新小球状态
        /// </summary>
        private void UpdateState()
        {
            m_stepsSinceLastGrounded += 1;
            m_stepsSinceLastJump += 1;
            m_velocity = m_rigidbody.velocity;
            if (CheckClimbing() || CheckSwimming() || OnGround || SnapToGround() || CheckSteepContacts())
            {
                m_stepsSinceLastGrounded = 0;
                if (m_stepsSinceLastJump > 1)
                {
                    m_jumpPhase = 0;
                }
                if (m_groundContactCount > 1)
                {
                    m_contactNormal.Normalize();
                }
            }
            else
            {
                // 空中跳跃时，小球直线上升
                m_contactNormal = m_upAxis;
            }

            if (m_connectedBody)
            {
                if (m_connectedBody.isKinematic || m_connectedBody.mass >= m_rigidbody.mass)
                {
                    UpdateConnectionState();
                }
            }
        }

        /// <summary>
        /// 更新连接状态
        /// </summary>
        private void UpdateConnectionState()
        {
            // 当前和先前的连接体相同时，计算这一帧的连接速度
            if (m_connectedBody == m_previousConnectedBody)
            {
                Vector3 connectionMovement = m_connectedBody.transform.TransformPoint(m_connectionLocalPosition)
                                             - m_connectionWorldPosition;
                m_connectionVelocity = connectionMovement / Time.deltaTime;
            }
            // 使用小球的位置计算连接点的世界坐标和局部坐标
            m_connectionWorldPosition = m_rigidbody.position;
            m_connectionLocalPosition = m_connectedBody.transform.InverseTransformPoint(m_connectionWorldPosition);
        }

        /// <summary>
        /// 重置小球的状态
        /// </summary>
        private void ClearState()
        {
            m_groundContactCount = m_steepContactCount = m_climbContactCount = 0;
            m_contactNormal = m_steepNormal = m_climbNormal = Vector3.zero;
            m_connectionVelocity = Vector3.zero;
            m_previousConnectedBody = m_connectedBody;
            m_connectedBody = null;
            m_submergence = 0;
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
        /// 检测小球与水体的接触
        /// </summary>
        /// <param name="other"></param>
        private void OnTriggerEnter(Collider other)
        {
            if ((m_waterMask & (1 << other.gameObject.layer)) != 0)
            {
                EvaluateSubmergence(other);
            }
        }

        /// <summary>
        /// 检测小球与水体的接触
        /// </summary>
        /// <param name="other"></param>
        private void OnTriggerStay(Collider other)
        {
            if ((m_waterMask & (1 << other.gameObject.layer)) != 0)
            {
                EvaluateSubmergence(other);
            }
        }

        private void EvaluateSubmergence(Collider collider)
        {
            // 计算小球浸入水中的深度
            if (Physics.Raycast(m_rigidbody.position + m_upAxis * m_submergenceOffset, -m_upAxis, out RaycastHit hit,
                    m_submergenceRange + 1f, m_waterMask, QueryTriggerInteraction.Collide))
            {
                m_submergence = 1f - hit.distance / m_submergenceRange;
            }
            // 如果检测失败，说明小球已经完全浸入水中
            else
            {
                m_submergence = 1f;
            }
            // 获取流动的水体的刚体，使小球跟随水体流动
            if (Swimming)
            {
                m_connectedBody = collider.attachedRigidbody;
            }
        }

        /// <summary>
        /// 小球跳跃
        /// </summary>
        private void Jump(Vector3 gravity)
        {
            Vector3 jumpDirection;
            if (OnGround)
                jumpDirection = m_contactNormal;
            // 蹬墙跳
            else if (OnSteep)
            {
                jumpDirection = m_steepNormal;
                m_jumpPhase = 0;
            }
            else if (m_maxAirJumps > 0 && m_jumpPhase <= m_maxAirJumps)
            {
                // 防止小球在未跳跃时沿表面掉下来时可以多跳一次
                if (m_jumpPhase == 0)
                {
                    m_jumpPhase = 1;
                }
                jumpDirection = m_contactNormal;
            }
            else
                return;
            m_stepsSinceLastJump = 0;
            m_jumpPhase += 1;
            float jumpSpeed = Mathf.Sqrt(2f * gravity.magnitude * m_jumpHeight);
            // 根据小球在水中的浸入程度调整跳跃速度
            if (InWater)
            {
                jumpSpeed *= Mathf.Max(0f, 1f - m_submergence / m_swimThreshold);
            }
            // 修正蹬墙跳跃时的朝向
            jumpDirection = (jumpDirection + m_upAxis).normalized;
            float alignedSpeed = Vector3.Dot(m_velocity, jumpDirection);
            // 限制小球的跳跃速度
            if (alignedSpeed > 0f)
            {
                jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
            }
            m_velocity += jumpDirection * jumpSpeed;
        }

        /// <summary>
        /// 评估小球与其他物体发生的碰撞
        /// </summary>
        /// <param name="collision"></param>
        private void EvaluateCollision(Collision collision)
        {
            if (Swimming)
                return;
            int layer = collision.gameObject.layer;
            float minDot = GetMinDot(layer);
            for (int i = 0; i < collision.contactCount; i++)
            {
                Vector3 normal = collision.GetContact(i).normal;
                float upDot = Vector3.Dot(m_upAxis, normal);
                // 小球在地面上
                if (upDot > minDot)
                {
                    m_groundContactCount += 1;
                    m_contactNormal += normal;
                    m_connectedBody = collision.rigidbody;
                }
                else
                {
                    // 小球在峭壁上
                    if (upDot > -0.01f)
                    {
                        m_steepContactCount += 1;
                        m_steepNormal += normal;
                        // 仅在没有地面接触的情况下才分配斜坡刚体
                        if (m_groundContactCount == 0)
                        {
                            m_connectedBody = collision.rigidbody;
                        }
                    }
                    // 小球在攀爬面上
                    if (m_desiresClimbing && upDot >= m_minClimbDotProduct && (m_climbMask & (1 << layer)) != 0)
                    {
                        m_climbContactCount += 1;
                        m_climbNormal += normal;
                        m_lastClimbNormal = normal;
                        m_connectedBody = collision.rigidbody;
                    }
                }
            }
        }

        /// <summary>
        /// 尝试将小球与峭壁的接触转换为虚拟地面
        /// </summary>
        /// <returns>转换是否成功</returns>
        private bool CheckSteepContacts()
        {
            if (m_steepContactCount > 1)
            {
                m_steepNormal.Normalize();
                float upDot = Vector3.Dot(m_upAxis, m_steepNormal);
                if (upDot >= m_minGroundDotProduct)
                {
                    m_groundContactCount = 1;
                    m_contactNormal = m_steepNormal;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 检测攀爬状态
        /// </summary>
        /// <returns></returns>
        private bool CheckClimbing()
        {
            if (Climbing)
            {
                // 如果小球处于裂缝中，则使用最后的攀爬法线而不是合计值
                if (m_climbContactCount > 1)
                {
                    m_climbNormal.Normalize();
                    float upDot = Vector3.Dot(m_upAxis, m_climbNormal);
                    if (upDot >= m_minGroundDotProduct)
                    {
                        m_climbNormal = m_lastClimbNormal;
                    }
                }
                m_groundContactCount = 1;
                m_contactNormal = m_climbNormal;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 检测游泳状态
        /// </summary>
        /// <returns></returns>
        private bool CheckSwimming()
        {
            if (Swimming)
            {
                m_groundContactCount = 0;
                m_contactNormal = m_upAxis;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 根据重力轴调整小球的控制方向
        /// </summary>
        /// <param name="direction">小球的控制轴向</param>
        /// <param name="normal">小球接触面的法线</param>
        /// <returns></returns>
        private Vector3 ProjectDirectionOnPlane(Vector3 direction, Vector3 normal)
        {
            return (direction - normal * Vector3.Dot(direction, normal)).normalized;
        }

        /// <summary>
        /// 根据接触平面上的投影来调整小球的速度
        /// </summary>
        private void AdjustVelocity()
        {
            float acceleration, speed;
            Vector3 xAxis, zAxis;
            // 当小球处于攀爬状态时，改变输入轴向
            if (Climbing)
            {
                acceleration = m_maxClimbAcceleration;
                speed = m_maxClimbSpeed;
                xAxis = Vector3.Cross(m_contactNormal, m_upAxis);
                zAxis = m_upAxis;
            }
            else if (InWater)
            {
                float swimFactor = Mathf.Min(1f, m_submergence / m_swimThreshold);
                acceleration = Mathf.LerpUnclamped(OnGround ? m_maxAcceleration : m_maxAirAcceleration, m_maxSwimAcceleration, swimFactor);
                speed = Mathf.LerpUnclamped(m_maxSpeed, m_maxSwimSpeed, swimFactor);
                xAxis = m_rightAxis;
                zAxis = m_forwardAxis;
            }
            else
            {
                acceleration = OnGround ? m_maxAcceleration : m_maxAirAcceleration;
                speed = OnGround && m_desiresClimbing ? m_maxClimbSpeed : m_maxSpeed;
                xAxis = m_rightAxis;
                zAxis = m_forwardAxis;
            }

            xAxis = ProjectDirectionOnPlane(xAxis, m_contactNormal);
            zAxis = ProjectDirectionOnPlane(zAxis, m_contactNormal);

            // 计算小球与其他物体接触时的相对速度
            Vector3 relativeVelocity = m_velocity - m_connectionVelocity;
            float currentX = Vector3.Dot(relativeVelocity, xAxis);
            float currentZ = Vector3.Dot(relativeVelocity, zAxis);

            // 当FixedUpdate被调用时，Time.deltaTime等于Time.fixedDeltaTime
            float maxSpeedChange = acceleration * Time.deltaTime;

            // 根据当前值和目标值以及最大允许的速度变化变换速度
            float newX = Mathf.MoveTowards(currentX, m_playerInput.x * speed, maxSpeedChange);
            float newZ = Mathf.MoveTowards(currentZ, m_playerInput.y * speed, maxSpeedChange);

            m_velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);

            if (Swimming)
            {
                float currentY = Vector3.Dot(relativeVelocity, m_upAxis);
                float newY = Mathf.MoveTowards(currentY, m_playerInput.z * speed, maxSpeedChange);
                m_velocity += m_upAxis * (newY - currentY);
            }
        }

        /// <summary>
        /// 使小球始终贴着地面
        /// </summary>
        /// <returns></returns>
        private bool SnapToGround()
        {
            // 由于碰撞数据的延迟，我们仍然认为启动跳跃后的步骤已接地。因此，如果我们在跳转后走了两个或更少的步骤，就必须中止
            if (m_stepsSinceLastGrounded > 1 || m_stepsSinceLastJump <= 2)
                return false;
            float speed = m_velocity.magnitude;
            if (speed > m_maxSnapSpeed)
            {
                return false;
            }
            if (!Physics.Raycast(m_rigidbody.position, -m_upAxis, out RaycastHit hit, m_probeDistance, m_probeMask, QueryTriggerInteraction.Ignore))
                return false;
            // 根据新的向上轴检查小球是否在地面
            float upDot = Vector3.Dot(m_upAxis, hit.normal);
            if (upDot < GetMinDot(hit.collider.gameObject.layer))
                return false;
            // 如果此时还没有中止，那么我们只是失去了与地面的接触，但仍然在地面上
            m_groundContactCount = 1;
            m_contactNormal = hit.normal;
            // 调整速度，使其和地面对齐
            float dot = Vector3.Dot(m_velocity, hit.normal);
            if (dot > 0f)
            {
                m_velocity = (m_velocity - hit.normal * dot).normalized * speed;
            }

            m_connectedBody = hit.rigidbody;
            return true;
        }

        /// <summary>
        /// 防止在加速区域时小球紧贴地面
        /// </summary>
        public void PreventSnapToGround()
        {
            m_stepsSinceLastJump = -1;
        }

        /// <summary>
        /// 计算给定图层的起跳的点乘最小值
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        private float GetMinDot(int layer)
        {
            return (m_stairsMask & (1 << layer)) == 0 ? m_minGroundDotProduct : m_minStairsDotProduct;
        }

        #endregion

        #region 属性

        /// <summary>
        /// 小球是否与地面接触
        /// </summary>
        private bool OnGround => m_groundContactCount > 0;

        /// <summary>
        /// 小球是否与峭壁表面接触
        /// </summary>
        private bool OnSteep => m_steepContactCount > 0;

        /// <summary>
        /// 小球是否与攀爬面接触
        /// </summary>
        private bool Climbing => m_climbContactCount > 0 && m_stepsSinceLastJump > 2;

        /// <summary>
        /// 小球是否在水中
        /// </summary>
        private bool InWater => m_submergence > 0f;

        /// <summary>
        /// 小球是否达到了游泳阈值
        /// </summary>
        private bool Swimming => m_submergence >= m_swimThreshold;

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 最大速度
        /// </summary>
        [SerializeField, Range(0f, 100f)]
        private float m_maxSpeed = 10f;

        /// <summary>
        /// 最大攀爬速度
        /// </summary>
        [SerializeField, Range(0f, 100f)]
        private float m_maxClimbSpeed = 2f;

        /// <summary>
        /// 最大游泳速度
        /// </summary>
        [SerializeField, Range(0f, 100f)]
        private float m_maxSwimSpeed = 5f;

        /// <summary>
        /// 玩家输入
        /// </summary>
        private Vector3 m_playerInput;

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
        /// 空中最大加速度
        /// </summary>
        [SerializeField, Range(0f, 100f)]
        private float m_maxAirAcceleration = 1f;

        /// <summary>
        /// 最大攀爬加速度
        /// </summary>
        [SerializeField, Range(0f, 100f)]
        private float m_maxClimbAcceleration = 20f;

        /// <summary>
        /// 最大游泳加速度
        /// </summary>
        [SerializeField, Range(0f, 100f)]
        private float m_maxSwimAcceleration = 5f;

        /// <summary>
        /// 刚体组件
        /// </summary>
        private Rigidbody m_rigidbody;

        /// <summary>
        /// 与小球连接的实体的刚体组件
        /// </summary>
        private Rigidbody m_connectedBody;

        /// <summary>
        /// 在上一个物理步长中与小球连接的实体的刚体组件
        /// </summary>
        private Rigidbody m_previousConnectedBody;

        /// <summary>
        /// 连接实体的速度
        /// </summary>
        private Vector3 m_connectionVelocity;

        /// <summary>
        /// 连接物体的世界坐标
        /// </summary>
        private Vector3 m_connectionWorldPosition;

        /// <summary>
        /// 连接物体的局部空间中的连接位置
        /// </summary>
        private Vector3 m_connectionLocalPosition;

        /// <summary>
        /// 是否执行跳跃
        /// </summary>
        private bool m_desiredJump;

        /// <summary>
        /// 是否执行攀爬
        /// </summary>
        private bool m_desiresClimbing;

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
        /// 小球能上的最大的楼梯角度
        /// </summary>
        [SerializeField, Range(0f, 90f)]
        private float m_maxStairsAngle = 50f;

        /// <summary>
        /// 最大攀爬角度
        /// </summary>
        [SerializeField, Range(90, 180)]
        private float m_maxClimbAngle = 140f;

        /// <summary>
        /// 与斜坡表面法线点乘的最小结果
        /// </summary>
        private float m_minGroundDotProduct;

        /// <summary>
        /// 与楼梯表面（近似成了斜面）法线点乘的最小结果
        /// </summary>
        private float m_minStairsDotProduct;

        /// <summary>
        /// 与攀爬表面法线点乘的最小结果
        /// </summary>
        private float m_minClimbDotProduct;

        /// <summary>
        /// 小球与斜坡接触点的表面法线
        /// </summary>
        private Vector3 m_contactNormal;

        /// <summary>
        /// 小球与峭壁接触点的表面法线
        /// </summary>
        private Vector3 m_steepNormal;

        /// <summary>
        /// 小球与攀爬表面接触点的表面法线
        /// </summary>
        private Vector3 m_climbNormal;

        /// <summary>
        /// 上一次攀爬的法线
        /// </summary>
        private Vector3 m_lastClimbNormal;

        /// <summary>
        /// 小球与地面的接触点数
        /// </summary>
        private int m_groundContactCount;

        /// <summary>
        /// 小球与峭壁的接触点数
        /// </summary>
        private int m_steepContactCount;

        /// <summary>
        /// 小球与攀爬面的接触点数
        /// </summary>
        private int m_climbContactCount;

        /// <summary>
        /// 追踪自从接地以来经历的物理步长
        /// </summary>
        private int m_stepsSinceLastGrounded;

        /// <summary>
        /// 追踪自从上次跳跃以来经历的物理步长
        /// </summary>
        private int m_stepsSinceLastJump;

        /// <summary>
        /// 最大的捕捉速度
        /// </summary>
        [SerializeField, Range(0f, 100f)]
        private float m_maxSnapSpeed = 100f;

        /// <summary>
        /// 射线的探测距离
        /// </summary>
        [SerializeField, Min(0f)]
        private float m_probeDistance = 1f;

        /// <summary>
        /// 探测掩码
        /// </summary>
        [SerializeField]
        private LayerMask m_probeMask = -1;

        /// <summary>
        /// 楼梯掩码
        /// </summary>
        [SerializeField]
        private LayerMask m_stairsMask = -1;

        /// <summary>
        /// 攀爬掩码
        /// </summary>
        [SerializeField]
        private LayerMask m_climbMask = -1;

        /// <summary>
        /// 水体掩码
        /// </summary>
        [SerializeField]
        private LayerMask m_waterMask = 0;

        /// <summary>
        /// 定义控制小球的输入空间
        /// </summary>
        [SerializeField]
        private Transform m_playerInputSpace = default;

        /// <summary>
        /// 小球的重力轴反方向
        /// </summary>
        [SerializeField]
        private Vector3 m_upAxis;

        /// <summary>
        /// 小球向右移动的轴向
        /// </summary>
        private Vector3 m_rightAxis;

        /// <summary>
        /// 小球向前移动的轴向
        /// </summary>
        private Vector3 m_forwardAxis;

        /// <summary>
        /// 普通材质
        /// </summary>
        [SerializeField]
        private Material m_normalMaterial = default;

        /// <summary>
        /// 攀爬材质
        /// </summary>
        [SerializeField]
        private Material m_climbingMaterial = default;

        /// <summary>
        /// 游泳材质
        /// </summary>
        [SerializeField]
        private Material m_swimmingMaterial = default;

        /// <summary>
        /// 小球的网格渲染组件
        /// </summary>
        private MeshRenderer m_meshRenderer;

        /// <summary>
        /// 小球浸入水中的程度，0表示没接触，1表示完全浸入
        /// </summary>
        private float m_submergence;

        /// <summary>
        /// 小球中心到顶部的偏移值（小球的半径）
        /// </summary>
        [SerializeField]
        private float m_submergenceOffset = 0.5f;

        /// <summary>
        /// 浸入范围
        /// </summary>
        [SerializeField]
        private float m_submergenceRange = 1f;

        /// <summary>
        /// 水的阻力
        /// </summary>
        [SerializeField, Range(0f, 10f)]
        private float m_waterDrag = 1f;

        /// <summary>
        /// 浮力
        /// </summary>
        [SerializeField, Min(0f)]
        private float m_buoyancy = 1f;

        /// <summary>
        /// 游泳阈值
        /// </summary>
        [SerializeField, Range(0.01f, 1f)]
        private float m_swimThreshold = 0.5f;

        #endregion
    }
}
#endif

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

#if Physics
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

#if SurfaceContact
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
            m_minStairsDotProduct = Mathf.Cos(m_maxStairsAngle * Mathf.Deg2Rad);
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

#if DEBUG
            // 根据小球与地面的接触点数改变小球的颜色
            //GetComponent<Renderer>().material.SetColor("_Color", OnGround ? Color.black : Color.white);
#endif
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
            m_stepsSinceLastGrounded += 1;
            m_stepsSinceLastJump += 1;
            m_velocity = m_rigidbody.velocity;
            if (OnGround || SnapToGround() || CheckSteepContacts())
            {
                m_stepsSinceLastGrounded = 0;
                if (m_stepsSinceLastJump > 1)
                {
                    m_jumpPhase = 0;
                }
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
            m_groundContactCount = m_steepContactCount = 0;
            m_contactNormal = m_steepNormal = Vector3.zero;
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
            Vector3 jumpDirection;
            if (OnGround)
                jumpDirection = m_contactNormal;
            // 蹬墙跳
            else if (OnSteep)
            {
                jumpDirection = m_steepNormal;
                m_jumpPhase = 0;
            }
            else if (m_maxAirJumps > 0 && m_jumpPhase <= m_maxAirJumps)
            {
                // 防止小球在未跳跃时沿表面掉下来时可以多跳一次
                if (m_jumpPhase == 0)
                {
                    m_jumpPhase = 1;
                }
                jumpDirection = m_contactNormal;
            }
            else
                return;
            m_stepsSinceLastJump = 0;
            m_jumpPhase += 1;
            float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * m_jumpHeight);
            // 修正蹬墙跳跃时的朝向
            jumpDirection = (jumpDirection + Vector3.up).normalized;
            float alignedSpeed = Vector3.Dot(m_velocity, jumpDirection);
            // 限制小球的跳跃速度
            if (alignedSpeed > 0f)
            {
                jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
            }
            m_velocity += jumpDirection * jumpSpeed;
        }

        /// <summary>
        /// 评估小球与其他物体发生的碰撞
        /// </summary>
        /// <param name="collision"></param>
        private void EvaluateCollision(Collision collision)
        {
            float minDot = GetMinDot(collision.gameObject.layer);
            for (int i = 0; i < collision.contactCount; i++)
            {
                Vector3 normal = collision.GetContact(i).normal;
                if (normal.y > minDot)
                {
                    m_groundContactCount += 1;
                    m_contactNormal += normal;
                }
                else if (normal.y > -0.01f)
                {
                    m_steepContactCount += 1;
                    m_steepNormal += normal;
                }
            }
        }

        /// <summary>
        /// 尝试将小球与峭壁的接触转换为虚拟地面
        /// </summary>
        /// <returns>转换是否成功</returns>
        private bool CheckSteepContacts()
        {
            if (m_steepContactCount > 1)
            {
                m_steepNormal.Normalize();
                if (m_steepNormal.y >= m_minGroundDotProduct)
                {
                    m_groundContactCount = 1;
                    m_contactNormal = m_steepNormal;
                    return true;
                }
            }
            return false;
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

        /// <summary>
        /// 使小球始终贴着地面
        /// </summary>
        /// <returns></returns>
        private bool SnapToGround()
        {
            // 由于碰撞数据的延迟，我们仍然认为启动跳跃后的步骤已接地。因此，如果我们在跳转后走了两个或更少的步骤，就必须中止。
            if (m_stepsSinceLastGrounded > 1 || m_stepsSinceLastJump <= 2)
                return false;
            float speed = m_velocity.magnitude;
            if (speed > m_maxSnapSpeed)
            {
                return false;
            }
            if (!Physics.Raycast(m_rigidbody.position, Vector3.down, out RaycastHit hit, m_probeDistance, m_probeMask))
                return false;
            if (hit.normal.y < GetMinDot(hit.collider.gameObject.layer))
                return false;
            // 如果此时还没有中止，那么我们只是失去了与地面的接触，但仍然在地面上
            m_groundContactCount = 1;
            m_contactNormal = hit.normal;
            // 调整速度，使其和地面对齐
            float dot = Vector3.Dot(m_velocity, hit.normal);
            if (dot > 0f)
            {
                m_velocity = (m_velocity - hit.normal * dot).normalized * speed;
            }
            return true;
        }

        /// <summary>
        /// 返回给定图层的适当最小值
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        private float GetMinDot(int layer)
        {
            return (m_stairsMask & (1 << layer)) == 0 ? m_minGroundDotProduct : m_minStairsDotProduct;
        }

#endregion

#region 属性

        /// <summary>
        /// 小球是否与地面接触
        /// </summary>
        private bool OnGround => m_groundContactCount > 0;

        /// <summary>
        /// 小球是否与峭壁表面接触
        /// </summary>
        private bool OnSteep => m_steepContactCount > 0;

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
        /// 小球能上的最大的楼梯角度
        /// </summary>
        [SerializeField, Range(0f, 90f)]
        private float m_maxStairsAngle = 50f;

        /// <summary>
        /// 与斜坡表面法线点乘的最小结果
        /// </summary>
        private float m_minGroundDotProduct;

        /// <summary>
        /// 与楼梯表面（近似成了斜面）法线点乘的最小结果
        /// </summary>
        private float m_minStairsDotProduct;

        /// <summary>
        /// 小球与斜坡接触点的表面法线
        /// </summary>
        private Vector3 m_contactNormal;

        /// <summary>
        /// 小球与峭壁接触点的表面法线
        /// </summary>
        private Vector3 m_steepNormal;

        /// <summary>
        /// 小球与地面的接触点数
        /// </summary>
        private int m_groundContactCount;

        /// <summary>
        /// 小球与峭壁的接触点数
        /// </summary>
        private int m_steepContactCount;

        /// <summary>
        /// 追踪自从接地以来经历的物理步长
        /// </summary>
        private int m_stepsSinceLastGrounded;

        /// <summary>
        /// 追踪自从上次跳跃以来经历的物理步长
        /// </summary>
        private int m_stepsSinceLastJump;

        /// <summary>
        /// 最大的捕捉速度
        /// </summary>
        [SerializeField, Range(0f, 100f)]
        private float m_maxSnapSpeed = 100f;

        /// <summary>
        /// 射线的探测距离
        /// </summary>
        [SerializeField, Min(0f)]
        private float m_probeDistance = 1f;

        /// <summary>
        /// 探测掩码
        /// </summary>
        [SerializeField]
        private LayerMask m_probeMask = -1;

        /// <summary>
        /// 楼梯掩码
        /// </summary>
        [SerializeField]
        private LayerMask m_stairsMask = -1;

#endregion
    }
}
#endif

#if OrbitCamera
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
            m_minStairsDotProduct = Mathf.Cos(m_maxStairsAngle * Mathf.Deg2Rad);
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
            if (m_playerInputSpace)
            {
                Vector3 forward = m_playerInputSpace.forward;
                forward.y = 0f;
                forward.Normalize();
                Vector3 right = m_playerInputSpace.right;
                right.y = 0f;
                right.Normalize();
                m_desiredVelocity = (forward * playerInput.y + right * playerInput.x) * m_maxSpeed;
            }
            else
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

            ClearState();
        }

        /// <summary>
        /// 更新小球状态
        /// </summary>
        private void UpdateState()
        {
            m_stepsSinceLastGrounded += 1;
            m_stepsSinceLastJump += 1;
            m_velocity = m_rigidbody.velocity;
            if (OnGround || SnapToGround() || CheckSteepContacts())
            {
                m_stepsSinceLastGrounded = 0;
                if (m_stepsSinceLastJump > 1)
                {
                    m_jumpPhase = 0;
                }
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
            m_groundContactCount = m_steepContactCount = 0;
            m_contactNormal = m_steepNormal = Vector3.zero;
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
            Vector3 jumpDirection;
            if (OnGround)
                jumpDirection = m_contactNormal;
            // 蹬墙跳
            else if (OnSteep)
            {
                jumpDirection = m_steepNormal;
                m_jumpPhase = 0;
            }
            else if (m_maxAirJumps > 0 && m_jumpPhase <= m_maxAirJumps)
            {
                // 防止小球在未跳跃时沿表面掉下来时可以多跳一次
                if (m_jumpPhase == 0)
                {
                    m_jumpPhase = 1;
                }
                jumpDirection = m_contactNormal;
            }
            else
                return;
            m_stepsSinceLastJump = 0;
            m_jumpPhase += 1;
            float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * m_jumpHeight);
            // 修正蹬墙跳跃时的朝向
            jumpDirection = (jumpDirection + Vector3.up).normalized;
            float alignedSpeed = Vector3.Dot(m_velocity, jumpDirection);
            // 限制小球的跳跃速度
            if (alignedSpeed > 0f)
            {
                jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
            }
            m_velocity += jumpDirection * jumpSpeed;
        }

        /// <summary>
        /// 评估小球与其他物体发生的碰撞
        /// </summary>
        /// <param name="collision"></param>
        private void EvaluateCollision(Collision collision)
        {
            float minDot = GetMinDot(collision.gameObject.layer);
            for (int i = 0; i < collision.contactCount; i++)
            {
                Vector3 normal = collision.GetContact(i).normal;
                if (normal.y > minDot)
                {
                    m_groundContactCount += 1;
                    m_contactNormal += normal;
                }
                else if (normal.y > -0.01f)
                {
                    m_steepContactCount += 1;
                    m_steepNormal += normal;
                }
            }
        }

        /// <summary>
        /// 尝试将小球与峭壁的接触转换为虚拟地面
        /// </summary>
        /// <returns>转换是否成功</returns>
        private bool CheckSteepContacts()
        {
            if (m_steepContactCount > 1)
            {
                m_steepNormal.Normalize();
                if (m_steepNormal.y >= m_minGroundDotProduct)
                {
                    m_groundContactCount = 1;
                    m_contactNormal = m_steepNormal;
                    return true;
                }
            }
            return false;
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

        /// <summary>
        /// 使小球始终贴着地面
        /// </summary>
        /// <returns></returns>
        private bool SnapToGround()
        {
            // 由于碰撞数据的延迟，我们仍然认为启动跳跃后的步骤已接地。因此，如果我们在跳转后走了两个或更少的步骤，就必须中止。
            if (m_stepsSinceLastGrounded > 1 || m_stepsSinceLastJump <= 2)
                return false;
            float speed = m_velocity.magnitude;
            if (speed > m_maxSnapSpeed)
            {
                return false;
            }
            if (!Physics.Raycast(m_rigidbody.position, Vector3.down, out RaycastHit hit, m_probeDistance, m_probeMask))
                return false;
            if (hit.normal.y < GetMinDot(hit.collider.gameObject.layer))
                return false;
            // 如果此时还没有中止，那么我们只是失去了与地面的接触，但仍然在地面上
            m_groundContactCount = 1;
            m_contactNormal = hit.normal;
            // 调整速度，使其和地面对齐
            float dot = Vector3.Dot(m_velocity, hit.normal);
            if (dot > 0f)
            {
                m_velocity = (m_velocity - hit.normal * dot).normalized * speed;
            }
            return true;
        }

        /// <summary>
        /// 返回给定图层的适当最小值
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        private float GetMinDot(int layer)
        {
            return (m_stairsMask & (1 << layer)) == 0 ? m_minGroundDotProduct : m_minStairsDotProduct;
        }

#endregion

#region 属性

        /// <summary>
        /// 小球是否与地面接触
        /// </summary>
        private bool OnGround => m_groundContactCount > 0;

        /// <summary>
        /// 小球是否与峭壁表面接触
        /// </summary>
        private bool OnSteep => m_steepContactCount > 0;

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
        /// 小球能上的最大的楼梯角度
        /// </summary>
        [SerializeField, Range(0f, 90f)]
        private float m_maxStairsAngle = 50f;

        /// <summary>
        /// 与斜坡表面法线点乘的最小结果
        /// </summary>
        private float m_minGroundDotProduct;

        /// <summary>
        /// 与楼梯表面（近似成了斜面）法线点乘的最小结果
        /// </summary>
        private float m_minStairsDotProduct;

        /// <summary>
        /// 小球与斜坡接触点的表面法线
        /// </summary>
        private Vector3 m_contactNormal;

        /// <summary>
        /// 小球与峭壁接触点的表面法线
        /// </summary>
        private Vector3 m_steepNormal;

        /// <summary>
        /// 小球与地面的接触点数
        /// </summary>
        private int m_groundContactCount;

        /// <summary>
        /// 小球与峭壁的接触点数
        /// </summary>
        private int m_steepContactCount;

        /// <summary>
        /// 追踪自从接地以来经历的物理步长
        /// </summary>
        private int m_stepsSinceLastGrounded;

        /// <summary>
        /// 追踪自从上次跳跃以来经历的物理步长
        /// </summary>
        private int m_stepsSinceLastJump;

        /// <summary>
        /// 最大的捕捉速度
        /// </summary>
        [SerializeField, Range(0f, 100f)]
        private float m_maxSnapSpeed = 100f;

        /// <summary>
        /// 射线的探测距离
        /// </summary>
        [SerializeField, Min(0f)]
        private float m_probeDistance = 1f;

        /// <summary>
        /// 探测掩码
        /// </summary>
        [SerializeField]
        private LayerMask m_probeMask = -1;

        /// <summary>
        /// 楼梯掩码
        /// </summary>
        [SerializeField]
        private LayerMask m_stairsMask = -1;

        /// <summary>
        /// 定义控制小球的输入空间
        /// </summary>
        [SerializeField]
        private Transform m_playerInputSpace = default;

#endregion
    }
}
#endif

#if CustomGravity
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
            m_rigidbody.useGravity = false;
            OnValidate();
        }

        /// <summary>
        /// 在播放模式下通过检查器更改角度时，更新相应字段值
        /// </summary>
        private void OnValidate()
        {
            m_minGroundDotProduct = Mathf.Cos(m_maxGroundAngle * Mathf.Deg2Rad);
            m_minStairsDotProduct = Mathf.Cos(m_maxStairsAngle * Mathf.Deg2Rad);
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
            if (m_playerInputSpace)
            {
                m_rightAxis = ProjectDirectionOnPlane(m_playerInputSpace.right, m_upAxis);
                m_forwardAxis = ProjectDirectionOnPlane(m_playerInputSpace.forward, m_upAxis);
            }
            else
            {
                m_rightAxis = ProjectDirectionOnPlane(Vector3.right, m_upAxis);
                m_forwardAxis = ProjectDirectionOnPlane(Vector3.forward, m_upAxis);
            }

            m_desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * m_maxSpeed;

            m_desiredJump |= Input.GetButtonDown("Jump");
        }

        /// <summary>
        /// 以固定的时间步长调整步速
        /// </summary>
        private void FixedUpdate()
        {
            Vector3 gravity = CustomGravity.GetGravity(m_rigidbody.position, out m_upAxis);

            UpdateState();
            AdjustVelocity();

            // 判断之前是否按下过跳跃键
            if (m_desiredJump)
            {
                m_desiredJump = false;
                Jump(gravity);
            }

            m_velocity += gravity * Time.deltaTime;

            m_rigidbody.velocity = m_velocity;

            ClearState();
        }

        /// <summary>
        /// 更新小球状态
        /// </summary>
        private void UpdateState()
        {
            m_stepsSinceLastGrounded += 1;
            m_stepsSinceLastJump += 1;
            m_velocity = m_rigidbody.velocity;
            if (OnGround || SnapToGround() || CheckSteepContacts())
            {
                m_stepsSinceLastGrounded = 0;
                if (m_stepsSinceLastJump > 1)
                {
                    m_jumpPhase = 0;
                }
                if (m_groundContactCount > 1)
                {
                    m_contactNormal.Normalize();
                }
            }
            else
            {
                // 空中跳跃时，小球直线上升
                m_contactNormal = m_upAxis;
            }
        }

        /// <summary>
        /// 重置小球的状态
        /// </summary>
        private void ClearState()
        {
            m_groundContactCount = m_steepContactCount = 0;
            m_contactNormal = m_steepNormal = Vector3.zero;
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
        private void Jump(Vector3 gravity)
        {
            Vector3 jumpDirection;
            if (OnGround)
                jumpDirection = m_contactNormal;
            // 蹬墙跳
            else if (OnSteep)
            {
                jumpDirection = m_steepNormal;
                m_jumpPhase = 0;
            }
            else if (m_maxAirJumps > 0 && m_jumpPhase <= m_maxAirJumps)
            {
                // 防止小球在未跳跃时沿表面掉下来时可以多跳一次
                if (m_jumpPhase == 0)
                {
                    m_jumpPhase = 1;
                }
                jumpDirection = m_contactNormal;
            }
            else
                return;
            m_stepsSinceLastJump = 0;
            m_jumpPhase += 1;
            float jumpSpeed = Mathf.Sqrt(2f * gravity.magnitude * m_jumpHeight);
            // 修正蹬墙跳跃时的朝向
            jumpDirection = (jumpDirection + m_upAxis).normalized;
            float alignedSpeed = Vector3.Dot(m_velocity, jumpDirection);
            // 限制小球的跳跃速度
            if (alignedSpeed > 0f)
            {
                jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
            }
            m_velocity += jumpDirection * jumpSpeed;
        }

        /// <summary>
        /// 评估小球与其他物体发生的碰撞
        /// </summary>
        /// <param name="collision"></param>
        private void EvaluateCollision(Collision collision)
        {
            float minDot = GetMinDot(collision.gameObject.layer);
            for (int i = 0; i < collision.contactCount; i++)
            {
                Vector3 normal = collision.GetContact(i).normal;
                float upDot = Vector3.Dot(m_upAxis, normal);
                if (upDot > minDot)
                {
                    m_groundContactCount += 1;
                    m_contactNormal += normal;
                }
                else if (upDot > -0.01f)
                {
                    m_steepContactCount += 1;
                    m_steepNormal += normal;
                }
            }
        }

        /// <summary>
        /// 尝试将小球与峭壁的接触转换为虚拟地面
        /// </summary>
        /// <returns>转换是否成功</returns>
        private bool CheckSteepContacts()
        {
            if (m_steepContactCount > 1)
            {
                m_steepNormal.Normalize();
                float upDot = Vector3.Dot(m_upAxis, m_steepNormal);
                if (upDot >= m_minGroundDotProduct)
                {
                    m_groundContactCount = 1;
                    m_contactNormal = m_steepNormal;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 根据重力轴调整小球的控制方向
        /// </summary>
        /// <param name="direction">小球的控制轴向</param>
        /// <param name="normal">小球接触面的法线</param>
        /// <returns></returns>
        private Vector3 ProjectDirectionOnPlane(Vector3 direction, Vector3 normal)
        {
            return (direction - normal * Vector3.Dot(direction, normal)).normalized;
        }

        /// <summary>
        /// 根据接触平面上的投影来调整小球的速度
        /// </summary>
        private void AdjustVelocity()
        {
            Vector3 xAxis = ProjectDirectionOnPlane(m_rightAxis, m_contactNormal);
            Vector3 zAxis = ProjectDirectionOnPlane(m_forwardAxis, m_contactNormal);

            float currentX = Vector3.Dot(m_velocity, xAxis);
            float currentZ = Vector3.Dot(m_velocity, zAxis);

            float acceleration = OnGround ? m_maxAcceleration : m_maxAirAcceleration;
            // 当FixedUpdate被调用时，Time.deltaTime等于Time.fixedDeltaTime
            float maxSpeedChange = acceleration * Time.deltaTime;

            // 根据当前值和目标值以及最大允许的速度变化变换速度
            float newX = Mathf.MoveTowards(currentX, m_desiredVelocity.x, maxSpeedChange);
            float newZ = Mathf.MoveTowards(currentZ, m_desiredVelocity.z, maxSpeedChange);

            m_velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
        }

        /// <summary>
        /// 使小球始终贴着地面
        /// </summary>
        /// <returns></returns>
        private bool SnapToGround()
        {
            // 由于碰撞数据的延迟，我们仍然认为启动跳跃后的步骤已接地。因此，如果我们在跳转后走了两个或更少的步骤，就必须中止。
            if (m_stepsSinceLastGrounded > 1 || m_stepsSinceLastJump <= 2)
                return false;
            float speed = m_velocity.magnitude;
            if (speed > m_maxSnapSpeed)
            {
                return false;
            }
            if (!Physics.Raycast(m_rigidbody.position, -m_upAxis, out RaycastHit hit, m_probeDistance, m_probeMask))
                return false;
            // 根据新的向上轴检查小球是否在地面
            float upDot = Vector3.Dot(m_upAxis, hit.normal);
            if (upDot < GetMinDot(hit.collider.gameObject.layer))
                return false;
            // 如果此时还没有中止，那么我们只是失去了与地面的接触，但仍然在地面上
            m_groundContactCount = 1;
            m_contactNormal = hit.normal;
            // 调整速度，使其和地面对齐
            float dot = Vector3.Dot(m_velocity, hit.normal);
            if (dot > 0f)
            {
                m_velocity = (m_velocity - hit.normal * dot).normalized * speed;
            }
            return true;
        }

        /// <summary>
        /// 计算给定图层的起跳的点乘最小值
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        private float GetMinDot(int layer)
        {
            return (m_stairsMask & (1 << layer)) == 0 ? m_minGroundDotProduct : m_minStairsDotProduct;
        }

#endregion

#region 属性

        /// <summary>
        /// 小球是否与地面接触
        /// </summary>
        private bool OnGround => m_groundContactCount > 0;

        /// <summary>
        /// 小球是否与峭壁表面接触
        /// </summary>
        private bool OnSteep => m_steepContactCount > 0;

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
        /// 小球能上的最大的楼梯角度
        /// </summary>
        [SerializeField, Range(0f, 90f)]
        private float m_maxStairsAngle = 50f;

        /// <summary>
        /// 与斜坡表面法线点乘的最小结果
        /// </summary>
        private float m_minGroundDotProduct;

        /// <summary>
        /// 与楼梯表面（近似成了斜面）法线点乘的最小结果
        /// </summary>
        private float m_minStairsDotProduct;

        /// <summary>
        /// 小球与斜坡接触点的表面法线
        /// </summary>
        private Vector3 m_contactNormal;

        /// <summary>
        /// 小球与峭壁接触点的表面法线
        /// </summary>
        private Vector3 m_steepNormal;

        /// <summary>
        /// 小球与地面的接触点数
        /// </summary>
        private int m_groundContactCount;

        /// <summary>
        /// 小球与峭壁的接触点数
        /// </summary>
        private int m_steepContactCount;

        /// <summary>
        /// 追踪自从接地以来经历的物理步长
        /// </summary>
        private int m_stepsSinceLastGrounded;

        /// <summary>
        /// 追踪自从上次跳跃以来经历的物理步长
        /// </summary>
        private int m_stepsSinceLastJump;

        /// <summary>
        /// 最大的捕捉速度
        /// </summary>
        [SerializeField, Range(0f, 100f)]
        private float m_maxSnapSpeed = 100f;

        /// <summary>
        /// 射线的探测距离
        /// </summary>
        [SerializeField, Min(0f)]
        private float m_probeDistance = 1f;

        /// <summary>
        /// 探测掩码
        /// </summary>
        [SerializeField]
        private LayerMask m_probeMask = -1;

        /// <summary>
        /// 楼梯掩码
        /// </summary>
        [SerializeField]
        private LayerMask m_stairsMask = -1;

        /// <summary>
        /// 定义控制小球的输入空间
        /// </summary>
        [SerializeField]
        private Transform m_playerInputSpace = default;

        /// <summary>
        /// 小球的重力轴反方向
        /// </summary>
        [SerializeField]
        private Vector3 m_upAxis;

        /// <summary>
        /// 小球向右移动的轴向
        /// </summary>
        private Vector3 m_rightAxis;

        /// <summary>
        /// 小球向前移动的轴向
        /// </summary>
        private Vector3 m_forwardAxis;

#endregion
    }
}
#endif

#if MovingtheGround
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
            m_rigidbody.useGravity = false;
            OnValidate();
        }

        /// <summary>
        /// 在播放模式下通过检查器更改角度时，更新相应字段值
        /// </summary>
        private void OnValidate()
        {
            m_minGroundDotProduct = Mathf.Cos(m_maxGroundAngle * Mathf.Deg2Rad);
            m_minStairsDotProduct = Mathf.Cos(m_maxStairsAngle * Mathf.Deg2Rad);
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
            if (m_playerInputSpace)
            {
                m_rightAxis = ProjectDirectionOnPlane(m_playerInputSpace.right, m_upAxis);
                m_forwardAxis = ProjectDirectionOnPlane(m_playerInputSpace.forward, m_upAxis);
            }
            else
            {
                m_rightAxis = ProjectDirectionOnPlane(Vector3.right, m_upAxis);
                m_forwardAxis = ProjectDirectionOnPlane(Vector3.forward, m_upAxis);
            }

            m_desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * m_maxSpeed;

            m_desiredJump |= Input.GetButtonDown("Jump");
        }

        /// <summary>
        /// 以固定的时间步长调整步速
        /// </summary>
        private void FixedUpdate()
        {
            Vector3 gravity = CustomGravity.GetGravity(m_rigidbody.position, out m_upAxis);

            UpdateState();
            AdjustVelocity();

            // 判断之前是否按下过跳跃键
            if (m_desiredJump)
            {
                m_desiredJump = false;
                Jump(gravity);
            }

            m_velocity += gravity * Time.deltaTime;

            m_rigidbody.velocity = m_velocity;

            ClearState();
        }

        /// <summary>
        /// 更新小球状态
        /// </summary>
        private void UpdateState()
        {
            m_stepsSinceLastGrounded += 1;
            m_stepsSinceLastJump += 1;
            m_velocity = m_rigidbody.velocity;
            if (OnGround || SnapToGround() || CheckSteepContacts())
            {
                m_stepsSinceLastGrounded = 0;
                if (m_stepsSinceLastJump > 1)
                {
                    m_jumpPhase = 0;
                }
                if (m_groundContactCount > 1)
                {
                    m_contactNormal.Normalize();
                }
            }
            else
            {
                // 空中跳跃时，小球直线上升
                m_contactNormal = m_upAxis;
            }

            if (m_connectedBody)
            {
                if (m_connectedBody.isKinematic || m_connectedBody.mass >= m_rigidbody.mass)
                {
                    UpdateConnectionState();
                }
            }
        }

        /// <summary>
        /// 更新连接状态
        /// </summary>
        private void UpdateConnectionState()
        {
            // 当前和先前的连接体相同时，计算这一帧的连接速度
            if (m_connectedBody == m_previousConnectedBody)
            {
                Vector3 connectionMovement = m_connectedBody.transform.TransformPoint(m_connectionLocalPosition) 
                                             - m_connectionWorldPosition;
                m_connectionVelocity = connectionMovement / Time.deltaTime;
            }
            // 使用小球的位置计算连接点的世界坐标和局部坐标
            m_connectionWorldPosition = m_rigidbody.position;
            m_connectionLocalPosition = m_connectedBody.transform.InverseTransformPoint(m_connectionWorldPosition);
        }

        /// <summary>
        /// 重置小球的状态
        /// </summary>
        private void ClearState()
        {
            m_groundContactCount = m_steepContactCount = 0;
            m_contactNormal = m_steepNormal = m_connectionVelocity = Vector3.zero;
            m_previousConnectedBody = m_connectedBody;
            m_connectedBody = null;
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
        private void Jump(Vector3 gravity)
        {
            Vector3 jumpDirection;
            if (OnGround)
                jumpDirection = m_contactNormal;
            // 蹬墙跳
            else if (OnSteep)
            {
                jumpDirection = m_steepNormal;
                m_jumpPhase = 0;
            }
            else if (m_maxAirJumps > 0 && m_jumpPhase <= m_maxAirJumps)
            {
                // 防止小球在未跳跃时沿表面掉下来时可以多跳一次
                if (m_jumpPhase == 0)
                {
                    m_jumpPhase = 1;
                }
                jumpDirection = m_contactNormal;
            }
            else
                return;
            m_stepsSinceLastJump = 0;
            m_jumpPhase += 1;
            float jumpSpeed = Mathf.Sqrt(2f * gravity.magnitude * m_jumpHeight);
            // 修正蹬墙跳跃时的朝向
            jumpDirection = (jumpDirection + m_upAxis).normalized;
            float alignedSpeed = Vector3.Dot(m_velocity, jumpDirection);
            // 限制小球的跳跃速度
            if (alignedSpeed > 0f)
            {
                jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
            }
            m_velocity += jumpDirection * jumpSpeed;
        }

        /// <summary>
        /// 评估小球与其他物体发生的碰撞
        /// </summary>
        /// <param name="collision"></param>
        private void EvaluateCollision(Collision collision)
        {
            float minDot = GetMinDot(collision.gameObject.layer);
            for (int i = 0; i < collision.contactCount; i++)
            {
                Vector3 normal = collision.GetContact(i).normal;
                float upDot = Vector3.Dot(m_upAxis, normal);
                // 小球在地面上
                if (upDot > minDot)
                {
                    m_groundContactCount += 1;
                    m_contactNormal += normal;
                    m_connectedBody = collision.rigidbody;
                }
                // 小球在峭壁上
                else if (upDot > -0.01f)
                {
                    m_steepContactCount += 1;
                    m_steepNormal += normal;
                    // 仅在没有地面接触的情况下才分配斜坡刚体
                    if (m_groundContactCount == 0)
                    {
                        m_connectedBody = collision.rigidbody;
                    }
                }
            }
        }

        /// <summary>
        /// 尝试将小球与峭壁的接触转换为虚拟地面
        /// </summary>
        /// <returns>转换是否成功</returns>
        private bool CheckSteepContacts()
        {
            if (m_steepContactCount > 1)
            {
                m_steepNormal.Normalize();
                float upDot = Vector3.Dot(m_upAxis, m_steepNormal);
                if (upDot >= m_minGroundDotProduct)
                {
                    m_groundContactCount = 1;
                    m_contactNormal = m_steepNormal;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 根据重力轴调整小球的控制方向
        /// </summary>
        /// <param name="direction">小球的控制轴向</param>
        /// <param name="normal">小球接触面的法线</param>
        /// <returns></returns>
        private Vector3 ProjectDirectionOnPlane(Vector3 direction, Vector3 normal)
        {
            return (direction - normal * Vector3.Dot(direction, normal)).normalized;
        }

        /// <summary>
        /// 根据接触平面上的投影来调整小球的速度
        /// </summary>
        private void AdjustVelocity()
        {
            Vector3 xAxis = ProjectDirectionOnPlane(m_rightAxis, m_contactNormal);
            Vector3 zAxis = ProjectDirectionOnPlane(m_forwardAxis, m_contactNormal);

            // 计算小球与其他物体接触时的相对速度
            Vector3 relativeVelocity = m_velocity - m_connectionVelocity;
            float currentX = Vector3.Dot(relativeVelocity, xAxis);
            float currentZ = Vector3.Dot(relativeVelocity, zAxis);

            float acceleration = OnGround ? m_maxAcceleration : m_maxAirAcceleration;
            // 当FixedUpdate被调用时，Time.deltaTime等于Time.fixedDeltaTime
            float maxSpeedChange = acceleration * Time.deltaTime;

            // 根据当前值和目标值以及最大允许的速度变化变换速度
            float newX = Mathf.MoveTowards(currentX, m_desiredVelocity.x, maxSpeedChange);
            float newZ = Mathf.MoveTowards(currentZ, m_desiredVelocity.z, maxSpeedChange);

            m_velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
        }

        /// <summary>
        /// 使小球始终贴着地面
        /// </summary>
        /// <returns></returns>
        private bool SnapToGround()
        {
            // 由于碰撞数据的延迟，我们仍然认为启动跳跃后的步骤已接地。因此，如果我们在跳转后走了两个或更少的步骤，就必须中止。
            if (m_stepsSinceLastGrounded > 1 || m_stepsSinceLastJump <= 2)
                return false;
            float speed = m_velocity.magnitude;
            if (speed > m_maxSnapSpeed)
            {
                return false;
            }
            if (!Physics.Raycast(m_rigidbody.position, -m_upAxis, out RaycastHit hit, m_probeDistance, m_probeMask))
                return false;
            // 根据新的向上轴检查小球是否在地面
            float upDot = Vector3.Dot(m_upAxis, hit.normal);
            if (upDot < GetMinDot(hit.collider.gameObject.layer))
                return false;
            // 如果此时还没有中止，那么我们只是失去了与地面的接触，但仍然在地面上
            m_groundContactCount = 1;
            m_contactNormal = hit.normal;
            // 调整速度，使其和地面对齐
            float dot = Vector3.Dot(m_velocity, hit.normal);
            if (dot > 0f)
            {
                m_velocity = (m_velocity - hit.normal * dot).normalized * speed;
            }

            m_connectedBody = hit.rigidbody;
            return true;
        }

        /// <summary>
        /// 计算给定图层的起跳的点乘最小值
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        private float GetMinDot(int layer)
        {
            return (m_stairsMask & (1 << layer)) == 0 ? m_minGroundDotProduct : m_minStairsDotProduct;
        }

#endregion

#region 属性

        /// <summary>
        /// 小球是否与地面接触
        /// </summary>
        private bool OnGround => m_groundContactCount > 0;

        /// <summary>
        /// 小球是否与峭壁表面接触
        /// </summary>
        private bool OnSteep => m_steepContactCount > 0;

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
        /// 与小球连接的实体的刚体组件
        /// </summary>
        private Rigidbody m_connectedBody;

        /// <summary>
        /// 在上一个物理步长中与小球连接的实体的刚体组件
        /// </summary>
        private Rigidbody m_previousConnectedBody;

        /// <summary>
        /// 连接实体的速度
        /// </summary>
        private Vector3 m_connectionVelocity;

        /// <summary>
        /// 连接物体的世界坐标
        /// </summary>
        private Vector3 m_connectionWorldPosition;

        /// <summary>
        /// 连接物体的局部空间中的连接位置
        /// </summary>
        private Vector3 m_connectionLocalPosition;

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
        /// 小球能上的最大的楼梯角度
        /// </summary>
        [SerializeField, Range(0f, 90f)]
        private float m_maxStairsAngle = 50f;

        /// <summary>
        /// 与斜坡表面法线点乘的最小结果
        /// </summary>
        private float m_minGroundDotProduct;

        /// <summary>
        /// 与楼梯表面（近似成了斜面）法线点乘的最小结果
        /// </summary>
        private float m_minStairsDotProduct;

        /// <summary>
        /// 小球与斜坡接触点的表面法线
        /// </summary>
        private Vector3 m_contactNormal;

        /// <summary>
        /// 小球与峭壁接触点的表面法线
        /// </summary>
        private Vector3 m_steepNormal;

        /// <summary>
        /// 小球与地面的接触点数
        /// </summary>
        private int m_groundContactCount;

        /// <summary>
        /// 小球与峭壁的接触点数
        /// </summary>
        private int m_steepContactCount;

        /// <summary>
        /// 追踪自从接地以来经历的物理步长
        /// </summary>
        private int m_stepsSinceLastGrounded;

        /// <summary>
        /// 追踪自从上次跳跃以来经历的物理步长
        /// </summary>
        private int m_stepsSinceLastJump;

        /// <summary>
        /// 最大的捕捉速度
        /// </summary>
        [SerializeField, Range(0f, 100f)]
        private float m_maxSnapSpeed = 100f;

        /// <summary>
        /// 射线的探测距离
        /// </summary>
        [SerializeField, Min(0f)]
        private float m_probeDistance = 1f;

        /// <summary>
        /// 探测掩码
        /// </summary>
        [SerializeField]
        private LayerMask m_probeMask = -1;

        /// <summary>
        /// 楼梯掩码
        /// </summary>
        [SerializeField]
        private LayerMask m_stairsMask = -1;

        /// <summary>
        /// 定义控制小球的输入空间
        /// </summary>
        [SerializeField]
        private Transform m_playerInputSpace = default;

        /// <summary>
        /// 小球的重力轴反方向
        /// </summary>
        [SerializeField]
        private Vector3 m_upAxis;

        /// <summary>
        /// 小球向右移动的轴向
        /// </summary>
        private Vector3 m_rightAxis;

        /// <summary>
        /// 小球向前移动的轴向
        /// </summary>
        private Vector3 m_forwardAxis;

#endregion
    }
}
#endif

#if Climbing
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
            m_rigidbody.useGravity = false;
            m_meshRenderer = GetComponent<MeshRenderer>();
            OnValidate();
        }

        /// <summary>
        /// 在播放模式下通过检查器更改角度时，更新相应字段值
        /// </summary>
        private void OnValidate()
        {
            m_minGroundDotProduct = Mathf.Cos(m_maxGroundAngle * Mathf.Deg2Rad);
            m_minStairsDotProduct = Mathf.Cos(m_maxStairsAngle * Mathf.Deg2Rad);
            m_minClimbDotProduct = Mathf.Cos(m_maxClimbAngle * Mathf.Deg2Rad);
        }

        /// <summary>
        /// 检测输入并更新小球位置
        /// </summary>
        private void Update()
        {
            m_playerInput.x = Input.GetAxis("Horizontal");
            m_playerInput.y = Input.GetAxis("Vertical");
            // 约束输入向量的大小永远小于等于1
            m_playerInput = Vector2.ClampMagnitude(m_playerInput, 1f);
            if (m_playerInputSpace)
            {
                m_rightAxis = ProjectDirectionOnPlane(m_playerInputSpace.right, m_upAxis);
                m_forwardAxis = ProjectDirectionOnPlane(m_playerInputSpace.forward, m_upAxis);
            }
            else
            {
                m_rightAxis = ProjectDirectionOnPlane(Vector3.right, m_upAxis);
                m_forwardAxis = ProjectDirectionOnPlane(Vector3.forward, m_upAxis);
            }

            m_desiredJump |= Input.GetButtonDown("Jump");
            m_desiresClimbing = Input.GetButton("Climb");

            // 区分攀爬状态和其他状态小球的颜色
            m_meshRenderer.material = Climbing ? m_climbingMaterial : m_normalMaterial;
        }

        /// <summary>
        /// 以固定的时间步长调整步速
        /// </summary>
        private void FixedUpdate()
        {
            Vector3 gravity = CustomGravity.GetGravity(m_rigidbody.position, out m_upAxis);

            UpdateState();
            AdjustVelocity();

            // 判断之前是否按下过跳跃键
            if (m_desiredJump)
            {
                m_desiredJump = false;
                Jump(gravity);
            }

            // 为小球攀爬施加抓地力
            if (Climbing)
            {
                m_velocity -= m_contactNormal * (m_maxClimbAcceleration * 0.9f * Time.deltaTime);
            }
            else if (OnGround && m_velocity.sqrMagnitude < 0.01f)
            {
                m_velocity += m_contactNormal * (Vector3.Dot(gravity, m_contactNormal) * Time.deltaTime);
            }
            // 球体在到达墙顶后仍想继续攀爬，则同时施加抓地力和重力
            else if (m_desiresClimbing && OnGround)
            {
                m_velocity += (gravity - m_contactNormal * (m_maxClimbAcceleration * 0.9f)) * Time.deltaTime;
            }
            // 只有在非攀爬状态下，才应用重力
            else
            {
                m_velocity += gravity * Time.deltaTime;
            }

            m_rigidbody.velocity = m_velocity;

            ClearState();
        }

        /// <summary>
        /// 更新小球状态
        /// </summary>
        private void UpdateState()
        {
            m_stepsSinceLastGrounded += 1;
            m_stepsSinceLastJump += 1;
            m_velocity = m_rigidbody.velocity;
            if (CheckClimbing() || OnGround || SnapToGround() || CheckSteepContacts())
            {
                m_stepsSinceLastGrounded = 0;
                if (m_stepsSinceLastJump > 1)
                {
                    m_jumpPhase = 0;
                }
                if (m_groundContactCount > 1)
                {
                    m_contactNormal.Normalize();
                }
            }
            else
            {
                // 空中跳跃时，小球直线上升
                m_contactNormal = m_upAxis;
            }

            if (m_connectedBody)
            {
                if (m_connectedBody.isKinematic || m_connectedBody.mass >= m_rigidbody.mass)
                {
                    UpdateConnectionState();
                }
            }
        }

        /// <summary>
        /// 更新连接状态
        /// </summary>
        private void UpdateConnectionState()
        {
            // 当前和先前的连接体相同时，计算这一帧的连接速度
            if (m_connectedBody == m_previousConnectedBody)
            {
                Vector3 connectionMovement = m_connectedBody.transform.TransformPoint(m_connectionLocalPosition)
                                             - m_connectionWorldPosition;
                m_connectionVelocity = connectionMovement / Time.deltaTime;
            }
            // 使用小球的位置计算连接点的世界坐标和局部坐标
            m_connectionWorldPosition = m_rigidbody.position;
            m_connectionLocalPosition = m_connectedBody.transform.InverseTransformPoint(m_connectionWorldPosition);
        }

        /// <summary>
        /// 重置小球的状态
        /// </summary>
        private void ClearState()
        {
            m_groundContactCount = m_steepContactCount = m_climbContactCount = 0;
            m_contactNormal = m_steepNormal = m_climbNormal = Vector3.zero;
            m_previousConnectedBody = m_connectedBody;
            m_connectedBody = null;
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
        private void Jump(Vector3 gravity)
        {
            Vector3 jumpDirection;
            if (OnGround)
                jumpDirection = m_contactNormal;
            // 蹬墙跳
            else if (OnSteep)
            {
                jumpDirection = m_steepNormal;
                m_jumpPhase = 0;
            }
            else if (m_maxAirJumps > 0 && m_jumpPhase <= m_maxAirJumps)
            {
                // 防止小球在未跳跃时沿表面掉下来时可以多跳一次
                if (m_jumpPhase == 0)
                {
                    m_jumpPhase = 1;
                }
                jumpDirection = m_contactNormal;
            }
            else
                return;
            m_stepsSinceLastJump = 0;
            m_jumpPhase += 1;
            float jumpSpeed = Mathf.Sqrt(2f * gravity.magnitude * m_jumpHeight);
            // 修正蹬墙跳跃时的朝向
            jumpDirection = (jumpDirection + m_upAxis).normalized;
            float alignedSpeed = Vector3.Dot(m_velocity, jumpDirection);
            // 限制小球的跳跃速度
            if (alignedSpeed > 0f)
            {
                jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
            }
            m_velocity += jumpDirection * jumpSpeed;
        }

        /// <summary>
        /// 评估小球与其他物体发生的碰撞
        /// </summary>
        /// <param name="collision"></param>
        private void EvaluateCollision(Collision collision)
        {
            int layer = collision.gameObject.layer;
            float minDot = GetMinDot(layer);
            for (int i = 0; i < collision.contactCount; i++)
            {
                Vector3 normal = collision.GetContact(i).normal;
                float upDot = Vector3.Dot(m_upAxis, normal);
                // 小球在地面上
                if (upDot > minDot)
                {
                    m_groundContactCount += 1;
                    m_contactNormal += normal;
                    m_connectedBody = collision.rigidbody;
                }
                else
                {
                    // 小球在峭壁上
                    if (upDot > -0.01f)
                    {
                        m_steepContactCount += 1;
                        m_steepNormal += normal;
                        // 仅在没有地面接触的情况下才分配斜坡刚体
                        if (m_groundContactCount == 0)
                        {
                            m_connectedBody = collision.rigidbody;
                        }
                    }
                    // 小球在攀爬面上
                    if (m_desiresClimbing && upDot >= m_minClimbDotProduct && (m_climbMask & (1 << layer)) != 0)
                    {
                        m_climbContactCount += 1;
                        m_climbNormal += normal;
                        m_lastClimbNormal = normal;
                        m_connectedBody = collision.rigidbody;
                    }
                }
            }
        }

        /// <summary>
        /// 尝试将小球与峭壁的接触转换为虚拟地面
        /// </summary>
        /// <returns>转换是否成功</returns>
        private bool CheckSteepContacts()
        {
            if (m_steepContactCount > 1)
            {
                m_steepNormal.Normalize();
                float upDot = Vector3.Dot(m_upAxis, m_steepNormal);
                if (upDot >= m_minGroundDotProduct)
                {
                    m_groundContactCount = 1;
                    m_contactNormal = m_steepNormal;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 检测攀爬状态
        /// </summary>
        /// <returns></returns>
        private bool CheckClimbing()
        {
            if (Climbing)
            {
                // 如果小球处于裂缝中，则使用最后的攀爬法线而不是合计值
                if (m_climbContactCount > 1)
                {
                    m_climbNormal.Normalize();
                    float upDot = Vector3.Dot(m_upAxis, m_climbNormal);
                    if (upDot >= m_minGroundDotProduct)
                    {
                        m_climbNormal = m_lastClimbNormal;
                    }
                }
                m_groundContactCount = 1;
                m_contactNormal = m_climbNormal;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 根据重力轴调整小球的控制方向
        /// </summary>
        /// <param name="direction">小球的控制轴向</param>
        /// <param name="normal">小球接触面的法线</param>
        /// <returns></returns>
        private Vector3 ProjectDirectionOnPlane(Vector3 direction, Vector3 normal)
        {
            return (direction - normal * Vector3.Dot(direction, normal)).normalized;
        }

        /// <summary>
        /// 根据接触平面上的投影来调整小球的速度
        /// </summary>
        private void AdjustVelocity()
        {
            float acceleration, speed;
            Vector3 xAxis, zAxis;
            // 当小球处于攀爬状态时，改变输入轴向
            if (Climbing)
            {
                acceleration = m_maxClimbAcceleration;
                speed = m_maxClimbSpeed;
                xAxis = Vector3.Cross(m_contactNormal, m_upAxis);
                zAxis = m_upAxis;
            }
            else
            {
                acceleration = OnGround ? m_maxAcceleration : m_maxAirAcceleration;
                speed = OnGround && m_desiresClimbing ? m_maxClimbSpeed : m_maxSpeed;
                xAxis = m_rightAxis;
                zAxis = m_forwardAxis;
            }

            xAxis = ProjectDirectionOnPlane(xAxis, m_contactNormal);
            zAxis = ProjectDirectionOnPlane(zAxis, m_contactNormal);

            // 计算小球与其他物体接触时的相对速度
            Vector3 relativeVelocity = m_velocity - m_connectionVelocity;
            float currentX = Vector3.Dot(relativeVelocity, xAxis);
            float currentZ = Vector3.Dot(relativeVelocity, zAxis);

            // 当FixedUpdate被调用时，Time.deltaTime等于Time.fixedDeltaTime
            float maxSpeedChange = acceleration * Time.deltaTime;

            // 根据当前值和目标值以及最大允许的速度变化变换速度
            float newX = Mathf.MoveTowards(currentX, m_playerInput.x * speed, maxSpeedChange);
            float newZ = Mathf.MoveTowards(currentZ, m_playerInput.y * speed, maxSpeedChange);

            m_velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
        }

        /// <summary>
        /// 使小球始终贴着地面
        /// </summary>
        /// <returns></returns>
        private bool SnapToGround()
        {
            // 由于碰撞数据的延迟，我们仍然认为启动跳跃后的步骤已接地。因此，如果我们在跳转后走了两个或更少的步骤，就必须中止。
            if (m_stepsSinceLastGrounded > 1 || m_stepsSinceLastJump <= 2)
                return false;
            float speed = m_velocity.magnitude;
            if (speed > m_maxSnapSpeed)
            {
                return false;
            }
            if (!Physics.Raycast(m_rigidbody.position, -m_upAxis, out RaycastHit hit, m_probeDistance, m_probeMask))
                return false;
            // 根据新的向上轴检查小球是否在地面
            float upDot = Vector3.Dot(m_upAxis, hit.normal);
            if (upDot < GetMinDot(hit.collider.gameObject.layer))
                return false;
            // 如果此时还没有中止，那么我们只是失去了与地面的接触，但仍然在地面上
            m_groundContactCount = 1;
            m_contactNormal = hit.normal;
            // 调整速度，使其和地面对齐
            float dot = Vector3.Dot(m_velocity, hit.normal);
            if (dot > 0f)
            {
                m_velocity = (m_velocity - hit.normal * dot).normalized * speed;
            }

            m_connectedBody = hit.rigidbody;
            return true;
        }

        /// <summary>
        /// 计算给定图层的起跳的点乘最小值
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        private float GetMinDot(int layer)
        {
            return (m_stairsMask & (1 << layer)) == 0 ? m_minGroundDotProduct : m_minStairsDotProduct;
        }

#endregion

#region 属性

        /// <summary>
        /// 小球是否与地面接触
        /// </summary>
        private bool OnGround => m_groundContactCount > 0;

        /// <summary>
        /// 小球是否与峭壁表面接触
        /// </summary>
        private bool OnSteep => m_steepContactCount > 0;

        /// <summary>
        /// 小球是否与攀爬面接触
        /// </summary>
        private bool Climbing => m_climbContactCount > 0 && m_stepsSinceLastJump > 2;

#endregion

#region 依赖的字段

        /// <summary>
        /// 最大速度
        /// </summary>
        [SerializeField, Range(0f, 100f)]
        private float m_maxSpeed = 10f;

        /// <summary>
        /// 最大攀爬速度
        /// </summary>
        [SerializeField, Range(0f, 100f)] 
        private float m_maxClimbSpeed = 2f;

        /// <summary>
        /// 玩家输入
        /// </summary>
        private Vector2 m_playerInput;

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
        /// 空中最大加速度
        /// </summary>
        [SerializeField, Range(0f, 100f)]
        private float m_maxAirAcceleration = 1f;

        /// <summary>
        /// 最大攀爬加速度
        /// </summary>
        [SerializeField, Range(0f, 100f)] 
        private float m_maxClimbAcceleration = 20f;

        /// <summary>
        /// 刚体组件
        /// </summary>
        private Rigidbody m_rigidbody;

        /// <summary>
        /// 与小球连接的实体的刚体组件
        /// </summary>
        private Rigidbody m_connectedBody;

        /// <summary>
        /// 在上一个物理步长中与小球连接的实体的刚体组件
        /// </summary>
        private Rigidbody m_previousConnectedBody;

        /// <summary>
        /// 连接实体的速度
        /// </summary>
        private Vector3 m_connectionVelocity;

        /// <summary>
        /// 连接物体的世界坐标
        /// </summary>
        private Vector3 m_connectionWorldPosition;

        /// <summary>
        /// 连接物体的局部空间中的连接位置
        /// </summary>
        private Vector3 m_connectionLocalPosition;

        /// <summary>
        /// 是否执行跳跃
        /// </summary>
        private bool m_desiredJump;

        /// <summary>
        /// 是否执行攀爬
        /// </summary>
        private bool m_desiresClimbing;

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
        /// 小球能上的最大的楼梯角度
        /// </summary>
        [SerializeField, Range(0f, 90f)]
        private float m_maxStairsAngle = 50f;

        /// <summary>
        /// 最大攀爬角度
        /// </summary>
        [SerializeField, Range(90, 180)]
        private float m_maxClimbAngle = 140f;

        /// <summary>
        /// 与斜坡表面法线点乘的最小结果
        /// </summary>
        private float m_minGroundDotProduct;

        /// <summary>
        /// 与楼梯表面（近似成了斜面）法线点乘的最小结果
        /// </summary>
        private float m_minStairsDotProduct;

        /// <summary>
        /// 与攀爬表面法线点乘的最小结果
        /// </summary>
        private float m_minClimbDotProduct;

        /// <summary>
        /// 小球与斜坡接触点的表面法线
        /// </summary>
        private Vector3 m_contactNormal;

        /// <summary>
        /// 小球与峭壁接触点的表面法线
        /// </summary>
        private Vector3 m_steepNormal;

        /// <summary>
        /// 小球与攀爬表面接触点的表面法线
        /// </summary>
        private Vector3 m_climbNormal;

        /// <summary>
        /// 上一次攀爬的法线
        /// </summary>
        private Vector3 m_lastClimbNormal;

        /// <summary>
        /// 小球与地面的接触点数
        /// </summary>
        private int m_groundContactCount;

        /// <summary>
        /// 小球与峭壁的接触点数
        /// </summary>
        private int m_steepContactCount;

        /// <summary>
        /// 小球与攀爬面的接触点数
        /// </summary>
        private int m_climbContactCount;

        /// <summary>
        /// 追踪自从接地以来经历的物理步长
        /// </summary>
        private int m_stepsSinceLastGrounded;

        /// <summary>
        /// 追踪自从上次跳跃以来经历的物理步长
        /// </summary>
        private int m_stepsSinceLastJump;

        /// <summary>
        /// 最大的捕捉速度
        /// </summary>
        [SerializeField, Range(0f, 100f)]
        private float m_maxSnapSpeed = 100f;

        /// <summary>
        /// 射线的探测距离
        /// </summary>
        [SerializeField, Min(0f)]
        private float m_probeDistance = 1f;

        /// <summary>
        /// 探测掩码
        /// </summary>
        [SerializeField]
        private LayerMask m_probeMask = -1;

        /// <summary>
        /// 楼梯掩码
        /// </summary>
        [SerializeField]
        private LayerMask m_stairsMask = -1;

        /// <summary>
        /// 攀爬掩码
        /// </summary>
        [SerializeField]
        private LayerMask m_climbMask = -1;

        /// <summary>
        /// 定义控制小球的输入空间
        /// </summary>
        [SerializeField]
        private Transform m_playerInputSpace = default;

        /// <summary>
        /// 小球的重力轴反方向
        /// </summary>
        [SerializeField]
        private Vector3 m_upAxis;

        /// <summary>
        /// 小球向右移动的轴向
        /// </summary>
        private Vector3 m_rightAxis;

        /// <summary>
        /// 小球向前移动的轴向
        /// </summary>
        private Vector3 m_forwardAxis;

        /// <summary>
        /// 普通材质
        /// </summary>
        [SerializeField]
        private Material m_normalMaterial = default;

        /// <summary>
        /// 攀爬材质
        /// </summary>
        [SerializeField]
        private Material m_climbingMaterial = default;

        /// <summary>
        /// 小球的网格渲染组件
        /// </summary>
        private MeshRenderer m_meshRenderer;

#endregion
    }
}
#endif

#if Swimming
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
            m_rigidbody.useGravity = false;
            m_meshRenderer = GetComponent<MeshRenderer>();
            OnValidate();
        }

        /// <summary>
        /// 在播放模式下通过检查器更改角度时，更新相应字段值
        /// </summary>
        private void OnValidate()
        {
            m_minGroundDotProduct = Mathf.Cos(m_maxGroundAngle * Mathf.Deg2Rad);
            m_minStairsDotProduct = Mathf.Cos(m_maxStairsAngle * Mathf.Deg2Rad);
            m_minClimbDotProduct = Mathf.Cos(m_maxClimbAngle * Mathf.Deg2Rad);
        }

        /// <summary>
        /// 检测输入并更新小球位置
        /// </summary>
        private void Update()
        {
            m_playerInput.x = Input.GetAxis("Horizontal");
            m_playerInput.y = Input.GetAxis("Vertical");
            m_playerInput.z = Swimming ? Input.GetAxis("UpDown") : 0f;
            // 约束输入向量的大小永远小于等于1
            m_playerInput = Vector3.ClampMagnitude(m_playerInput, 1f);
            if (m_playerInputSpace)
            {
                m_rightAxis = ProjectDirectionOnPlane(m_playerInputSpace.right, m_upAxis);
                m_forwardAxis = ProjectDirectionOnPlane(m_playerInputSpace.forward, m_upAxis);
            }
            else
            {
                m_rightAxis = ProjectDirectionOnPlane(Vector3.right, m_upAxis);
                m_forwardAxis = ProjectDirectionOnPlane(Vector3.forward, m_upAxis);
            }

            if (Swimming)
            {
                m_desiresClimbing = false;
            }
            else
            {
                m_desiredJump |= Input.GetButtonDown("Jump");
                m_desiresClimbing = Input.GetButton("Climb");
            }

            // 区分攀爬/游泳状态和其他状态小球的颜色
            m_meshRenderer.material = Climbing ? m_climbingMaterial : Swimming ? m_swimmingMaterial : m_normalMaterial;
        }

        /// <summary>
        /// 以固定的时间步长调整步速
        /// </summary>
        private void FixedUpdate()
        {
            Vector3 gravity = CustomGravity.GetGravity(m_rigidbody.position, out m_upAxis);

            UpdateState();
            // 如果小球在水中，则给小球应用阻力
            if (InWater)
            {
                m_velocity *= 1f - m_waterDrag * m_submergence * Time.deltaTime;
            }

            AdjustVelocity();

            // 判断之前是否按下过跳跃键
            if (m_desiredJump)
            {
                m_desiredJump = false;
                Jump(gravity);
            }

            // 为小球攀爬施加抓地力
            if (Climbing)
            {
                m_velocity -= m_contactNormal * (m_maxClimbAcceleration * 0.9f * Time.deltaTime);
            }
            // 如果小球在水中，则为小球施加浮力
            else if (InWater)
            {
                m_velocity += gravity * ((1f - m_buoyancy * m_submergence) * Time.deltaTime);
            }
            else if (OnGround && m_velocity.sqrMagnitude < 0.01f)
            {
                m_velocity += m_contactNormal * (Vector3.Dot(gravity, m_contactNormal) * Time.deltaTime);
            }
            // 球体在到达墙顶后仍想继续攀爬，则同时施加抓地力和重力
            else if (m_desiresClimbing && OnGround)
            {
                m_velocity += (gravity - m_contactNormal * (m_maxClimbAcceleration * 0.9f)) * Time.deltaTime;
            }
            // 只有在非攀爬状态下，才应用重力
            else
            {
                m_velocity += gravity * Time.deltaTime;
            }

            m_rigidbody.velocity = m_velocity;

            ClearState();
        }

        /// <summary>
        /// 更新小球状态
        /// </summary>
        private void UpdateState()
        {
            m_stepsSinceLastGrounded += 1;
            m_stepsSinceLastJump += 1;
            m_velocity = m_rigidbody.velocity;
            if (CheckClimbing() || CheckSwimming() || OnGround || SnapToGround() || CheckSteepContacts())
            {
                m_stepsSinceLastGrounded = 0;
                if (m_stepsSinceLastJump > 1)
                {
                    m_jumpPhase = 0;
                }
                if (m_groundContactCount > 1)
                {
                    m_contactNormal.Normalize();
                }
            }
            else
            {
                // 空中跳跃时，小球直线上升
                m_contactNormal = m_upAxis;
            }

            if (m_connectedBody)
            {
                if (m_connectedBody.isKinematic || m_connectedBody.mass >= m_rigidbody.mass)
                {
                    UpdateConnectionState();
                }
            }
        }

        /// <summary>
        /// 更新连接状态
        /// </summary>
        private void UpdateConnectionState()
        {
            // 当前和先前的连接体相同时，计算这一帧的连接速度
            if (m_connectedBody == m_previousConnectedBody)
            {
                Vector3 connectionMovement = m_connectedBody.transform.TransformPoint(m_connectionLocalPosition)
                                             - m_connectionWorldPosition;
                m_connectionVelocity = connectionMovement / Time.deltaTime;
            }
            // 使用小球的位置计算连接点的世界坐标和局部坐标
            m_connectionWorldPosition = m_rigidbody.position;
            m_connectionLocalPosition = m_connectedBody.transform.InverseTransformPoint(m_connectionWorldPosition);
        }

        /// <summary>
        /// 重置小球的状态
        /// </summary>
        private void ClearState()
        {
            m_groundContactCount = m_steepContactCount = m_climbContactCount = 0;
            m_contactNormal = m_steepNormal = m_climbNormal = Vector3.zero;
            m_connectionVelocity = Vector3.zero;
            m_previousConnectedBody = m_connectedBody;
            m_connectedBody = null;
            m_submergence = 0;
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
        /// 检测小球与水体的接触
        /// </summary>
        /// <param name="other"></param>
        private void OnTriggerEnter(Collider other)
        {
            if ((m_waterMask & (1 << other.gameObject.layer)) != 0)
            {
                EvaluateSubmergence(other);
            }
        }

        /// <summary>
        /// 检测小球与水体的接触
        /// </summary>
        /// <param name="other"></param>
        private void OnTriggerStay(Collider other)
        {
            if ((m_waterMask & (1 << other.gameObject.layer)) != 0)
            {
                EvaluateSubmergence(other);
            }
        }

        private void EvaluateSubmergence(Collider collider)
        {
            // 计算小球浸入水中的深度
            if (Physics.Raycast(m_rigidbody.position + m_upAxis * m_submergenceOffset, -m_upAxis, out RaycastHit hit,
                    m_submergenceRange + 1f, m_waterMask, QueryTriggerInteraction.Collide))
            {
                m_submergence = 1f - hit.distance / m_submergenceRange;
            }
            // 如果检测失败，说明小球已经完全浸入水中
            else
            {
                m_submergence = 1f;
            }
            // 获取流动的水体的刚体，使小球跟随水体流动
            if (Swimming)
            {
                m_connectedBody = collider.attachedRigidbody;
            }
        }

        /// <summary>
        /// 小球跳跃
        /// </summary>
        private void Jump(Vector3 gravity)
        {
            Vector3 jumpDirection;
            if (OnGround)
                jumpDirection = m_contactNormal;
            // 蹬墙跳
            else if (OnSteep)
            {
                jumpDirection = m_steepNormal;
                m_jumpPhase = 0;
            }
            else if (m_maxAirJumps > 0 && m_jumpPhase <= m_maxAirJumps)
            {
                // 防止小球在未跳跃时沿表面掉下来时可以多跳一次
                if (m_jumpPhase == 0)
                {
                    m_jumpPhase = 1;
                }
                jumpDirection = m_contactNormal;
            }
            else
                return;
            m_stepsSinceLastJump = 0;
            m_jumpPhase += 1;
            float jumpSpeed = Mathf.Sqrt(2f * gravity.magnitude * m_jumpHeight);
            // 根据小球在水中的浸入程度调整跳跃速度
            if (InWater)
            {
                jumpSpeed *= Mathf.Max(0f, 1f - m_submergence / m_swimThreshold);
            }
            // 修正蹬墙跳跃时的朝向
            jumpDirection = (jumpDirection + m_upAxis).normalized;
            float alignedSpeed = Vector3.Dot(m_velocity, jumpDirection);
            // 限制小球的跳跃速度
            if (alignedSpeed > 0f)
            {
                jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
            }
            m_velocity += jumpDirection * jumpSpeed;
        }

        /// <summary>
        /// 评估小球与其他物体发生的碰撞
        /// </summary>
        /// <param name="collision"></param>
        private void EvaluateCollision(Collision collision)
        {
            if (Swimming)
                return;
            int layer = collision.gameObject.layer;
            float minDot = GetMinDot(layer);
            for (int i = 0; i < collision.contactCount; i++)
            {
                Vector3 normal = collision.GetContact(i).normal;
                float upDot = Vector3.Dot(m_upAxis, normal);
                // 小球在地面上
                if (upDot > minDot)
                {
                    m_groundContactCount += 1;
                    m_contactNormal += normal;
                    m_connectedBody = collision.rigidbody;
                }
                else
                {
                    // 小球在峭壁上
                    if (upDot > -0.01f)
                    {
                        m_steepContactCount += 1;
                        m_steepNormal += normal;
                        // 仅在没有地面接触的情况下才分配斜坡刚体
                        if (m_groundContactCount == 0)
                        {
                            m_connectedBody = collision.rigidbody;
                        }
                    }
                    // 小球在攀爬面上
                    if (m_desiresClimbing && upDot >= m_minClimbDotProduct && (m_climbMask & (1 << layer)) != 0)
                    {
                        m_climbContactCount += 1;
                        m_climbNormal += normal;
                        m_lastClimbNormal = normal;
                        m_connectedBody = collision.rigidbody;
                    }
                }
            }
        }

        /// <summary>
        /// 尝试将小球与峭壁的接触转换为虚拟地面
        /// </summary>
        /// <returns>转换是否成功</returns>
        private bool CheckSteepContacts()
        {
            if (m_steepContactCount > 1)
            {
                m_steepNormal.Normalize();
                float upDot = Vector3.Dot(m_upAxis, m_steepNormal);
                if (upDot >= m_minGroundDotProduct)
                {
                    m_groundContactCount = 1;
                    m_contactNormal = m_steepNormal;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 检测攀爬状态
        /// </summary>
        /// <returns></returns>
        private bool CheckClimbing()
        {
            if (Climbing)
            {
                // 如果小球处于裂缝中，则使用最后的攀爬法线而不是合计值
                if (m_climbContactCount > 1)
                {
                    m_climbNormal.Normalize();
                    float upDot = Vector3.Dot(m_upAxis, m_climbNormal);
                    if (upDot >= m_minGroundDotProduct)
                    {
                        m_climbNormal = m_lastClimbNormal;
                    }
                }
                m_groundContactCount = 1;
                m_contactNormal = m_climbNormal;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 检测游泳状态
        /// </summary>
        /// <returns></returns>
        private bool CheckSwimming()
        {
            if (Swimming)
            {
                m_groundContactCount = 0;
                m_contactNormal = m_upAxis;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 根据重力轴调整小球的控制方向
        /// </summary>
        /// <param name="direction">小球的控制轴向</param>
        /// <param name="normal">小球接触面的法线</param>
        /// <returns></returns>
        private Vector3 ProjectDirectionOnPlane(Vector3 direction, Vector3 normal)
        {
            return (direction - normal * Vector3.Dot(direction, normal)).normalized;
        }

        /// <summary>
        /// 根据接触平面上的投影来调整小球的速度
        /// </summary>
        private void AdjustVelocity()
        {
            float acceleration, speed;
            Vector3 xAxis, zAxis;
            // 当小球处于攀爬状态时，改变输入轴向
            if (Climbing)
            {
                acceleration = m_maxClimbAcceleration;
                speed = m_maxClimbSpeed;
                xAxis = Vector3.Cross(m_contactNormal, m_upAxis);
                zAxis = m_upAxis;
            }
            else if (InWater)
            {
                float swimFactor = Mathf.Min(1f, m_submergence / m_swimThreshold);
                acceleration = Mathf.LerpUnclamped(OnGround ? m_maxAcceleration : m_maxAirAcceleration, m_maxSwimAcceleration, swimFactor);
                speed = Mathf.LerpUnclamped(m_maxSpeed, m_maxSwimSpeed, swimFactor);
                xAxis = m_rightAxis;
                zAxis = m_forwardAxis;
            }
            else
            {
                acceleration = OnGround ? m_maxAcceleration : m_maxAirAcceleration;
                speed = OnGround && m_desiresClimbing ? m_maxClimbSpeed : m_maxSpeed;
                xAxis = m_rightAxis;
                zAxis = m_forwardAxis;
            }

            xAxis = ProjectDirectionOnPlane(xAxis, m_contactNormal);
            zAxis = ProjectDirectionOnPlane(zAxis, m_contactNormal);

            // 计算小球与其他物体接触时的相对速度
            Vector3 relativeVelocity = m_velocity - m_connectionVelocity;
            float currentX = Vector3.Dot(relativeVelocity, xAxis);
            float currentZ = Vector3.Dot(relativeVelocity, zAxis);

            // 当FixedUpdate被调用时，Time.deltaTime等于Time.fixedDeltaTime
            float maxSpeedChange = acceleration * Time.deltaTime;

            // 根据当前值和目标值以及最大允许的速度变化变换速度
            float newX = Mathf.MoveTowards(currentX, m_playerInput.x * speed, maxSpeedChange);
            float newZ = Mathf.MoveTowards(currentZ, m_playerInput.y * speed, maxSpeedChange);

            m_velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);

            if (Swimming)
            {
                float currentY = Vector3.Dot(relativeVelocity, m_upAxis);
                float newY = Mathf.MoveTowards(currentY, m_playerInput.z * speed, maxSpeedChange);
                m_velocity += m_upAxis * (newY - currentY);
            }
        }

        /// <summary>
        /// 使小球始终贴着地面
        /// </summary>
        /// <returns></returns>
        private bool SnapToGround()
        {
            // 由于碰撞数据的延迟，我们仍然认为启动跳跃后的步骤已接地。因此，如果我们在跳转后走了两个或更少的步骤，就必须中止
            if (m_stepsSinceLastGrounded > 1 || m_stepsSinceLastJump <= 2)
                return false;
            float speed = m_velocity.magnitude;
            if (speed > m_maxSnapSpeed)
            {
                return false;
            }
            if (!Physics.Raycast(m_rigidbody.position, -m_upAxis, out RaycastHit hit, m_probeDistance, m_probeMask, QueryTriggerInteraction.Ignore))
                return false;
            // 根据新的向上轴检查小球是否在地面
            float upDot = Vector3.Dot(m_upAxis, hit.normal);
            if (upDot < GetMinDot(hit.collider.gameObject.layer))
                return false;
            // 如果此时还没有中止，那么我们只是失去了与地面的接触，但仍然在地面上
            m_groundContactCount = 1;
            m_contactNormal = hit.normal;
            // 调整速度，使其和地面对齐
            float dot = Vector3.Dot(m_velocity, hit.normal);
            if (dot > 0f)
            {
                m_velocity = (m_velocity - hit.normal * dot).normalized * speed;
            }

            m_connectedBody = hit.rigidbody;
            return true;
        }

        /// <summary>
        /// 计算给定图层的起跳的点乘最小值
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        private float GetMinDot(int layer)
        {
            return (m_stairsMask & (1 << layer)) == 0 ? m_minGroundDotProduct : m_minStairsDotProduct;
        }

#endregion

#region 属性

        /// <summary>
        /// 小球是否与地面接触
        /// </summary>
        private bool OnGround => m_groundContactCount > 0;

        /// <summary>
        /// 小球是否与峭壁表面接触
        /// </summary>
        private bool OnSteep => m_steepContactCount > 0;

        /// <summary>
        /// 小球是否与攀爬面接触
        /// </summary>
        private bool Climbing => m_climbContactCount > 0 && m_stepsSinceLastJump > 2;

        /// <summary>
        /// 小球是否在水中
        /// </summary>
        private bool InWater => m_submergence > 0f;

        /// <summary>
        /// 小球是否达到了游泳阈值
        /// </summary>
        private bool Swimming => m_submergence >= m_swimThreshold;

#endregion

#region 依赖的字段

        /// <summary>
        /// 最大速度
        /// </summary>
        [SerializeField, Range(0f, 100f)]
        private float m_maxSpeed = 10f;

        /// <summary>
        /// 最大攀爬速度
        /// </summary>
        [SerializeField, Range(0f, 100f)]
        private float m_maxClimbSpeed = 2f;

        /// <summary>
        /// 最大游泳速度
        /// </summary>
        [SerializeField, Range(0f, 100f)]
        private float m_maxSwimSpeed = 5f;

        /// <summary>
        /// 玩家输入
        /// </summary>
        private Vector3 m_playerInput;

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
        /// 空中最大加速度
        /// </summary>
        [SerializeField, Range(0f, 100f)]
        private float m_maxAirAcceleration = 1f;

        /// <summary>
        /// 最大攀爬加速度
        /// </summary>
        [SerializeField, Range(0f, 100f)]
        private float m_maxClimbAcceleration = 20f;

        /// <summary>
        /// 最大游泳加速度
        /// </summary>
        [SerializeField, Range(0f, 100f)] 
        private float m_maxSwimAcceleration = 5f;

        /// <summary>
        /// 刚体组件
        /// </summary>
        private Rigidbody m_rigidbody;

        /// <summary>
        /// 与小球连接的实体的刚体组件
        /// </summary>
        private Rigidbody m_connectedBody;

        /// <summary>
        /// 在上一个物理步长中与小球连接的实体的刚体组件
        /// </summary>
        private Rigidbody m_previousConnectedBody;

        /// <summary>
        /// 连接实体的速度
        /// </summary>
        private Vector3 m_connectionVelocity;

        /// <summary>
        /// 连接物体的世界坐标
        /// </summary>
        private Vector3 m_connectionWorldPosition;

        /// <summary>
        /// 连接物体的局部空间中的连接位置
        /// </summary>
        private Vector3 m_connectionLocalPosition;

        /// <summary>
        /// 是否执行跳跃
        /// </summary>
        private bool m_desiredJump;

        /// <summary>
        /// 是否执行攀爬
        /// </summary>
        private bool m_desiresClimbing;

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
        /// 小球能上的最大的楼梯角度
        /// </summary>
        [SerializeField, Range(0f, 90f)]
        private float m_maxStairsAngle = 50f;

        /// <summary>
        /// 最大攀爬角度
        /// </summary>
        [SerializeField, Range(90, 180)]
        private float m_maxClimbAngle = 140f;

        /// <summary>
        /// 与斜坡表面法线点乘的最小结果
        /// </summary>
        private float m_minGroundDotProduct;

        /// <summary>
        /// 与楼梯表面（近似成了斜面）法线点乘的最小结果
        /// </summary>
        private float m_minStairsDotProduct;

        /// <summary>
        /// 与攀爬表面法线点乘的最小结果
        /// </summary>
        private float m_minClimbDotProduct;

        /// <summary>
        /// 小球与斜坡接触点的表面法线
        /// </summary>
        private Vector3 m_contactNormal;

        /// <summary>
        /// 小球与峭壁接触点的表面法线
        /// </summary>
        private Vector3 m_steepNormal;

        /// <summary>
        /// 小球与攀爬表面接触点的表面法线
        /// </summary>
        private Vector3 m_climbNormal;

        /// <summary>
        /// 上一次攀爬的法线
        /// </summary>
        private Vector3 m_lastClimbNormal;

        /// <summary>
        /// 小球与地面的接触点数
        /// </summary>
        private int m_groundContactCount;

        /// <summary>
        /// 小球与峭壁的接触点数
        /// </summary>
        private int m_steepContactCount;

        /// <summary>
        /// 小球与攀爬面的接触点数
        /// </summary>
        private int m_climbContactCount;

        /// <summary>
        /// 追踪自从接地以来经历的物理步长
        /// </summary>
        private int m_stepsSinceLastGrounded;

        /// <summary>
        /// 追踪自从上次跳跃以来经历的物理步长
        /// </summary>
        private int m_stepsSinceLastJump;

        /// <summary>
        /// 最大的捕捉速度
        /// </summary>
        [SerializeField, Range(0f, 100f)]
        private float m_maxSnapSpeed = 100f;

        /// <summary>
        /// 射线的探测距离
        /// </summary>
        [SerializeField, Min(0f)]
        private float m_probeDistance = 1f;

        /// <summary>
        /// 探测掩码
        /// </summary>
        [SerializeField]
        private LayerMask m_probeMask = -1;

        /// <summary>
        /// 楼梯掩码
        /// </summary>
        [SerializeField]
        private LayerMask m_stairsMask = -1;

        /// <summary>
        /// 攀爬掩码
        /// </summary>
        [SerializeField]
        private LayerMask m_climbMask = -1;

        /// <summary>
        /// 水体掩码
        /// </summary>
        [SerializeField]
        private LayerMask m_waterMask = 0;

        /// <summary>
        /// 定义控制小球的输入空间
        /// </summary>
        [SerializeField]
        private Transform m_playerInputSpace = default;

        /// <summary>
        /// 小球的重力轴反方向
        /// </summary>
        [SerializeField]
        private Vector3 m_upAxis;

        /// <summary>
        /// 小球向右移动的轴向
        /// </summary>
        private Vector3 m_rightAxis;

        /// <summary>
        /// 小球向前移动的轴向
        /// </summary>
        private Vector3 m_forwardAxis;

        /// <summary>
        /// 普通材质
        /// </summary>
        [SerializeField]
        private Material m_normalMaterial = default;

        /// <summary>
        /// 攀爬材质
        /// </summary>
        [SerializeField]
        private Material m_climbingMaterial = default;

        /// <summary>
        /// 游泳材质
        /// </summary>
        [SerializeField]
        private Material m_swimmingMaterial = default;

        /// <summary>
        /// 小球的网格渲染组件
        /// </summary>
        private MeshRenderer m_meshRenderer;

        /// <summary>
        /// 小球浸入水中的程度，0表示没接触，1表示完全浸入
        /// </summary>
        private float m_submergence;

        /// <summary>
        /// 小球中心到顶部的偏移值（小球的半径）
        /// </summary>
        [SerializeField]
        private float m_submergenceOffset = 0.5f;

        /// <summary>
        /// 浸入范围
        /// </summary>
        [SerializeField]
        private float m_submergenceRange = 1f;

        /// <summary>
        /// 水的阻力
        /// </summary>
        [SerializeField, Range(0f, 10f)]
        private float m_waterDrag = 1f;

        /// <summary>
        /// 浮力
        /// </summary>
        [SerializeField, Min(0f)]
        private float m_buoyancy = 1f;

        /// <summary>
        /// 游泳阈值
        /// </summary>
        [SerializeField, Range(0.01f, 1f)]
        private float m_swimThreshold = 0.5f;

#endregion
    }
}
#endif