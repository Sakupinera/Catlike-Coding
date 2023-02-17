using System.IO;
using UnityEngine;

namespace Assets.Scripts.Object_Management
{
    /// <summary>
    /// 游戏数据读取器
    /// </summary>
    public class GameDataReader
    {
        #region 方法

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="reader"></param>
        public GameDataReader(BinaryReader reader)
        {
            this.m_reader = reader;
        }

        /// <summary>
        /// 读取浮点型数据
        /// </summary>
        /// <returns></returns>
        public float ReadFloat()
        {
            return m_reader.ReadSingle();
        }

        /// <summary>
        /// 读取32位整型数据
        /// </summary>
        /// <returns></returns>
        public int ReadInt()
        {
            return m_reader.ReadInt32();
        }

        /// <summary>
        /// 读取四元数数据
        /// </summary>
        /// <returns></returns>
        public Quaternion ReadQuaternion()
        {
            Quaternion value;
            value.x = m_reader.ReadSingle();
            value.y = m_reader.ReadSingle();
            value.z = m_reader.ReadSingle();
            value.w = m_reader.ReadSingle();
            return value;
        }

        /// <summary>
        /// 读取三维向量数据
        /// </summary>
        /// <returns></returns>
        public Vector3 ReadVector3()
        {
            Vector3 value;
            value.x = m_reader.ReadSingle();
            value.y = m_reader.ReadSingle();
            value.z = m_reader.ReadSingle();
            return value;
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 二进制流读适配器
        /// </summary>
        private BinaryReader m_reader;

        #endregion
    }
}
