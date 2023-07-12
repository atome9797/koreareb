using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if CHART_TMPRO
using TMPro;
#endif

namespace ChartUtil
{
    public class RoseChart : ChartBase
    {
        ChartGridCircle chartGrid;
        float barWidth;
        float[] offset;
        RoseChartBar[] barList;
        Material barMat;
        Material barMatFade;

        private void OnDestroy()
        {
            if (barMat != null) ChartHelper.Destroy(barMat);
            if (barMatFade != null) ChartHelper.Destroy(barMatFade);
        }

        public override void UpdateChart()
        {
            chartGrid = new ChartGridCircle();
            chartGrid.chart = this;
            chartGrid.midGrid = false;
            chartGrid.circularGrid = true;
            chartGrid.innerSize = chartOptions.pane.innerSize;
            chartGrid.outerSize = chartOptions.pane.outerSize;
            chartGrid.UpdateGrid();

            dataRect = ChartHelper.CreateEmptyRect("DataRect", transform, true);
            labelRect = ChartHelper.CreateEmptyRect("LabelRect", transform, true);
            UpdateItems();
            chartGrid.labelRect.SetSiblingIndex(labelRect.GetSiblingIndex() - 1);
            if (chartOptions.plotOptions.frontGrid) chartGrid.gridRect.SetSiblingIndex(dataRect.GetSiblingIndex() + 1);
        }

        protected override int FindCategory()
        {
            return chartGrid.GetItemIndex(localMousePosition);
        }

        protected override int FindSeries()
        {
            int index = -1;
            float rInner = chartSize.y * 0.5f;
            float rRange = (chartSize.x - chartSize.y) * 0.5f;
            int posIndex = chartOptions.xAxis.reversed ? chartData.categories.Count - currCate - 1 : currCate;
            for (int i = 0; i < chartData.series.Count; ++i)
            {
                if (!chartData.series[i].show) continue;
                float pos = chartGrid.unitWidth * posIndex + chartGrid.unitWidth * 0.5f + offset[i] - barWidth * 0.5f;
                float distStart = rInner + rRange * barList[i].data[currCate].y;
                float dist = distStart + rRange * barList[i].data[currCate].x;
                if (chartGrid.mouseAngle > pos && chartGrid.mouseAngle < pos + barWidth &&
                    localMousePosition.sqrMagnitude > distStart * distStart && localMousePosition.sqrMagnitude < dist * dist)
                { index = i; break; }
            }
            return index;
        }

        protected override void HighlightCurrentCategory()
        {
            chartGrid.HighlightItem(currCate);
        }

        protected override void UnhighlightCurrentCategory()
        {
            chartGrid.UnhighlightItem(currCate);
        }

        protected override void HighlightCurrentSeries()
        {
            for (int i = 0; i < barList.Length; ++i)
            {
                if (barList[i] == null || i == currSeries) continue;
                barList[i].material = barMatFade;
            }
        }

        protected override void UnhighlightCurrentSeries()
        {
            for (int i = 0; i < barList.Length; ++i)
            {
                if (barList[i] == null || i == currSeries) continue;
                barList[i].material = null;
            }
        }

        protected override void UpdateTooltip()
        {
            tooltip.tooltipText.text = GetFormattedHeaderText(currCate, chartOptions.tooltip.headerFormat);
            if (chartOptions.tooltip.share)
            {
                for (int i = 0; i < chartData.series.Count; ++i)
                {
                    if (!chartData.series[i].show || !IsValid(i, currCate)) continue;
                    tooltip.tooltipText.text += "\n" + GetFormattedPointText(i, currCate, chartOptions.tooltip.pointFormat, 0);
                }
            }
            else
            {
                if (chartData.series[currSeries].show || !IsValid(currSeries, currCate))
                    tooltip.tooltipText.text += "\n" + GetFormattedPointText(currSeries, currCate, chartOptions.tooltip.pointFormat, 0);
            }
            tooltip.background.rectTransform.sizeDelta = new Vector2(tooltip.tooltipText.preferredWidth + 16, tooltip.tooltipText.preferredHeight + 6);
        }

