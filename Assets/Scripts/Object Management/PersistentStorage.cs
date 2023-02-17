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
        public void Save(PersistableObject o)
        {
            using var writer = new BinaryWriter(File.Open(m_savePath, FileMode.Create));
            o.Save(new GameDataWriter(writer));
        }

        /// <summary>
        /// 读档
        /// </summary>
        /// <param name="o"></param>
        public void Load(PersistableObject o)
        {
            using var reader = new BinaryReader(File.Open(m_savePath, FileMode.Open));
            o.Load(new GameDataReader(reader));
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
