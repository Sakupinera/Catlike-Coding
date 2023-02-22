using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Assets.Scripts.Object_Management
{
    /// <summary>
    /// 游戏
    /// </summary>
    [DisallowMultipleComponent]
    public class Game : PersistableObject
    {
        #region 方法

        /// <summary>
        /// 初始化
        /// </summary>
        private void Start()
        {
            m_mainRandomState = Random.state;

            m_shapes = new List<Shape>();

            if (Application.isEditor)
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene loadedScene = SceneManager.GetSceneAt(i);
                    if (loadedScene.name.Contains("Level "))
                    {
                        SceneManager.SetActiveScene(loadedScene);
                        m_loadedLevelBuildIndex = loadedScene.buildIndex;
                        return;
                    }
                }
            }

            BeginNewGame();
            StartCoroutine(LoadLevel(1));
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
                StartCoroutine(LoadLevel(m_loadedLevelBuildIndex));
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
            else
            {
                for (int i = 1; i <= m_levelCount; i++)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha0 + i))
                    {
                        BeginNewGame();
                        StartCoroutine(LoadLevel(i));
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// 固定创建新的游戏对象的时间步长
        /// </summary>
        private void FixedUpdate()
        {
            // 更新每个形状单独的逻辑
            for (int i = 0; i < m_shapes.Count; i++)
            {
                m_shapes[i].GameUpdate();
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
            GameLevel.Current.ConfigureSpawn(instance);
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
            Random.state = m_mainRandomState;
            int seed = Random.Range(0, int.MaxValue) ^ (int)Time.unscaledTime;
            m_mainRandomState = Random.state;
            Random.InitState(seed);

            m_creationSpeedSlider.value = CreationSpeed = 0;
            m_destructionSpeedSlider.value = DestructionSpeed = 0;

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
            writer.Write(Random.state);
            writer.Write(CreationSpeed);
            writer.Write(m_creationProgress);
            writer.Write(DestructionSpeed);
            writer.Write(m_destructionProgress);
            writer.Write(m_loadedLevelBuildIndex);
            GameLevel.Current.Save(writer);
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

            StartCoroutine(LoadGame(reader));
        }

        /// <summary>
        /// 加载游戏
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private IEnumerator LoadGame(GameDataReader reader)
        {
            int version = reader.Version;
            int count = version <= 0 ? -version : reader.ReadInt();

            if (version >= 3)
            {
                // 判断是否要更换随机数种子
                Random.State state = reader.ReadRandomState();
                if (!m_reseedOnLoad)
                {
                    Random.state = state;
                }

                m_creationSpeedSlider.value = CreationSpeed = reader.ReadFloat();
                m_creationProgress = reader.ReadFloat();
                m_destructionSpeedSlider.value = DestructionSpeed = reader.ReadFloat();
                m_destructionProgress = reader.ReadFloat();
            }

            yield return LoadLevel(version < 2 ? 1 : reader.ReadInt());
            if (version >= 3)
            {
                GameLevel.Current.Load(reader);
            }
            for (int i = 0; i < count; i++)
            {
                int shapeId = version > 0 ? reader.ReadInt() : 0;
                int materialId = version > 0 ? reader.ReadInt() : 0;
                Shape instance = m_shapeFactory.Get(shapeId, materialId);
                instance.Load(reader);
                m_shapes.Add(instance);
            }
        }

        /// <summary>
        /// 加载场景
        /// </summary>
        private IEnumerator LoadLevel(int levelBuildIndex)
        {
            enabled = false;
            if (m_loadedLevelBuildIndex > 0)
            {
                yield return SceneManager.UnloadSceneAsync(m_loadedLevelBuildIndex);
            }
            yield return SceneManager.LoadSceneAsync(levelBuildIndex , LoadSceneMode.Additive);
            SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(levelBuildIndex));
            m_loadedLevelBuildIndex = levelBuildIndex;
            enabled = true;
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
        [SerializeField]
        private ShapeFactory m_shapeFactory;

        /// <summary>
        /// 持久化存储
        /// </summary>
        [SerializeField]
        private PersistentStorage m_storage;

        /// <summary>
        /// 创建游戏对象的输入
        /// </summary>
        [SerializeField]
        private KeyCode m_createKey = KeyCode.C;

        /// <summary>
        /// 销毁游戏对象的输入
        /// </summary>
        [SerializeField]
        private KeyCode m_destroyKey = KeyCode.X;

        /// <summary>
        /// 开始新游戏的输入
        /// </summary>
        [SerializeField]
        private KeyCode m_newGameKey = KeyCode.N;

        /// <summary>
        /// 保存游戏的输入
        /// </summary>
        [SerializeField]
        private KeyCode m_saveKey = KeyCode.S;

        /// <summary>
        /// 读取游戏的输入
        /// </summary>
        [SerializeField]
        private KeyCode m_loadKey = KeyCode.L;

        /// <summary>
        /// 关卡数
        /// </summary>
        [SerializeField]
        private int m_levelCount;

        /// <summary>
        /// 加载游戏时是否更换随机数种子
        /// </summary>
        [SerializeField]
        private bool m_reseedOnLoad;

        /// <summary>
        /// 创建速度滑动条
        /// </summary>
        [SerializeField]
        private Slider m_creationSpeedSlider;

        /// <summary>
        /// 销毁速度滑动条
        /// </summary>
        [SerializeField]
        private Slider m_destructionSpeedSlider;

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

        /// <summary>
        /// 当前加载的关卡场景下标
        /// </summary>
        private int m_loadedLevelBuildIndex;

        /// <summary>
        /// 随机状态
        /// </summary>
        private Random.State m_mainRandomState;

        #endregion

        #region 常量

        /// <summary>
        /// 存档版本
        /// </summary>
        private const int SaveVersion = 4;

        #endregion
    }
}
