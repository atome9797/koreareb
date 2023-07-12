using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if CHART_TMPRO
using TMPro;
#endif

namespace ChartUtil
{
    public class ChartGridRect : ChartGrid
    {
        protected Image highlight;
        protected Vector2 offsetMin = new Vector2();
        protected Vector2 yTickSize, xTickSize;
        protected float yAxisWidth, xAxisWidth;
#if CHART_TMPRO
        protected TextMeshProUGUI xTitle, yTitle;
#else
        protected Text xTitle, yTitle;
#endif
        protected ChartGraphicRect yGrid;
        protected ChartGraphicRect xTicks;

        public override int GetItemIndex(Vector2 pos)
        {
            int index = 0;
            index = Mathf.FloorToInt(pos.x / unitWidth);
            index = Mathf.Clamp(index, 0, chart.chartData.categories.Count - 1);
            if (chart.chartOptions.xAxis.reversed) index = chart.chartData.categories.Count - index - 1;
            return index;
        }

        public override void HighlightItem(int index)
        {
            if (chart.chartOptions.xAxis.reversed) index = chart.chartData.categories.Count - index - 1;
            highlight.transform.localPosition = new Vector2(unitWidth * (index + 0.5f) - chart.chartSize.x * 0.5f, 0.0f);
            highlight.gameObject.SetActive(true);
        }

        public override void UnhighlightItem(int index)
        {
            highlight.gameObject.SetActive(false);
        }

        public override void UpdateGrid()
        {
            yTickSize = chart.chartOptions.yAxis.enableTick ? chart.chartOptions.yAxis.tickSize : new Vector2();
            xTickSize = chart.chartOptions.xAxis.enableTick ? chart.chartOptions.xAxis.tickSize : new Vector2();
            yAxisWidth = chart.chartOptions.yAxis.enableAxisLine ? chart.chartOptions.yAxis.axisLineWidth : 0.0f;
            xAxisWidth = chart.chartOptions.xAxis.enableAxisLine ? chart.chartOptions.xAxis.axisLineWidth : 0.0f;

            gridRect = ChartHelper.CreateEmptyRect("GridRect", chart.transform, true);
            gridRect.pivot = Vector2.zero;
            labelRect = ChartHelper.CreateEmptyRect("GridLabelRect", chart.transform, true);
            labelRect.pivot = Vector2.zero;

            if (chart.chartOptions.yAxis.enableTitle) UpdateYAxisTitle();
            if (chart.chartOptions.xAxis.enableTitle) UpdateXAxisTitle();

            ComputeYAxis();
            if (chart.chartOptions.yAxis.enableGridLine) UpdateYAxisGrid();
            if (chart.chartOptions.yAxis.enableTick) UpdateYAxisTick();
            if (chart.chartOptions.yAxis.enableLabel) UpdateYAxisLabel();

            ComputeXAxis();
            if (chart.chartOptions.xAxis.enableGridLine) UpdateXAxisGrid();
            if (chart.chartOptions.xAxis.enableTick) UpdateXAxisTick();
            if (chart.chartOptions.xAxis.enableLabel) UpdateXAxisLabel();

            chart.GetComponent<RectTransform>().offsetMin += offsetMin;
            chart.chartSize -= offsetMin;

            if (yTitle != null) AdjustYAxisTitle();
            if (xTitle != null) AdjustXAxisTitle();

            UpdateHighlight();
            if (chart.chartOptions.plotOptions.enableBackground) UpdateBackground();
            if (chart.chartOptions.xAxis.enableAxisLine) UpdateXAxisLine();
            if (chart.chartOptions.yAxis.enableAxisLine) UpdateYAxisLine();
        }

        protected virtual void UpdateHighlight()
        {
            highlight = ChartHelper.CreateImage("Highlight", chart.transform);
            highlight.transform.SetAsFirstSibling();
            highlight.color = chart.chartOptions.plotOptions.itemHighlightColor;
            highlight.rectTransform.sizeDelta = new Vector2(unitWidth, chart.chartSize.y);
            highlight.gameObject.SetActive(false);
        }

