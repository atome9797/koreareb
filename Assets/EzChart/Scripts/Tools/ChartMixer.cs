using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartUtil
{
    public enum MixMethod
    {
        None, BarToLine, LineToBar
    }

    public class ChartMixer : MonoBehaviour
    {
        [SerializeField] MixMethod mixMethod;
        [SerializeField] List<int> seriesToMix;

        public void UpdateChart(ChartBase chart)
        {
            if (seriesToMix.Count == 0) { chart.UpdateChart(); return; }

            switch (mixMethod)
            {
                case MixMethod.BarToLine:
                    {
                        BarChart barChart = chart.GetComponent<BarChart>();
                        if (barChart != null)
                        {
                            LineChart lineChart = barChart.gameObject.AddComponent<LineChart>();
                            lineChart.tooltip = barChart.tooltip;
                            lineChart.chartData = barChart.chartData;
                            lineChart.chartOptions = barChart.chartOptions;
                            lineChart.chartDataInfo = barChart.chartDataInfo;
                            for (int i = 0; i < barChart.chartData.series.Count; ++i)
                            {
                                if (seriesToMix.Contains(i)) barChart.skipSeries.Add(i);
                                else lineChart.skipSeries.Add(i);
                            }
                            barChart.UpdateChart();
                            lineChart.chartGrid = barChart.chartGrid;
                            lineChart.UpdateChart();
                        }
                        else chart.UpdateChart();
                    }
                    break;
                case MixMethod.LineToBar:
                    {
                        LineChart lineChart = chart.GetComponent<LineChart>();
                        if (lineChart != null)
                        {
                            BarChart barChart = lineChart.gameObject.AddComponent<BarChart>();
                            barChart.tooltip = lineChart.tooltip;
                            barChart.chartData = lineChart.chartData;
                            barChart.chartOptions = lineChart.chartOptions;
                            barChart.chartDataInfo = lineChart.chartDataInfo;
                            for (int i = 0; i < lineChart.chartData.series.Count; ++i)
                            {
                                if (seriesToMix.Contains(i)) lineChart.skipSeries.Add(i);
                                else barChart.skipSeries.Add(i);
                            }
                            lineChart.UpdateChart();
                            barChart.chartGrid = lineChart.chartGrid;
                            barChart.UpdateChart();
                        }
                        else chart.UpdateChart();
                    }
                    break;
                default:
                    chart.UpdateChart();
                    break;
            }
        }
    }
}