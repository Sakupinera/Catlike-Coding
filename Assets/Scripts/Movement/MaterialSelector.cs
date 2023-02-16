using UnityEngine;

namespace Assets.Scripts.Movement
{
    /// <summary>
    /// 材质选择器
    /// </summary>
    public class MaterialSelector : MonoBehaviour
    {
        #region 方法

        /// <summary>
        /// 为网格渲染器应用相应的材质
        /// </summary>
        /// <param name="index"></param>
        public void Select(int index)
        {
            if (m_meshRenderer && m_materials != null && index >= 0 && index < m_materials.Length)
            {
                m_meshRenderer.material = m_materials[index];
            }
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 材质列表
        /// </summary>
        [SerializeField] 
        private Material[] m_materials = default;

        /// <summary>
        /// 网格渲染器
        /// </summary>
        [SerializeField]
        private MeshRenderer m_meshRenderer = default;

        #endregion
    }
}
