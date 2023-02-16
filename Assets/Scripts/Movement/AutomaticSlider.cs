using UnityEngine;
using UnityEngine.Events;

namespace Assets.Scripts.Movement
{
    public class AutomaticSlider : MonoBehaviour
    {
        #region 方法

        /// <summary>
        /// 每帧进行插值
        /// </summary>
        private void FixedUpdate()
        {
            float delta = Time.deltaTime / m_duration;
            if (Reversed)
            {
                m_value -= delta;
                if (m_value <= 0f)
                {
                    if (m_autoReverse)
                    {
                        m_value = Mathf.Min(1f, -m_value);
                        Reversed = false;
                    }
                    else
                    {
                        m_value = 0f;
                        enabled = false;
                    }
                }
            }
            else
            {
                m_value += delta;
                if (m_value >= 1f)
                {
                    if (m_autoReverse)
                    {
                        m_value = Mathf.Max(0f, 2f - m_value);
                        Reversed = true;
                    }
                    else
                    {
                        m_value = 1f;
                        enabled = false;
                    }
                }
            }
            OnValueChanged.Invoke(m_smoothstep ? SmoothedValue : m_value);
        }

        #endregion

        #region 属性

        /// <summary>
        /// 是否反向
        /// </summary>
        public bool Reversed { get; set; }

        /// <summary>
        /// 自动反向
        /// </summary>
        public bool AutoReverse
        {
            get => m_autoReverse;
            set => m_autoReverse = value;
        }

        /// <summary>
        /// 平滑插值
        /// </summary>
        private float SmoothedValue => 3f * m_value * m_value - 2f * m_value * m_value * m_value;

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 持续时间
        /// </summary>
        [SerializeField, Min(0.01f)]
        private float m_duration = 1f;

        /// <summary>
        /// 自动倒置运动
        /// </summary>
        [SerializeField]
        private bool m_autoReverse = false;

        /// <summary>
        /// 是否平滑步长
        /// </summary>
        [SerializeField]
        private bool m_smoothstep = false;

        /// <summary>
        /// 用于插值的值
        /// </summary>
        private float m_value;

        /// <summary>
        /// 当值改变时触发的事件
        /// </summary>
        [SerializeField]
        private UnityEvent<float> OnValueChanged = default;

        #endregion
    }
}