        protected virtual void UpdateBackground()
        {
            Image background = ChartHelper.CreateImage("Background", chart.transform, false, true);
            background.transform.SetAsFirstSibling();
            background.color = chart.chartOptions.plotOptions.backgroundColor;
        }

        protected virtual void UpdateYAxisTitle()
        {
            yTitle = ChartHelper.CreateText("YTitle", chart.transform, chart.chartOptions.yAxis.titleOption, chart.chartOptions.plotOptions.generalFont);
            yTitle.text = chart.chartOptions.yAxis.title;
            float height = yTitle.fontSize * 1.2f;
            offsetMin.x = height;
        }

        protected virtual void UpdateXAxisTitle()
        {
            xTitle = ChartHelper.CreateText("XTitle", chart.transform, chart.chartOptions.xAxis.titleOption, chart.chartOptions.plotOptions.generalFont);
            xTitle.text = chart.chartOptions.xAxis.title;
            float height = xTitle.fontSize * 1.2f;
            offsetMin.y = height;
        }

        protected virtual void AdjustYAxisTitle()
        {
            float height = yTitle.fontSize * 1.2f;
            float width = yTitle.preferredWidth;
            yTitle.rectTransform.anchorMin = new Vector2(0.0f, 0.5f);
            yTitle.rectTransform.anchorMax = new Vector2(0.0f, 0.5f);
            yTitle.rectTransform.sizeDelta = new Vector2(width, height);
            yTitle.rectTransform.anchoredPosition = new Vector2(-offsetMin.x + height * 0.5f, 0.0f);
            yTitle.rectTransform.localRotation = Quaternion.Euler(0.0f, 0.0f, 90.0f);
        }

        protected virtual void AdjustXAxisTitle()
        {
            float height = xTitle.fontSize * 1.2f;
            float width = xTitle.preferredWidth;
            xTitle.rectTransform.anchorMin = new Vector2(0.5f, 0.0f);
            xTitle.rectTransform.anchorMax = new Vector2(0.5f, 0.0f);
            xTitle.rectTransform.sizeDelta = new Vector2(width, height);
            xTitle.rectTransform.anchoredPosition = new Vector2(0.0f, -offsetMin.y + height * 0.5f);
        }

        protected virtual void ComputeYAxis()
        {
            yAxisInfo = new AxisInfo();
            switch (chart.chartOptions.plotOptions.columnStacking)
            {
                case ColumnStacking.None:
                    if (chart.chartOptions.yAxis.autoAxisValues)
                    {
                        yAxisInfo.Compute(chart.chartDataInfo.min, chart.chartDataInfo.max, chart.chartOptions.yAxis.axisDivision, chart.chartOptions.yAxis.startFromZero);
                    }
                    else
                    {
                        yAxisInfo.Compute(chart.chartOptions.yAxis.min, chart.chartOptions.yAxis.max, chart.chartOptions.yAxis.axisDivision);
                    }
                    break;
                case ColumnStacking.Normal:
                    yAxisInfo.Compute(chart.chartDataInfo.minSum, chart.chartDataInfo.maxSum, chart.chartOptions.yAxis.axisDivision, chart.chartOptions.yAxis.startFromZero);
                    break;
                case ColumnStacking.Percent:
                    yAxisInfo.Compute(chart.chartDataInfo.minSum < 0.0f ? -1.0f : 0.0f, chart.chartDataInfo.maxSum > 0.0f ? 1.0f : 0.0f,
                        chart.chartDataInfo.minSum < 0.0f && chart.chartDataInfo.maxSum > 0.0f ? 10 : 5);
                    break;
                default:
                    break;
            }
        }

        protected virtual void UpdateYAxisLine()
        {
            Image yAxis = ChartHelper.CreateImage("YAxis", gridRect);
            yAxis.color = chart.chartOptions.yAxis.axisLineColor;
            yAxis.rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            yAxis.rectTransform.anchorMax = new Vector2(0.0f, 1.0f);
            yAxis.rectTransform.sizeDelta = new Vector2(yAxisWidth, 0.0f);
            yAxis.rectTransform.offsetMin -= new Vector2(0.0f, xTickSize.y + yAxisWidth * 0.5f);
            yAxis.rectTransform.offsetMax += new Vector2(0.0f,
                chart.chartOptions.yAxis.tickSize.x > chart.chartOptions.xAxis.gridLineWidth ?
                chart.chartOptions.yAxis.tickSize.x * 0.5f : chart.chartOptions.xAxis.gridLineWidth * 0.5f);
        }

