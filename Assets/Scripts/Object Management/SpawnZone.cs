using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Scripts.Object_Management
{
    /// <summary>
    /// 游戏对象生成区域
    /// </summary>
    public abstract class SpawnZone : GameLevelObject
    {
        #region 方法

        /// <summary>
        /// 对象更新逻辑
        /// </summary>
        public override void GameUpdate()
        {
            m_spawnProgress += Time.deltaTime * m_spawnSpeed;
            while (m_spawnProgress >= 1f)
            {
                m_spawnProgress -= 1f;
                SpawnShapes();
            }
        }

        /// <summary>
        /// 存档
        /// </summary>
        /// <param name="writer"></param>
        public override void Save(GameDataWriter writer)
        {
            writer.Write(m_spawnProgress);
        }

        /// <summary>
        /// 读档
        /// </summary>
        /// <param name="reader"></param>
        public override void Load(GameDataReader reader)
        {
            m_spawnProgress = reader.ReadFloat();
        }

        /// <summary>
        /// 生成游戏对象
        /// </summary>
        /// <returns></returns>
        public virtual void SpawnShapes()
        {
            int factoryIndex = Random.Range(0, m_spawnConfig.m_factories.Length);
            Shape shape = m_spawnConfig.m_factories[factoryIndex].GetRandom();
            shape.gameObject.layer = gameObject.layer;

            Transform t = shape.transform;
            t.localPosition = SpawnPoint;
            t.localRotation = Random.rotation;
            t.localScale = Vector3.one * m_spawnConfig.m_scale.RandomValueInRange;
            SetupColor(shape);

            float angularSpeed = m_spawnConfig.m_angularSpeed.RandomValueInRange;
            if (angularSpeed != 0f)
            {
                var rotation = shape.AddBehavior<RotationShapeBehavior>();
                rotation.AngularVelocity = Random.onUnitSphere * angularSpeed;
            }

            float speed = m_spawnConfig.m_speed.RandomValueInRange;
            if (speed != 0f)
            {
                var movement = shape.AddBehavior<MovementShapeBehavior>();
                movement.Velocity = GetDirectionVector(m_spawnConfig.m_movementDirection, t) * speed;
            }

            Vector3 lifecycleDurations = m_spawnConfig.m_lifecycle.RandomDurations;

            //SetupOscillation(shape);
            int satelliteCount = m_spawnConfig.m_satellite.m_amount.RandomValueInRange;
            for (int i = 0; i < satelliteCount; i++)
            {
                CreateSatelliteFor(shape,
                    m_spawnConfig.m_satellite.m_uniformLifecycles
                        ? lifecycleDurations
                        : m_spawnConfig.m_lifecycle.RandomDurations);
            }

            SetupLifecycle(shape, lifecycleDurations);
        }

        /// <summary>
        /// 判断生成游戏对象的移动方向
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        private Vector3 GetDirectionVector(SpawnConfiguration.MovementDirection direction, Transform t)
        {
            switch (m_spawnConfig.m_movementDirection)
            {
                case SpawnConfiguration.MovementDirection.Upward:
                    return transform.up;
                case SpawnConfiguration.MovementDirection.Outward:
                    return (t.localPosition - transform.position).normalized;
                case SpawnConfiguration.MovementDirection.Random:
                    return Random.onUnitSphere;
                default:
                    return transform.forward;
            }
        }

        /// <summary>
        /// 设置震荡参数
        /// </summary>
        /// <param name="shape"></param>
        private void SetupOscillation(Shape shape)
        {
            float amplitude = m_spawnConfig.m_oscillationAmplitude.RandomValueInRange;
            float frequency = m_spawnConfig.m_oscillationFrequency.RandomValueInRange;
            if (amplitude == 0f || frequency == 0f)
            {
                return;
            }
            var oscillation = shape.AddBehavior<OscillationShapeBehavior>();
            oscillation.Offset = GetDirectionVector(m_spawnConfig.m_oscillationDirection, shape.transform) * amplitude;
            oscillation.Frequency = frequency;
        }

        /// <summary>
        /// 为形状创建一个卫星
        /// </summary>
        /// <param name="focalShape"></param>
        private void CreateSatelliteFor(Shape focalShape, Vector3 lifecycleDurations)
        {
            int factoryIndex = Random.Range(0, m_spawnConfig.m_factories.Length);
            Shape shape = m_spawnConfig.m_factories[factoryIndex].GetRandom();
            shape.gameObject.layer = gameObject.layer;

            Transform t = shape.transform;
            t.localRotation = Random.rotation;
            t.localScale = focalShape.transform.localScale *
                           m_spawnConfig.m_satellite.m_relativeScale.RandomValueInRange;
            SetupColor(shape);
            shape.AddBehavior<SatelliteShapeBehavior>().Initialize(shape, focalShape,
                m_spawnConfig.m_satellite.m_orbitRadius.RandomValueInRange,
                m_spawnConfig.m_satellite.m_orbitFrequency.RandomValueInRange);

            SetupLifecycle(shape, lifecycleDurations);
        }

        /// <summary>
        /// 设置颜色
        /// </summary>
        /// <param name="shape"></param>
        private void SetupColor(Shape shape)
        {
            if (m_spawnConfig.m_uniformColor)
            {
                shape.SetColor(m_spawnConfig.m_color.RandomInRange);
            }
            else
            {
                for (int i = 0; i < shape.ColorCount; i++)
                {
                    shape.SetColor(m_spawnConfig.m_color.RandomInRange, i);
                }
            }
        }

        /// <summary>
        /// 设置生命周期行为
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="durations"></param>
        private void SetupLifecycle(Shape shape, Vector3 durations)
        {
            if (durations.x > 0f)
            {
                if (durations.y > 0f || durations.z > 0f)
                {
                    shape.AddBehavior<LifecycleShapeBehavior>()
                        .Initialize(shape, durations.x, durations.y, durations.z);
                }
                else
                {
                    shape.AddBehavior<GrowingShapeBehavior>().Initialize(shape, durations.x);
                }
            }
            else if (durations.y > 0f)
            {
                shape.AddBehavior<LifecycleShapeBehavior>()
                    .Initialize(shape, durations.x, durations.y, durations.z);
            }
            else if (durations.z > 0f)
            {
                shape.AddBehavior<DyingShapeBehavior>().Initialize(shape, durations.z);
            }
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

        /// <summary>
        /// 生成速度
        /// </summary>
        [SerializeField, Range(0f, 50f)]
        private float m_spawnSpeed;

        /// <summary>
        /// 生成进度
        /// </summary>
        private float m_spawnProgress;

        #endregion

        #region 枚举

        /// <summary>
        /// 生成配置
        /// </summary>
        [Serializable]
        public struct SpawnConfiguration
        {
            /// <summary>
            /// 形状工厂数组
            /// </summary>
            public ShapeFactory[] m_factories;

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
            /// 是否统一颜色
            /// </summary>
            public bool m_uniformColor;

            /// <summary>
            /// 震荡的方向
            /// </summary>
            public MovementDirection m_oscillationDirection;

            /// <summary>
            /// 振幅的范围
            /// </summary>
            public FloatRange m_oscillationAmplitude;

            /// <summary>
            /// 震荡的频率
            /// </summary>
            public FloatRange m_oscillationFrequency;

            /// <summary>
            /// 卫星配置
            /// </summary>
            public SatelliteConfiguration m_satellite;

            /// <summary>
            /// 生命周期配置
            /// </summary>
            public LifecycleConfiguration m_lifecycle;

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

            /// <summary>
            /// 卫星配置结构
            /// </summary>
            [Serializable]
            public struct SatelliteConfiguration
            {
                /// <summary>
                /// 卫星数量
                /// </summary>
                public IntRange m_amount;

                /// <summary>
                /// 相对大小
                /// </summary>
                [FloatRangeSlider(0.1f, 1f)]
                public FloatRange m_relativeScale;

                /// <summary>
                /// 环绕半径
                /// </summary>
                public FloatRange m_orbitRadius;

                /// <summary>
                /// 环绕焦点的的速度
                /// </summary>
                public FloatRange m_orbitFrequency;

                /// <summary>
                /// 是否用统一的生命周期
                /// </summary>
                public bool m_uniformLifecycles;

            }

            /// <summary>
            /// 生命周期配置
            /// </summary>
            [Serializable]
            public struct LifecycleConfiguration
            {
                /// <summary>
                /// 生成的持续时间
                /// </summary>
                [FloatRangeSlider(0f, 2f)]
                public FloatRange m_growingDuration;

                /// <summary>
                /// 成年持续时间
                /// </summary>
                [FloatRangeSlider(0f, 100f)]
                public FloatRange m_adultDuration;

                /// <summary>
                /// 消失的持续时间
                /// </summary>
                [FloatRangeSlider(0f, 2f)]
                public FloatRange m_dyingDuration;

                /// <summary>
                /// 随机的持续时间
                /// </summary>
                public Vector3 RandomDurations
                {
                    get
                    {
                        return new Vector3(m_growingDuration.RandomValueInRange,
                            m_adultDuration.RandomValueInRange,
                            m_dyingDuration.RandomValueInRange);
                    }
                }
            }
        }

        #endregion

    }
}
