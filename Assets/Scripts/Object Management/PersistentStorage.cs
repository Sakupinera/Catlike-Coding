using System.IO;
using UnityEngine;

namespace Assets.Scripts.Object_Management
{
    /// <summary>
    /// 持久化存储
    /// </summary>
    public class PersistentStorage : MonoBehaviour
    {
        #region 方法

        /// <summary>
        /// 初始化
        /// </summary>
        private void Awake()
        {
            m_savePath = Path.Combine(Application.persistentDataPath, "saveFile");
        }
        
        /// <summary>
        /// 存档
        /// </summary>
        /// <param name="o"></param>
        public void Save(PersistableObject o, int version)
        {
            using var writer = new BinaryWriter(File.Open(m_savePath, FileMode.Create));
            writer.Write(-version);
            o.Save(new GameDataWriter(writer));
        }

        /// <summary>
        /// 读档
        /// </summary>
        /// <param name="o"></param>
        public void Load(PersistableObject o)
        {
            byte[] data = File.ReadAllBytes(m_savePath);
            var reader = new BinaryReader(new MemoryStream(data));
            o.Load(new GameDataReader(reader, -reader.ReadInt32()));
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 存档路径
        /// </summary>
        private string m_savePath;

        #endregion
    }
}
