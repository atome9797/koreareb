using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ChartUtil
{
    [System.Serializable]
    public class SeriesToggleEvent : UnityEvent<int, bool>
    {
    }

    [System.Serializable]
    public class ItemClickEvent : UnityEvent<int, int>
    {
    }

    public class ChartEvents : MonoBehaviour
    {
        [Tooltip("Triggered when series is toggled on/off. Parameters: series index, on/off")]
        public SeriesToggleEvent seriesToggleEvent = new SeriesToggleEvent();
        [Tooltip("Triggered when chart items are clicked. Parametrs: category index, series index. (-1 means invalid value)")]
        public ItemClickEvent itemClickEvent = new ItemClickEvent();
    }
}