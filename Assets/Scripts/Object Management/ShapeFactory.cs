using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Scripts.Object_Management
{
    /// <summary>
    /// 形状工厂
    /// </summary>
    [CreateAssetMenu]
    public class ShapeFactory : ScriptableObject
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
            Shape instance;
            if (m_recycle)
            {
                if (m_pools == null)
                {
                    CreatePools();
                }
                List<Shape> pool = m_pools[shapeId];
                int lastIndex = pool.Count - 1;
                // 当对象池中有对象时，则取出一个对象
                if (lastIndex >= 0)
                {
                    instance = pool[lastIndex];
                    instance.gameObject.SetActive(true);
                    pool.RemoveAt(lastIndex);
                }
                else
                {
                    instance = Instantiate(m_prefabs[shapeId]);
                    instance.OriginFactory = this;
                    instance.ShapeId = shapeId;
                    SceneManager.MoveGameObjectToScene(instance.gameObject, m_poolScene);
                }
            }
            else
            {
                instance = Instantiate(m_prefabs[shapeId]);
                instance.ShapeId = shapeId;
            }
            instance.SetMaterial(m_materials[materialId], materialId);

            Game.Instance.AddShape(instance);
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

        /// <summary>
        /// 回收对象
        /// </summary>
        /// <param name="shapeToRecycle"></param>
        public void Reclaim(Shape shapeToRecycle)
        {
            if (shapeToRecycle.OriginFactory != this)
            {
                Debug.LogError("Tried to reclaim shape with wrong factory");
            }

            if (m_recycle)
            {
                if (m_pools == null)
                {
                    CreatePools();
                }

                m_pools[shapeToRecycle.ShapeId].Add(shapeToRecycle);
                shapeToRecycle.gameObject.SetActive(false);
            }
            else
            {
                Destroy(shapeToRecycle.gameObject);
            }
        }

        /// <summary>
        /// 创建对象池
        /// </summary>
        private void CreatePools()
        {
            m_pools = new List<Shape>[m_prefabs.Length];
            for (int i = 0; i < m_pools.Length; i++)
            {
                m_pools[i] = new List<Shape>();
            }

            // 在编辑器模式下重新编译时，重新获取场景
            if (Application.isEditor)
            {
                m_poolScene = SceneManager.GetSceneByName(name);
                if (m_poolScene.isLoaded)
                {
                    GameObject[] rootObjects = m_poolScene.GetRootGameObjects();
                    for (int i = 0; i < rootObjects.Length; i++)
                    {
                        Shape pooledShape = rootObjects[i].GetComponent<Shape>();
                        if (!pooledShape.gameObject.activeSelf)
                        {
                            m_pools[pooledShape.ShapeId].Add(pooledShape);
                        }
                    }
                    return;
                }
            }

            m_poolScene = SceneManager.CreateScene(name);
        }

        #endregion

        #region 属性

        /// <summary>
        /// 工厂Id
        /// </summary>
        public int FactoryId
        {
            get { return m_factoryId; }
            set
            {
                if (m_factoryId == int.MinValue && value != int.MinValue)
                {
                    m_factoryId = value;
                }
                else
                {
                    Debug.Log("Not allowed to change factoryId.");
                }
            }
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

        /// <summary>
        /// 是否回收对象
        /// </summary>
        [SerializeField]
        private bool m_recycle;

        /// <summary>
        /// 对象池
        /// </summary>
        private List<Shape>[] m_pools;

        /// <summary>
        /// 存放对象池对象的场景
        /// </summary>
        private Scene m_poolScene;

        /// <summary>
        /// 工厂Id
        /// </summary>
        [NonSerialized]
        private int m_factoryId = int.MinValue;

        #endregion
    }
}
