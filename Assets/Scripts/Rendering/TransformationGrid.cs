using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Rendering
{
    /// <summary>
    /// 变换网格
    /// </summary>
    public class TransformationGrid : MonoBehaviour
    {
        #region 方法

        /// <summary>
        /// 初始化
        /// </summary>
        private void Awake()
        {
            m_grid = new Transform[m_gridResolution * m_gridResolution * m_gridResolution];
            for (int i = 0, z = 0; z < m_gridResolution; z++)
            {
                for (int y = 0; y < m_gridResolution; y++)
                {
                    for (int x = 0; x < m_gridResolution; x++, i++)
                    {
                        m_grid[i] = CreateGridPoint(x, y, z);
                    }
                }
            }

            m_transformations = new List<Transformation>();
        }

        /// <summary>
        /// 逐帧更新
        /// </summary>
        private void Update()
        {
            UpdateTransformation();
            for (int i = 0, z = 0; z < m_gridResolution; z++)
            {
                for (int y = 0; y < m_gridResolution; y++)
                {
                    for (int x = 0; x < m_gridResolution; x++, i++)
                    {
                        m_grid[i].localPosition = TransformPoint(x, y, z);
                    }
                }
            }
        }

        /// <summary>
        /// 合并变换矩阵
        /// </summary>
        private void UpdateTransformation()
        {
            GetComponents<Transformation>(m_transformations);
            if (m_transformations.Count > 0)
            {
                m_transformation = m_transformations[0].Matrix;
                for (int i = 1; i < m_transformations.Count; i++)
                {
                    m_transformation = m_transformations[i].Matrix * m_transformation;
                }
            }
        }

        /// <summary>
        /// 应用变换
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        private Vector3 TransformPoint(int x, int y, int z)
        {
            Vector3 coordinates = GetCoordinates(x, y, z);
            return m_transformation.MultiplyPoint(coordinates);
        }

        /// <summary>
        /// 创建网格
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        private Transform CreateGridPoint(int x, int y, int z)
        {
            Transform point = Instantiate<Transform>(m_prefab);
            point.localPosition = GetCoordinates(x, y, z);
            point.GetComponent<MeshRenderer>().material.color = new Color((float)x / m_gridResolution,
                (float)y / m_gridResolution, (float)z / m_gridResolution);
            return point;
        }

        /// <summary>
        /// 获取立方体的坐标点
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        private Vector3 GetCoordinates(int x, int y, int z)
        {
            return new Vector3(
                x - (m_gridResolution - 1) * 0.5f,
                y - (m_gridResolution - 1) * 0.5f,
                z - (m_gridResolution - 1) * 0.5f
            );
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 预制体
        /// </summary>
        public Transform m_prefab;

        /// <summary>
        /// 网格分辨率
        /// </summary>
        public int m_gridResolution = 10;

        /// <summary>
        /// 网格数组
        /// </summary>
        private Transform[] m_grid;

        /// <summary>
        /// 变换列表
        /// </summary>
        private List<Transformation> m_transformations;

        /// <summary>
        /// 变换矩阵
        /// </summary>
        private Matrix4x4 m_transformation;

        #endregion
    }
}
