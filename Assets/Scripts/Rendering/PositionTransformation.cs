using UnityEngine;

namespace Assets.Scripts.Rendering
{
    /// <summary>
    /// 位移变换
    /// </summary>
    internal class PositionTransformation : Transformation
    {

        #region 属性

        /// <summary>
        /// 变换矩阵
        /// </summary>
        public override Matrix4x4 Matrix
        {
            get
            {
                Matrix4x4 matrix = new Matrix4x4();
                matrix.SetRow(0, new Vector4(1f, 0f, 0f, m_position.x));
                matrix.SetRow(1, new Vector4(0f, 1f, 0f, m_position.y));
                matrix.SetRow(2, new Vector4(0f, 0f, 1f, m_position.z));
                matrix.SetRow(3, new Vector4(0f, 0f, 0f, 1f));
                return matrix;
            }
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 位置
        /// </summary>
        public Vector3 m_position;

        #endregion
    }
}
