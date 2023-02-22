using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Scripts.Object_Management
{
    /// <summary>
    /// 游戏对象生成区域
    /// </summary>
    public abstract class SpawnZone : PersistableObject
    {
        #region 方法

        /// <summary>
        /// 生成游戏对象
        /// </summary>
        /// <param name="shape"></param>
        public virtual void ConfigureSpawn(Shape shape)
        {
            Transform t = shape.transform;
            t.localPosition = SpawnPoint;
            t.localRotation = Random.rotation;
            t.localScale = Vector3.one * m_spawnConfig.m_scale.RandomValueInRange;
            shape.SetColor(m_spawnConfig.m_color.RandomInRange);
            shape.AngularVelocity = Random.onUnitSphere * m_spawnConfig.m_angularSpeed.RandomValueInRange;

            // 判断生成游戏对象的移动方向
            Vector3 direction;
            switch (m_spawnConfig.m_movementDirection)
            {
                case SpawnConfiguration.MovementDirection.Upward:
                    direction = transform.up;
                    break;
                case SpawnConfiguration.MovementDirection.Outward:
                    direction = (t.localPosition - transform.position).normalized;
                    break;
                case SpawnConfiguration.MovementDirection.Random:
                    direction = Random.onUnitSphere;
                    break;
                default:
                    direction = transform.forward;
                    break;
            }
            
            shape.Velocity = direction * m_spawnConfig.m_speed.RandomValueInRange;
        }

        #endregion

        #region 属性

        /// <summary>
        /// 获取一个任意的生成点
        /// </summary>
        public abstract Vector3 SpawnPoint { get; }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 生成配置信息
        /// </summary>
        [SerializeField]
        private SpawnConfiguration m_spawnConfig;

        #endregion

        #region 枚举

        /// <summary>
        /// 生成配置
        /// </summary>
        [Serializable]
        public struct SpawnConfiguration
        {
            /// <summary>
            /// 运动方向
            /// </summary>
            public MovementDirection m_movementDirection;

            /// <summary>
            /// 游戏对象的生成速度
            /// </summary>
            public FloatRange m_speed;

            /// <summary>
            /// 游戏对象的角速度
            /// </summary>
            public FloatRange m_angularSpeed;

            /// <summary>
            /// 游戏对象的比例
            /// </summary>
            public FloatRange m_scale;

            /// <summary>
            /// HSV颜色范围
            /// </summary>
            public ColorRangeHSV m_color;

            /// <summary>
            /// 生成游戏对象的运动方向
            /// </summary>
            public enum MovementDirection
            {
                /// <summary>
                /// 向上
                /// </summary>
                Forward,

                /// <summary>
                /// 向前
                /// </summary>
                Upward,

                /// <summary>
                /// 向外
                /// </summary>
                Outward,

                /// <summary>
                /// 随机
                /// </summary>
                Random,
            }
        }

        #endregion

    }
}
