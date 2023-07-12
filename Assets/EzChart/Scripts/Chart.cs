using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if CHART_TMPRO
using TMPro;
#endif

namespace ChartUtil
{
    [ExecuteInEditMode]
    public class Chart : MonoBehaviour
    {
        public ChartOptions chartOptions = null;
        public ChartData chartData = null;
        public ChartType chartType = ChartType.BarChart;
        [SerializeField] bool updateOnAwake = true;

        Vector2 offsetMin, offsetMax;
        ChartBase chart;
        RectTransform chartRect;
        RectTransform legendRect;
        public RectTransform ygridRect;

        public void Clear()
        {
            ChartHelper.Clear(transform);
            if(ygridRect != null)
            {
                ChartHelper.Clear(ygridRect);
            }            
        }

        private void Start()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
#endif
            if (updateOnAwake) UpdateChart();
        }

        public void ToggleSeries(int index)
        {
            chartData.series[index].show = !chartData.series[index].show;
            UpdateChart();
            if (chart.chartEvents != null) chart.chartEvents.seriesToggleEvent.Invoke(index, chartData.series[index].show);
        }

        public void UpdateChart()
        {
            if (chartOptions == null || chartData == null) return;
            Clear();
#if CHART_TMPRO
            if (chartOptions.plotOptions.generalFont == null)
                chartOptions.plotOptions.generalFont = Resources.Load("Fonts & Materials/LiberationSans SDF", typeof(TMP_FontAsset)) as TMP_FontAsset;
#else
            if (chartOptions.plotOptions.generalFont == null)
                chartOptions.plotOptions.generalFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif

            chartRect = ChartHelper.CreateEmptyRect("Content", transform);
            legendRect = ChartHelper.CreateEmptyRect("Legends", transform);
            switch (chartType)
            {
                case ChartType.BarChart:
                    chart = chartRect.gameObject.AddComponent<BarChart>();
                    break;
                case ChartType.LineChart:
                    chart = chartRect.gameObject.AddComponent<LineChart>();
                    break;
                case ChartType.PieChart:
                    chart = chartRect.gameObject.AddComponent<PieChart>();
                    break;
                case ChartType.RoseChart:
                    chart = chartRect.gameObject.AddComponent<RoseChart>();
                    break;
                case ChartType.RadarChart:
                    chart = chartRect.gameObject.AddComponent<RadarChart>();
                    break;
                case ChartType.Gauge:
                    chart = chartRect.gameObject.AddComponent<Gauge>();
                    break;
                case ChartType.SolidGauge:
                    chart = chartRect.gameObject.AddComponent<SolidGauge>();
                    break;
            }

            //Debug.Log($"<color=yellow>[UpdateChart]</color>");

            if (ygridRect != null)
            {
                //Debug.Log($"<color=yellow>[UpdateYAxisLabel]{ygridRect.name}</color>");

                chart.m = ygridRect;
            }

            offsetMin = offsetMax = Vector2.zero;
            chart.chartData = chartData;
            chart.chartOptions = chartOptions;
            chart.chartSize = GetComponent<RectTransform>().rect.size;
            chart.chartEvents = GetComponent<ChartEvents>();
            chart.chartDataInfo = new ChartDataInfo();
            if (chartType == ChartType.PieChart) chart.chartDataInfo.ComputeByCategory(chartData, 0);
            else if (chartType == ChartType.SolidGauge || chartType == ChartType.Gauge) chart.chartDataInfo.max = chart.chartDataInfo.maxSum = chartOptions.yAxis.max;
            else if (chartType == ChartType.RadarChart || chartType == ChartType.RoseChart) chart.chartDataInfo.Compute(chartData, false);
            else chart.chartDataInfo.Compute(chartData);

            if (chartOptions.tooltip.enable) UpdateTooltip();
            if (chartOptions.title.enableMainTitle) UpdateMainTitle();
            if (chartOptions.title.enableSubTitle) UpdateSubTitle();
            if (chartOptions.legend.enable && chartType != ChartType.Gauge) UpdateLegend();
            UpdateContent();
        }

