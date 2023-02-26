#if UNITY_EDITOR

using UnityEngine;

namespace Assets.Scripts.Object_Management
{
    /// <summary>
    /// 游戏关卡
    /// </summary>
    public partial class GameLevel : PersistableObject
    {
        #region 方法

        /// <summary>
        /// 移除丢失的关卡对象
        /// </summary>
        public void RemoveMissingLevelObjects()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("Do not invoke in play mode!");
                return;
            }

            int holes = 0;
            for (int i = 0; i < m_levelObjects.Length - holes; i++)
            {
                if (m_levelObjects[i] == null)
                {
                    holes += 1;
                    System.Array.Copy(m_levelObjects, i + 1, m_levelObjects,
                        i, m_levelObjects.Length - i - holes);
                }

                i -= 1;
            }

            // Once we're done with that we have to get rid of the redundant tail of the array, by reducing its length by the number of holes.
            System.Array.Resize(ref m_levelObjects, m_levelObjects.Length - holes);
        }

        /// <summary>
        /// 当前是否存在某关卡对象
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public bool HasLevelObject(GameLevelObject o)
        {
            if (m_levelObjects != null)
            {
                for (int i = 0; i < m_levelObjects.Length; i++)
                {
                    if (m_levelObjects[i] == o)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 注册关卡对象
        /// </summary>
        /// <param name="o"></param>
        public void RegisterLevelObject(GameLevelObject o)
        {
            if (Application.isPlaying)
            {
                Debug.LogError("Do not invoke in play mode!");
                return;
            }

            if (HasLevelObject(o))
                return;

            if (m_levelObjects == null)
            {
                m_levelObjects = new GameLevelObject[] { o };
            }
            else
            {
                System.Array.Resize(ref m_levelObjects, m_levelObjects.Length + 1);
                m_levelObjects[m_levelObjects.Length - 1] = o;
            }
        }

        #endregion

        #region 属性

        /// <summary>
        /// 是否有丢失的关卡对象
        /// </summary>
        public bool HasMissingLevelObjects
        {
            get
            {
                if (m_levelObjects != null)
                {
                    for (int i = 0; i < m_levelObjects.Length; i++)
                    {
                        if (m_levelObjects[i] == null)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        #endregion
    }
}

#endif
