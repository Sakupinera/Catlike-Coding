#define OrganicVariety

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
using quaternion = Unity.Mathematics.quaternion;
using Random = UnityEngine.Random;

#if Jobs
namespace Assets.Scripts.Basics
{
    /// <summary>
    /// 分形
    /// </summary>
    public class Fractal : MonoBehaviour
    {

#region 方法

        /// <summary>
        /// 组件启用时重新为缓冲区和Native数组赋值
        /// </summary>
        void OnEnable()
        {
            m_parts = new NativeArray<FractalPart>[m_depth];
            m_matrices = new NativeArray<float3x4>[m_depth];
            m_matricesBuffers = new ComputeBuffer[m_depth];
            int stride = 12 * 4;
            for (int i = 0, length = 1; i < m_parts.Length; i++, length *= 5)
            {
                m_parts[i] = new NativeArray<FractalPart>(length, Allocator.Persistent);
                m_matrices[i] = new NativeArray<float3x4>(length, Allocator.Persistent);
                m_matricesBuffers[i] = new ComputeBuffer(length, stride);
            }

            m_parts[0][0] = CreatePart(0);
            for (int li = 1; li < m_parts.Length; li++)
            {
                NativeArray<FractalPart> levelParts = m_parts[li];
                for (int fpi = 0; fpi < levelParts.Length; fpi += 5)
                {
                    for (int ci = 0; ci < 5; ci++)
                    {
                        levelParts[fpi + ci] = CreatePart(ci);
                    }
                }
            }

            s_propertyBlock ??= new MaterialPropertyBlock();
        }

        /// <summary>
        /// 组件被禁用或破坏时释放缓冲区以及Native数组内存
        /// </summary>
        void OnDisable()
        {
            for (int i = 0; i < m_matricesBuffers.Length; i++)
            {
                m_matricesBuffers[i].Release();
                m_parts[i].Dispose();
                m_matrices[i].Dispose();
            }
            m_parts = null;
            m_matrices = null;
            m_matricesBuffers = null;
        }

        /// <summary>
        /// 通过检查器或撤销/重做操作对组件进行更改后，将调用OnValidate方法
        /// </summary>
        void OnValidate()
        {
            if (m_parts != null && enabled)
            {
                OnDisable();
                OnEnable();
            }
        }

        /// <summary>
        /// 创建部件
        /// </summary>
        /// <param name="childIndex"></param>
        /// <returns></returns>
        FractalPart CreatePart(int childIndex) => new FractalPart
        {
            m_direction = s_directions[childIndex],
            m_rotation = s_rotations[childIndex]
        };

        /// <summary>
        /// 更新分形的位置和旋转
        /// </summary>
        void Update()
        {
            float spinAngleDelta = 0.125f * PI * Time.deltaTime;
            FractalPart rootPart = m_parts[0][0];
            rootPart.m_spinAngle += spinAngleDelta;
            rootPart.m_worldRotation = mul(transform.rotation,
                mul(rootPart.m_rotation, quaternion.RotateY(rootPart.m_spinAngle))
            );
            rootPart.m_worldPosition = transform.position;
            m_parts[0][0] = rootPart;
            float objectScale = transform.lossyScale.x;
            float3x3 r = float3x3(rootPart.m_worldRotation) * objectScale;
            m_matrices[0][0] = float3x4(r.c0, r.c1, r.c2, rootPart.m_worldPosition);

            float scale = objectScale;
            JobHandle jobHandle = default;
            for (int li = 1; li < m_parts.Length; li++)
            {
                scale *= 0.5f;
                // 安排Job执行循环，并将最后一个工作句柄转递给下一个工作句柄
                jobHandle = new UpdateFractalLevelJob
                {
                    m_spinAngleDelta = spinAngleDelta,
                    m_scale = scale,
                    m_parents = m_parts[li - 1],
                    m_parts = m_parts[li],
                    m_matrices = m_matrices[li]
                }.ScheduleParallel(m_parts[li].Length, 5, jobHandle);
            }
            jobHandle.Complete();

            // 对所有级别都使用边长为3的立方体作为边界
            var bounds = new Bounds(rootPart.m_worldPosition, 3f * objectScale * Vector3.one);
            for (int i = 0; i < m_matricesBuffers.Length; i++)
            {
                ComputeBuffer buffer = m_matricesBuffers[i];
                // 将矩阵上载到GPU
                buffer.SetData(m_matrices[i]);
                // 将缓冲区设置在属性块上，而不是直接在材质上
                s_propertyBlock.SetBuffer(s_matricesId, buffer);
                Graphics.DrawMeshInstancedProcedural(
                    m_mesh, 0, m_material, bounds, buffer.count, s_propertyBlock
                );
            }
        }

#endregion

#region 依赖的字段

