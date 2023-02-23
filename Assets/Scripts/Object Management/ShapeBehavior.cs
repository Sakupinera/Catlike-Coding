using UnityEngine;

namespace Assets.Scripts.Object_Management
{
    /// <summary>
    /// 形状的行为
    /// </summary>
    public abstract class ShapeBehavior
#if UNITY_EDITOR
        : ScriptableObject
#endif
    {
        #region 方法

        /// <summary>
        /// 更新逻辑
        /// </summary>
        public abstract void GameUpdate(Shape shape);

        /// <summary>
        /// 存档
        /// </summary>
        /// <param name="writer"></param>
        public abstract void Save(GameDataWriter writer);

        /// <summary>
        /// 读档
        /// </summary>
        /// <param name="reader"></param>
        public abstract void Load(GameDataReader reader);

        /// <summary>
        /// 回收对象
        /// </summary>
        public abstract void Recycle();

#if UNITY_EDITOR
        /// <summary>
        /// 组件被重新加载时，重新回收对象（热重载）
        /// </summary>
        private void OnEnable()
        {
            if (IsReclaimed)
            {
                Recycle();
            }
        }
#endif

#endregion

        #region 属性

        /// <summary>
        /// 行为类型
        /// </summary>
        public abstract ShapeBehaviorType BehaviorType { get; }

#if UNITY_EDITOR
        /// <summary>
        /// 是否被回收
        /// </summary>
        public bool IsReclaimed { get; set; }
#endif

#endregion

    }

    /// <summary>
    /// 形状行为类型
    /// </summary>
    public enum ShapeBehaviorType
    {
        /// <summary>
        /// 运动
        /// </summary>
        Movement,
        
        /// <summary>
        /// 旋转
        /// </summary>
        Rotation,

        /// <summary>
        /// 摆动
        /// </summary>
        Oscillation
    }

    /// <summary>
    /// 形状行为类型扩展方法
    /// </summary>
    public static class ShapeBehaviorTypeMethods
    {
        /// <summary>
        /// 根据行为类型添加组件
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static ShapeBehavior GetInstance(this ShapeBehaviorType type)
        {
            switch (type)
            {
                case ShapeBehaviorType.Movement:
                    return ShapeBehaviorPool<MovementShapeBehavior>.Get();
                case ShapeBehaviorType.Rotation:
                    return ShapeBehaviorPool<RotationShapeBehavior>.Get();
                case ShapeBehaviorType.Oscillation:
                    return ShapeBehaviorPool<OscillationShapeBehavior>.Get();
            }

            Debug.LogError("Forgot to support " + type);
            return null;
        }
    }
}
