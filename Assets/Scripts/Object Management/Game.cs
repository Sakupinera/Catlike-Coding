using System.Collections.Generic;
using UnityEditor.Rendering;
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
            m_shapes = new List<Shape>();
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
                m_storage.Save(this, SaveVersion);
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
            Shape instance = m_shapeFactory.GetRandom();
            Transform t = instance.transform;
            t.localPosition = Random.insideUnitSphere * 5f;
            t.localRotation = Random.rotation;
            t.localScale = Vector3.one * Random.Range(0.1f, 1f);
            instance.SetColor(Random.ColorHSV(hueMin: 0f, hueMax: 1f,
                saturationMin: 0.5f, saturationMax: 1f,
                valueMin: 0.25f, valueMax: 1f,
                alphaMin: 1f, alphaMax: 1f));
            m_shapes.Add(instance);
        }

        /// <summary>
        /// 开始新的游戏
        /// </summary>
        private void BeginNewGame()
        {
            foreach (var t in m_shapes)
            {
                Destroy(t.gameObject);
            }
            m_shapes.Clear();
        }

        /// <summary>
        /// 存档
        /// </summary>
        /// <param name="writer"></param>
        public override void Save(GameDataWriter writer)
        {
            writer.Write(m_shapes.Count);
            foreach (var t in m_shapes)
            {
                writer.Write(t.ShapeId);
                writer.Write(t.MaterialId);
                t.Save(writer);
            }
        }

        /// <summary>
        /// 读档
        /// </summary>
        /// <param name="reader"></param>
        public override void Load(GameDataReader reader)
        {
            int version = reader.Version;
            if (version > SaveVersion)
            {
                Debug.LogError("Unsupported future save version " + version);
            }
            int count = version <= 0 ? -version : reader.ReadInt();
            for (int i = 0; i < count; i++)
            {
                int shapeId = version > 0 ? reader.ReadInt() : 0;
                int materialId = version > 0 ? reader.ReadInt() : 0;
                Shape instance = m_shapeFactory.Get(shapeId, materialId);
                instance.Load(reader);
                m_shapes.Add(instance);
            }
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 预制体工厂
        /// </summary>
        public ShapeFactory m_shapeFactory;

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
        private List<Shape> m_shapes;

        #endregion

        #region 常量

        /// <summary>
        /// 存档版本
        /// </summary>
        private const int SaveVersion = 1;

        #endregion
    }
}