        /// <summary>
        /// 矩阵缓冲区的标识符
        /// </summary>
        private static readonly int s_matricesId = Shader.PropertyToID("_Matrices");

        /// <summary>
        /// 方向数组
        /// </summary>
        private static float3[] s_directions = {
            up(), right(), left(), forward(), back()
        };

        /// <summary>
        /// 旋转角度数组
        /// </summary>
        private static quaternion[] s_rotations = {
            quaternion.identity,
            quaternion.RotateZ(-0.5f * PI), quaternion.RotateZ(0.5f * PI),
            quaternion.RotateX(0.5f * PI), quaternion.RotateX(-0.5f * PI)
        };

        /// <summary>
        /// 用于将每个缓冲区链接到特定的绘制区域
        /// </summary>
        private static MaterialPropertyBlock s_propertyBlock;

        /// <summary>
        /// 递归深度
        /// </summary>
        [SerializeField, Range(1, 8)]
        private int m_depth = 4;

        /// <summary>
        /// 所需网格
        /// </summary>
        [SerializeField]
        private Mesh m_mesh;

        /// <summary>
        /// 所需材质
        /// </summary>
        [SerializeField]
        private Material m_material;

        /// <summary>
        /// 分形部件Native数组
        /// </summary>
        private NativeArray<FractalPart>[] m_parts;

        /// <summary>
        /// 矩阵Native数组
        /// </summary>
        private NativeArray<float3x4>[] m_matrices;

        /// <summary>
        /// 计算缓冲区（每个级别使用一个单独的缓冲区）
        /// </summary>
        private ComputeBuffer[] m_matricesBuffers;

#endregion

#region 结构

        /// <summary>
        /// Job结构，指示Unity使用Burst编译
        /// </summary>
        [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
        struct UpdateFractalLevelJob : IJobFor
        {
            /// <summary>
            /// 部件旋转角度
            /// </summary>
            public float m_spinAngleDelta;

            /// <summary>
            /// 部件缩放比例
            /// </summary>
            public float m_scale;

            /// <summary>
            /// 父部件Native数组
            /// </summary>
            [ReadOnly]
            public NativeArray<FractalPart> m_parents;

            /// <summary>
            /// 部件Native数组
            /// </summary>
            public NativeArray<FractalPart> m_parts;

            /// <summary>
            /// 矩阵Native数组
            /// </summary>
            [WriteOnly]
            public NativeArray<float3x4> m_matrices;

            /// <summary>
            /// 计算每层遍历的物件的位置和旋转角
            /// </summary>
            /// <param name="i"></param>
            public void Execute(int i)
            {
                FractalPart parent = m_parents[i / 5];
                FractalPart part = m_parts[i];
                part.m_spinAngle += m_spinAngleDelta;
                part.m_worldRotation = mul(parent.m_worldRotation,
                    mul(part.m_rotation, quaternion.RotateY(part.m_spinAngle))
                );
                part.m_worldPosition =
                    parent.m_worldPosition +
                    mul(parent.m_worldRotation, 1.5f * m_scale * part.m_direction);
                m_parts[i] = part;

                float3x3 r = float3x3(part.m_worldRotation) * m_scale;
                m_matrices[i] = float3x4(r.c0, r.c1, r.c2, part.m_worldPosition);
            }
        }

        /// <summary>
        /// 存储分形部件的结构
        /// </summary>
        struct FractalPart
        {
            /// <summary>
            /// 部件的方向
            /// </summary>
            public float3 m_direction;

            /// <summary>
            /// 部件的世界坐标位置
            /// </summary>
            public float3 m_worldPosition;

            /// <summary>
            /// 部件的旋转角度
            /// </summary>
            public quaternion m_rotation;

            /// <summary>
            /// 部件的世界坐标旋转角度
            /// </summary>
            public quaternion m_worldRotation;

            /// <summary>
            /// 旋转角度（每次更新时使用新的四元数开始）
            /// </summary>
            public float m_spinAngle;
        }


#endregion
    }
}
#endif

#if OrganicVariety
namespace Assets.Scripts.Basics
{
    /// <summary>
    /// 分形
    /// </summary>
    public class Fractal : MonoBehaviour
    {

