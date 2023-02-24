using UnityEngine;

namespace Assets.Scripts.Object_Management
{
    /// <summary>
    /// 形状移动行为
    /// </summary>
    public sealed class MovementShapeBehavior : ShapeBehavior
    {
        #region 方法

        /// <summary>
        /// 更新逻辑
        /// </summary>
        /// <param name="shape"></param>
        public override bool GameUpdate(Shape shape)
        {
            shape.transform.localPosition += Velocity * Time.deltaTime;

            return true;
        }

        /// <summary>
        /// 存档
        /// </summary>
        /// <param name="writer"></param>
        public override void Save(GameDataWriter writer)
        {
            writer.Write(Velocity);
        }

        /// <summary>
        /// 读档
        /// </summary>
        /// <param name="reader"></param>
        public override void Load(GameDataReader reader)
        {
            Velocity = reader.ReadVector3();
        }

        /// <summary>
        /// 回收对象
        /// </summary>
        public override void Recycle()
        {
            ShapeBehaviorPool<MovementShapeBehavior>.Reclaim(this);
        }

        #endregion

        #region 属性

        /// <summary>
        /// 速度
        /// </summary>
        public Vector3 Velocity { get; set; }

        /// <summary>
        /// 行为类型
        /// </summary>
        public override ShapeBehaviorType BehaviorType
        {
            get { return ShapeBehaviorType.Movement; }
        }

        #endregion
    }
}
