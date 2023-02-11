using UnityEngine;

namespace Assets.Scripts.Movement
{
    /// <summary>
    /// 自定义重力
    /// </summary>
    public static class CustomGravity
    {
        /// <summary>
        /// 根据位置来获取重力
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static Vector3 GetGravity(Vector3 position)
        {
            return position.normalized * Physics.gravity.y;
        }

        /// <summary>
        /// 根据位置来获取重力以及向上轴
        /// </summary>
        /// <param name="position"></param>
        /// <param name="upAxis"></param>
        /// <returns></returns>
        public static Vector3 GetGravity(Vector3 position, out Vector3 upAxis)
        {
            Vector3 up = position.normalized;
            upAxis = Physics.gravity.y < 0f ? up : -up;
            return up * Physics.gravity.y;
        }

        /// <summary>
        /// 根据重力来确定小球和轨道相机的向上轴
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static Vector3 GetUpAxis(Vector3 position)
        {
            Vector3 up = position.normalized;
            return Physics.gravity.y < 0f ? up : -up;
        }
    }
}
