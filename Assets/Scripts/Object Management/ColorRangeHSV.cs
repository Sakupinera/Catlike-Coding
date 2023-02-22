using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Scripts.Object_Management
{
    /// <summary>
    /// HSV颜色范围
    /// </summary>
    [Serializable]
    public struct ColorRangeHSV
    {

        /// <summary>
        /// 色度
        /// </summary>
        [FloatRangeSlider(0f, 1f)]
        public FloatRange m_hue;

        /// <summary>
        /// 饱和度
        /// </summary>
        [FloatRangeSlider(0f, 1f)]
        public FloatRange m_saturation;

        /// <summary>
        /// 纯度
        /// </summary>
        [FloatRangeSlider(0f, 1f)]
        public FloatRange m_value;

        /// <summary>
        /// 返回一个随机颜色值
        /// </summary>
        public Color RandomInRange
        {
            get
            {
                return Random.ColorHSV(m_hue.min, m_hue.max,
                    m_saturation.min, m_saturation.max,
                    m_value.min, m_value.max,
                    1f, 1f);
            }
        }

    }
}
