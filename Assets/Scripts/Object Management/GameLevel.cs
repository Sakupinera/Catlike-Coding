using System;
using UnityEngine;

namespace Assets.Scripts.Object_Management
{
    /// <summary>
    /// 游戏关卡
    /// </summary>
    public class GameLevel : PersistableObject
    {
        #region 方法

        /// <summary>
        /// 当游戏关卡被激活时，重新赋值
        /// </summary>
        private void OnEnable()
        {
            Current = this;
            if (m_persistentObjects == null)
            {
                m_persistentObjects = Array.Empty<PersistableObject>();
            }
        }

        #endregion

        #region 属性

        /// <summary>
        /// 游戏关卡
        /// </summary>
        public static GameLevel Current { get; private set; }

        /// <summary>
        /// 生成点
        /// </summary>
        public Vector3 SpawnPoint
        {
            get
            {
                return m_spawnZone.SpawnPoint;
            }
        }

        /// <summary>
        /// 存档
        /// </summary>
        /// <param name="writer"></param>
        public override void Save(GameDataWriter writer)
        {
            writer.Write(m_persistentObjects.Length);
            for (int i = 0; i < m_persistentObjects.Length; i++)
            {
                m_persistentObjects[i].Save(writer);
            }
        }

        /// <summary>
        /// 读档
        /// </summary>
        /// <param name="reader"></param>
        public override void Load(GameDataReader reader)
        {
            int savedCount = reader.ReadInt();
            for (int i = 0; i < savedCount; i++)
            {
                m_persistentObjects[i].Load(reader);
            }
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 游戏对象的生成领域
        /// </summary>
        [SerializeField]
        private SpawnZone m_spawnZone;

        /// <summary>
        /// 持续化存储对象的数组
        /// </summary>
        [SerializeField]
        private PersistableObject[] m_persistentObjects;

        #endregion
    }
}
