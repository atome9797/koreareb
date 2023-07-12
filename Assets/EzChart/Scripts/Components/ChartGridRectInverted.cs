using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if CHART_TMPRO
using TMPro;
#endif

namespace ChartUtil
{
    public class ChartGridRectInverted : ChartGridRect
    {
        public override int GetItemIndex(Vector2 pos)
        {
            int index = 0;
            index = Mathf.FloorToInt(pos.y / unitWidth);
            index = Mathf.Clamp(index, 0, chart.chartData.categories.Count - 1);
            if (!chart.chartOptions.xAxis.reversed) index = chart.chartData.categories.Count - index - 1;
            return index;
        }

        public override void HighlightItem(int index)
        {
            if (!chart.chartOptions.xAxis.reversed) index = chart.chartData.categories.Count - index - 1;
            highlight.transform.localPosition = new Vector2(0.0f, unitWidth * (index + 0.5f) - chart.chartSize.y * 0.5f);
            highlight.gameObject.SetActive(true);
        }

        protected override void UpdateHighlight()
        {
            highlight = ChartHelper.CreateImage("Highlight", chart.transform);
            highlight.transform.SetAsFirstSibling();
            highlight.color = chart.chartOptions.plotOptions.itemHighlightColor;
            highlight.rectTransform.sizeDelta = new Vector2(chart.chartSize.x, unitWidth);
            highlight.gameObject.SetActive(false);
        }

        protected override void UpdateYAxisTitle()
        {
            yTitle = ChartHelper.CreateText("YTitle", chart.transform, chart.chartOptions.yAxis.titleOption, chart.chartOptions.plotOptions.generalFont);
            yTitle.text = chart.chartOptions.yAxis.title;
            float height = yTitle.fontSize * 1.2f;
            offsetMin.y = height;
        }

        protected override void AdjustYAxisTitle()
        {
            float height = yTitle.fontSize * 1.2f;
            float width = yTitle.preferredWidth;
            yTitle.rectTransform.anchorMin = new Vector2(0.5f, 0.0f);
            yTitle.rectTransform.anchorMax = new Vector2(0.5f, 0.0f);
            yTitle.rectTransform.sizeDelta = new Vector2(width, height);
            yTitle.rectTransform.anchoredPosition = new Vector2(0.0f, -offsetMin.y + height * 0.5f);
        }

        protected override void UpdateXAxisTitle()
        {
            xTitle = ChartHelper.CreateText("XTitle", chart.transform, chart.chartOptions.xAxis.titleOption, chart.chartOptions.plotOptions.generalFont);
            xTitle.text = chart.chartOptions.xAxis.title;
            float height = xTitle.fontSize * 1.2f;
            offsetMin.x = height;
        }

        protected override void AdjustXAxisTitle()
        {
            float height = xTitle.fontSize * 1.2f;
            float width = xTitle.preferredWidth;
            xTitle.rectTransform.anchorMin = new Vector2(0.0f, 0.5f);
            xTitle.rectTransform.anchorMax = new Vector2(0.0f, 0.5f);
            xTitle.rectTransform.sizeDelta = new Vector2(width, height);
            xTitle.rectTransform.anchoredPosition = new Vector2(-offsetMin.x + height * 0.5f, 0.0f);
            xTitle.rectTransform.localRotation = Quaternion.Euler(0.0f, 0.0f, 90.0f);
        }

        protected override void UpdateYAxisLine()
        {
            Image yAxis = ChartHelper.CreateImage("YAxis", gridRect);
            yAxis.color = chart.chartOptions.yAxis.axisLineColor;
            yAxis.rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            yAxis.rectTransform.anchorMax = new Vector2(1.0f, 0.0f);
            yAxis.rectTransform.sizeDelta = new Vector2(0.0f, yAxisWidth);
            yAxis.rectTransform.offsetMin -= new Vector2(xTickSize.y + yAxisWidth * 0.5f, 0.0f);
            yAxis.rectTransform.offsetMax += new Vector2(
                chart.chartOptions.yAxis.tickSize.x > chart.chartOptions.xAxis.gridLineWidth ?
                chart.chartOptions.yAxis.tickSize.x * 0.5f : chart.chartOptions.xAxis.gridLineWidth * 0.5f, 0.0f);
        }

        protected override void UpdateYAxisGrid()
        {
            yGrid = ChartHelper.CreateEmptyRect("YGrid", gridRect, true).gameObject.AddComponent<ChartGraphicRect>();
            yGrid.color = chart.chartOptions.yAxis.gridLineColor;
            yGrid.width = chart.chartOptions.yAxis.gridLineWidth;
            yGrid.num = chart.chartData.categories.Count;
            yGrid.mid = midGrid;
            yGrid.inverted = chart.chartOptions.plotOptions.inverted;
        }

