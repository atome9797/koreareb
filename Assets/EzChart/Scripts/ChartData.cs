using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartUtil
{
    [System.Serializable]
    public class Data
    {
        public bool show = true;
        public float value = 0.0f;
        //public Vector2 coordinate;

        public Data()
        {

        }

        public Data(float value, bool show)
        {
            this.value = value;
            this.show = show;
        }
    }

    [System.Serializable]
    public class Series
    {
        public string name = "";
        public bool show = true;
        public List<Data> data = new List<Data>();
    }

    public class ChartData : MonoBehaviour
    {
        public List<Series> series = new List<Series>();
        public List<string> categories = new List<string>();
    }
    
    public class ChartDataInfo
    {
        public float min = 0.0f;
        public float max = 0.0f;
        public float minSum = 0.0f;
        public float maxSum = 0.0f;
        public float[] posSum = null;
        public float[] negSum = null;

        public void ComputeByCategory(ChartData data, int cateIndex)
        {
            if (data.categories == null || data.categories.Count == 0) return;

            min = minSum = float.MaxValue;
            max = maxSum = float.MinValue;

            float pSum = 0.0f, nSum = 0.0f;
            for (int j = 0; j < data.series.Count; ++j)
            {
                if (!data.series[j].show || data.series[j].data.Count == 0 || !data.series[j].data[0].show) continue;

                float value = data.series[j].data[0].value;
                if (value >= 0.0f) pSum += value;
                else nSum += value;
                if (value < min) min = value;
                if (value > max) max = value;
            }

            if (pSum > maxSum) maxSum = pSum;
            if (nSum < minSum) minSum = nSum;
        }

        public void Compute(ChartData data, bool doNeg = true)
        {
            if (data.categories == null || data.categories.Count == 0) return;

            min = minSum = float.MaxValue;
            max = maxSum = float.MinValue;
            posSum = new float[data.categories.Count];
            if (doNeg) negSum = new float[data.categories.Count];
            for (int i = 0; i < data.categories.Count; ++i)
            {
                float pSum = 0.0f, nSum = 0.0f;
                for (int j = 0; j < data.series.Count; ++j)
                {
                    if (!data.series[j].show || data.series[j].data.Count <= i || !data.series[j].data[i].show) continue;

                    float value = data.series[j].data[i].value;
                    if (value >= 0.0f) pSum += value;
                    else nSum += value;
                    if (value < min) min = value;
                    if (value > max) max = value;
                }

                if (pSum > maxSum) maxSum = pSum;
                if (nSum < minSum) minSum = nSum;
                posSum[i] = pSum;
                if (negSum != null) negSum[i] = -nSum;
            }
        }
    }
}