using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.Scripts.Movement
{
    /// <summary>
    /// 检测区域
    /// </summary>
    public class DetectionZone : MonoBehaviour
    {
        #region 方法

        /// <summary>
        /// 初始化
        /// </summary>
        private void Awake()
        {
            enabled = false;
        }

        /// <summary>
        /// 在碰撞体列表为空时保证事件被触发
        /// </summary>
        private void FixedUpdate()
        {
            for (int i = 0; i < m_colliders.Count; i++)
            {
                Collider collider = m_colliders[i];
                if (!collider || !collider.gameObject.activeInHierarchy)
                {
                    m_colliders.RemoveAt(i--);
                    if (m_colliders.Count == 0)
                    {
                        OnLastExit.Invoke();
                        enabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// 当物体进入时触发事件
        /// </summary>
        /// <param name="other"></param>
        private void OnTriggerEnter(Collider other)
        {
            if (m_colliders.Count == 0)
            {
                OnFirstEnter.Invoke();
                enabled = true;
            }

            m_colliders.Add(other);
        }

        /// <summary>
        /// 当物体退出时触发事件
        /// </summary>
        /// <param name="other"></param>
        private void OnTriggerExit(Collider other)
        {
            // 只有在碰撞体列表为空时才触发退出事件
            if (m_colliders.Remove(other) && m_colliders.Count == 0)
            {
                OnLastExit.Invoke();
                enabled = false;
            }
        }

        /// <summary>
        /// 当脚本自身被停用或销毁时，清理自身并触发退出事件
        /// </summary>
        private void OnDisable()
        {
            // 防止编辑器热重载时自动调用OnDisable
#if UNITY_EDITOR
            if (enabled && gameObject.activeInHierarchy)
            {
                Debug.LogError("HHH");
                return;
            }
#endif
            if (m_colliders.Count > 0)
            {
                m_colliders.Clear();
                OnLastExit.Invoke();
            }
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 是否有物体进入
        /// </summary>
        [SerializeField]
        private UnityEvent OnFirstEnter = default;
        
        /// <summary>
        /// 是否有物体退出
        /// </summary>
        [SerializeField]
        private UnityEvent OnLastExit = default;

        /// <summary>
        /// 当前区域的碰撞体列表
        /// </summary>
        private List<Collider> m_colliders = new List<Collider>();

        #endregion
    }
}
