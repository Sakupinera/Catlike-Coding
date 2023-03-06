using UnityEngine;

namespace Assets.Scripts.Rendering
{
    /// <summary>
    /// 变换
    /// </summary>
    public abstract class Transformation : MonoBehaviour
    {
        /// <summary>
        /// 应用变换
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Vector3 Apply(Vector3 point)
        {
            return Matrix.MultiplyPoint(point);
        }
        
        /// <summary>
        /// 变换矩阵
        /// </summary>
        public abstract Matrix4x4 Matrix { get; }
    }
}
