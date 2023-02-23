using UnityEngine;

namespace Assets.Scripts.Object_Management
{
    /// <summary>
    /// 形状摆动行为
    /// </summary>
    public class OscillationShapeBehavior : ShapeBehavior
    {
        #region 方法

        /// <summary>
        /// 更新逻辑
        /// </summary>
        /// <param name="shape"></param>
        public override void GameUpdate(Shape shape)
        {
            float oscillation = Mathf.Sin(2f * Mathf.PI * Frequency * shape.Age);
            shape.transform.localPosition += (oscillation - m_previousOscillation) * Offset;
            m_previousOscillation = oscillation;
        }

        /// <summary>
        /// 存档
        /// </summary>
        /// <param name="writer"></param>
        public override void Save(GameDataWriter writer)
        {
            writer.Write(Offset);
            writer.Write(Frequency);
            writer.Write(m_previousOscillation);
        }

        /// <summary>
        /// 读档
        /// </summary>
        /// <param name="reader"></param>
        public override void Load(GameDataReader reader)
        {
            Offset = reader.ReadVector3();
            Frequency = reader.ReadFloat();
            m_previousOscillation = reader.ReadFloat();
        }

        /// <summary>
        /// 回收对象
        /// </summary>
        public override void Recycle()
        {
            m_previousOscillation = 0f;
            ShapeBehaviorPool<OscillationShapeBehavior>.Reclaim(this);
        }

        #endregion

        #region 属性

        /// <summary>
        /// 角速度
        /// </summary>
        public Vector3 AngularVelocity { get; set; }

        /// <summary>
        /// 行为类型
        /// </summary>
        public override ShapeBehaviorType BehaviorType
        {
            get
            {
                return ShapeBehaviorType.Oscillation;
            }
        }

        /// <summary>
        /// 偏移量
        /// </summary>
        public Vector3 Offset { get; set; }

        /// <summary>
        /// 频率
        /// </summary>
        public float Frequency { get; set; }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 之前的震荡值
        /// </summary>
        private float m_previousOscillation;

        #endregion
    }
}