        protected override void UpdateYAxisTick()
        {
            ChartGraphicRect yTicks = ChartHelper.CreateEmptyRect("YTicks", gridRect).gameObject.AddComponent<ChartGraphicRect>();
            yTicks.color = chart.chartOptions.yAxis.tickColor;
            yTicks.width = chart.chartOptions.yAxis.tickSize.x;
            yTicks.num = yAxisInfo.count;
            yTicks.inverted = !chart.chartOptions.plotOptions.inverted;

            yTicks.rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            yTicks.rectTransform.anchorMax = new Vector2(1.0f, 0.0f);
            yTicks.rectTransform.anchoredPosition = new Vector2(0.0f, -yTickSize.y * 0.5f - yAxisWidth * 0.5f);
            yTicks.rectTransform.sizeDelta = new Vector2(0.0f, yTickSize.y);
        }

        protected override void UpdateYAxisLabel()
        {
#if CHART_TMPRO
            TextMeshProUGUI labelTemp;
#else
            Text labelTemp;
#endif

            if (chart.m != null)
            {
                //Debug.Log($"<color=yellow>[Invested][UpdateYAxisLabel]{chart.m.name}</color>");

                labelTemp = ChartHelper.CreateText("YGridLabel", chart.transform, chart.chartOptions.yAxis.labelOption, chart.chartOptions.plotOptions.generalFont);
                labelTemp.rectTransform.sizeDelta = Vector2.zero;

                float spacing = 1.0f / yAxisInfo.count;
                float labelSize = yTickSize.y + yAxisWidth * 0.5f + labelTemp.fontSize * 0.1f + chart.chartOptions.xAxis.minPadding;
                labelTemp.rectTransform.anchoredPosition = new Vector2(0.0f, -labelSize);
                labelTemp.alignment = ChartHelper.ConvertAlignment(TextAnchor.UpperCenter);

                for (int i = 0; i < yAxisInfo.count + 1; ++i)
                {
                    var label = GameObject.Instantiate(labelTemp, chart.m.transform);
                    label.rectTransform.anchorMin = new Vector2(spacing * i, 0.0f);
                    label.rectTransform.anchorMax = new Vector2(spacing * i, 0.0f);
                    float value = yAxisInfo.min + yAxisInfo.interval * i;
                    if (chart.chartOptions.yAxis.absoluteValue) value = Mathf.Abs(value);
                    if (chart.chartOptions.plotOptions.columnStacking == ColumnStacking.Percent) label.text = (value * 100).ToString("f0") + "%";
                    else label.text = chart.chartOptions.yAxis.labelFormat.Replace("{value}", value.ToString(yAxisInfo.labelFormat));
                }

                labelSize += labelTemp.fontSize * 1.2f;
                offsetMin.y += labelSize;
                offsetMin.y = Mathf.Clamp(offsetMin.y, 0.0f, chart.chartSize.y * 0.5f);

                ChartHelper.Destroy(labelTemp.gameObject);
            }
            else
            {
                labelTemp = ChartHelper.CreateText("YGridLabel", chart.transform, chart.chartOptions.yAxis.labelOption, chart.chartOptions.plotOptions.generalFont);
                labelTemp.rectTransform.sizeDelta = Vector2.zero;

                float spacing = 1.0f / yAxisInfo.count;
                float labelSize = yTickSize.y + yAxisWidth * 0.5f + labelTemp.fontSize * 0.1f + chart.chartOptions.xAxis.minPadding;
                labelTemp.rectTransform.anchoredPosition = new Vector2(0.0f, -labelSize);
                labelTemp.alignment = ChartHelper.ConvertAlignment(TextAnchor.UpperCenter);

                for (int i = 0; i < yAxisInfo.count + 1; ++i)
                {
                    var label = GameObject.Instantiate(labelTemp, labelRect);
                    label.rectTransform.anchorMin = new Vector2(spacing * i, 0.0f);
                    label.rectTransform.anchorMax = new Vector2(spacing * i, 0.0f);
                    float value = yAxisInfo.min + yAxisInfo.interval * i;
                    if (chart.chartOptions.yAxis.absoluteValue) value = Mathf.Abs(value);
                    if (chart.chartOptions.plotOptions.columnStacking == ColumnStacking.Percent) label.text = (value * 100).ToString("f0") + "%";
                    else label.text = chart.chartOptions.yAxis.labelFormat.Replace("{value}", value.ToString(yAxisInfo.labelFormat));
                }

                labelSize += labelTemp.fontSize * 1.2f;
                offsetMin.y += labelSize;
                offsetMin.y = Mathf.Clamp(offsetMin.y, 0.0f, chart.chartSize.y * 0.5f);

                ChartHelper.Destroy(labelTemp.gameObject);
            }
        }

