using System.IO;
using UnityEngine;

namespace Assets.Scripts.Object_Management
{
    /// <summary>
    /// 游戏数据写入器
    /// </summary>
    public class GameDataWriter
    {
        #region 方法

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="writer"></param>
        public GameDataWriter(BinaryWriter writer)
        {
            this.m_writer = writer;
        }

        /// <summary>
        /// 写入浮点型数据
        /// </summary>
        /// <param name="value"></param>
        public void Write(float value)
        {
            m_writer.Write(value);
        }

        /// <summary>
        /// 写入整型数据
        /// </summary>
        /// <param name="value"></param>
        public void Write(int value)
        {
            m_writer.Write(value);
        }

        /// <summary>
        /// 写入四元数数据
        /// </summary>
        /// <param name="value"></param>
        public void Write(Quaternion value)
        {
            m_writer.Write(value.x);
            m_writer.Write(value.y);
            m_writer.Write(value.z);
            m_writer.Write(value.w);
        }

        /// <summary>
        /// 写入三维向量数据
        /// </summary>
        /// <param name="value"></param>
        public void Write(Vector3 value)
        {
            m_writer.Write(value.x);
            m_writer.Write(value.y);
            m_writer.Write(value.z);
        }

        /// <summary>
        /// 写入颜色数据
        /// </summary>
        /// <param name="value"></param>
        public void Write(Color value)
        {
            m_writer.Write(value.r);
            m_writer.Write(value.g);
            m_writer.Write(value.b);
            m_writer.Write(value.a);
        }

        /// <summary>
        /// 写入随机数的状态
        /// </summary>
        /// <param name="value"></param>
        public void Write(Random.State value)
        {
            m_writer.Write(JsonUtility.ToJson(value));
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 二进制流写适配器
        /// </summary>
        private BinaryWriter m_writer;

        #endregion
    }
}
