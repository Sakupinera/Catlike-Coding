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
        void Awake()
        {
            m_colors = new Color[m_meshRenderers.Length];
        }

        /// <summary>
        /// 游戏逻辑更新
        /// </summary>
        public void GameUpdate()
        {
            transform.Rotate(AngularVelocity * Time.deltaTime);
            transform.localPosition += Velocity * Time.deltaTime;
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
            writer.Write(AngularVelocity);
            writer.Write(Velocity);
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
            AngularVelocity = reader.Version >= 4 ? reader.ReadVector3() : Vector3.zero;
            Velocity = reader.Version >=4 ? reader.ReadVector3() : Vector3.zero;
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
            OriginFactory.Reclaim(this);
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
        /// 角速度
        /// </summary>
        public Vector3 AngularVelocity { get; set; }

        /// <summary>
        /// 速度
        /// </summary>
        public Vector3 Velocity { get; set; }

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

        #endregion
    }
}
