using UnityEngine;

namespace Assets.Scripts.Rendering
{
    /// <summary>
    /// 切线空间可视化
    /// </summary>
    public class TangentSpaceVisualizer : MonoBehaviour
    {
        /// <summary>
        /// 绘画Gizmos
        /// </summary>
        void OnDrawGizmos()
        {
            MeshFilter filter = GetComponent<MeshFilter>();
            if (filter)
            {
                Mesh mesh = filter.sharedMesh;
                if (mesh)
                {
                    ShowTangentSpace(mesh);
                }
            }
        }

        /// <summary>
        /// 显示切线空间
        /// </summary>
        /// <param name="mesh"></param>
        void ShowTangentSpace(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Vector4[] tangents = mesh.tangents;
            for (int i = 0; i < vertices.Length; i++)
            {
                ShowTangentSpace(
                    transform.TransformPoint(vertices[i]), 
                    transform.TransformDirection(normals[i]),
                    transform.TransformDirection(tangents[i]),
                    tangents[i].w
                    );
            }
        }

        /// <summary>
        /// 重载方法
        /// </summary>
        /// <param name="vertex"></param>
        /// <param name="normal"></param>
        /// <param name="tangent"></param>
        /// <param name="binormalSign"></param>
        void ShowTangentSpace(Vector3 vertex, Vector3 normal, Vector3 tangent, float binormalSign)
        {
            vertex += normal * m_offset;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(vertex, vertex + normal * m_scale);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(vertex, vertex + tangent * m_scale);
            Vector3 binormal = Vector3.Cross(normal, tangent) * binormalSign;
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(vertex, vertex + binormal * m_scale);
        }

        /// <summary>
        /// 画线偏移量
        /// </summary>
        public float m_offset = 0.01f;
        
        /// <summary>
        /// 画线比例
        /// </summary>
        public float m_scale = 0.1f;
    }
}