        protected virtual void UpdateYAxisGrid()
        {
            yGrid = ChartHelper.CreateEmptyRect("YGrid", gridRect, true).gameObject.AddComponent<ChartGraphicRect>();
            yGrid.color = chart.chartOptions.yAxis.gridLineColor;
            yGrid.width = chart.chartOptions.yAxis.gridLineWidth;
            yGrid.num = chart.chartData.categories.Count;
            yGrid.mid = midGrid;
            yGrid.inverted = chart.chartOptions.plotOptions.inverted;
        }

        protected virtual void UpdateYAxisTick()
        {
            ChartGraphicRect yTicks = ChartHelper.CreateEmptyRect("YTicks", gridRect).gameObject.AddComponent<ChartGraphicRect>();
            yTicks.color = chart.chartOptions.yAxis.tickColor;
            yTicks.width = chart.chartOptions.yAxis.tickSize.x;
            yTicks.num = yAxisInfo.count;
            yTicks.inverted = !chart.chartOptions.plotOptions.inverted;

            yTicks.rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            yTicks.rectTransform.anchorMax = new Vector2(0.0f, 1.0f);
            yTicks.rectTransform.anchoredPosition = new Vector2(-yTickSize.y * 0.5f - yAxisWidth * 0.5f, 0.0f);
            yTicks.rectTransform.sizeDelta = new Vector2(yTickSize.y, 0.0f);
        }

