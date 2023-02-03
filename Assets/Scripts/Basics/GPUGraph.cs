using UnityEngine;

namespace Assets.Scripts.Basics
{
    public class GPUGraph : MonoBehaviour
    {
        [SerializeField]
        ComputeShader m_computeShader;

        [SerializeField]
        Material m_material;

        [SerializeField]
        Mesh m_mesh;

        const int MaxResolusion = 1000;

        [SerializeField, Range(10, MaxResolusion)]
        int m_resolution = 10;

        [SerializeField]
        FunctionLibrary.FunctionName m_function = default;

        public enum TransitionMode { Cycle, Random };

        [SerializeField]
        TransitionMode m_transitionMode;

        [SerializeField, Min(0f)]
        float m_functionDuration = 1f, m_transitionDuration = 1f;

        float m_duration;

        bool m_transitioning;

        FunctionLibrary.FunctionName m_transitionFunction;

        ComputeBuffer m_positionBuffer;

        static readonly int
            s_positionsId = Shader.PropertyToID("_Positions"),
            s_resolutionId = Shader.PropertyToID("_Resolution"),
            s_stepId = Shader.PropertyToID("_Step"),
            s_timeId = Shader.PropertyToID("_Time"),
            s_transitionProgressId = Shader.PropertyToID("_TransitionProgress");

        private void OnEnable()
        {
            //positionBuffer = new ComputeBuffer(resolution * resolution, 3 * 4);
            m_positionBuffer = new ComputeBuffer(MaxResolusion * MaxResolusion, 3 * 4);
        }

        private void OnDisable()
        {
            m_positionBuffer.Release();
            m_positionBuffer = null;
        }

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

        void FunctionOnGPUUpdate()
        {
            float step = 2f / m_resolution;
            m_computeShader.SetInt(s_resolutionId, m_resolution);
            m_computeShader.SetFloat(s_stepId, step);
            m_computeShader.SetFloat(s_timeId, Time.time);
            if (m_transitioning)
            {
                m_computeShader.SetFloat(
                    s_transitionProgressId,
                    Mathf.SmoothStep(0f, 1f, m_duration / m_transitionDuration)
                );
            }

            var kernelIndex =
                (int)m_function + (int)(m_transitioning ? m_transitionFunction : m_function) * FunctionLibrary.FunctionCount;
            m_computeShader.SetBuffer(kernelIndex, s_positionsId, m_positionBuffer);

            int groups = Mathf.CeilToInt(m_resolution / 8f);
            m_computeShader.Dispatch(kernelIndex, groups, groups, 1);

            m_material.SetBuffer(s_positionsId, m_positionBuffer);
            m_material.SetFloat(s_stepId, step);
            var bounds = new Bounds(Vector3.zero, Vector3.one * (2f + 2f / m_resolution));
            //Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, positionBuffer.count);
            Graphics.DrawMeshInstancedProcedural(m_mesh, 0, m_material, bounds, m_resolution * m_resolution);
        }

        void NextFunctionPick()
        {
            m_function = m_transitionMode == TransitionMode.Cycle ?
                FunctionLibrary.NextFunctionNameGet(m_function) :
                FunctionLibrary.RandomFunctionNameOtherThanGet(m_function);
        }
    }
}
