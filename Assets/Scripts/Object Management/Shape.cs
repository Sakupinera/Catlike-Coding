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
            m_meshRenderer = GetComponent<MeshRenderer>();
        }

        /// <summary>
        /// 设置材质
        /// </summary>
        /// <param name="material"></param>
        /// <param name="materialId"></param>
        public void SetMaterial(Material material, int materialId)
        {
            m_meshRenderer.material = material;
            MaterialId = materialId;
        }

        /// <summary>
        /// 设置颜色
        /// </summary>
        /// <param name="color"></param>
        public void SetColor(Color color)
        {
            this.m_color = color;
            if (s_sharedPropertyBlock == null)
            {
                s_sharedPropertyBlock = new MaterialPropertyBlock();
            }
            // 避免每次设置颜色都创建新的材质
            s_sharedPropertyBlock.SetColor(s_colorPropertyId, color);
            m_meshRenderer.SetPropertyBlock(s_sharedPropertyBlock);
        }

        /// <summary>
        /// 存档
        /// </summary>
        /// <param name="writer"></param>
        public override void Save(GameDataWriter writer)
        {
            base.Save(writer);
            writer.Write(m_color);
        }

        /// <summary>
        /// 读档
        /// </summary>
        /// <param name="reader"></param>
        public override void Load(GameDataReader reader)
        {
            base.Load(reader);
            SetColor(reader.Version > 0 ? reader.ReadColor() : Color.white);
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

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 形状Id
        /// </summary>
        private int m_shapeId = int.MinValue;

        /// <summary>
        /// 颜色
        /// </summary>
        private Color m_color;

        /// <summary>
        /// 网格渲染器
        /// </summary>
        private MeshRenderer m_meshRenderer;

        /// <summary>
        /// 材质颜色属性Id
        /// </summary>
        private static int s_colorPropertyId = Shader.PropertyToID("_Color");

        /// <summary>
        /// 材质属性块
        /// </summary>
        private static MaterialPropertyBlock s_sharedPropertyBlock;

        #endregion
    }
}
