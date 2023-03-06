using System.Drawing;
using UnityEngine;

namespace Assets.Scripts.Rendering
{
    /// <summary>
    /// 位移变换
    /// </summary>
    internal class RotationTransformation : Transformation
    {

        #region 属性

        /// <summary>
        /// 变换矩阵
        /// </summary>
        public override Matrix4x4 Matrix
        {
            get
            {
                float radX = m_rotation.x * Mathf.Deg2Rad;
                float radY = m_rotation.y * Mathf.Deg2Rad;
                float radZ = m_rotation.z * Mathf.Deg2Rad;
                float sinX = Mathf.Sin(radX);
                float cosX = Mathf.Cos(radX);
                float sinY = Mathf.Sin(radY);
                float cosY = Mathf.Cos(radY);
                float sinZ = Mathf.Sin(radZ);
                float cosZ = Mathf.Cos(radZ);

                Matrix4x4 matrix = new Matrix4x4();
                matrix.SetColumn(0, new Vector4(
                    cosY * cosZ,
                    cosX * sinZ + sinX * sinY * cosZ,
                    sinX * sinZ - cosX * sinY * cosZ,
                    0f
                    ));
                matrix.SetColumn(1, new Vector4(
                    -cosY * sinZ,
                    cosX * cosZ - sinX * sinY * sinZ,
                    sinX * cosZ + cosX * sinY * sinZ,
                    0f
                    ));
                matrix.SetColumn(2, new Vector4(
                    sinY,
                    -sinX * cosY,
                    cosX * cosY
                    ));
                matrix.SetColumn(3, new Vector4(0f, 0f, 0f, 1f));
                return matrix;
            }
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 旋转
        /// </summary>
        public Vector3 m_rotation;

        #endregion
    }
}