        public bool IsValid(int seriesIndex, int cateIndex)
        {
            return cateIndex < chartData.series[seriesIndex].data.Count && chartData.series[seriesIndex].data[cateIndex].show && chartData.series[seriesIndex].data[cateIndex].value >= 0.0f;
        }

        string GetFormattedHeaderText(int cateIndex, string format)
        {
            format = format.Replace("{category}", chartData.categories[cateIndex]);
            return format;
        }

        string GetFormattedPointText(int seriesIndex, int cateIndex, string format, int type)
        {
            string f = type == 0 ? chartOptions.tooltip.pointNumericFormat : chartOptions.label.numericFormat;
            format = format.Replace("{category}", chartData.categories[cateIndex]);
            format = format.Replace("{series.name}", chartData.series[seriesIndex].name);
            format = format.Replace("{data.value}", GetValueString(chartData.series[seriesIndex].data[cateIndex].value, f));
            format = format.Replace("{data.percentage}", GetPercentageString(chartData.series[seriesIndex].data[cateIndex].value / chartDataInfo.posSum[cateIndex] * 100, f));
            return format;
        }

        Vector2 GetDataRatio(int seriesIndex, int cateIndex, float[] stackValueList)
        {
            Vector2 value = new Vector2();
            if (!IsValid(seriesIndex, cateIndex)) return value;
            switch (chartOptions.plotOptions.columnStacking)
            {
                case ColumnStacking.None:
                    value.x = (chartData.series[seriesIndex].data[cateIndex].value - chartGrid.yAxisInfo.min) / chartGrid.yAxisInfo.span;
                    value.y = 0.0f;
                    break;
                case ColumnStacking.Normal:
                    value.x = (chartData.series[seriesIndex].data[cateIndex].value - chartGrid.yAxisInfo.min) / chartGrid.yAxisInfo.span;
                    value.y = stackValueList[cateIndex];
                    stackValueList[cateIndex] += value.x;
                    break;
                case ColumnStacking.Percent:
                    value.x = (chartData.series[seriesIndex].data[cateIndex].value - chartGrid.yAxisInfo.min) / chartDataInfo.posSum[cateIndex] / chartGrid.yAxisInfo.span;
                    value.y = stackValueList[cateIndex];
                    stackValueList[cateIndex] += value.x;
                    break;
                default:
                    break;
            }
            value.x = Mathf.Clamp01(value.x);
            value.y = Mathf.Clamp01(value.y);
            return value;
        }

