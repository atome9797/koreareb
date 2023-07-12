using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartUtil.Demo
{
    public class ChartDemoMassiveData : MonoBehaviour
    {
        [SerializeField] int length = 1000;
        [SerializeField] ChartData chartData;

        [SerializeField] Chart barChart;
        [SerializeField] Chart lineChart;

        void Start()
        {
            GenerateData();
        }

        public void GenerateData()
        {
            if (chartData == null) return;

            //new category
            chartData.categories = new List<string>();

            //new series
            chartData.series = new List<Series>();
            chartData.series.Add(new Series());
            chartData.series[0].name = "Massive Data";

            //generate data
            for (int i = 0; i < length; ++i)
            {
                chartData.categories.Add(i.ToString());
                float value = (1000 + i) * Random.Range(0.5f, 1.0f);
                chartData.series[0].data.Add(new Data(value, true));
            }

            //update chart
            if (barChart != null) barChart.UpdateChart();
            if (lineChart != null) lineChart.UpdateChart();
        }
    }
}
