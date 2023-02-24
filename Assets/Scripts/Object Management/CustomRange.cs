using System;
using Random = UnityEngine.Random;

namespace Assets.Scripts.Object_Management
{
    /// <summary>
    /// 浮点数范围
    /// </summary>
    [Serializable]
    public struct FloatRange
    {
        /// <summary>
        /// 最小值
        /// </summary>
        public float min;

        /// <summary>
        /// 最大值
        /// </summary>
        public float max;

        /// <summary>
        /// 范围随机值
        /// </summary>
        public float RandomValueInRange
        {
            get
            {
                return Random.Range(min, max);
            }
        }
    }

    /// <summary>
    /// 整数范围
    /// </summary>
    [Serializable]
    public struct IntRange
    {
        /// <summary>
        /// 最小值
        /// </summary>
        public int min;

        /// <summary>
        /// 最大值
        /// </summary>
        public int max;

        /// <summary>
        /// 范围随机值
        /// </summary>
        public int RandomValueInRange
        {
            get
            {
                return Random.Range(min, max + 1);
            }
        }
    }
}
