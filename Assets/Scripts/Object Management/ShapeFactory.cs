using UnityEngine;

namespace Assets.Scripts.Object_Management
{
    /// <summary>
    /// 形状工厂
    /// </summary>
    [CreateAssetMenu]
    public class ShapeFactory :ScriptableObject
    {
        #region 方法

        /// <summary>
        /// 通过Id获取某一形状预制体，并指定它的材质
        /// </summary>
        /// <param name="shapeId"></param>
        /// <param name="materialId"></param>
        /// <returns></returns>
        public Shape Get(int shapeId = 0, int materialId = 0)
        {
            Shape instance = Instantiate(m_prefabs[shapeId]);
            instance.ShapeId = shapeId;
            instance.SetMaterial(m_materials[materialId], materialId);
            return instance;
        }

        /// <summary>
        /// 随机获取某一形状预制体
        /// </summary>
        /// <returns></returns>
        public Shape GetRandom()
        {
            return Get(Random.Range(0, m_prefabs.Length),
                Random.Range(0, m_materials.Length));
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 形状预制体数组
        /// </summary>
        [SerializeField] 
        private Shape[] m_prefabs;

        /// <summary>
        /// 材质数组
        /// </summary>
        [SerializeField]
        private Material[] m_materials;

        #endregion
    }
}
