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
                writer.Write(t.OriginFactory.FactoryId);
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
        /// 添加形状到引用数组中
        /// </summary>
        /// <param name="shape"></param>
        public void AddShape(Shape shape)
        {
            shape.SaveIndex = m_shapes.Count;
            m_shapes.Add(shape);
        }

        /// <summary>
        /// 获取形状
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Shape GetShape(int index)
        {
            return m_shapes[index];
        }

        /// <summary>
        /// 销毁形状
        /// </summary>
        /// <param name="shape"></param>
        public void Kill(Shape shape)
        {
            if (m_inGameUpdateLoop)
            {
                m_killList.Add(shape);
            }
            else
            {
                KillImmediately(shape);
            }
        }

        /// <summary>
        /// 将对象标记为垂死状态
        /// </summary>
        /// <param name="shape"></param>
        public void MarkAsDying(Shape shape)
        {
            if (m_inGameUpdateLoop)
            {
                m_markAsDyingList.Add(shape);
            }
            else
            {
                MarkAsDyingImmediately(shape);
            }
        }

        /// <summary>
        /// 游戏对象是否被标记为垂死状态
        /// </summary>
        /// <param name="shape"></param>
        /// <returns></returns>
        public bool IsMarkedAsDying(Shape shape)
        {
            return shape.SaveIndex < m_dyingShapeCount;
        }

        /// <summary>
        /// 立即杀死游戏对象
        /// </summary>
        /// <param name="shape"></param>
        private void KillImmediately(Shape shape)
        {
            int index = shape.SaveIndex;
            shape.Recycle();

            // Double move when killing a dying shape.
            if (index < m_dyingShapeCount && index < --m_dyingShapeCount)
            {
                m_shapes[m_dyingShapeCount].SaveIndex = index;
                m_shapes[index] = m_shapes[m_dyingShapeCount];
                index = m_dyingShapeCount;
            }

            int lastIndex = m_shapes.Count - 1;
            if (index < lastIndex)
            {
                m_shapes[lastIndex].SaveIndex = index;
                m_shapes[index] = m_shapes[lastIndex];
            }
            m_shapes.RemoveAt(lastIndex);
        }

        /// <summary>
        /// 初始化
        /// </summary>
        private void Start()
        {
            m_mainRandomState = Random.state;

            m_shapes = new List<Shape>();
            m_killList = new List<ShapeInstance>();
            m_markAsDyingList = new List<ShapeInstance>();

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
                GameLevel.Current.SpawnShapes();
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
            m_inGameUpdateLoop = true;
            // 更新每个形状单独的逻辑
            for (int i = 0; i < m_shapes.Count; i++)
            {
                m_shapes[i].GameUpdate();
            }
            GameLevel.Current.GameUpdate();
            m_inGameUpdateLoop = false;

            // 当进度达到1时，创建新的游戏对象
            m_creationProgress += Time.deltaTime * CreationSpeed;
            while (m_creationProgress >= 1f)
            {
                m_creationProgress -= 1f;
                GameLevel.Current.SpawnShapes();
            }

            m_destructionProgress += Time.deltaTime * DestructionSpeed;
            while (m_destructionProgress >= 1f)
            {
                m_destructionProgress -= 1f;
                DestroyShape();
            }

            // 限制游戏对象的数量
            int limit = GameLevel.Current.PopulationLimit;
            if (limit > 0)
            {
                while (m_shapes.Count - m_dyingShapeCount > limit)
                {
                    DestroyShape();
                }
            }

            if (m_killList.Count > 0)
            {
                for (int i = 0; i < m_killList.Count; i++)
                {
                    if (m_killList[i].IsValid)
                    {
                        KillImmediately(m_killList[i].Shape);
                    }
                }

                m_killList.Clear();
            }

            if (m_markAsDyingList.Count > 0)
            {
                for (int i = 0; i < m_markAsDyingList.Count; i++)
                {
                    if (m_markAsDyingList[i].IsValid)
                    {
                        MarkAsDyingImmediately(m_markAsDyingList[i].Shape);
                    }
                }

                m_markAsDyingList.Clear();
            }
        }

        /// <summary>
        /// 当激活组件时，重新赋值
        /// </summary>
        private void OnEnable()
        {
            Instance = this;

            // 保证工厂Id只被赋值一次
            if (m_shapeFactories[0].FactoryId != 0)
            {
                for (int i = 0; i < m_shapeFactories.Length; i++)
                {
                    m_shapeFactories[i].FactoryId = i;
                }
            }
        }

        /// <summary>
        /// 销毁游戏对象
        /// </summary>
        private void DestroyShape()
        {
            if (m_shapes.Count - m_dyingShapeCount > 0)
            {
                Shape shape = m_shapes[Random.Range(m_dyingShapeCount, m_shapes.Count)];
                if (m_destroyDuration <= 0f)
                {
                    KillImmediately(shape);
                }
                else
                {
                    shape.AddBehavior<DyingShapeBehavior>().Initialize(shape, m_destroyDuration);
                }
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
                t.Recycle();
            }
            m_shapes.Clear();
            m_dyingShapeCount = 0;
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
                int factoryId = version >= 5 ? reader.ReadInt() : 0;
                int shapeId = version > 0 ? reader.ReadInt() : 0;
                int materialId = version > 0 ? reader.ReadInt() : 0;
                Shape instance = m_shapeFactories[factoryId].Get(shapeId, materialId);
                instance.Load(reader);
            }

            for (int i = 0; i < m_shapes.Count; i++)
            {
                m_shapes[i].ResolveShapeInstances();
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
            yield return SceneManager.LoadSceneAsync(levelBuildIndex, LoadSceneMode.Additive);
            SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(levelBuildIndex));
            m_loadedLevelBuildIndex = levelBuildIndex;
            enabled = true;
        }

        /// <summary>
        /// 标记垂死游戏对象
        /// </summary>
        /// <param name="shape"></param>
        private void MarkAsDyingImmediately(Shape shape)
        {
            int index = shape.SaveIndex;
            if (index < m_dyingShapeCount)
            {
                return;
            }
            m_shapes[m_dyingShapeCount].SaveIndex = index;
            m_shapes[index] = m_shapes[m_dyingShapeCount];
            shape.SaveIndex = m_dyingShapeCount;
            m_shapes[m_dyingShapeCount++] = shape;
        }

        #endregion

        #region 属性

        /// <summary>
        /// 游戏单例对象
        /// </summary>
        public static Game Instance { get; private set; }

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
        private ShapeFactory[] m_shapeFactories;

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
        /// 销毁持续时间
        /// </summary>
        [SerializeField]
        private float m_destroyDuration;

        /// <summary>
        /// 游戏对象列表
        /// </summary>
        private List<Shape> m_shapes;

        /// <summary>
        /// 需要杀死的游戏对象列表
        /// </summary>
        private List<ShapeInstance> m_killList;

        /// <summary>
        /// 标记为垂死的游戏对象列表
        /// </summary>
        private List<ShapeInstance> m_markAsDyingList;

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

        /// <summary>
        /// 是否处于游戏逻辑循环
        /// </summary>
        private bool m_inGameUpdateLoop;

        /// <summary>
        /// 垂死游戏对象的数量
        /// </summary>
        private int m_dyingShapeCount;

        #endregion

        #region 常量

        /// <summary>
        /// 存档版本
        /// </summary>
        private const int SaveVersion = 7;

        #endregion
    }
}
