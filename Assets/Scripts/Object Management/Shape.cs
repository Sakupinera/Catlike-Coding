using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Object_Management
{
    /// <summary>
    /// 形状
    /// </summary>
    public class Shape : PersistableObject
    {
        #region 方法

        /// <summary>
        /// 初始化
        /// </summary>
        private void Awake()
        {
            m_colors = new Color[m_meshRenderers.Length];
        }

        /// <summary>
        /// 游戏逻辑更新
        /// </summary>
        public void GameUpdate()
        {
            Age += Time.deltaTime;
            for (int i = 0; i < m_behaviorList.Count; i++)
            {
                if (!m_behaviorList[i].GameUpdate(this))
                {
                    m_behaviorList[i].Recycle();
                    m_behaviorList.RemoveAt(i--);
                }
            }
        }

        /// <summary>
        /// 设置材质
        /// </summary>
        /// <param name="material"></param>
        /// <param name="materialId"></param>
        public void SetMaterial(Material material, int materialId)
        {
            for (int i = 0; i < m_meshRenderers.Length; i++)
            {
                m_meshRenderers[i].material = material;
            }
            MaterialId = materialId;
        }

        /// <summary>
        /// 设置颜色
        /// </summary>
        /// <param name="color"></param>
        public void SetColor(Color color)
        {
            if (s_sharedPropertyBlock == null)
            {
                s_sharedPropertyBlock = new MaterialPropertyBlock();
            }
            // 避免每次设置颜色都创建新的材质
            s_sharedPropertyBlock.SetColor(s_colorPropertyId, color);
            for (int i = 0; i < m_meshRenderers.Length; i++)
            {
                m_colors[i] = color;
                m_meshRenderers[i].SetPropertyBlock(s_sharedPropertyBlock);
            }
        }

        /// <summary>
        /// 通过下标设置颜色
        /// </summary>
        /// <param name="color"></param>
        /// <param name="index"></param>
        public void SetColor(Color color, int index)
        {
            if (s_sharedPropertyBlock == null)
            {
                s_sharedPropertyBlock = new MaterialPropertyBlock();
            }

            s_sharedPropertyBlock.SetColor(s_colorPropertyId, color);
            m_colors[index] = color;
            m_meshRenderers[index].SetPropertyBlock(s_sharedPropertyBlock);
        }

        /// <summary>
        /// 存档
        /// </summary>
        /// <param name="writer"></param>
        public override void Save(GameDataWriter writer)
        {
            base.Save(writer);
            writer.Write(m_colors.Length);
            for (int i = 0; i < m_colors.Length; i++)
            {
                writer.Write(m_colors[i]);
            }

            writer.Write(Age);
            writer.Write(m_behaviorList.Count);
            for (int i = 0; i < m_behaviorList.Count; i++)
            {
                writer.Write((int)m_behaviorList[i].BehaviorType);
                m_behaviorList[i].Save(writer);
            }
        }

        /// <summary>
        /// 读档
        /// </summary>
        /// <param name="reader"></param>
        public override void Load(GameDataReader reader)
        {
            base.Load(reader);
            if (reader.Version >= 5)
            {
                LoadColors(reader);
            }
            else
            {
                SetColor(reader.Version > 0 ? reader.ReadColor() : Color.white);
            }

            // 读取行为组件类型
            if (reader.Version >= 6)
            {
                Age = reader.ReadFloat();
                int behaviorCount = reader.ReadInt();
                for (int i = 0; i < behaviorCount; i++)
                {
                    ShapeBehavior behavior = ((ShapeBehaviorType)reader.ReadInt()).GetInstance();
                    m_behaviorList.Add(behavior);
                    behavior.Load(reader);
                }
            }
            else if (reader.Version >= 4)
            {
                AddBehavior<RotationShapeBehavior>().AngularVelocity = reader.ReadVector3();
                AddBehavior<MovementShapeBehavior>().Velocity = reader.ReadVector3();
            }
        }

        /// <summary>
        /// 添加行为组件
        /// </summary>
        /// <param name="behavior"></param>
        public T AddBehavior<T>() where T : ShapeBehavior, new()
        {
            T behavior = ShapeBehaviorPool<T>.Get();
            m_behaviorList.Add(behavior);
            return behavior;
        }

        /// <summary>
        /// 死亡
        /// </summary>
        public void Die()
        {
            Game.Instance.Kill(this);
        }

        /// <summary>
        /// 读取颜色信息
        /// </summary>
        /// <param name="reader"></param>
        private void LoadColors(GameDataReader reader)
        {
            int count = reader.ReadInt();
            int max = count <= m_colors.Length ? count : m_colors.Length;
            int i = 0;
            for (; i < max; i++)
            {
                SetColor(reader.ReadColor(), i);
            }
            // 如果存储的颜色数大于当前颜色数
            if (count > m_colors.Length)
            {
                for (; i < count; i++)
                {
                    reader.ReadColor();
                }
            }
            // 如果当前需要的颜色数大于存储颜色数
            else if (count < m_colors.Length)
            {
                for (; i < m_colors.Length; i++)
                {
                    SetColor(Color.white, i);
                }
            }
        }

        /// <summary>
        /// 回收对象
        /// </summary>
        public void Recycle()
        {
            Age = 0f;
            InstanceId += 1;
            for (int i = 0; i < m_behaviorList.Count; i++)
            {
                m_behaviorList[i].Recycle();
            }
            m_behaviorList.Clear();
            OriginFactory.Reclaim(this);
        }

        /// <summary>
        /// 解析形状实例
        /// </summary>
        public void ResolveShapeInstances()
        {
            for (int i = 0; i < m_behaviorList.Count; i++)
            {
                m_behaviorList[i].ResolveShapeInstances();
            }
        }

        /// <summary>
        /// 将游戏对象标记为垂死状态
        /// </summary>
        public void MarkAsDying()
        {
            Game.Instance.MarkAsDying(this);
        }

        #endregion

        #region 属性

        /// <summary>
        /// 形状Id
        /// </summary>
        public int ShapeId
        {
            get { return m_shapeId; }
            set
            {
                // 检查原先的Id是否是默认值，并且新的值不等于默认值
                if (m_shapeId == int.MinValue && value != int.MinValue)
                {
                    m_shapeId = value;
                }
                else
                {
                    Debug.LogError("Not allowed to change shapeId.");
                }
            }
        }

        /// <summary>
        /// 材质Id
        /// </summary>
        public int MaterialId { get; private set; }

        /// <summary>
        /// 颜色数
        /// </summary>
        public int ColorCount
        {
            get { return m_colors.Length; }
        }

        /// <summary>
        /// 原始的形状工厂
        /// </summary>
        public ShapeFactory OriginFactory
        {
            get { return m_originFactory; }
            set
            {
                if (m_originFactory == null)
                {
                    m_originFactory = value;
                }
                else
                {
                    Debug.LogError("Not allowed to change origin factory.");
                }
            }
        }

        /// <summary>
        /// 游戏对象的存在时长
        /// </summary>
        public float Age { get; private set; }

        /// <summary>
        /// 实例Id
        /// </summary>
        public int InstanceId { get; private set; }

        /// <summary>
        /// 存档时的下标
        /// </summary>
        public int SaveIndex { get; set; }

        /// <summary>
        /// 游戏对象是否被标记为垂死状态
        /// </summary>
        public bool IsMarkedAsDying
        {
            get { return Game.Instance.IsMarkedAsDying(this); }
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 网格渲染器数组
        /// </summary>
        [SerializeField]
        private MeshRenderer[] m_meshRenderers;

        /// <summary>
        /// 形状Id
        /// </summary>
        private int m_shapeId = int.MinValue;

        /// <summary>
        /// 颜色数组
        /// </summary>
        private Color[] m_colors;

        /// <summary>
        /// 材质颜色属性Id
        /// </summary>
        private static int s_colorPropertyId = Shader.PropertyToID("_Color");

        /// <summary>
        /// 材质属性块
        /// </summary>
        private static MaterialPropertyBlock s_sharedPropertyBlock;

        /// <summary>
        /// 形状工厂
        /// </summary>
        private ShapeFactory m_originFactory;

        /// <summary>
        /// 行为对象列表
        /// </summary>
        private List<ShapeBehavior> m_behaviorList = new List<ShapeBehavior>();

        #endregion
    }

    /// <summary>
    /// 形状实例
    /// </summary>
    public struct ShapeInstance
    {
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="shape"></param>
        public ShapeInstance(Shape shape)
        {
            Shape = shape;
            m_instanceIdOrSaveIndex = shape.InstanceId;
        }

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="saveIndex"></param>
        public ShapeInstance(int saveIndex)
        {
            Shape = null;
            m_instanceIdOrSaveIndex = saveIndex;
        }

        /// <summary>
        /// 隐式转型
        /// </summary>
        /// <param name="shape"></param>
        public static implicit operator ShapeInstance(Shape shape)
        {
            return new ShapeInstance(shape);
        }

        /// <summary>
        /// 解析实例Id/存档下标
        /// </summary>
        public void Resolve()
        {
            if (m_instanceIdOrSaveIndex >= 0)
            {
                Shape = Game.Instance.GetShape(m_instanceIdOrSaveIndex);
                m_instanceIdOrSaveIndex = Shape.InstanceId;
            }
        }

        /// <summary>
        /// 作为焦点的形状是否有效
        /// </summary>
        public bool IsValid
        {
            get
            {
                return Shape && m_instanceIdOrSaveIndex == Shape.InstanceId;
            }
        }

        /// <summary>
        /// 形状
        /// </summary>
        public Shape Shape { get; private set; }

        /// <summary>
        /// 实例Id/存档下标
        /// </summary>
        private int m_instanceIdOrSaveIndex;
    }
}
