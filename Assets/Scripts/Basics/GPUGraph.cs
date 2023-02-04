using UnityEngine;

namespace Assets.Scripts.Basics
{
    /// <summary>
    /// 通过GPU实现的视图
    /// </summary>
    public class GPUGraph : MonoBehaviour
    {
        #region 方法

        /// <summary>
        /// 组件启用时重新为ComputeBuffer赋值
        /// </summary>
        private void OnEnable()
        {
            // positionBuffer = new ComputeBuffer(resolution * resolution, 3 * 4);
            // 始终使用最大分辨率的平方作为缓冲区中元素的数量
            m_positionBuffer = new ComputeBuffer(MaxResolusion * MaxResolusion, 3 * 4);
        }

        /// <summary>
        /// 组件被禁用或破坏时释放ComputeBuffer内存
        /// </summary>
        private void OnDisable()
        {
            m_positionBuffer.Release();
            m_positionBuffer = null;
        }

        /// <summary>
        /// 判断状态以及更新函数
        /// </summary>
        private void Update()
        {
            m_duration += Time.deltaTime;
            if (m_transitioning)
            {
                if (m_duration >= m_transitionDuration)
                {
                    m_duration -= m_transitionDuration;
                    m_transitioning = false;
                }
            }
            else if (m_duration >= m_functionDuration)
            {
                m_duration -= m_functionDuration;
                m_transitioning = true;
                m_transitionFunction = m_function;
                NextFunctionPick();
            }

            FunctionOnGPUUpdate();
        }

        /// <summary>
        /// 在GPU上更新函数上点的位置
        /// </summary>
        private void FunctionOnGPUUpdate()
        {
            float step = 2f / m_resolution;
            m_computeShader.SetInt(s_resolutionId, m_resolution);
            m_computeShader.SetFloat(s_stepId, step);
            m_computeShader.SetFloat(s_timeId, Time.time);
            // 判断是否进行过渡
            if (m_transitioning)
            {
                m_computeShader.SetFloat(
                    s_transitionProgressId,
                    Mathf.SmoothStep(0f, 1f, m_duration / m_transitionDuration)
                );
            }

            // 根据是否需要过渡计算出函数对应的内核索引
            var kernelIndex =
                (int)m_function + (int)(m_transitioning ? m_transitionFunction : m_function) * FunctionLibrary.FunctionCount;
            // 设置positions缓冲区，该缓冲区不会复制任何数据，但会将缓冲区链接到内核
            m_computeShader.SetBuffer(kernelIndex, s_positionsId, m_positionBuffer);

            // 由于固定了8*8的群组大小，因此在X和Y维度上需要的群组数量等于分辨率除以8（四舍五入）
            int groups = Mathf.CeilToInt(m_resolution / 8f);
            // 运行内核
            m_computeShader.Dispatch(kernelIndex, groups, groups, 1);

            // 设置材质的属性
            m_material.SetBuffer(s_positionsId, m_positionBuffer);
            m_material.SetFloat(s_stepId, step);
            // 设置边界盒指示要绘制的内容的空间范围
            var bounds = new Bounds(Vector3.zero, Vector3.one * (2f + 2f / m_resolution));

            // 进行过程绘制
            // Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, positionBuffer.count);
            Graphics.DrawMeshInstancedProcedural(m_mesh, 0, m_material, bounds, m_resolution * m_resolution);
        }

        /// <summary>
        /// 根据转换模式选出下一个转换的函数
        /// </summary>
        private void NextFunctionPick()
        {
            m_function = m_transitionMode == TransitionMode.Cycle ?
                FunctionLibrary.NextFunctionNameGet(m_function) :
                FunctionLibrary.RandomFunctionNameOtherThanGet(m_function);
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 用于调度Compute Shader内核
        /// </summary>
        [SerializeField]
        private ComputeShader m_computeShader;

        /// <summary>
        /// 所需材质
        /// </summary>
        [SerializeField]
        private Material m_material;

        /// <summary>
        /// 所需网格
        /// </summary>
        [SerializeField]
        private Mesh m_mesh;

        /// <summary>
        /// 最大分辨率
        /// </summary>
        private const int MaxResolusion = 1000;

        /// <summary>
        /// 分辨率
        /// </summary>
        [SerializeField, Range(10, MaxResolusion)]
        private int m_resolution = 10;

        /// <summary>
        /// 目标函数名
        /// </summary>
        [SerializeField]
        private FunctionLibrary.FunctionName m_function = default;

        /// <summary>
        /// 函数转换的模式
        /// </summary>
        [SerializeField]
        private TransitionMode m_transitionMode;

        /// <summary>
        /// 函数持续时间
        /// </summary>
        [SerializeField, Min(0f)]
        private float m_functionDuration = 1f;

        /// <summary>
        /// 函数转换时间
        /// </summary>
        [SerializeField, Min(0f)]
        private float m_transitionDuration = 1f;

        /// <summary>
        /// 记录当前持续的时间
        /// </summary>
        private float m_duration;

        /// <summary>
        /// 是否进行过渡
        /// </summary>
        private bool m_transitioning;

        /// <summary>
        /// 转换前的函数名
        /// </summary>
        private FunctionLibrary.FunctionName m_transitionFunction;

        /// <summary>
        /// 用于在GPU上存储位置
        /// </summary>
        private ComputeBuffer m_positionBuffer;

        /// <summary>
        /// Shader所拥有的属性
        /// </summary>
        private static readonly int
            s_positionsId = Shader.PropertyToID("_Positions"),
            s_resolutionId = Shader.PropertyToID("_Resolution"),
            s_stepId = Shader.PropertyToID("_Step"),
            s_timeId = Shader.PropertyToID("_Time"),
            s_transitionProgressId = Shader.PropertyToID("_TransitionProgress");

        #endregion

        #region 枚举

        /// <summary>
        /// 转换模式
        /// </summary>
        public enum TransitionMode { Cycle, Random };

        #endregion
    }
}
