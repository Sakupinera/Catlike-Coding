using System;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Object_Management
{
    /// <summary>
    /// 游戏关卡
    /// </summary>
    public partial class GameLevel : PersistableObject
    {
        #region 方法

        /// <summary>
        /// 游戏更新逻辑
        /// </summary>
        public void GameUpdate()
        {
            for (int i = 0; i < m_levelObjects.Length; i++)
            {
                m_levelObjects[i].GameUpdate();
            }
        }

        /// <summary>
        /// 当游戏关卡被激活时，重新赋值
        /// </summary>
        private void OnEnable()
        {
            Current = this;
            if (m_levelObjects == null)
            {
                m_levelObjects = Array.Empty<GameLevelObject>();
            }
        }

        /// <summary>
        /// 生成游戏对象
        /// </summary>
        /// <param name="shape"></param>
        public void SpawnShapes()
        {
            m_spawnZone.SpawnShapes();
        }

        #endregion

        #region 属性

        /// <summary>
        /// 游戏关卡
        /// </summary>
        public static GameLevel Current { get; private set; }

        /// <summary>
        /// 存档
        /// </summary>
        /// <param name="writer"></param>
        public override void Save(GameDataWriter writer)
        {
            writer.Write(m_levelObjects.Length);
            for (int i = 0; i < m_levelObjects.Length; i++)
            {
                m_levelObjects[i].Save(writer);
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
                m_levelObjects[i].Load(reader);
            }
        }

        #endregion

        #region 属性

        /// <summary>
        /// 游戏对象的数量限制
        /// </summary>
        public int PopulationLimit
        {
            get { return m_populationLimit; }
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
        [UnityEngine.Serialization.FormerlySerializedAs("m_persistentObjects")]
        [SerializeField]
        private GameLevelObject[] m_levelObjects;

        /// <summary>
        /// 游戏对象的数量限制
        /// </summary>
        [SerializeField]
        private int m_populationLimit;

        #endregion
    }
}
