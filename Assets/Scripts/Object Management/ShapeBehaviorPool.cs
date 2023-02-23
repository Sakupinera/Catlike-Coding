using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Object_Management
{
    /// <summary>
    /// 形状行为池
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class ShapeBehaviorPool<T> where T : ShapeBehavior, new()
    {
        #region 方法

        /// <summary>
        /// 获取行为对象
        /// </summary>
        /// <returns></returns>
        public static T Get()
        {
            if (m_stack.Count > 0)
            {
                T behavior = m_stack.Pop();
#if UNITY_EDITOR
                behavior.IsReclaimed = false;
#endif
                return behavior;
            }

#if UNITY_EDITOR
            return ScriptableObject.CreateInstance<T>();
#else
            return new T();
#endif
        }

        /// <summary>
        /// 回收行为对象
        /// </summary>
        /// <param name="behavior"></param>
        public static void Reclaim(T behavior)
        {
#if UNITY_EDITOR
            behavior.IsReclaimed = true;
#endif
            m_stack.Push(behavior);
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 未使用行为对象的栈
        /// </summary>
        private static Stack<T> m_stack = new Stack<T>();

        #endregion
    }
}