        protected virtual void UpdateYAxisLabel()
        {

#if CHART_TMPRO
            TextMeshProUGUI labelTemp;
#else
            Text labelTemp;
#endif
            if(chart.m != null)
            {
                //Debug.Log($"<color=yellow>[UpdateYAxisLabel]{chart.m.name}</color>");

                labelTemp = ChartHelper.CreateText("YGridLabel", chart.m.transform, chart.chartOptions.yAxis.labelOption, chart.chartOptions.plotOptions.generalFont);
                labelTemp.rectTransform.sizeDelta = Vector2.zero;

                float spacing = 1.0f / yAxisInfo.count;
                float labelSize = yTickSize.y + yAxisWidth * 0.5f + labelTemp.fontSize * 0.5f + chart.chartOptions.xAxis.minPadding;
                labelTemp.rectTransform.anchoredPosition = new Vector2(-labelSize, 0.0f);
                labelTemp.alignment = ChartHelper.ConvertAlignment(TextAnchor.MiddleRight);

                for (int i = 0; i < yAxisInfo.count + 1; ++i)
                {
                    var label = GameObject.Instantiate(labelTemp, chart.m.transform);
                    label.rectTransform.anchorMin = new Vector2(0.0f, spacing * i);
                    label.rectTransform.anchorMax = new Vector2(0.0f, spacing * i);
                    float value = yAxisInfo.min + yAxisInfo.interval * i;
                    if (chart.chartOptions.yAxis.absoluteValue) value = Mathf.Abs(value);
                    if (chart.chartOptions.plotOptions.columnStacking == ColumnStacking.Percent) label.text = (value * 100).ToString("f0") + "%";
                    else label.text = chart.chartOptions.yAxis.labelFormat.Replace("{value}", value.ToString(yAxisInfo.labelFormat));
                }

                var firstLabel = ChartHelper.GetTextComponent(chart.m.transform.GetChild(1).gameObject);
                var lastLabel = ChartHelper.GetTextComponent(chart.m.transform.GetChild(chart.m.transform.childCount - 1).gameObject);
                var temp = firstLabel.text.Length > lastLabel.text.Length ? firstLabel : lastLabel;

                labelSize += temp.preferredWidth + labelTemp.fontSize;
                offsetMin.x += labelSize;
                offsetMin.x = Mathf.Clamp(offsetMin.x, 0.0f, chart.chartSize.x * 0.5f);

                ChartHelper.Destroy(labelTemp.gameObject);
            }
            else
            {
                labelTemp = ChartHelper.CreateText("YGridLabel", chart.transform, chart.chartOptions.yAxis.labelOption, chart.chartOptions.plotOptions.generalFont);
                labelTemp.rectTransform.sizeDelta = Vector2.zero;

                float spacing = 1.0f / yAxisInfo.count;
                float labelSize = yTickSize.y + yAxisWidth * 0.5f + labelTemp.fontSize * 0.5f + chart.chartOptions.xAxis.minPadding;
                labelTemp.rectTransform.anchoredPosition = new Vector2(-labelSize, 0.0f);
                labelTemp.alignment = ChartHelper.ConvertAlignment(TextAnchor.MiddleRight);

                for (int i = 0; i < yAxisInfo.count + 1; ++i)
                {
                    var label = GameObject.Instantiate(labelTemp, labelRect);
                    label.rectTransform.anchorMin = new Vector2(0.0f, spacing * i);
                    label.rectTransform.anchorMax = new Vector2(0.0f, spacing * i);
                    float value = yAxisInfo.min + yAxisInfo.interval * i;
                    if (chart.chartOptions.yAxis.absoluteValue) value = Mathf.Abs(value);
                    if (chart.chartOptions.plotOptions.columnStacking == ColumnStacking.Percent) label.text = (value * 100).ToString("f0") + "%";
                    else label.text = chart.chartOptions.yAxis.labelFormat.Replace("{value}", value.ToString(yAxisInfo.labelFormat));
                }

                var firstLabel = ChartHelper.GetTextComponent(labelRect.GetChild(1).gameObject);
                var lastLabel = ChartHelper.GetTextComponent(labelRect.GetChild(labelRect.childCount - 1).gameObject);
                var temp = firstLabel.text.Length > lastLabel.text.Length ? firstLabel : lastLabel;

                labelSize += temp.preferredWidth + labelTemp.fontSize;
                offsetMin.x += labelSize;
                offsetMin.x = Mathf.Clamp(offsetMin.x, 0.0f, chart.chartSize.x * 0.5f);

                ChartHelper.Destroy(labelTemp.gameObject);
            }
            
        }

        protected virtual void ComputeXAxis()
        {
            unitWidth = (chart.chartSize.x - offsetMin.x) / chart.chartData.categories.Count;
        }

        protected virtual void UpdateXAxisLine()
        {
            Image xAxis = ChartHelper.CreateImage("XAxis", gridRect);
            xAxis.rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            xAxis.rectTransform.anchorMax = new Vector2(1.0f, 0.0f);
            xAxis.gameObject.name = "XAxis";
            xAxis.color = chart.chartOptions.xAxis.axisLineColor;
            xAxis.rectTransform.sizeDelta = new Vector2(0.0f, xAxisWidth);
            xAxis.rectTransform.offsetMin -= new Vector2(yTickSize.y + yAxisWidth * 0.5f, 0.0f);
            xAxis.rectTransform.offsetMax += new Vector2(
                xTickSize.x > chart.chartOptions.yAxis.gridLineWidth ?
                xTickSize.x * 0.5f : chart.chartOptions.yAxis.gridLineWidth * 0.5f, 0.0f);
        }

        protected virtual void UpdateXAxisGrid()
        {
            ChartGraphicRect xGrid = ChartHelper.CreateEmptyRect("XGrid", gridRect, true).gameObject.AddComponent<ChartGraphicRect>();
            xGrid.color = chart.chartOptions.xAxis.gridLineColor;
            xGrid.width = chart.chartOptions.xAxis.gridLineWidth;
            xGrid.num = yAxisInfo.count;
            xGrid.inverted = !chart.chartOptions.plotOptions.inverted;
        }

