using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

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
                CreateShape();
            }
            else if (Input.GetKeyDown(m_destroyKey))
            {
                DestroyShape();
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

            // 当进度达到1时，创建新的游戏对象
            m_creationProgress += Time.deltaTime * CreationSpeed;
            while (m_creationProgress >= 1f)
            {
                m_creationProgress -= 1f;
                CreateShape();
            }

            m_destructionProgress += Time.deltaTime * DestructionSpeed;
            while (m_destructionProgress >= 1f)
            {
                m_destructionProgress -= 1f;
                DestroyShape();
            }
        }

        /// <summary>
        /// 创建游戏对象
        /// </summary>
        private void CreateShape()
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
        /// 销毁游戏对象
        /// </summary>
        private void DestroyShape()
        {
            if (m_shapes.Count > 0)
            {
                int index = Random.Range(0, m_shapes.Count);
                m_shapeFactory.Reclaim(m_shapes[index]);
                // 提升数组性能
                int lastIndex = m_shapes.Count - 1;
                m_shapes[index] = m_shapes[lastIndex];
                m_shapes.RemoveAt(lastIndex);
            }
        }

        /// <summary>
        /// 开始新的游戏
        /// </summary>
        private void BeginNewGame()
        {
            foreach (var t in m_shapes)
            {
                m_shapeFactory.Reclaim(t);
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

        #region 属性

        /// <summary>
        /// 创建游戏对象的速度
        /// </summary>
        public float CreationSpeed { get; set; }

        /// <summary>
        /// 销毁游戏对象的速度
        /// </summary>
        public float DestructionSpeed { get; set; }

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
        /// 销毁游戏对象的输入
        /// </summary>
        public KeyCode m_destroyKey = KeyCode.X;

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

        /// <summary>
        /// 创建新的游戏对象的进度
        /// </summary>
        private float m_creationProgress;

        /// <summary>
        /// 销毁游戏对象的进度
        /// </summary>
        private float m_destructionProgress;

        #endregion

        #region 常量

        /// <summary>
        /// 存档版本
        /// </summary>
        private const int SaveVersion = 1;

        #endregion
    }
}
