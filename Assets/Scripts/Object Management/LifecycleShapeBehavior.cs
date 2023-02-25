using UnityEngine;

namespace Assets.Scripts.Object_Management
{
    /// <summary>
    /// 生命周期行为
    /// </summary>
    public sealed class LifecycleShapeBehavior : ShapeBehavior
    {
        #region 方法

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="duration"></param>
        public void Initialize(Shape shape, float growingDuration, float adultDuration, float dyingDuration)
        {
            this.m_adultDuration = adultDuration;
            this.m_dyingDuration = dyingDuration;
            m_dyingAge = growingDuration + m_adultDuration;

            if (growingDuration > 0f)
            {
                shape.AddBehavior<GrowingShapeBehavior>().Initialize(shape, growingDuration);
            }
        }

        /// <summary>
        /// 更新逻辑
        /// </summary>
        /// <param name="shape"></param>
        /// <returns>是否不需要显式进行回收</returns>
        public override bool GameUpdate(Shape shape)
        {
            if (shape.Age >= m_dyingAge)
            {
                if (m_dyingDuration <= 0f)
                {
                    shape.Die();
                    return true;
                }

                if (!shape.IsMarkedAsDying)
                {
                    shape.AddBehavior<DyingShapeBehavior>().Initialize(shape, m_dyingDuration + m_dyingAge - shape.Age);
                }
                return false;
            }

            return true;
        }

        /// <summary>
        /// 存档
        /// </summary>
        /// <param name="writer"></param>
        public override void Save(GameDataWriter writer)
        {
            writer.Write(m_adultDuration);
            writer.Write(m_dyingDuration);
            writer.Write(m_dyingAge);
        }

        /// <summary>
        /// 读档
        /// </summary>
        /// <param name="reader"></param>
        public override void Load(GameDataReader reader)
        {
            m_adultDuration = reader.ReadFloat();
            m_dyingDuration = reader.ReadFloat();
            m_dyingAge = reader.ReadFloat();
        }

        /// <summary>
        /// 回收对象
        /// </summary>
        public override void Recycle()
        {
            ShapeBehaviorPool<LifecycleShapeBehavior>.Reclaim(this);
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
                return ShapeBehaviorType.Lifecycle;
            }
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 成年时间
        /// </summary>
        private float m_adultDuration;

        /// <summary>
        /// 死亡持续时间
        /// </summary>
        private float m_dyingDuration;

        /// <summary>
        /// 死亡时的时间
        /// </summary>
        private float m_dyingAge;

        #endregion
    }
}