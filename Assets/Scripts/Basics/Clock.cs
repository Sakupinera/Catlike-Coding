using System;
using UnityEngine;

namespace Assets.Scripts.Basics
{
    /// <summary>
    /// 时钟
    /// </summary>
    public class Clock : MonoBehaviour
    {
        #region 方法

        /// <summary>
        /// 更新时钟时间
        /// </summary>
        private void Update()
        {
            TimeSpan time = DateTime.Now.TimeOfDay;
            m_hoursPivot.localRotation = Quaternion.Euler(0f, 0f, HoursToDegrees * (float)time.TotalHours);
            m_minutesPivot.localRotation = Quaternion.Euler(0f, 0f, MinutesToDegrees * (float)time.TotalMinutes);
            m_secondsPivot.localRotation = Quaternion.Euler(0f, 0f, SecondsToDegrees * (float)time.TotalSeconds);
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 时、分、秒指针对应的Transform
        /// </summary>
        [SerializeField]
        private Transform m_hoursPivot, m_minutesPivot, m_secondsPivot;

        #endregion

        #region 常量

        /// <summary>
        /// 时、分、秒指针每刻度的旋转角度
        /// </summary>
        private const float HoursToDegrees = -30f, MinutesToDegrees = -6f, SecondsToDegrees = -6f;

        #endregion
    }
}
