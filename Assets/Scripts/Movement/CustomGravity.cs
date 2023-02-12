using UnityEngine;
using System.Collections.Generic;

namespace Assets.Scripts.Movement
{
    /// <summary>
    /// 自定义重力
    /// </summary>
    public static class CustomGravity
    {
        #region 方法

        /// <summary>
        /// 根据位置获取重力
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static Vector3 GetGravity(Vector3 position)
        {
            Vector3 g = Vector3.zero;
            for (int i = 0; i < m_sources.Count; i++)
            {
                g += m_sources[i].GetGravity(position);
            }

            return g;
        }

        /// <summary>
        /// 根据位置来获取重力以及向上轴
        /// </summary>
        /// <param name="position"></param>
        /// <param name="upAxis"></param>
        /// <returns></returns>
        public static Vector3 GetGravity(Vector3 position, out Vector3 upAxis)
        {
            Vector3 g = Vector3.zero;
            for (int i = 0; i < m_sources.Count; i++)
            {
                g += m_sources[i].GetGravity(position);
            }

            upAxis = -g.normalized;
            return g;
        }

        /// <summary>
        /// 根据重力来确定小球和轨道相机的向上轴
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static Vector3 GetUpAxis(Vector3 position)
        {
            Vector3 g = Vector3.zero;
            for (int i = 0; i < m_sources.Count; i++)
            {
                g += m_sources[i].GetGravity(position);
            }

            return -g.normalized;
        }

        /// <summary>
        /// 向列表中添加重力源
        /// </summary>
        /// <param name="source"></param>
        public static void Register(GravitySource source)
        {
            Debug.Assert(!m_sources.Contains(source),
                "Duplicate registration of gravity source!", source);
            m_sources.Add(source);
        }

        /// <summary>
        /// 从列表中移除重力源
        /// </summary>
        /// <param name="source"></param>
        public static void Unregister(GravitySource source)
        {
            Debug.Assert(m_sources.Contains(source),
                "Unregistration of unknown gravity source!", source);
            m_sources.Remove(source);
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 重力源列表
        /// </summary>
        private static List<GravitySource> m_sources = new List<GravitySource>();

        #endregion
    }
}
