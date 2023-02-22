using UnityEngine;

namespace Assets.Scripts.Object_Management
{
    /// <summary>
    /// 浮点范围滑动条特性
    /// </summary>
    public class FloatRangeSliderAttribute : PropertyAttribute
    {
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        public FloatRangeSliderAttribute(float min, float max)
        {
            if (max < min)
            {
                max = min;
            }

            Min = min;
            Max = max;
        }

        /// <summary>
        /// 最小值
        /// </summary>
        public float Min { get; private set; }

        /// <summary>
        /// 最大值
        /// </summary>
        public float Max { get; private set; }
    }
}