        void UpdateTooltip()
        {
            chart.tooltip = ChartHelper.CreateEmptyRect("ChartTooltip", transform).gameObject.AddComponent<ChartTooltip>();

            chart.tooltip.background = chart.tooltip.gameObject.AddComponent<Image>();
            chart.tooltip.background.rectTransform.anchorMin = Vector2.zero;
            chart.tooltip.background.rectTransform.anchorMax = Vector2.zero;
            chart.tooltip.background.rectTransform.pivot = new Vector2(0.5f, 0.0f);
            chart.tooltip.background.sprite = Resources.Load<Sprite>("Images/Chart_Square");
            chart.tooltip.background.color = chartOptions.tooltip.backgroundColor;
            chart.tooltip.background.type = Image.Type.Sliced;
            chart.tooltip.background.raycastTarget = false;

            Canvas c = chart.tooltip.gameObject.AddComponent<Canvas>();
            c.overrideSorting = true;
            c.sortingOrder = 10000;
            chart.tooltip.tooltipText = ChartHelper.CreateText("TooltipText", chart.tooltip.transform, chartOptions.tooltip.textOption, chartOptions.plotOptions.generalFont, TextAnchor.UpperLeft, true);
            chart.tooltip.tooltipText.rectTransform.offsetMin = new Vector2(8, 3);
            chart.tooltip.tooltipText.rectTransform.offsetMax = new Vector2(-8, -3);

            chart.tooltip.Init();
            chart.tooltip.chartSize = chart.chartSize;
            chart.tooltip.parentPivot = GetComponent<RectTransform>().pivot;
            chart.tooltip.gameObject.SetActive(false);
        }

        void UpdateMainTitle()
        {
            var mainTitle = ChartHelper.CreateText("MainTitle", transform, chartOptions.title.mainTitleOption, chartOptions.plotOptions.generalFont, TextAnchor.MiddleRight);
            mainTitle.text = chartOptions.title.mainTitle;
            if (mainTitle.preferredWidth > chart.chartSize.x) ChartHelper.TruncateText(mainTitle, chart.chartSize.x);

            mainTitle.rectTransform.anchorMin = new Vector2(1.0f, 0.0f);
            mainTitle.rectTransform.anchorMax = new Vector2(1.0f, 0.0f);

            mainTitle.rectTransform.pivot = new Vector2(1.0f, 1.0f);
            mainTitle.rectTransform.anchoredPosition = new Vector2(0, 0);
            mainTitle.rectTransform.sizeDelta = new Vector2(120, 16);




            //float height = mainTitle.fontSize * 1.4f;
            //mainTitle.rectTransform.anchorMin = new Vector2(0.0f, 1.0f);
            //mainTitle.rectTransform.anchorMax = new Vector2(1.0f, 1.0f);
            //mainTitle.rectTransform.offsetMin = new Vector2(0.0f, -height);
            //mainTitle.rectTransform.offsetMax = new Vector2(0.0f, 0.0f);

            //offsetMax.y -= height;
        }

        void UpdateSubTitle()
        {
            var subTitle = ChartHelper.CreateText("SubTitle", transform, chartOptions.title.subTitleOption, chartOptions.plotOptions.generalFont);
            subTitle.text = chartOptions.title.subTitle;
            if (subTitle.preferredWidth > chart.chartSize.x) ChartHelper.TruncateText(subTitle, chart.chartSize.x);

            float height = subTitle.fontSize * 1.2f;
            subTitle.rectTransform.anchorMin = new Vector2(0.0f, 1.0f);
            subTitle.rectTransform.anchorMax = new Vector2(1.0f, 1.0f);
            subTitle.rectTransform.offsetMin = new Vector2(0.0f, offsetMax.y - height);
            subTitle.rectTransform.offsetMax = new Vector2(0.0f, offsetMax.y);
            offsetMax.y -= height;
        }

