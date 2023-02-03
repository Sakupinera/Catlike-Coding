using TMPro;
using UnityEngine;

namespace Assets.Scripts.Basics
{
    /// <summary>
    /// 帧率显示器
    /// </summary>
    public class FrameRateCounter : MonoBehaviour
    {
        #region 方法

        /// <summary>
        /// UI更新逻辑
        /// </summary>
        private void Update()
        {
            float frameDuration = Time.unscaledDeltaTime;
            m_frames += 1;
            m_duration += frameDuration;
            //display.SetText("FPS\n{0:0}\n000\n000", 1f / frameDuration);

            if (frameDuration < m_bestDuration)
            {
                m_bestDuration = frameDuration;
            }
            if (frameDuration > m_worstDuration)
            {
                m_worstDuration = frameDuration;
            }

            if (m_duration >= m_sampleDuration)
            {
                if (m_displayMode == DisplayMode.FPS)
                {
                    m_display.SetText("FPS\n{0:0}\n{1:0}\n{2:0}", 1f / m_bestDuration, m_frames / m_duration, 1f / m_worstDuration);
                }
                else
                {
                    m_display.SetText("FPS\n{0:1}\n{1:1}\n{2:1}", 1000f * m_bestDuration, 1000f * m_duration / m_frames, 1000f * m_worstDuration);
                }
                m_frames = 0;
                m_duration = 0f;
                m_bestDuration = float.MaxValue;
                m_worstDuration = 0f;
            }
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// Text组件
        /// </summary>
        [SerializeField]
        TextMeshProUGUI m_display;

        /// <summary>
        /// 当前显示方式
        /// </summary>
        [SerializeField]
        private DisplayMode m_displayMode = DisplayMode.FPS;

        /// <summary>
        /// 文本更新时间
        /// </summary>
        [SerializeField, Range(0.1f, 2f)]
        private float m_sampleDuration = 1f;

        /// <summary>
        /// 帧率
        /// </summary>
        private int m_frames;

        /// <summary>
        /// 当前帧间隔时间
        /// </summary>
        private float m_duration;

        /// <summary>
        /// 最快的帧间隔时间
        /// </summary>
        private float m_bestDuration = float.MaxValue;

        /// <summary>
        /// 最慢的帧间隔时间
        /// </summary>
        private float m_worstDuration;

        #endregion

        #region 枚举

        /// <summary>
        /// 帧率显示方式
        /// </summary>
        public enum DisplayMode { FPS, MS }

        #endregion
    }
}
