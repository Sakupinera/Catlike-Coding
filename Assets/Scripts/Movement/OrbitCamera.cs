#define ComplexGravity
using Assets.Scripts.Movement;
using UnityEngine;

#if OrbitCamera
namespace Assets.Scripts.Movement
{
    /// <summary>
    /// 轨道摄像机
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class OrbitCamera : MonoBehaviour
    {
#region 方法

        /// <summary>
        /// 初始化
        /// </summary>
        private void Awake()
        {
            m_regularCamera = GetComponent<Camera>();
            m_focusPoint = m_focus.position;
            transform.localRotation = Quaternion.Euler(m_orbitAngles);
        }

        /// <summary>
        /// 相机跟随小球
        /// </summary>
        private void LateUpdate()
        {
            UpdateFocusPoint();
            Quaternion lookRotation;
            // 旋转相机
            if (ManualRotation() || AutomaticRotation())
            {
                ConstrainAngles();
                lookRotation = Quaternion.Euler(m_orbitAngles);
            }
            else
            {
                lookRotation = transform.localRotation;
            }
            Vector3 lookDirection = transform.forward;
            Vector3 lookPosition = m_focusPoint - lookDirection * m_distance;

            // 根据小球的实际位置来计算盒投影的方向和距离
            Vector3 rectOffset = lookDirection * m_regularCamera.nearClipPlane;
            Vector3 rectPosition = lookPosition + rectOffset;
            Vector3 castFrom = m_focus.position;
            Vector3 castLine = rectPosition - castFrom;
            float castDistance = castLine.magnitude;
            Vector3 castDirection = castLine / castDistance;

            // 检测相机是否与墙体发生碰撞，调整摄像机的位置
            if (Physics.BoxCast(castFrom, CameraHalfExtends, castDirection, out RaycastHit hit, 
                    lookRotation, castDistance, m_obstructionMask))
            {
                rectPosition = castFrom + castDirection * hit.distance;
                lookPosition = rectPosition - rectOffset;
            }

            // 设置相机的方位
            transform.SetPositionAndRotation(lookPosition, lookRotation);
        }

        /// <summary>
        /// 约束检查器配置数据时的合法性
        /// </summary>
        private void OnValidate()
        {
            if (m_maxVerticalAngle < m_minVerticalAngle)
            {
                m_maxVerticalAngle = m_minVerticalAngle;
            }
        }

        /// <summary>
        /// 更新目标焦点的位置
        /// </summary>
        private void UpdateFocusPoint()
        {
            m_previousFocusPoint = m_focusPoint;
            Vector3 targetPoint = m_focus.position;
            if (m_focusRadius > 0f)
            {
                float distance = Vector3.Distance(targetPoint, m_focusPoint);
                float t = 1f;
                // 将焦点向目标中心拉近
                if (distance > 0.01f && m_focusCentering > 0f)
                {
                    t = Mathf.Pow(1f - m_focusCentering, Time.unscaledDeltaTime);
                }
                // 当相机距离小球距离大于聚焦半径时
                if (distance > m_focusRadius)
                {
                    // 控制相机与小球的距离始终保持在聚焦半径左右
                    t = Mathf.Min(t, m_focusRadius / distance);
                }

                m_focusPoint = Vector3.Lerp(targetPoint, m_focusPoint, t);
            }
            else
            {
                m_focusPoint = targetPoint;
            }
        }

        /// <summary>
        /// 手动控制相机
        /// </summary>
        /// <returns>是否手动操作了相机</returns>x
        private bool ManualRotation()
        {
            Vector2 input = new Vector2(Input.GetAxis("Vertical Camera"), Input.GetAxis("Horizontal Camera"));
            const float e = 0.001f;
            if (input.x < -e || input.x > e || input.y < -e || input.y > e)
            {
                m_orbitAngles += m_rotationSpeed * Time.unscaledDeltaTime * input;
                m_lastManualRotationTime = Time.unscaledTime;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 相机自动进行对齐
        /// </summary>
        /// <returns></returns>
        private bool AutomaticRotation()
        {
            if (Time.unscaledTime - m_lastManualRotationTime < m_alignDelay)
            {
                return false;
            }

            // 计算当前帧焦点的运动矢量
            Vector2 movement = new Vector2(
                m_focusPoint.x - m_previousFocusPoint.x,
                m_focusPoint.z - m_previousFocusPoint.z
            );
            float movementDeltaSqr = movement.sqrMagnitude;
            if (movementDeltaSqr < 0.0001f)
            {
                return false;
            }

            // 获取水平航向角
            float headingAngle = GetAngle(movement / Mathf.Sqrt(movementDeltaSqr));
            float deltaAbs = Mathf.Abs(Mathf.DeltaAngle(m_orbitAngles.y, headingAngle));
            // 当前帧的旋转角度的变化
            float rotationChange = m_rotationSpeed * Mathf.Min(Time.unscaledDeltaTime, movementDeltaSqr);
            // 如果角度变换在平滑范围内，则进行平滑过渡
            if (deltaAbs < m_alignSmoothRange)
            {
                rotationChange *= deltaAbs / m_alignSmoothRange;
            }
            else if (180f - deltaAbs < m_alignSmoothRange)
            {
                rotationChange *= (180f - deltaAbs) / m_alignSmoothRange;
            }
            m_orbitAngles.y = Mathf.MoveTowardsAngle(m_orbitAngles.y, headingAngle, rotationChange); 

            return true;
        }

        /// <summary>
        /// 约束相机的旋转角度
        /// </summary>
        private void ConstrainAngles()
        {
            // 约束相机沿X轴的转动幅度
            m_orbitAngles.x = Mathf.Clamp(m_orbitAngles.x, m_minVerticalAngle, m_maxVerticalAngle);

            // 约束相机沿Y轴旋转时数值的范围
            if (m_orbitAngles.y < 0f)
            {
                m_orbitAngles.y += 360f;
            }
            else if (m_orbitAngles.y >= 360f)
            {
                m_orbitAngles.y -= 360f;
            }
        }

        /// <summary>
        /// 根据方向计算水平角度
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        private static float GetAngle(Vector2 direction)
        {
            float angle = Mathf.Acos(direction.y) * Mathf.Rad2Deg;
            return direction.x < 0f ? 360f - angle : angle;
        }

#endregion

#region 属性

        /// <summary>
        /// 相机近裁剪平面的半个大小
        /// </summary>
        private Vector3 CameraHalfExtends
        {
            get
            {
                Vector3 halfExtends;
                halfExtends.y = m_regularCamera.nearClipPlane *
                                Mathf.Tan(0.5f * Mathf.Deg2Rad * m_regularCamera.fieldOfView);
                halfExtends.x = halfExtends.y * m_regularCamera.aspect;
                halfExtends.z = 0f;
                return halfExtends;
            }
        }

#endregion

#region 依赖的字段

        /// <summary>
        /// 相机聚焦的对象
        /// </summary>
        [SerializeField] 
        private Transform m_focus = default;

        /// <summary>
        /// 轨道与小球之间的距离
        /// </summary>
        [SerializeField, Range(1f, 20f)] 
        private float m_distance = 5f;

        /// <summary>
        /// 焦点半径，只有焦点与目标焦点位置相差较大时才进行相机移动
        /// </summary>
        [SerializeField, Min(0f)]
        private float m_focusRadius = 1f;

        /// <summary>
        /// 当前焦点位置
        /// </summary>
        private Vector3 m_focusPoint;

        /// <summary>
        /// 先前焦点的位置
        /// </summary>
        private Vector3 m_previousFocusPoint;

        /// <summary>
        /// 焦点居中系数，用于控制焦点回到小球位置的速度
        /// </summary>
        [SerializeField, Range(0f, 1f)] 
        private float m_focusCentering = 0.5f;

        /// <summary>
        /// 相机的环绕角度
        /// </summary>
        private Vector2 m_orbitAngles = new Vector2(45f, 0f);

        /// <summary>
        /// 相机的转动速度
        /// </summary>
        [SerializeField, Range(1f, 360f)]
        private float m_rotationSpeed = 90f;

        /// <summary>
        /// 相机的最小垂直角度
        /// </summary>
        [SerializeField, Range(-89f, 89f)] 
        private float m_minVerticalAngle = -30f;

        /// <summary>
        /// 相机的最大垂直角度
        /// </summary>
        [SerializeField, Range(-89f, 89f)]
        private float m_maxVerticalAngle = 60f;

        /// <summary>
        /// 相机自动对齐的延迟
        /// </summary>
        [SerializeField, Min(0f)] 
        private float m_alignDelay = 5f;

        /// <summary>
        /// 最后一次手动旋转相机的时间
        /// </summary>
        private float m_lastManualRotationTime;

        /// <summary>
        /// 平滑相机转速的角度范围
        /// </summary>
        [SerializeField, Range(0f, 90f)] 
        private float m_alignSmoothRange = 45f;

        /// <summary>
        /// 相机组件
        /// </summary>
        private Camera m_regularCamera;

        /// <summary>
        /// 相机遮罩
        /// </summary>
        [SerializeField] 
        private LayerMask m_obstructionMask = -1;

#endregion
    }
}
#endif

#if CustomGravity
namespace Assets.Scripts.Movement
{
    /// <summary>
    /// 轨道摄像机
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class OrbitCamera : MonoBehaviour
    {
#region 方法

        /// <summary>
        /// 初始化
        /// </summary>
        private void Awake()
        {
            m_regularCamera = GetComponent<Camera>();
            m_focusPoint = m_focus.position;
            transform.localRotation = m_orbitRotation = Quaternion.Euler(m_orbitAngles);
        }

        /// <summary>
        /// 相机跟随小球
        /// </summary>
        private void LateUpdate()
        {
            // 计算新的对齐方式
            m_gravityAlignment =
                Quaternion.FromToRotation(m_gravityAlignment * Vector3.up, CustomGravity.GetUpAxis(m_focusPoint)) *
                m_gravityAlignment;

            UpdateFocusPoint();
            // 旋转相机
            if (ManualRotation() || AutomaticRotation())
            {
                ConstrainAngles();
                m_orbitRotation = Quaternion.Euler(m_orbitAngles);
            }

            Quaternion lookRotation = m_gravityAlignment * m_orbitRotation;
            Vector3 lookDirection = transform.forward;
            Vector3 lookPosition = m_focusPoint - lookDirection * m_distance;

            // 根据小球的实际位置来计算盒投影的方向和距离
            Vector3 rectOffset = lookDirection * m_regularCamera.nearClipPlane;
            Vector3 rectPosition = lookPosition + rectOffset;
            Vector3 castFrom = m_focus.position;
            Vector3 castLine = rectPosition - castFrom;
            float castDistance = castLine.magnitude;
            Vector3 castDirection = castLine / castDistance;

            // 检测相机是否与墙体发生碰撞，调整摄像机的位置
            if (Physics.BoxCast(castFrom, CameraHalfExtends, castDirection, out RaycastHit hit,
                    lookRotation, castDistance, m_obstructionMask))
            {
                rectPosition = castFrom + castDirection * hit.distance;
                lookPosition = rectPosition - rectOffset;
            }

            // 设置相机的方位
            transform.SetPositionAndRotation(lookPosition, lookRotation);
        }

        /// <summary>
        /// 约束检查器配置数据时的合法性
        /// </summary>
        private void OnValidate()
        {
            if (m_maxVerticalAngle < m_minVerticalAngle)
            {
                m_maxVerticalAngle = m_minVerticalAngle;
            }
        }

        /// <summary>
        /// 更新目标焦点的位置
        /// </summary>
        private void UpdateFocusPoint()
        {
            m_previousFocusPoint = m_focusPoint;
            Vector3 targetPoint = m_focus.position;
            if (m_focusRadius > 0f)
            {
                float distance = Vector3.Distance(targetPoint, m_focusPoint);
                float t = 1f;
                // 将焦点向目标中心拉近
                if (distance > 0.01f && m_focusCentering > 0f)
                {
                    t = Mathf.Pow(1f - m_focusCentering, Time.unscaledDeltaTime);
                }
                // 当相机距离小球距离大于聚焦半径时
                if (distance > m_focusRadius)
                {
                    // 控制相机与小球的距离始终保持在聚焦半径左右
                    t = Mathf.Min(t, m_focusRadius / distance);
                }

                m_focusPoint = Vector3.Lerp(targetPoint, m_focusPoint, t);
            }
            else
            {
                m_focusPoint = targetPoint;
            }
        }

        /// <summary>
        /// 手动控制相机
        /// </summary>
        /// <returns>是否手动操作了相机</returns>x
        private bool ManualRotation()
        {
            Vector2 input = new Vector2(Input.GetAxis("Vertical Camera"), Input.GetAxis("Horizontal Camera"));
            const float e = 0.001f;
            if (input.x < -e || input.x > e || input.y < -e || input.y > e)
            {
                m_orbitAngles += m_rotationSpeed * Time.unscaledDeltaTime * input;
                m_lastManualRotationTime = Time.unscaledTime;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 相机自动进行对齐
        /// </summary>
        /// <returns></returns>
        private bool AutomaticRotation()
        {
            if (Time.unscaledTime - m_lastManualRotationTime < m_alignDelay)
            {
                return false;
            }

            // 反重力对齐
            Vector3 alignedDelta = Quaternion.Inverse(m_gravityAlignment) * (m_focusPoint - m_previousFocusPoint);
            // 计算当前帧焦点的运动矢量
            Vector2 movement = new Vector2(
                alignedDelta.x,
                alignedDelta.z
            );
            float movementDeltaSqr = movement.sqrMagnitude;
            if (movementDeltaSqr < 0.0001f)
            {
                return false;
            }

            // 获取水平航向角
            float headingAngle = GetAngle(movement / Mathf.Sqrt(movementDeltaSqr));
            float deltaAbs = Mathf.Abs(Mathf.DeltaAngle(m_orbitAngles.y, headingAngle));
            // 当前帧的旋转角度的变化
            float rotationChange = m_rotationSpeed * Mathf.Min(Time.unscaledDeltaTime, movementDeltaSqr);
            // 如果角度变换在平滑范围内，则进行平滑过渡
            if (deltaAbs < m_alignSmoothRange)
            {
                rotationChange *= deltaAbs / m_alignSmoothRange;
            }
            else if (180f - deltaAbs < m_alignSmoothRange)
            {
                rotationChange *= (180f - deltaAbs) / m_alignSmoothRange;
            }
            m_orbitAngles.y = Mathf.MoveTowardsAngle(m_orbitAngles.y, headingAngle, rotationChange);

            return true;
        }

        /// <summary>
        /// 约束相机的旋转角度
        /// </summary>
        private void ConstrainAngles()
        {
            // 约束相机沿X轴的转动幅度
            m_orbitAngles.x = Mathf.Clamp(m_orbitAngles.x, m_minVerticalAngle, m_maxVerticalAngle);

            // 约束相机沿Y轴旋转时数值的范围
            if (m_orbitAngles.y < 0f)
            {
                m_orbitAngles.y += 360f;
            }
            else if (m_orbitAngles.y >= 360f)
            {
                m_orbitAngles.y -= 360f;
            }
        }

        /// <summary>
        /// 根据方向计算水平角度
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        private static float GetAngle(Vector2 direction)
        {
            float angle = Mathf.Acos(direction.y) * Mathf.Rad2Deg;
            return direction.x < 0f ? 360f - angle : angle;
        }

#endregion

#region 属性

        /// <summary>
        /// 相机近裁剪平面的半个大小
        /// </summary>
        private Vector3 CameraHalfExtends
        {
            get
            {
                Vector3 halfExtends;
                halfExtends.y = m_regularCamera.nearClipPlane *
                                Mathf.Tan(0.5f * Mathf.Deg2Rad * m_regularCamera.fieldOfView);
                halfExtends.x = halfExtends.y * m_regularCamera.aspect;
                halfExtends.z = 0f;
                return halfExtends;
            }
        }

#endregion

#region 依赖的字段

        /// <summary>
        /// 相机聚焦的对象
        /// </summary>
        [SerializeField]
        private Transform m_focus = default;

        /// <summary>
        /// 轨道与小球之间的距离
        /// </summary>
        [SerializeField, Range(1f, 20f)]
        private float m_distance = 5f;

        /// <summary>
        /// 焦点半径，只有焦点与目标焦点位置相差较大时才进行相机移动
        /// </summary>
        [SerializeField, Min(0f)]
        private float m_focusRadius = 1f;

        /// <summary>
        /// 当前焦点位置
        /// </summary>
        private Vector3 m_focusPoint;

        /// <summary>
        /// 先前焦点的位置
        /// </summary>
        private Vector3 m_previousFocusPoint;

        /// <summary>
        /// 焦点居中系数，用于控制焦点回到小球位置的速度
        /// </summary>
        [SerializeField, Range(0f, 1f)]
        private float m_focusCentering = 0.5f;

        /// <summary>
        /// 相机的环绕角度
        /// </summary>
        private Vector2 m_orbitAngles = new Vector2(45f, 0f);

        /// <summary>
        /// 相机的转动速度
        /// </summary>
        [SerializeField, Range(1f, 360f)]
        private float m_rotationSpeed = 90f;

        /// <summary>
        /// 相机的最小垂直角度
        /// </summary>
        [SerializeField, Range(-89f, 89f)]
        private float m_minVerticalAngle = -30f;

        /// <summary>
        /// 相机的最大垂直角度
        /// </summary>
        [SerializeField, Range(-89f, 89f)]
        private float m_maxVerticalAngle = 60f;

        /// <summary>
        /// 相机自动对齐的延迟
        /// </summary>
        [SerializeField, Min(0f)]
        private float m_alignDelay = 5f;

        /// <summary>
        /// 最后一次手动旋转相机的时间
        /// </summary>
        private float m_lastManualRotationTime;

        /// <summary>
        /// 平滑相机转速的角度范围
        /// </summary>
        [SerializeField, Range(0f, 90f)]
        private float m_alignSmoothRange = 45f;

        /// <summary>
        /// 相机组件
        /// </summary>
        private Camera m_regularCamera;

        /// <summary>
        /// 相机遮罩
        /// </summary>
        [SerializeField]
        private LayerMask m_obstructionMask = -1;

        /// <summary>
        /// 重力对齐四元数
        /// </summary>
        private Quaternion m_gravityAlignment = Quaternion.identity;

        /// <summary>
        /// 轨道旋转四元数
        /// </summary>
        private Quaternion m_orbitRotation;

#endregion
    }
}
#endif

#if ComplexGravity
namespace Assets.Scripts.Movement
{
    /// <summary>
    /// 轨道摄像机
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class OrbitCamera : MonoBehaviour
    {
        #region 方法

        /// <summary>
        /// 初始化
        /// </summary>
        private void Awake()
        {
            m_regularCamera = GetComponent<Camera>();
            m_focusPoint = m_focus.position;
            transform.localRotation = m_orbitRotation = Quaternion.Euler(m_orbitAngles);
        }

        /// <summary>
        /// 相机跟随小球
        /// </summary>
        private void LateUpdate()
        {
            UpdateGravityAlignment();
            UpdateFocusPoint();
            // 旋转相机
            if (ManualRotation() || AutomaticRotation())
            {
                ConstrainAngles();
                m_orbitRotation = Quaternion.Euler(m_orbitAngles);
            }

            Quaternion lookRotation = m_gravityAlignment * m_orbitRotation;
            Vector3 lookDirection = transform.forward;
            Vector3 lookPosition = m_focusPoint - lookDirection * m_distance;

            // 根据小球的实际位置来计算盒投影的方向和距离
            Vector3 rectOffset = lookDirection * m_regularCamera.nearClipPlane;
            Vector3 rectPosition = lookPosition + rectOffset;
            Vector3 castFrom = m_focus.position;
            Vector3 castLine = rectPosition - castFrom;
            float castDistance = castLine.magnitude;
            Vector3 castDirection = castLine / castDistance;

            // 检测相机是否与墙体发生碰撞，调整摄像机的位置
            if (Physics.BoxCast(castFrom, CameraHalfExtends, castDirection, out RaycastHit hit,
                    lookRotation, castDistance, m_obstructionMask))
            {
                rectPosition = castFrom + castDirection * hit.distance;
                lookPosition = rectPosition - rectOffset;
            }

            // 设置相机的方位
            transform.SetPositionAndRotation(lookPosition, lookRotation);
        }

        /// <summary>
        /// 更新重力的对齐方式
        /// </summary>
        private void UpdateGravityAlignment()
        {
            Vector3 fromUp = m_gravityAlignment * Vector3.up;
            Vector3 toUp = CustomGravity.GetUpAxis(m_focusPoint);

            float dot = Mathf.Clamp(Vector3.Dot(fromUp, toUp), -1f, 1f);
            float angle = Mathf.Acos(dot) * Mathf.Rad2Deg;
            float maxAngle = m_upAlignmentSpeed * Time.deltaTime;

            Quaternion newAlignment =
                Quaternion.FromToRotation(fromUp, toUp) * m_gravityAlignment;
            // 如果角度足够小，便像往常一样直接使用新的对准
            if (angle <= maxAngle)
            {
                m_gravityAlignment = newAlignment;
            }
            // 否则就在当前和期望的旋转之间进行插值
            else
            {
                m_gravityAlignment = Quaternion.SlerpUnclamped(
                    m_gravityAlignment, newAlignment, maxAngle / angle);
            }
        }

        /// <summary>
        /// 约束检查器配置数据时的合法性
        /// </summary>
        private void OnValidate()
        {
            if (m_maxVerticalAngle < m_minVerticalAngle)
            {
                m_maxVerticalAngle = m_minVerticalAngle;
            }
        }

        /// <summary>
        /// 更新目标焦点的位置
        /// </summary>
        private void UpdateFocusPoint()
        {
            m_previousFocusPoint = m_focusPoint;
            Vector3 targetPoint = m_focus.position;
            if (m_focusRadius > 0f)
            {
                float distance = Vector3.Distance(targetPoint, m_focusPoint);
                float t = 1f;
                // 将焦点向目标中心拉近
                if (distance > 0.01f && m_focusCentering > 0f)
                {
                    t = Mathf.Pow(1f - m_focusCentering, Time.unscaledDeltaTime);
                }
                // 当相机距离小球距离大于聚焦半径时
                if (distance > m_focusRadius)
                {
                    // 控制相机与小球的距离始终保持在聚焦半径左右
                    t = Mathf.Min(t, m_focusRadius / distance);
                }

                m_focusPoint = Vector3.Lerp(targetPoint, m_focusPoint, t);
            }
            else
            {
                m_focusPoint = targetPoint;
            }
        }

        /// <summary>
        /// 手动控制相机
        /// </summary>
        /// <returns>是否手动操作了相机</returns>x
        private bool ManualRotation()
        {
            Vector2 input = new Vector2(Input.GetAxis("Vertical Camera"), Input.GetAxis("Horizontal Camera"));
            const float e = 0.001f;
            if (input.x < -e || input.x > e || input.y < -e || input.y > e)
            {
                m_orbitAngles += m_rotationSpeed * Time.unscaledDeltaTime * input;
                m_lastManualRotationTime = Time.unscaledTime;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 相机自动进行对齐
        /// </summary>
        /// <returns></returns>
        private bool AutomaticRotation()
        {
            if (Time.unscaledTime - m_lastManualRotationTime < m_alignDelay)
            {
                return false;
            }

            // 反重力对齐
            Vector3 alignedDelta = Quaternion.Inverse(m_gravityAlignment) * (m_focusPoint - m_previousFocusPoint);
            // 计算当前帧焦点的运动矢量
            Vector2 movement = new Vector2(
                alignedDelta.x,
                alignedDelta.z
            );
            float movementDeltaSqr = movement.sqrMagnitude;
            if (movementDeltaSqr < 0.0001f)
            {
                return false;
            }

            // 获取水平航向角
            float headingAngle = GetAngle(movement / Mathf.Sqrt(movementDeltaSqr));
            float deltaAbs = Mathf.Abs(Mathf.DeltaAngle(m_orbitAngles.y, headingAngle));
            // 当前帧的旋转角度的变化
            float rotationChange = m_rotationSpeed * Mathf.Min(Time.unscaledDeltaTime, movementDeltaSqr);
            // 如果角度变换在平滑范围内，则进行平滑过渡
            if (deltaAbs < m_alignSmoothRange)
            {
                rotationChange *= deltaAbs / m_alignSmoothRange;
            }
            else if (180f - deltaAbs < m_alignSmoothRange)
            {
                rotationChange *= (180f - deltaAbs) / m_alignSmoothRange;
            }
            m_orbitAngles.y = Mathf.MoveTowardsAngle(m_orbitAngles.y, headingAngle, rotationChange);

            return true;
        }

        /// <summary>
        /// 约束相机的旋转角度
        /// </summary>
        private void ConstrainAngles()
        {
            // 约束相机沿X轴的转动幅度
            m_orbitAngles.x = Mathf.Clamp(m_orbitAngles.x, m_minVerticalAngle, m_maxVerticalAngle);

            // 约束相机沿Y轴旋转时数值的范围
            if (m_orbitAngles.y < 0f)
            {
                m_orbitAngles.y += 360f;
            }
            else if (m_orbitAngles.y >= 360f)
            {
                m_orbitAngles.y -= 360f;
            }
        }

        /// <summary>
        /// 根据方向计算水平角度
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        private static float GetAngle(Vector2 direction)
        {
            float angle = Mathf.Acos(direction.y) * Mathf.Rad2Deg;
            return direction.x < 0f ? 360f - angle : angle;
        }

        #endregion

        #region 属性

        /// <summary>
        /// 相机近裁剪平面的半个大小
        /// </summary>
        private Vector3 CameraHalfExtends
        {
            get
            {
                Vector3 halfExtends;
                halfExtends.y = m_regularCamera.nearClipPlane *
                                Mathf.Tan(0.5f * Mathf.Deg2Rad * m_regularCamera.fieldOfView);
                halfExtends.x = halfExtends.y * m_regularCamera.aspect;
                halfExtends.z = 0f;
                return halfExtends;
            }
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 相机聚焦的对象
        /// </summary>
        [SerializeField]
        private Transform m_focus = default;

        /// <summary>
        /// 轨道与小球之间的距离
        /// </summary>
        [SerializeField, Range(1f, 20f)]
        private float m_distance = 5f;

        /// <summary>
        /// 焦点半径，只有焦点与目标焦点位置相差较大时才进行相机移动
        /// </summary>
        [SerializeField, Min(0f)]
        private float m_focusRadius = 1f;

        /// <summary>
        /// 当前焦点位置
        /// </summary>
        private Vector3 m_focusPoint;

        /// <summary>
        /// 先前焦点的位置
        /// </summary>
        private Vector3 m_previousFocusPoint;

        /// <summary>
        /// 焦点居中系数，用于控制焦点回到小球位置的速度
        /// </summary>
        [SerializeField, Range(0f, 1f)]
        private float m_focusCentering = 0.5f;

        /// <summary>
        /// 相机的环绕角度
        /// </summary>
        private Vector2 m_orbitAngles = new Vector2(45f, 0f);

        /// <summary>
        /// 相机的转动速度
        /// </summary>
        [SerializeField, Range(1f, 360f)]
        private float m_rotationSpeed = 90f;

        /// <summary>
        /// 相机的最小垂直角度
        /// </summary>
        [SerializeField, Range(-89f, 89f)]
        private float m_minVerticalAngle = -30f;

        /// <summary>
        /// 相机的最大垂直角度
        /// </summary>
        [SerializeField, Range(-89f, 89f)]
        private float m_maxVerticalAngle = 60f;

        /// <summary>
        /// 相机自动对齐的延迟
        /// </summary>
        [SerializeField, Min(0f)]
        private float m_alignDelay = 5f;

        /// <summary>
        /// 最后一次手动旋转相机的时间
        /// </summary>
        private float m_lastManualRotationTime;

        /// <summary>
        /// 平滑相机转速的角度范围
        /// </summary>
        [SerializeField, Range(0f, 90f)]
        private float m_alignSmoothRange = 45f;

        /// <summary>
        /// 相机组件
        /// </summary>
        private Camera m_regularCamera;

        /// <summary>
        /// 相机遮罩
        /// </summary>
        [SerializeField]
        private LayerMask m_obstructionMask = -1;

        /// <summary>
        /// 重力对齐四元数
        /// </summary>
        private Quaternion m_gravityAlignment = Quaternion.identity;

        /// <summary>
        /// 轨道旋转四元数
        /// </summary>
        private Quaternion m_orbitRotation;

        /// <summary>
        /// 相机的对齐速度
        /// </summary>
        [SerializeField, Min(0f)] 
        private float m_upAlignmentSpeed = 360f;

        #endregion
    }
}
#endif