        string GetFormattedLegendText(int seriesIndex, string format, float sum)
        {
            string f = chartOptions.legend.numericFormat;
            format = format.Replace("\\n", "\n");
            format = format.Replace("{series.name}", chartData.series[seriesIndex].name);
            if ((chartType == ChartType.PieChart || chartType == ChartType.SolidGauge))
            {
                format = format.Replace("{data.value}", ChartBase.GetValueString(chartData.series[seriesIndex].data[0].value, f));
                float pValue = chartData.series[seriesIndex].show && chartData.series[seriesIndex].data[0].show ? chartData.series[seriesIndex].data[0].value / sum * 100 : 0.0f;
                format = format.Replace("{data.percentage}", ChartBase.GetPercentageString(pValue, f));
            }
            else
            {
                format = format.Replace("{data.value}", "");
                format = format.Replace("{data.percentage}", "");
            }
            return format;
        }

        void UpdateLegend()
        {
            //legend template
            ChartLegend legendTemp = ChartHelper.CreateEmptyRect("ChartLegend", transform).gameObject.AddComponent<ChartLegend>();
            legendTemp.background = legendTemp.gameObject.AddComponent<Image>();
            legendTemp.background.sprite = Resources.Load<Sprite>("Images/Chart_Square");
            legendTemp.background.color = chartOptions.legend.backgroundColor;
            legendTemp.background.type = Image.Type.Sliced;
            legendTemp.text = ChartHelper.CreateText("LegendLabel", legendTemp.transform, chartOptions.legend.textOption, chartOptions.plotOptions.generalFont, TextAnchor.MiddleLeft, true);
            legendTemp.text.rectTransform.offsetMin = new Vector2(chartOptions.legend.enableIcon? legendTemp.text.fontSize * 1.5f : 0.0f, 0.0f);
            if (chartOptions.legend.enableIcon && 
                !(chartType == ChartType.BarChart && chartOptions.plotOptions.barChartOption.colorByCategories) &&
                !(chartType == ChartType.RoseChart && chartOptions.plotOptions.roseChartOption.colorByCategories))
            {
                legendTemp.icon = ChartHelper.CreateImage("Icon", legendTemp.transform);
                legendTemp.icon.rectTransform.anchorMin = new Vector2(0.0f, 0.5f);
                legendTemp.icon.rectTransform.anchorMax = new Vector2(0.0f, 0.5f);
                legendTemp.icon.rectTransform.sizeDelta = new Vector2(legendTemp.text.fontSize * 0.75f, legendTemp.text.fontSize * 0.75f);
                legendTemp.icon.rectTransform.anchoredPosition = new Vector2(legendTemp.text.fontSize * 0.75f, 0.0f);
            }

            //update items
            float itemMaxWidth = 0.0f;
            float baseWidth = legendTemp.icon == null ? 0.0f : legendTemp.text.fontSize * 2.0f;
            Vector2 itemSumSize = Vector2.zero;
            List<ChartLegend> legendList = new List<ChartLegend>();
            for (int i = 0; i < chartData.series.Count; ++i)
            {
                ChartLegend legend = Instantiate(legendTemp, legendRect);
                legend.gameObject.name = chartData.series[i].name;
                legend.text.text = GetFormattedLegendText(i, chartOptions.legend.format, chart.chartDataInfo.maxSum);

                float width = legend.text.preferredWidth + baseWidth;
                if (width > itemMaxWidth) itemMaxWidth = width;
                legend.background.rectTransform.sizeDelta = new Vector2(width, legendTemp.text.fontSize * 1.5f);
                legend.Init(i, this);
                legend.SetStatus(chartData.series[i].show);
                itemSumSize += legend.background.rectTransform.sizeDelta;
                legendList.Add(legend);
            }

            //update rect
            int alignment = 0;
            float limitW = 0.0f, limitH = 0.0f;
            bool controlW = false, controlH = false;
            float offset = 0.0f;
            if (chartOptions.legend.itemLayout == RectTransform.Axis.Horizontal)
            {
                int rows = chartOptions.legend.horizontalRows < 1 ? 1 : chartOptions.legend.horizontalRows;
                switch (chartOptions.legend.alignment)
                {
                    case TextAnchor.LowerCenter:
                    case TextAnchor.LowerLeft:
                    case TextAnchor.LowerRight:
                        alignment = (int)chartOptions.legend.alignment % 3;
                        limitW = chart.chartSize.x;
                        limitH = chart.chartSize.y - chart.chartSize.x > chart.chartSize.x ? chart.chartSize.y - chart.chartSize.x : chart.chartSize.y * 0.4f;
                        offset = Mathf.Clamp(legendTemp.text.fontSize * 1.5f * rows, 0.0f, limitH);
                        legendRect.anchorMin = new Vector2(0.0f, 0.0f);
                        legendRect.anchorMax = new Vector2(1.0f, 0.0f);
                        legendRect.offsetMin = new Vector2(0.0f, 0.0f);
                        legendRect.offsetMax = new Vector2(0.0f, offset);
                        offsetMin.y += offset;
                        break;
                    case TextAnchor.UpperCenter:
                    case TextAnchor.UpperLeft:
                    case TextAnchor.UpperRight:
                        alignment = (int)chartOptions.legend.alignment % 3;
                        limitW = chart.chartSize.x;
                        limitH = chart.chartSize.y - chart.chartSize.x > chart.chartSize.x ? chart.chartSize.y - chart.chartSize.x : chart.chartSize.y * 0.4f;
                        offset = Mathf.Clamp(legendTemp.text.fontSize * 1.5f * rows, 0.0f, limitH);
                        legendRect.anchorMin = new Vector2(0.0f, 1.0f);
                        legendRect.anchorMax = new Vector2(1.0f, 1.0f);
                        legendRect.offsetMin = new Vector2(0.0f, offsetMax.y - offset);
                        legendRect.offsetMax = new Vector2(0.0f, offsetMax.y);
                        offsetMax.y -= offset;
                        break;
                    case TextAnchor.MiddleLeft:
                        alignment = 1;
                        limitW = chart.chartSize.x - chart.chartSize.y > chart.chartSize.y ? chart.chartSize.x - chart.chartSize.y : chart.chartSize.x * 0.4f;
                        limitH = chart.chartSize.y;
                        offset = Mathf.Clamp(itemSumSize.x, 0.0f, limitW);
                        legendRect.anchorMin = new Vector2(0.0f, 0.0f);
                        legendRect.anchorMax = new Vector2(0.0f, 1.0f);
                        legendRect.offsetMin = new Vector2(0.0f, 0.0f);
                        legendRect.offsetMax = new Vector2(offset, offsetMax.y);
                        offsetMin.x += offset;
                        break;
                    case TextAnchor.MiddleRight:
                        alignment = 1;
                        limitW = chart.chartSize.x - chart.chartSize.y > chart.chartSize.y ? chart.chartSize.x - chart.chartSize.y : chart.chartSize.x * 0.4f;
                        limitH = chart.chartSize.y;
                        offset = Mathf.Clamp(itemSumSize.x, 0.0f, limitW);
                        legendRect.anchorMin = new Vector2(1.0f, 0.0f);
                        legendRect.anchorMax = new Vector2(1.0f, 1.0f);
                        legendRect.offsetMin = new Vector2(-offset, 0.0f);
                        legendRect.offsetMax = new Vector2(0.0f, offsetMax.y);
                        offsetMax.x -= offset;
                        break;
                    default:
                        alignment = 1;
                        limitW = chart.chartSize.x;
                        limitH = chart.chartSize.y;
                        legendRect.anchorMin = new Vector2(0.0f, 0.0f);
                        legendRect.anchorMax = new Vector2(1.0f, 1.0f);
                        legendRect.offsetMin = new Vector2(0.0f, 0.0f);
                        legendRect.offsetMax = new Vector2(0.0f, offsetMax.y);
                        break;
                }

                if (rows > 1)
                {
                    ChartHelper.AddVerticalLayout(legendRect.gameObject, true, false, alignment);
                    List<RectTransform> legendRow = new List<RectTransform>();
                    List<ChartLegend>[] legends = new List<ChartLegend>[rows];
                    float[] sumWidth = new float[rows];
                    for (int i = 0; i < rows; ++i)
                    {
                        RectTransform row = ChartHelper.CreateEmptyRect("Legends", legendRect);
                        row.sizeDelta = new Vector2(0.0f, legendTemp.text.fontSize * 1.5f);
                        legendRow.Add(row);
                        legends[i] = new List<ChartLegend>();
                    }

                    int num = Mathf.CeilToInt(legendList.Count / (float)rows);
                    for (int i = 0; i < legendList.Count; ++i)
                    {
                        int index = i / num;
                        legendList[i].transform.SetParent(legendRow[index]);
                        legends[index].Add(legendList[i]);
                        sumWidth[index] += legendList[i].background.rectTransform.sizeDelta.x;
                    }

                    for (int i = 0; i < rows; ++i)
                    {
                        if (sumWidth[i] > limitW)
                        {
                            controlW = true;
                            float wLimit = limitW / legends[i].Count - legendTemp.text.fontSize * 1.5f;
                            for (int j = 0; j < legends[i].Count; ++j)
                            {
                                if (legends[i][j].text.preferredWidth > wLimit) ChartHelper.TruncateText(legends[i][j].text, wLimit);
                            }
                        }
                        else
                        {
                            controlW = false;
                        }
                        ChartHelper.AddHorizontalLayout(legendRow[i].gameObject, controlW, controlH, alignment);
                    }
                }
                else
                {
                    if (itemSumSize.x > limitW)
                    {
                        controlW = true;
                        float wLimit = limitW / legendList.Count - legendTemp.text.fontSize * 1.5f;
                        foreach (ChartLegend l in legendList) if (l.text.preferredWidth > wLimit) ChartHelper.TruncateText(l.text, wLimit);
                    }
                    ChartHelper.AddHorizontalLayout(legendRect.gameObject, controlW, controlH, alignment);
                }
            }
            else
            {
                limitH = Mathf.Clamp(itemSumSize.y, 0.0f, chart.chartSize.y * 0.4f);
                switch (chartOptions.legend.alignment)
                {
                    case TextAnchor.MiddleLeft:
                    case TextAnchor.UpperLeft:
                    case TextAnchor.LowerLeft:
                        alignment = (int)chartOptions.legend.alignment / 3;
                        limitW = chart.chartSize.x - chart.chartSize.y > chart.chartSize.y ? chart.chartSize.x - chart.chartSize.y : chart.chartSize.x * 0.4f;
                        limitH = chart.chartSize.y;
                        offset = Mathf.Clamp(itemMaxWidth, 0.0f, limitW);
                        legendRect.anchorMin = new Vector2(0.0f, 0.0f);
                        legendRect.anchorMax = new Vector2(0.0f, 1.0f);
                        legendRect.offsetMin = new Vector2(0.0f, 0.0f);
                        legendRect.offsetMax = new Vector2(offset, offsetMax.y);
                        offsetMin.x += offset;
                        break;
                    case TextAnchor.MiddleRight:
                    case TextAnchor.UpperRight:
                    case TextAnchor.LowerRight:
                        alignment = (int)chartOptions.legend.alignment / 3;
                        limitW = chart.chartSize.x - chart.chartSize.y > chart.chartSize.y ? chart.chartSize.x - chart.chartSize.y : chart.chartSize.x * 0.4f;
                        limitH = chart.chartSize.y;
                        offset = Mathf.Clamp(itemMaxWidth, 0.0f, limitW);
                        legendRect.anchorMin = new Vector2(1.0f, 0.0f);
                        legendRect.anchorMax = new Vector2(1.0f, 1.0f);
                        legendRect.offsetMin = new Vector2(-offset, 0.0f);
                        legendRect.offsetMax = new Vector2(0.0f, offsetMax.y);
                        offsetMax.x -= offset;
                        break;
                    case TextAnchor.LowerCenter:
                        alignment = 1;
                        limitW = chart.chartSize.x;
                        limitH = chart.chartSize.y - chart.chartSize.x > chart.chartSize.x ? chart.chartSize.y - chart.chartSize.x : chart.chartSize.y * 0.4f;
                        offset = Mathf.Clamp(itemSumSize.y, 0.0f, limitH);
                        legendRect.anchorMin = new Vector2(0.0f, 0.0f);
                        legendRect.anchorMax = new Vector2(1.0f, 0.0f);
                        legendRect.offsetMin = new Vector2(0.0f, 0.0f);
                        legendRect.offsetMax = new Vector2(0.0f, offset);
                        offsetMin.y += offset;
                        break;
                    case TextAnchor.UpperCenter:
                        alignment = 1;
                        limitW = chart.chartSize.x;
                        limitH = chart.chartSize.y - chart.chartSize.x > chart.chartSize.x ? chart.chartSize.y - chart.chartSize.x : chart.chartSize.y * 0.4f;
                        offset = Mathf.Clamp(itemSumSize.y, 0.0f, limitH);
                        legendRect.anchorMin = new Vector2(0.0f, 1.0f);
                        legendRect.anchorMax = new Vector2(1.0f, 1.0f);
                        legendRect.offsetMin = new Vector2(0.0f, offsetMax.y - offset);
                        legendRect.offsetMax = new Vector2(0.0f, offsetMax.y);
                        offsetMax.y -= offset;
                        break;
                    default:
                        alignment = 1;
                        limitW = chart.chartSize.x;
                        limitH = chart.chartSize.y;
                        legendRect.anchorMin = new Vector2(0.0f, 0.0f);
                        legendRect.anchorMax = new Vector2(1.0f, 1.0f);
                        legendRect.offsetMin = new Vector2(0.0f, 0.0f);
                        legendRect.offsetMax = new Vector2(0.0f, offsetMax.y);
                        break;
                }
                if (itemMaxWidth > limitW)
                {
                    controlW = true;
                    foreach (ChartLegend l in legendList) if (l.text.preferredWidth > limitW) ChartHelper.TruncateText(l.text, limitW);
                }
                if (itemSumSize.y > limitH) controlH = true;
                ChartHelper.AddVerticalLayout(legendRect.gameObject, controlW, controlH, alignment);
            }

            ChartHelper.Destroy(legendTemp.gameObject);
        }