        protected override void UpdateXAxisLine()
        {
            Image xAxis = ChartHelper.CreateImage("XAxis", gridRect);
            xAxis.rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            xAxis.rectTransform.anchorMax = new Vector2(0.0f, 1.0f);
            xAxis.gameObject.name = "XAxis";
            xAxis.color = chart.chartOptions.xAxis.axisLineColor;
            xAxis.rectTransform.sizeDelta = new Vector2(xAxisWidth, 0.0f);
            xAxis.rectTransform.offsetMin -= new Vector2(0.0f, yTickSize.y + yAxisWidth * 0.5f);
            xAxis.rectTransform.offsetMax += new Vector2(0.0f,
                xTickSize.x > chart.chartOptions.yAxis.gridLineWidth ?
                xTickSize.x * 0.5f : chart.chartOptions.yAxis.gridLineWidth * 0.5f);
        }

        protected override void UpdateXAxisGrid()
        {
            ChartGraphicRect xGrid = ChartHelper.CreateEmptyRect("XGrid", gridRect, true).gameObject.AddComponent<ChartGraphicRect>();
            xGrid.color = chart.chartOptions.xAxis.gridLineColor;
            xGrid.width = chart.chartOptions.xAxis.gridLineWidth;
            xGrid.num = yAxisInfo.count;
            xGrid.inverted = !chart.chartOptions.plotOptions.inverted;
        }

        protected override void UpdateXAxisTick()
        {
            xTicks = ChartHelper.CreateEmptyRect("XTicks", gridRect).gameObject.AddComponent<ChartGraphicRect>();
            xTicks.color = chart.chartOptions.xAxis.tickColor;
            xTicks.width = xTickSize.x;
            xTicks.num = chart.chartData.categories.Count;
            xTicks.mid = midGrid;
            xTicks.inverted = chart.chartOptions.plotOptions.inverted;

            xTicks.rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            xTicks.rectTransform.anchorMax = new Vector2(0.0f, 1.0f);
            xTicks.rectTransform.anchoredPosition = new Vector2(-xTickSize.y * 0.5f - xAxisWidth * 0.5f, 0.0f);
            xTicks.rectTransform.sizeDelta = new Vector2(xTickSize.y, 0.0f);
        }

        protected override void UpdateXAxisLabel()
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
            unitWidth = (chart.chartSize.y - offsetMin.y) / chart.chartData.categories.Count;

            float labelSize = xTickSize.y + xAxisWidth * 0.5f + labelTemp.fontSize * 0.5f + chart.chartOptions.yAxis.minPadding;
            labelTemp.rectTransform.anchoredPosition = new Vector2(-labelSize, 0.0f);

            labelTemp.alignment = ChartHelper.ConvertAlignment(TextAnchor.MiddleLeft);

            labelSize += maxWidth + labelTemp.fontSize;
            labelSize = Mathf.Clamp(labelSize, 0.0f, chart.chartSize.x * 0.5f - offsetMin.x);
            maxWidth = labelSize;

            int interval = Mathf.RoundToInt(chart.chartOptions.xAxis.interval);
            if (interval < 1) interval = Mathf.CeilToInt(labelTemp.fontSize * 1.25f / unitWidth + 0.0001f);
            if (xTicks != null) xTicks.interval = interval;
            if (yGrid != null) yGrid.interval = interval;

            for (int i = 0; i < chart.chartData.categories.Count; i += interval)
            {
                int posIndex = !chart.chartOptions.xAxis.reversed ? chart.chartData.categories.Count - i - 1 : i;
                var label = GameObject.Instantiate(labelTemp, labelRect);
                label.text = chart.chartData.categories[i];
                label.rectTransform.anchorMin = new Vector2(0.0f, spacing * (posIndex + 0.5f));
                label.rectTransform.anchorMax = new Vector2(0.0f, spacing * (posIndex + 0.5f));
                maxWidth = 250f;

                if (label.preferredWidth > maxWidth)                    
                    ChartHelper.TruncateText(label, maxWidth);
            }

            offsetMin.x += labelSize;
            offsetMin.x = Mathf.Clamp(offsetMin.x, 0.0f, chart.chartSize.x * 0.5f);
            /*if(labelRect.transform.GetChild(0).name.Contains("YGridLabel"))
            {
                labelRect.parent.GetComponent<RectTransform>().offsetMin = new Vector2(138f, labelRect.parent.GetComponent<RectTransform>().offsetMin.y);
            }*/
            //labelRect.offsetMin = new Vector2(-110f, 0.0f);
            int childSize = labelRect.childCount;
            float grapMode = -150f;

            for (int i = 0; i < childSize; i++)
            {
                if (labelRect.GetChild(i).name.Contains("XGridLabel"))
                {
                    labelRect.GetChild(i).GetComponent<RectTransform>().offsetMin = new Vector2(grapMode, 0.0f);
                }
            }
            ChartHelper.Destroy(labelTemp.gameObject);
        }
    }
}