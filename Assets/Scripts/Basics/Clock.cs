using System;
using UnityEngine;

public class Clock : MonoBehaviour
{
    const float HoursToDegrees = -30f, MinutesToDegrees = -6f, SecondsToDegrees = -6;

    [SerializeField]
    Transform m_hoursPivot, m_minutesPivot, m_secondsPivot;

    private void Update()
    {
        TimeSpan time = DateTime.Now.TimeOfDay;
        m_hoursPivot.localRotation = Quaternion.Euler(0f, 0f, HoursToDegrees * (float)time.TotalHours);
        m_minutesPivot.localRotation = Quaternion.Euler(0f, 0f, MinutesToDegrees * (float)time.TotalMinutes);
        m_secondsPivot.localRotation = Quaternion.Euler(0f, 0f, SecondsToDegrees * (float)time.TotalSeconds);
    }
}