        #region 方法

        /// <summary>
        /// 组件启用时重新为缓冲区和Native数组赋值
        /// </summary>
        void OnEnable()
        {
            m_parts = new NativeArray<FractalPart>[m_depth];
            m_matrices = new NativeArray<float3x4>[m_depth];
            m_matricesBuffers = new ComputeBuffer[m_depth];
            m_sequenceNumbers = new Vector4[m_depth];
            int stride = 12 * 4;
            for (int i = 0, length = 1; i < m_parts.Length; i++, length *= 5)
            {
                m_parts[i] = new NativeArray<FractalPart>(length, Allocator.Persistent);
                m_matrices[i] = new NativeArray<float3x4>(length, Allocator.Persistent);
                m_matricesBuffers[i] = new ComputeBuffer(length, stride);
                m_sequenceNumbers[i] =
                    new Vector4(Random.value, Random.value, Random.value, Random.value);
            }

            m_parts[0][0] = CreatePart(0);
            for (int li = 1; li < m_parts.Length; li++)
            {
                NativeArray<FractalPart> levelParts = m_parts[li];
                for (int fpi = 0; fpi < levelParts.Length; fpi += 5)
                {
                    for (int ci = 0; ci < 5; ci++)
                    {
                        levelParts[fpi + ci] = CreatePart(ci);
                    }
                }
            }

            s_propertyBlock ??= new MaterialPropertyBlock();
        }

        /// <summary>
        /// 组件被禁用或破坏时释放缓冲区以及Native数组内存
        /// </summary>
        void OnDisable()
        {
            for (int i = 0; i < m_matricesBuffers.Length; i++)
            {
                m_matricesBuffers[i].Release();
                m_parts[i].Dispose();
                m_matrices[i].Dispose();
            }
            m_parts = null;
            m_matrices = null;
            m_matricesBuffers = null;
            m_sequenceNumbers = null;
        }

        /// <summary>
        /// 通过检查器或撤销/重做操作对组件进行更改后，将调用OnValidate方法
        /// </summary>
        void OnValidate()
        {
            if (m_parts != null && enabled)
            {
                OnDisable();
                OnEnable();
            }
        }

        /// <summary>
        /// 创建部件
        /// </summary>
        /// <param name="childIndex"></param>
        /// <returns></returns>
        FractalPart CreatePart(int childIndex) => new FractalPart
        {
            m_maxSagAngle = radians(Random.Range(m_maxSagAngleA, m_maxSagAngleB)),
            m_rotation = s_rotations[childIndex],
            m_spinVelocity =
                (Random.value < m_reverseSpinChance ? -1f : 1f) *
                radians(Random.Range(m_spinSpeedA, m_spinSpeedB))
        };

