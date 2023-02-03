using TMPro;
using UnityEngine;

namespace Assets.Scripts.Basics
{
    /// <summary>
    /// ֡����ʾ��
    /// </summary>
    public class FrameRateCounter : MonoBehaviour
    {
        #region ����

        /// <summary>
        /// UI�����߼�
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

        #region �������ֶ�

        /// <summary>
        /// Text���
        /// </summary>
        [SerializeField]
        TextMeshProUGUI m_display;

        /// <summary>
        /// ��ǰ��ʾ��ʽ
        /// </summary>
        [SerializeField]
        private DisplayMode m_displayMode = DisplayMode.FPS;

        /// <summary>
        /// �ı�����ʱ��
        /// </summary>
        [SerializeField, Range(0.1f, 2f)]
        private float m_sampleDuration = 1f;

        /// <summary>
        /// ֡��
        /// </summary>
        private int m_frames;

        /// <summary>
        /// ��ǰ֡���ʱ��
        /// </summary>
        private float m_duration;

        /// <summary>
        /// ����֡���ʱ��
        /// </summary>
        private float m_bestDuration = float.MaxValue;

        /// <summary>
        /// ������֡���ʱ��
        /// </summary>
        private float m_worstDuration;

        #endregion

        #region ö��

        /// <summary>
        /// ֡����ʾ��ʽ
        /// </summary>
        public enum DisplayMode { FPS, MS }

        #endregion
    }
}