        void UpdateContent()
        {
            if (chartType == ChartType.BarChart || chartType == ChartType.LineChart)
            {
                if (chartOptions.plotOptions.inverted)
                {
                    offsetMax -= new Vector2(chartOptions.yAxis.maxPadding, chartOptions.xAxis.maxPadding);
                }
                else
                {
                    offsetMax -= new Vector2(chartOptions.xAxis.maxPadding, chartOptions.yAxis.maxPadding);
                }
            }
            offsetMin.x = Mathf.Clamp(offsetMin.x, 0.0f, 0);
            offsetMin.y = Mathf.Clamp(offsetMin.y, 0.0f, chart.chartSize.y * 0.4f);
            
            offsetMax.x = Mathf.Clamp(offsetMax.x, -chart.chartSize.x * 0.4f, 0.0f);
            offsetMax.y = Mathf.Clamp(offsetMax.y, -chart.chartSize.y * 0.4f, 0.0f);
            chart.chartSize -= offsetMin - offsetMax;
            
            chartRect.anchorMin = Vector2.zero;
            chartRect.anchorMax = Vector2.one;
            chartRect.offsetMin = offsetMin;
            chartRect.offsetMax = offsetMax;

            Image img = chartRect.gameObject.AddComponent<Image>();
            img.color = Color.clear;
            img.raycastTarget = chartOptions.plotOptions.mouseTracking;

            ChartMixer mixer = GetComponent<ChartMixer>();
            if (mixer == null) chart.UpdateChart();
            else mixer.UpdateChart(chart);

            if (chartType == ChartType.SolidGauge && chartOptions.pane.semicircle && chartOptions.legend.alignment == TextAnchor.MiddleCenter)
            {
                legendRect.GetComponent<LayoutGroup>().childAlignment = TextAnchor.LowerCenter;
                float offset = chart.GetComponent<ChartGridCircleInverted>().centerOffset.y;
                legendRect.offsetMin += new Vector2(0.0f, chart.chartSize.y * 0.5f + offset);
            }
            if (chartType == ChartType.PieChart && chartOptions.pane.semicircle && chartOptions.legend.alignment == TextAnchor.MiddleCenter)
            {
                legendRect.GetComponent<LayoutGroup>().childAlignment = TextAnchor.LowerCenter;
                float offset = chart.GetComponent<PieChart>().centerOffset.y;
                legendRect.offsetMin += new Vector2(0.0f, chart.chartSize.y * 0.5f + offset);
            }

            if(ygridRect != null)
            {
                ygridRect.offsetMin = new Vector2(chartRect.offsetMin.x, ygridRect.offsetMin.y);
            }

        }
    }
}
