#define MeasuringPerformance

using UnityEngine;

#if BuildingAGraph
namespace Assets.Scripts.Basics
{
    /// <summary>
    /// 视图
    /// </summary>
    public class Graph : MonoBehaviour
    {
#region 方法

        /// <summary>
        /// 初始化所有的点
        /// </summary>
        private void Awake()
        {
            float step = 2f / m_resolution;
            var position = Vector3.zero;
            var scale = Vector3.one * step;
            m_points = new Transform[m_resolution];
            for (int i = 0; i < m_points.Length; i++)
            {
                Transform point = m_points[i] = Instantiate(m_pointPrefab);
                position.x = (i + 0.5f) * step - 1f;
                point.localPosition = position;
                point.localScale = scale;
                point.SetParent(transform, false);
            }
        }

        /// <summary>
        /// 每帧更新点的位置
        /// </summary>
        private void Update()
        {
            float time = Time.time;
            for (int i = 0; i < m_points.Length; i++)
            {
                Transform point = m_points[i];
                Vector3 position = point.localPosition;
                position.y = Mathf.Sin(Mathf.PI * (position.x + time));
                point.localPosition = position;
            }
        }

#endregion

#region 依赖的字段

        /// <summary>
        /// 每个点的预制体
        /// </summary>
        [SerializeField]
        private Transform m_pointPrefab;

        /// <summary>
        /// 视图的分辨率（每一维上点的个数）
        /// </summary>
        [SerializeField, Range(10, 100)]
        private int m_resolution = 10;

        /// <summary>
        /// 存储点的数组
        /// </summary>
        private Transform[] m_points;

#endregion
    }
}
#endif

#if MathematicalSurfaces
namespace Assets.Scripts.Basics
{
    /// <summary>
    /// 视图
    /// </summary>
    public class Graph : MonoBehaviour 
    {
#region 方法

        /// <summary>
        /// 初始化所有的点
        /// </summary>
        private void Awake () {
            float step = 2f / m_resolution;
            var scale = Vector3.one * step;
            m_points = new Transform[m_resolution * m_resolution];
            for (int i = 0; i < m_points.Length; i++) {
                Transform point = m_points[i] = Instantiate(m_pointPrefab);
                point.localScale = scale;
                point.SetParent(transform, false);
            }
        }

        /// <summary>
        /// 每帧更新点的位置
        /// </summary>
        private void Update () {
            FunctionLibrary.FunctionEventHandler f = FunctionLibrary.FunctionGet(m_function);
            float time = Time.time;
            float step = 2f / m_resolution;
            float v = 0.5f * step - 1f;
            for (int i = 0, x = 0, z = 0; i < m_points.Length; i++, x++) {
                if (x == m_resolution) {
                    x = 0;
                    z += 1;
                    v = (z + 0.5f) * step - 1f;
                }
                float u = (x + 0.5f) * step - 1f;
                m_points[i].localPosition = f(u, v, time);
            }
        }

#endregion

#region 依赖的字段

        /// <summary>
        /// 每个点的预制体
        /// </summary>
        [SerializeField]
        private Transform m_pointPrefab;

        /// <summary>
        /// 视图的分辨率（每一维上点的个数）
        /// </summary>
        [SerializeField, Range(10, 100)]
        private int m_resolution = 10;

        /// <summary>
        /// 当前的函数名
        /// </summary>
        [SerializeField]
        private FunctionLibrary.FunctionName m_function;

        /// <summary>
        /// 存储点的数组
        /// </summary>
        private Transform[] m_points;

#endregion
    }
}
#endif

#if MeasuringPerformance
namespace Assets.Scripts.Basics
{
    /// <summary>
    /// 视图
    /// </summary>
    public class Graph : MonoBehaviour
    {
        #region 方法

        /// <summary>
        /// 初始化所有的点
        /// </summary>
        private void Awake()
        {
            float step = 2f / m_resolution;
            var scale = Vector3.one * step;
            m_points = new Transform[m_resolution * m_resolution];
            for (int i = 0; i < m_points.Length; i++)
            {
                Transform point = m_points[i] = Instantiate(m_pointPrefab);
                point.localScale = scale;
                point.SetParent(transform, false);
            }
        }

        /// <summary>
        /// 每帧更新点的位置
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

            if (m_transitioning)
            {
                FunctionTransitionUpdate();
            }
            else
            {
                FunctionUpdate();
            }
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

        /// <summary>
        /// 函数随时间变换
        /// </summary>
        private void FunctionUpdate()
        {
            FunctionLibrary.FunctionEventHandler f = FunctionLibrary.FunctionGet(m_function);
            float time = Time.time;
            float step = 2f / m_resolution;
            float v = 0.5f * step - 1f;
            for (int i = 0, x = 0, z = 0; i < m_points.Length; i++, x++)
            {
                if (x == m_resolution)
                {
                    x = 0;
                    z += 1;
                    v = (z + 0.5f) * step - 1f;
                }
                float u = (x + 0.5f) * step - 1f;
                m_points[i].localPosition = f(u, v, time);
            }
        }

        /// <summary>
        /// 函数转换过渡
        /// </summary>
        private void FunctionTransitionUpdate()
        {
            FunctionLibrary.FunctionEventHandler
                from = FunctionLibrary.FunctionGet(m_transitionFunction),
                to = FunctionLibrary.FunctionGet(m_function);
            float progress = m_duration / m_transitionDuration;
            float time = Time.time;
            float step = 2f / m_resolution;
            float v = 0.5f * step - 1f;
            for (int i = 0, x = 0, z = 0; i < m_points.Length; i++, x++)
            {
                if (x == m_resolution)
                {
                    x = 0;
                    z += 1;
                    v = (z + 0.5f) * step - 1f;
                }
                float u = (x + 0.5f) * step - 1f;
                m_points[i].localPosition = FunctionLibrary.Morph(
                    u, v, time, from, to, progress
                );
            }
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 每个点的预制体
        /// </summary>
        [SerializeField]
        private Transform m_pointPrefab;

        /// <summary>
        /// 视图的分辨率（每一维上点的个数）
        /// </summary>
        [SerializeField, Range(10, 100)]
        private int m_resolution = 10;

        /// <summary>
        /// 当前的函数名
        /// </summary>
        [SerializeField]
        private FunctionLibrary.FunctionName m_function;

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
        /// 存储点的数组
        /// </summary>
        private Transform[] m_points;

        /// <summary>
        /// 函数持续的时间
        /// </summary>
        private float m_duration;

        /// <summary>
        /// 判定当前是否需要转换函数
        /// </summary>
        private bool m_transitioning;

        /// <summary>
        /// 转换成的函数名
        /// </summary>
        private FunctionLibrary.FunctionName m_transitionFunction;

        #endregion

        #region 枚举

        /// <summary>
        /// 转换模式
        /// </summary>
        public enum TransitionMode { Cycle, Random }

        #endregion
    }
}
#endif
