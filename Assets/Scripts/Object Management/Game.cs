using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Object_Management
{
    /// <summary>
    /// 游戏
    /// </summary>
    public class Game : PersistableObject
    {
        #region 方法

        /// <summary>
        /// 初始化
        /// </summary>
        private void Awake()
        {
            m_objects = new List<PersistableObject>();
        }

        /// <summary>
        /// 检测输入，逐帧更新
        /// </summary>
        private void Update()
        {
            if (Input.GetKeyDown(m_createKey))
            {
                CreateObject();
            }
            else if (Input.GetKey(m_newGameKey))
            {
                BeginNewGame();
            }
            else if (Input.GetKeyDown(m_saveKey))
            {
                m_storage.Save(this);
            }
            else if (Input.GetKeyDown(m_loadKey))
            {
                BeginNewGame();
                m_storage.Load(this);
            }
        }

        /// <summary>
        /// 创建游戏对象
        /// </summary>
        private void CreateObject()
        {
            PersistableObject o = Instantiate(m_prefab);
            Transform t = o.transform;
            t.localPosition = Random.insideUnitSphere * 5f;
            t.localRotation = Random.rotation;
            t.localScale = Vector3.one * Random.Range(0.1f, 1f);
            m_objects.Add(o);
        }

        /// <summary>
        /// 开始新的游戏
        /// </summary>
        private void BeginNewGame()
        {
            foreach (var t in m_objects)
            {
                Destroy(t.gameObject);
            }
            m_objects.Clear();
        }

        /// <summary>
        /// 存档
        /// </summary>
        /// <param name="writer"></param>
        public override void Save(GameDataWriter writer)
        {
            writer.Write(m_objects.Count);
            foreach (var t in m_objects)
            {
                t.Save(writer);
            }
        }

        /// <summary>
        /// 读档
        /// </summary>
        /// <param name="reader"></param>
        public override void Load(GameDataReader reader)
        {
            int count = reader.ReadInt();
            for (int i = 0; i < count; i++)
            {
                PersistableObject o = Instantiate(m_prefab);
                o.Load(reader);
                m_objects.Add(o);
            }
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 预制体
        /// </summary>
        public PersistableObject m_prefab;

        /// <summary>
        /// 持久化存储
        /// </summary>
        public PersistentStorage m_storage;

        /// <summary>
        /// 创建游戏对象的输入
        /// </summary>
        public KeyCode m_createKey = KeyCode.C;

        /// <summary>
        /// 开始新游戏的输入
        /// </summary>
        public KeyCode m_newGameKey = KeyCode.N;

        /// <summary>
        /// 保存游戏的输入
        /// </summary>
        public KeyCode m_saveKey = KeyCode.S;

        /// <summary>
        /// 读取游戏的输入
        /// </summary>
        public KeyCode m_loadKey = KeyCode.L;

        /// <summary>
        /// 游戏对象列表
        /// </summary>
        private List<PersistableObject> m_objects;

        #endregion
    }
}
