using UnityEngine;

namespace Assets.Scripts.Movement
{
    /// <summary>
    /// 重力源
    /// </summary>
    public class GravitySource : MonoBehaviour
    {
        #region 方法

        /// <summary>
        /// 根据位置获取重力源的重力
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public virtual Vector3 GetGravity(Vector3 position)
        {
            return Physics.gravity;
        }

        /// <summary>
        /// 注册重力源
        /// </summary>
        private void OnEnable()
        {
            CustomGravity.Register(this);
        }

        /// <summary>
        /// 注销重力源
        /// </summary>
        private void OnDisable()
        {
            CustomGravity.Unregister(this);
        }

        #endregion
    }
}