        protected virtual void UpdateXAxisTick()
        {
            xTicks = ChartHelper.CreateEmptyRect("XTicks", gridRect).gameObject.AddComponent<ChartGraphicRect>();
            xTicks.color = chart.chartOptions.xAxis.tickColor;
            xTicks.width = xTickSize.x;
            xTicks.num = chart.chartData.categories.Count;
            xTicks.mid = midGrid;
            xTicks.inverted = chart.chartOptions.plotOptions.inverted;

            xTicks.rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            xTicks.rectTransform.anchorMax = new Vector2(1.0f, 0.0f);
            xTicks.rectTransform.anchoredPosition = new Vector2(0.0f, -xTickSize.y * 0.5f - xAxisWidth * 0.5f);
            xTicks.rectTransform.sizeDelta = new Vector2(0.0f, xTickSize.y);
        }

        protected virtual void UpdateXAxisLabel()
        {
#if CHART_TMPRO
            TextMeshProUGUI labelTemp;
#else
            Text labelTemp;
#endif
            labelTemp = ChartHelper.CreateText("XGridLabel", chart.transform, chart.chartOptions.xAxis.labelOption, chart.chartOptions.plotOptions.generalFont);
            labelTemp.rectTransform.sizeDelta = Vector2.zero;

            float spacing = 1.0f / chart.chartData.categories.Count;
            float maxWidth = 0.0f;
            if (chart.chartOptions.xAxis.enableLabel)
            {
                string maxStr = "";
                for (int i = 0; i < chart.chartData.categories.Count; ++i)
                {
                    if (chart.chartData.categories[i].Length > maxStr.Length) maxStr = chart.chartData.categories[i];
                }
                var label = GameObject.Instantiate(labelTemp, chart.transform);
                label.text = maxStr;
                maxWidth = label.preferredWidth;
                ChartHelper.Destroy(label.gameObject);
            }
            bool useLongLabel = maxWidth > unitWidth * 0.8f && chart.chartOptions.xAxis.autoRotateLabel;

            float labelSize = xTickSize.y + xAxisWidth * 0.5f + labelTemp.fontSize * 0.1f + chart.chartOptions.yAxis.minPadding;
            labelTemp.rectTransform.anchoredPosition = new Vector2(0.0f, -labelSize);
            if (useLongLabel)
            {
                labelTemp.alignment = ChartHelper.ConvertAlignment(TextAnchor.MiddleRight);
                labelTemp.rectTransform.localRotation = Quaternion.Euler(0.0f, 0.0f, 45.0f);
            }
            else
            {
                labelTemp.alignment = ChartHelper.ConvertAlignment(TextAnchor.UpperCenter);
            }

            labelSize += useLongLabel ? maxWidth * 0.8f : labelTemp.fontSize * 1.2f;
            labelSize = Mathf.Clamp(labelSize, 0.0f, chart.chartSize.y * 0.5f - offsetMin.y);
            maxWidth = useLongLabel ? labelSize * 1.414f : unitWidth * 0.9f;

            int interval = Mathf.RoundToInt(chart.chartOptions.xAxis.interval);
            if (interval < 1) interval = Mathf.CeilToInt(labelTemp.fontSize * 1.25f / unitWidth + 0.0001f);
            if (xTicks != null) xTicks.interval = interval;
            if (yGrid != null) yGrid.interval = interval;
            maxWidth *= interval;

            for (int i = 0; i < chart.chartData.categories.Count; i += interval)
            {
                int posIndex = chart.chartOptions.xAxis.reversed ? chart.chartData.categories.Count - i - 1 : i;
                var label = GameObject.Instantiate(labelTemp, labelRect);
                label.text = chart.chartData.categories[i];
                label.rectTransform.anchorMin = new Vector2(spacing * (posIndex + 0.5f), 0.0f);
                label.rectTransform.anchorMax = new Vector2(spacing * (posIndex + 0.5f), 0.0f);
                if (label.preferredWidth > maxWidth) ChartHelper.TruncateText(label, maxWidth);
            }

            offsetMin.y += labelSize;
            offsetMin.y = Mathf.Clamp(offsetMin.y, 0.0f, chart.chartSize.y * 0.5f);

            ChartHelper.Destroy(labelTemp.gameObject);
        }
    }
}