        void UpdateItems()
        {
            //template
#if CHART_TMPRO
            TextMeshProUGUI labelTemp;
#else
            Text labelTemp;
#endif
            labelTemp = ChartHelper.CreateText("Label", transform, chartOptions.label.textOption, chartOptions.plotOptions.generalFont);
            labelTemp.rectTransform.sizeDelta = Vector2.zero;

            //material
            float smoothness = Mathf.Clamp01(4.0f / (chartSize.x - chartSize.y));
            barMat = new Material(Resources.Load<Material>("Materials/Chart_VBlur"));
            barMat.SetFloat("_Smoothness", smoothness);
            barMatFade = new Material(barMat);
            barMatFade.color = new Color(1.0f, 1.0f, 1.0f, fadeValue);

            //item
            float maxBarWidth = chartOptions.plotOptions.columnStacking == ColumnStacking.None ? chartGrid.unitWidth / chartData.series.Count : chartGrid.unitWidth;
            barWidth = Mathf.Clamp(chartOptions.plotOptions.roseChartOption.barWidth, 0.0f, maxBarWidth);
            float barSpace = Mathf.Clamp(chartOptions.plotOptions.roseChartOption.itemSeparation, -barWidth * 0.5f, maxBarWidth - barWidth);
            float barUnit = barWidth + barSpace;

            offset = new float[chartData.series.Count];
            if (chartOptions.plotOptions.columnStacking == ColumnStacking.None)
            {
                float offsetMin = 0.0f;
                for (int i = 0; i < chartData.series.Count; ++i)
                {
                    if (skipSeries.Contains(i) || !chartData.series[i].show) continue;
                    offsetMin += barUnit;
                }
                offsetMin = -(offsetMin - barUnit) * 0.5f;
                int activeCount = 0;
                for (int i = 0; i < chartData.series.Count; ++i)
                {
                    if (skipSeries.Contains(i) || !chartData.series[i].show) continue;
                    offset[i] = offsetMin + barUnit * activeCount;
                    activeCount++;
                }
            }
            else
            {
                for (int i = 0; i < chartData.series.Count; ++i) offset[i] = 0.0f;
            }

            barList = new RoseChartBar[chartData.series.Count];
            float[] stackValueList = chartOptions.plotOptions.columnStacking == ColumnStacking.None ? null : new float[chartData.categories.Count];
            for (int i = 0; i < chartData.series.Count; ++i)
            {
                RectTransform seriesRect = ChartHelper.CreateEmptyRect(chartData.series[i].name, dataRect);
                seriesRect.sizeDelta = new Vector2(chartSize.x, chartSize.x);
                seriesRect.SetAsFirstSibling();
                RectTransform seriesLabelRect = ChartHelper.CreateEmptyRect(chartData.series[i].name, labelRect);
                seriesLabelRect.sizeDelta = new Vector2(chartSize.x, chartSize.x);
                seriesLabelRect.SetAsFirstSibling();
                if (skipSeries.Contains(i) || !chartData.series[i].show) continue;

                //bar
                barList[i] = ChartHelper.CreateEmptyRect("Bar", seriesRect, true).gameObject.AddComponent<RoseChartBar>();
                barList[i].material = barMat;
                barList[i].color = chartOptions.plotOptions.dataColor[i % chartOptions.plotOptions.dataColor.Length];
                barList[i].width = barWidth;
                barList[i].offset = offset[i];
                barList[i].reverse = chartOptions.xAxis.reversed ^ chartOptions.plotOptions.inverted;
                barList[i].innerSize = chartOptions.pane.innerSize;
                barList[i].seriesIndex = i;
                barList[i].innerExtend = 1.0f;
                barList[i].chart = this;
                barList[i].data = new Vector2[chartData.categories.Count];
                for (int j = 0; j < chartData.categories.Count; ++j) barList[i].data[j] = GetDataRatio(i, j, stackValueList);

                //label
                if (chartOptions.label.enable)
                {
                    float rInner = chartSize.y * 0.5f;
                    float rRange = (chartSize.x - chartSize.y) * 0.5f;
                    for (int j = 0; j < chartData.categories.Count; ++j)
                    {
                        if (!IsValid(i, j)) continue;
                        float dist = rInner + rRange * (barList[i].data[j].y + barList[i].data[j].x * chartOptions.label.anchoredPosition) + chartOptions.label.offset;
                        int posIndex = chartOptions.xAxis.reversed ? chartData.categories.Count - j - 1 : j;
                        float pos = chartGrid.unitWidth * posIndex + chartGrid.unitWidth * 0.5f + offset[i];
                        var label = Instantiate(labelTemp, seriesLabelRect);
                        label.text = GetFormattedPointText(i, j, chartOptions.label.format, 1);
                        label.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, -pos);
                        label.rectTransform.anchoredPosition = label.transform.up * dist;
                        label.transform.localEulerAngles += new Vector3(0.0f, 0.0f, chartOptions.label.rotation);
                    }
                }
            }

            ChartHelper.Destroy(labelTemp.gameObject);
        }
    }
}