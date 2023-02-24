using UnityEngine;

namespace Assets.Scripts.Object_Management
{
    /// <summary>
    /// 卫星行为
    /// </summary>
    public class SatelliteShapeBehavior : ShapeBehavior
    {
        #region 方法

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="focalShape"></param>
        /// <param name="radius"></param>
        /// <param name="frequency"></param>
        public void Initialize(Shape shape, Shape focalShape, float radius, float frequency)
        {
            this.m_focalShape = focalShape;
            this.m_frequency = frequency;
            Vector3 orbitAxis = Random.onUnitSphere;
            do
            {
                m_cosOffset = Vector3.Cross(orbitAxis, Random.onUnitSphere).normalized;
            } while (m_cosOffset.sqrMagnitude < 0.1f);
            m_sinOffset = Vector3.Cross(m_cosOffset, orbitAxis);
            m_cosOffset *= radius;
            m_sinOffset *= radius;

            shape.AddBehavior<RotationShapeBehavior>().AngularVelocity = -360f * frequency * 
                shape.transform.InverseTransformDirection(orbitAxis);

            // 确保卫星的初始位置是有效的
            GameUpdate(shape);
            m_previousPosition = shape.transform.localPosition;
        }

        /// <summary>
        /// 更新逻辑
        /// </summary>
        public override bool GameUpdate(Shape shape)
        {
            if (m_focalShape.IsValid)
            {
                float t = 2f * Mathf.PI * m_frequency * shape.Age;
                m_previousPosition = shape.transform.localPosition;
                shape.transform.localPosition = m_focalShape.Shape.transform.localPosition + m_cosOffset * Mathf.Cos(t) +
                                                m_sinOffset * Mathf.Sin(t);

                return true;
            }

            // 当行星脱离运动时，给它加上一个运动行为
            shape.AddBehavior<MovementShapeBehavior>().Velocity = 
                (shape.transform.localPosition - m_previousPosition) / Time.deltaTime;

            return false;
        }

        /// <summary>
        /// 存档
        /// </summary>
        /// <param name="writer"></param>
        public override void Save(GameDataWriter writer)
        {
            writer.Write(m_focalShape);
            writer.Write(m_frequency);
            writer.Write(m_cosOffset);
            writer.Write(m_sinOffset);
            writer.Write(m_previousPosition);
        }

        /// <summary>
        /// 读档
        /// </summary>
        /// <param name="reader"></param>
        public override void Load(GameDataReader reader)
        {
            m_focalShape = reader.ReadShapeInstance();
            m_frequency = reader.ReadFloat();
            m_cosOffset = reader.ReadVector3();
            m_sinOffset = reader.ReadVector3();
            m_previousPosition = reader.ReadVector3();
        }

        /// <summary>
        /// 回收对象
        /// </summary>
        public override void Recycle()
        {
            ShapeBehaviorPool<SatelliteShapeBehavior>.Reclaim(this);
        }

        /// <summary>
        /// 解析形状实例
        /// </summary>
        public override void ResolveShapeInstances()
        {
            m_focalShape.Resolve();
        }

        #endregion

        #region 属性

        /// <summary>
        /// 行为类型
        /// </summary>
        public override ShapeBehaviorType BehaviorType
        {
            get
            {
                return ShapeBehaviorType.Satellite;
            }
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 焦点形状对象
        /// </summary>
        private ShapeInstance m_focalShape;

        /// <summary>
        /// 频率
        /// </summary>
        private float m_frequency;

        /// <summary>
        /// Cos偏移量
        /// </summary>
        private Vector3 m_cosOffset;

        /// <summary>
        /// Sin偏移量
        /// </summary>
        private Vector3 m_sinOffset;

        /// <summary>
        /// 上一帧时的位置
        /// </summary>
        private Vector3 m_previousPosition;

        #endregion
    }
}
