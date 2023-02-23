using UnityEngine;

namespace Assets.Scripts.Object_Management
{
    /// <summary>
    /// 形状移动行为
    /// </summary>
    public sealed class RotationShapeBehavior : ShapeBehavior
    {
        #region 方法

        /// <summary>
        /// 更新逻辑
        /// </summary>
        /// <param name="shape"></param>
        public override void GameUpdate(Shape shape)
        {
            shape.transform.Rotate(AngularVelocity * Time.deltaTime);
        }

        /// <summary>
        /// 存档
        /// </summary>
        /// <param name="writer"></param>
        public override void Save(GameDataWriter writer)
        {
            writer.Write(AngularVelocity);
        }

        /// <summary>
        /// 读档
        /// </summary>
        /// <param name="reader"></param>
        public override void Load(GameDataReader reader)
        {
            AngularVelocity = reader.ReadVector3();
        }

        /// <summary>
        /// 回收对象
        /// </summary>
        public override void Recycle()
        {
            ShapeBehaviorPool<RotationShapeBehavior>.Reclaim(this);
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
                return ShapeBehaviorType.Rotation;
            }
        }

        #endregion
    }
}