#define MathematicalSurfaces

using UnityEngine;

#if BuildingAGraph
namespace Assets.Scripts.Basics
{
    public class Graph : MonoBehaviour
    {

        [SerializeField]
        Transform m_pointPrefab;

        [SerializeField, Range(10, 100)]
        int m_resolution = 10;

        Transform[] m_points;

        void Awake()
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

        void Update()
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
    }
}
#endif

#if MathematicalSurfaces
namespace Assets.Scripts.Basics
{
    public class Graph : MonoBehaviour {

        [SerializeField]
        Transform m_pointPrefab;

        [SerializeField, Range(10, 100)]
        int m_resolution = 10;

        [SerializeField]
        FunctionLibrary.FunctionName m_function;

        Transform[] m_points;

        void Awake () {
            float step = 2f / m_resolution;
            var scale = Vector3.one * step;
            m_points = new Transform[m_resolution * m_resolution];
            for (int i = 0; i < m_points.Length; i++) {
                Transform point = m_points[i] = Instantiate(m_pointPrefab);
                point.localScale = scale;
                point.SetParent(transform, false);
            }
        }

        void Update () {
            FunctionLibrary.FunctionEventHandler f = FunctionLibrary.GetFunction(m_function);
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
    }
}
#endif