using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartUtil
{
    public class ChartGrid
    {
        public class AxisInfo
        {
            public float min = 0.0f;
            public float max = 1.0f;
            public float count = 1;
            public float span = 1.0f;
            public float interval = 1.0f;
            public float baseLine = 0.0f;
            public float baseLineRatio = 0.0f;
            public string labelFormat = "N0";

            public void Compute(float minValue, float maxValue, int division)
            {
                if (minValue >= maxValue || division <= 0) return;

                min = minValue;
                max = maxValue;
                count = division;
                span = max - min;
                interval = span / count;

                if (interval >= 1.0f) labelFormat = "N0";
                else labelFormat = "N" + ChartHelper.FindFloatDisplayPrecision(interval).ToString();

                baseLine = 0.0f;
                if (min >= 0.0f) baseLine = min;
                else if (max < 0.0f) baseLine = max - interval;
                baseLineRatio = (baseLine - min) / span;
            }

            public void Compute(float minValue, float maxValue, int division, bool zeroBased)
            {
                if (!(minValue != 0 && maxValue != 0 && maxValue == minValue) && minValue >= maxValue)
                    return;

                if (zeroBased) { if (minValue > 0.0f) minValue = 0.0f; if (maxValue < 0.0f) maxValue = 0.0f; }

                count = division >= 1 ? division : 1;
                interval = (maxValue - minValue) / count;
                if (interval >= 1.0f)
                {
                    int i = Mathf.CeilToInt(interval);
                    int l = ChartHelper.FindIntegerLength(i);
                    int unit = (int)Mathf.Pow(10, l - 1);
                    int r = i % unit;
                    i = i - r;
                    if (r > (unit / 2)) i += unit;
                    interval = i;
                }
                else
                {
                    float l = Mathf.Pow(10, ChartHelper.FindFloatDisplayPrecision(interval));
                    interval = Mathf.Floor(interval * l) / l;
                }

                int minStep = Mathf.FloorToInt(minValue / interval);
                int maxStep = Mathf.CeilToInt(maxValue / interval);
                min = minStep * interval;
                max = maxStep * interval;
                count = maxStep - minStep;
                span = max - min;

                if (interval >= 1.0f) labelFormat = "N0";
                else labelFormat = "N" + ChartHelper.FindFloatDisplayPrecision(interval).ToString();

                baseLine = 0.0f;
                if (min >= 0.0f) baseLine = min;
                else if (max < 0.0f) baseLine = max - interval;
                baseLineRatio = (baseLine - min) / span;
            }
        }

        [HideInInspector] public ChartBase chart = null;
        [HideInInspector] public bool midGrid = false;
        [HideInInspector] public float unitWidth = 0.0f;
        [HideInInspector] public AxisInfo xAxisInfo = null;
        [HideInInspector] public AxisInfo yAxisInfo = null;
        [HideInInspector] public RectTransform gridRect;
        [HideInInspector] public RectTransform labelRect;

        public virtual void UpdateGrid() { }
        public virtual int GetItemIndex(Vector2 mousePosition) { return -1; }
        public virtual void HighlightItem(int index) { }
        public virtual void UnhighlightItem(int index) { }
    }
}