        /// <summary>
        /// 更新分形的位置和旋转
        /// </summary>
        void Update()
        {
            float deltaTime = Time.deltaTime;
            FractalPart rootPart = m_parts[0][0];
            rootPart.m_spinAngle += rootPart.m_spinVelocity * deltaTime;
            rootPart.m_worldRotation = mul(transform.rotation,
                mul(rootPart.m_rotation, quaternion.RotateY(rootPart.m_spinAngle))
            );
            rootPart.m_worldPosition = transform.position;
            m_parts[0][0] = rootPart;
            float objectScale = transform.lossyScale.x;
            float3x3 r = float3x3(rootPart.m_worldRotation) * objectScale;
            m_matrices[0][0] = float3x4(r.c0, r.c1, r.c2, rootPart.m_worldPosition);

            float scale = objectScale;
            JobHandle jobHandle = default;
            for (int li = 1; li < m_parts.Length; li++)
            {
                scale *= 0.5f;
                // 安排Job执行循环，并将最后一个工作句柄转递给下一个工作句柄
                jobHandle = new UpdateFractalLevelJob
                {
                    m_deltaTime = deltaTime,
                    m_scale = scale,
                    m_parents = m_parts[li - 1],
                    m_parts = m_parts[li],
                    m_matrices = m_matrices[li]
                }.ScheduleParallel(m_parts[li].Length, 5, jobHandle);
            }
            jobHandle.Complete();

            // 对所有级别都使用边长为3的立方体作为边界
            var bounds = new Bounds(rootPart.m_worldPosition, 3f * objectScale * Vector3.one);
            int leafIndex = m_matricesBuffers.Length - 1;
            for (int i = 0; i < m_matricesBuffers.Length; i++)
            {
                ComputeBuffer buffer = m_matricesBuffers[i];
                // 将矩阵上载到GPU
                buffer.SetData(m_matrices[i]);
                Color colorA, colorB;
                Mesh instanceMesh;
                if (i == leafIndex)
                {
                    colorA = m_leafColorA;
                    colorB = m_leafColorB;
                    instanceMesh = m_leafMesh;
                }
                else
                {
                    float gradientInterpolator = i / (m_matricesBuffers.Length - 2f);
                    colorA = m_gradientA.Evaluate(gradientInterpolator);
                    colorB = m_gradientB.Evaluate(gradientInterpolator);
                    instanceMesh = m_mesh;
                }
                s_propertyBlock.SetColor(s_colorAId, colorA);
                s_propertyBlock.SetColor(s_colorBId, colorB);
                // 将缓冲区设置在属性块上，而不是直接在材质上
                s_propertyBlock.SetBuffer(s_matricesId, buffer);
                s_propertyBlock.SetVector(s_sequenceNumbersId, m_sequenceNumbers[i]);
                Graphics.DrawMeshInstancedProcedural(
                    instanceMesh, 0, m_material, bounds, buffer.count, s_propertyBlock
                );
            }
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 矩阵缓冲区的标识符
        /// </summary>
        private static readonly int
            s_colorAId = Shader.PropertyToID("_ColorA"),
            s_colorBId = Shader.PropertyToID("_ColorB"),
            s_matricesId = Shader.PropertyToID("_Matrices"),
            s_sequenceNumbersId = Shader.PropertyToID("_SequenceNumbers");

        /// <summary>
        /// 方向数组
        /// </summary>
        private static float3[] s_directions = {
            up(), right(), left(), forward(), back()
        };

        /// <summary>
        /// 旋转角度数组
        /// </summary>
        private static quaternion[] s_rotations = {
            quaternion.identity,
            quaternion.RotateZ(-0.5f * PI), quaternion.RotateZ(0.5f * PI),
            quaternion.RotateX(0.5f * PI), quaternion.RotateX(-0.5f * PI)
        };

        /// <summary>
        /// 用于将每个缓冲区链接到特定的绘制区域
        /// </summary>
        private static MaterialPropertyBlock s_propertyBlock;

        /// <summary>
        /// 递归深度
        /// </summary>
        [SerializeField, Range(1, 8)]
        private int m_depth = 4;

        /// <summary>
        /// 除叶子外所用网格
        /// </summary>
        [SerializeField]
        private Mesh m_mesh;

        /// <summary>
        /// 叶子网格
        /// </summary>
        [SerializeField]
        private Mesh m_leafMesh;

        /// <summary>
        /// 最大下垂角度A
        /// </summary>
        [SerializeField, Range(0f, 90f)] 
        private float m_maxSagAngleA = 15f;

        /// <summary>
        /// 最大下垂角度B
        /// </summary>
        [SerializeField, Range(0f, 90f)]
        private float m_maxSagAngleB = 25f;

        /// <summary>
        /// 旋转速度A
        /// </summary>
        [SerializeField, Range(0f, 90f)] 
        private float m_spinSpeedA = 20f;

        /// <summary>
        /// 旋转速度B
        /// </summary>
        [SerializeField, Range(0f, 90f)]
        private float m_spinSpeedB = 25f;

        /// <summary>
        /// 反向旋转的概率大小
        /// </summary>
        [SerializeField, Range(0f, 1f)]
        private float m_reverseSpinChance = 0.25f;

        /// <summary>
        /// 所需材质
        /// </summary>
        [SerializeField]
        private Material m_material;

        /// <summary>
        /// 颜色梯度A
        /// </summary>
        [SerializeField] 
        private Gradient m_gradientA;

        /// <summary>
        /// 颜色梯度B
        /// </summary>
        [SerializeField] 
        private Gradient m_gradientB;

        /// <summary>
        /// 叶子颜色A
        /// </summary>
        [SerializeField] 
        private Color m_leafColorA;

        /// <summary>
        /// 叶子颜色B
        /// </summary>
        [SerializeField] 
        private Color m_leafColorB;

        /// <summary>
        /// 分形部件Native数组
        /// </summary>
        private NativeArray<FractalPart>[] m_parts;

        /// <summary>
        /// 矩阵Native数组
        /// </summary>
        private NativeArray<float3x4>[] m_matrices;

        /// <summary>
        /// 计算缓冲区（每个级别使用一个单独的缓冲区）
        /// </summary>
        private ComputeBuffer[] m_matricesBuffers;

        /// <summary>
        /// 为每个级别添加一个序列号数组
        /// </summary>
        private Vector4[] m_sequenceNumbers;

        #endregion

        #region 结构

        /// <summary>
        /// Job结构，指示Unity使用Burst编译
        /// </summary>
        [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
        struct UpdateFractalLevelJob : IJobFor
        {
            /// <summary>
            /// 部件缩放比例
            /// </summary>
            public float m_scale;

            /// <summary>
            /// 时间增量
            /// </summary>
            public float m_deltaTime;

            /// <summary>
            /// 父部件Native数组
            /// </summary>
            [ReadOnly]
            public NativeArray<FractalPart> m_parents;

            /// <summary>
            /// 部件Native数组
            /// </summary>
            public NativeArray<FractalPart> m_parts;

            /// <summary>
            /// 矩阵Native数组
            /// </summary>
            [WriteOnly]
            public NativeArray<float3x4> m_matrices;

            /// <summary>
            /// 计算每层遍历的物件的位置和旋转角
            /// </summary>
            /// <param name="i"></param>
            public void Execute(int i)
            {
                FractalPart parent = m_parents[i / 5];
                FractalPart part = m_parts[i];
                part.m_spinAngle += part.m_spinVelocity * m_deltaTime;

                float3 upAxis = mul(mul(parent.m_worldRotation, part.m_rotation), up());
                float3 sagAxis = cross(up(), upAxis);

                float sagMagnitude = length(sagAxis);
                quaternion baseRotation;
                if (sagMagnitude > 0f)
                {
                    sagAxis /= sagMagnitude;
                    quaternion sagRotation =
                        quaternion.AxisAngle(sagAxis, part.m_maxSagAngle * sagMagnitude);
                    baseRotation = mul(sagRotation, parent.m_worldRotation);
                }
                else
                {
                    baseRotation = parent.m_worldRotation;
                }

                part.m_worldRotation = mul(baseRotation,
                    mul(part.m_rotation, quaternion.RotateY(part.m_spinAngle))
                );
                part.m_worldPosition =
                    parent.m_worldPosition +
                    mul(part.m_worldRotation, float3(0f, 1.5f * m_scale, 0f));
                m_parts[i] = part;

                float3x3 r = float3x3(part.m_worldRotation) * m_scale;
                m_matrices[i] = float3x4(r.c0, r.c1, r.c2, part.m_worldPosition);
            }
        }

        /// <summary>
        /// 存储分形部件的结构
        /// </summary>
        struct FractalPart
        {
            /// <summary>
            /// 部件的世界坐标位置
            /// </summary>
            public float3 m_worldPosition;

            /// <summary>
            /// 部件的旋转角度
            /// </summary>
            public quaternion m_rotation;

            /// <summary>
            /// 部件的世界坐标旋转角度
            /// </summary>
            public quaternion m_worldRotation;

            /// <summary>
            /// 部件最大的下垂角度
            /// </summary>
            public float m_maxSagAngle;

            /// <summary>
            /// 旋转角度（每次更新时使用新的四元数开始）
            /// </summary>
            public float m_spinAngle;

            /// <summary>
            /// 旋转速度
            /// </summary>
            public float m_spinVelocity;
        }

        #endregion
    }
}
#endif