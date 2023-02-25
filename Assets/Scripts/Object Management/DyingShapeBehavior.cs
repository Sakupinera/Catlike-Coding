using UnityEngine;

namespace Assets.Scripts.Object_Management
{
    /// <summary>
    /// 形状生长行为
    /// </summary>
    public sealed class DyingShapeBehavior : ShapeBehavior
    {
        #region 方法

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="duration"></param>
        public void Initialize(Shape shape, float duration)
        {
            m_originalScale = shape.transform.localScale;
            this.m_duration = duration;
            m_dyingAge = shape.Age;

            shape.MarkAsDying();
        }

        /// <summary>
        /// 更新逻辑
        /// </summary>
        /// <param name="shape"></param>
        public override bool GameUpdate(Shape shape)
        {
            float dyingDuration = shape.Age - m_dyingAge;
            if (dyingDuration < m_duration)
            {
                float s = 1f - dyingDuration / m_duration;
                s = (3f - 2f * s) * s * s;
                shape.transform.localScale = s * m_originalScale;
                return true;
            }

            shape.Die();
            return true;
        }

        /// <summary>
        /// 存档
        /// </summary>
        /// <param name="writer"></param>
        public override void Save(GameDataWriter writer)
        {
            writer.Write(m_originalScale);
            writer.Write(m_duration);
            writer.Write(m_dyingAge);
        }

        /// <summary>
        /// 读档
        /// </summary>
        /// <param name="reader"></param>
        public override void Load(GameDataReader reader)
        {
            m_originalScale = reader.ReadVector3();
            m_duration = reader.ReadFloat();
            m_dyingAge = reader.ReadFloat();
        }

        /// <summary>
        /// 回收对象
        /// </summary>
        public override void Recycle()
        {
            ShapeBehaviorPool<DyingShapeBehavior>.Reclaim(this);
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
                return ShapeBehaviorType.Dying;
            }
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 形状原始大小
        /// </summary>
        private Vector3 m_originalScale;

        /// <summary>
        /// 变回原始大小所需时间
        /// </summary>
        private float m_duration;

        /// <summary>
        /// 死亡时的时间
        /// </summary>
        private float m_dyingAge;

        #endregion
    }
}