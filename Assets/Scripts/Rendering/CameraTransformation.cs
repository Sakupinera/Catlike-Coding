using UnityEngine;

namespace Assets.Scripts.Rendering
{
    internal class CameraTransformation : Transformation
    {
        #region 属性

        /// <summary>
        /// 矩阵变换
        /// </summary>
        public override Matrix4x4 Matrix
        {
            get
            {
                Matrix4x4 matrix = new Matrix4x4();
                matrix.SetRow(0, new Vector4(m_focalLength, 0f, 0f, 0f));
                matrix.SetRow(1, new Vector4(0f, m_focalLength, 0f, 0f));
                matrix.SetRow(2, new Vector4(0f, 0f, 0f, 0f));
                matrix.SetRow(3, new Vector4(0f, 0f, 1f, 0f));
                return matrix;
            }
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 焦点距离
        /// </summary>
        public float m_focalLength = 1f;

        #endregion
    }
}
