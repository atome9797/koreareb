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
    public class BarChart : ChartBase
    {
        [HideInInspector] public ChartGridRect chartGrid;
        float barWidth;
        float[] offset;
        BarChartBar[] barList;
        Material barMatFade;

        private void OnDestroy()
        {
            if (barMatFade != null) ChartHelper.Destroy(barMatFade);
        }

        public override void UpdateChart()
        {
            if (chartGrid == null)
            {
                if (chartOptions.plotOptions.inverted) chartGrid = new ChartGridRectInverted();
                else chartGrid = new ChartGridRect();
                chartGrid.chart = this;
                chartGrid.midGrid = false;
                chartGrid.UpdateGrid();
            }
            dataRect = ChartHelper.CreateEmptyRect("DataRect", transform, true);
            labelRect = ChartHelper.CreateEmptyRect("LabelRect", transform, true);
            UpdateItems();
            chartGrid.labelRect.SetSiblingIndex(labelRect.GetSiblingIndex() - 1);
            if (chartOptions.plotOptions.frontGrid) chartGrid.gridRect.SetSiblingIndex(dataRect.GetSiblingIndex() + 1);
        }

        protected override int FindCategory()
        {
            return chartGrid.GetItemIndex(localMousePosition + chartSize * 0.5f);
        }

        protected override int FindSeries()
        {
            int index = -1;
            Vector2 mousePos = localMousePosition + chartSize * 0.5f;
            if (chartOptions.plotOptions.inverted)
            {
                for (int i = 0; i < chartData.series.Count; ++i)
                {
                    if (!chartData.series[i].show) continue;
                    Vector2 size = new Vector2(Mathf.Abs(barList[i].data[currCate].x) * chartSize.x, barWidth);
                    Vector2 pos = new Vector2((barList[i].data[currCate].y + barList[i].data[currCate].x * 0.5f) * chartSize.x, chartGrid.unitWidth * (currCate + 0.5f));
                    if (!chartOptions.xAxis.reversed) pos.y = chartSize.y - pos.y;
                    pos.y += offset[i];
                    Vector2 dir = mousePos - pos;
                    if (Mathf.Abs(dir.x) < size.x * 0.5f && Mathf.Abs(dir.y) < size.y * 0.5f) { index = i; break; }
                }
            }
            else
            {
                for (int i = 0; i < chartData.series.Count; ++i)
                {
                    if (!chartData.series[i].show) continue;
                    Vector2 size = new Vector2(barWidth, Mathf.Abs(barList[i].data[currCate].x) * chartSize.y);
                    Vector2 pos = new Vector2(chartGrid.unitWidth * (currCate + 0.5f), (barList[i].data[currCate].y + barList[i].data[currCate].x * 0.5f) * chartSize.y);
                    if (chartOptions.xAxis.reversed) pos.x = chartSize.x - pos.x;
                    pos.x += offset[i];
                    Vector2 dir = mousePos - pos;
                    if (Mathf.Abs(dir.x) < size.x * 0.5f && Mathf.Abs(dir.y) < size.y * 0.5f) { index = i; break; }
                }
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
            //tooltip.tooltipText.text = GetFormattedHeaderText(currCate, chartOptions.tooltip.headerFormat);
            tooltip.tooltipText.text = "";

            if (chartOptions.tooltip.share)
            {
                for (int i = 0; i < chartData.series.Count; ++i)
                {
                    if (!chartData.series[i].show || !IsValid(i, currCate)) continue;
                    //tooltip.tooltipText.text += "\n" + GetFormattedPointText(i, currCate, chartOptions.tooltip.pointFormat, 0);
                    tooltip.tooltipText.text += GetFormattedPointText(i, currCate, chartOptions.tooltip.pointFormat, 0);
                }
            }
            else
            {
                if (chartData.series[currSeries].show || !IsValid(currSeries, currCate))
                    //tooltip.tooltipText.text += "\n" + GetFormattedPointText(currSeries, currCate, chartOptions.tooltip.pointFormat, 0);
                    tooltip.tooltipText.text += GetFormattedPointText(currSeries, currCate, chartOptions.tooltip.pointFormat, 0);
            }
            tooltip.background.rectTransform.sizeDelta = new Vector2(tooltip.tooltipText.preferredWidth + 16, tooltip.tooltipText.preferredHeight + 12);
        }

        public bool IsValid(int seriesIndex, int cateIndex)
        {
            return cateIndex < chartData.series[seriesIndex].data.Count && chartData.series[seriesIndex].data[cateIndex].show;
        }

        Vector2 GetDataRatio(int seriesIndex, int cateIndex, float[] stackValueList, float[] stackValueListNeg)
        {
            Vector2 value = new Vector2();
            if (!IsValid(seriesIndex, cateIndex)) return value;
                switch (chartOptions.plotOptions.columnStacking)
            {
                case ColumnStacking.None:
                    value.x = (chartData.series[seriesIndex].data[cateIndex].value - chartGrid.yAxisInfo.baseLine) / chartGrid.yAxisInfo.span;
                    value.y = chartGrid.yAxisInfo.baseLineRatio;
                    break;
                case ColumnStacking.Normal:
                    value.x = (chartData.series[seriesIndex].data[cateIndex].value - chartGrid.yAxisInfo.baseLine) / chartGrid.yAxisInfo.span;
                    if (chartData.series[seriesIndex].data[cateIndex].value >= 0.0f)
                    {
                        value.y = chartGrid.yAxisInfo.baseLineRatio + stackValueList[cateIndex];
                        stackValueList[cateIndex] += value.x;
                    }
                    else
                    {
                        value.y = chartGrid.yAxisInfo.baseLineRatio + stackValueListNeg[cateIndex];
                        stackValueListNeg[cateIndex] += value.x;
                    }
                    break;
                case ColumnStacking.Percent:
                    if (chartData.series[seriesIndex].data[cateIndex].value >= 0.0f)
                    {
                        value.x = (chartData.series[seriesIndex].data[cateIndex].value / chartDataInfo.posSum[cateIndex] - chartGrid.yAxisInfo.baseLine) / chartGrid.yAxisInfo.span;
                        value.y = chartGrid.yAxisInfo.baseLineRatio + stackValueList[cateIndex];
                        stackValueList[cateIndex] += value.x;
                    }
                    else
                    {
                        value.x = (chartData.series[seriesIndex].data[cateIndex].value / chartDataInfo.negSum[cateIndex] - chartGrid.yAxisInfo.baseLine) / chartGrid.yAxisInfo.span;
                        value.y = chartGrid.yAxisInfo.baseLineRatio + stackValueListNeg[cateIndex];
                        stackValueListNeg[cateIndex] += value.x;
                    }
                    break;
                default:
                    break;
            }
            value.x = Mathf.Clamp(value.x, -1.0f, 1.0f);
            value.y = Mathf.Clamp(value.y, -1.0f, 1.0f);
            return value;
        }

        string GetFormattedHeaderText(int cateIndex, string format)
        {
            format = format.Replace("{category}", chartData.categories[cateIndex]);
            return format;
        }

        string GetFormattedPointText(int seriesIndex, int cateIndex, string format, int type)
        {
            float value = chartData.series[seriesIndex].data[cateIndex].value;
            string f;
            if (type == 0)
            {
                f = chartOptions.tooltip.pointNumericFormat;
                if (chartOptions.tooltip.absoluteValue) value = Mathf.Abs(value);
            }
            else
            {
                f = chartOptions.label.numericFormat;
                if (chartOptions.label.absoluteValue) value = Mathf.Abs(value);
            }
            float sum = value >= 0.0f ? chartDataInfo.posSum[cateIndex] : chartDataInfo.negSum[cateIndex];
            format = format.Replace("{category}", chartData.categories[cateIndex]);
            format = format.Replace("{series.name}", chartData.series[seriesIndex].name);
            format = format.Replace("{data.value}", GetValueString(value, f));
            format = format.Replace("{data.percentage}", GetPercentageString(value / sum * 100, f));
            return format;
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
            labelTemp.rectTransform.anchorMin = Vector2.zero;
            labelTemp.rectTransform.anchorMax = Vector2.zero;
            labelTemp.rectTransform.sizeDelta = Vector2.zero;
            labelTemp.rectTransform.localRotation = Quaternion.Euler(0.0f, 0.0f, chartOptions.label.rotation);

            //material
            barMatFade = new Material(Resources.Load<Material>("Materials/Chart_UI"));
            barMatFade.color = new Color(1.0f, 1.0f, 1.0f, fadeValue);

            //item
            float maxBarWidth = chartOptions.plotOptions.columnStacking == ColumnStacking.None ? chartGrid.unitWidth / chartData.series.Count : chartGrid.unitWidth;
            barWidth = Mathf.Clamp(chartOptions.plotOptions.barChartOption.barWidth, 0.0f, maxBarWidth);
            float barSpace = Mathf.Clamp(chartOptions.plotOptions.barChartOption.itemSeparation, -barWidth * 0.5f, maxBarWidth - barWidth);
            float barUnit = barWidth + barSpace;
            Debug.Log("maxBarwidth : " + maxBarWidth);

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

            BarChartBar background = null;
            barList = new BarChartBar[chartData.series.Count];
            float[] stackValueList = chartOptions.plotOptions.columnStacking == ColumnStacking.None ? null : new float[chartData.categories.Count];
            float[] stackValueListNeg = chartOptions.plotOptions.columnStacking == ColumnStacking.None ? null : new float[chartData.categories.Count];
            for (int i = 0; i < chartData.series.Count; ++i)
            {
                RectTransform seriesRect = ChartHelper.CreateEmptyRect(chartData.series[i].name, dataRect, true);
                seriesRect.SetAsFirstSibling();
                RectTransform seriesLabelRect = ChartHelper.CreateEmptyRect(chartData.series[i].name, labelRect, true);
                seriesLabelRect.SetAsFirstSibling();
                if (skipSeries.Contains(i) || !chartData.series[i].show) continue;

                //bar
                barList[i] = ChartHelper.CreateEmptyRect("Bar", seriesRect, true).gameObject.AddComponent<BarChartBar>();
                barList[i].color = chartOptions.plotOptions.dataColor[i % chartOptions.plotOptions.dataColor.Length];
                barList[i].width = barWidth;
                barList[i].offset = offset[i];
                barList[i].inverted = chartOptions.plotOptions.inverted;
                barList[i].reverse = chartOptions.xAxis.reversed ^ chartOptions.plotOptions.inverted;
                barList[i].seriesIndex = i;
                barList[i].chart = this;
                barList[i].data = new Vector2[chartData.categories.Count];
                for (int j = 0; j < chartData.categories.Count; ++j) barList[i].data[j] = GetDataRatio(i, j, stackValueList, stackValueListNeg);

                //background
                if (chartOptions.plotOptions.barChartOption.enableBarBackground && 
                    (chartOptions.plotOptions.columnStacking == ColumnStacking.None ||
                    (chartOptions.plotOptions.columnStacking != ColumnStacking.None && background == null)))
                {
                    background = ChartHelper.CreateEmptyRect("Background", seriesRect, true).gameObject.AddComponent<BarChartBar>();
                    background.transform.SetAsFirstSibling();
                    background.color = chartOptions.plotOptions.barChartOption.barBackgroundColor;
                    background.width = Mathf.Clamp(chartOptions.plotOptions.barChartOption.barBackgroundWidth, 0.0f, maxBarWidth);
                    background.offset = barList[i].offset;
                    background.inverted = barList[i].inverted;
                    background.reverse = barList[i].reverse;
                    background.isBackground = true;
                    background.seriesIndex = i;
                    background.chart = this;
                    background.data = new Vector2[chartData.categories.Count];
                    for (int j = 0; j < chartData.categories.Count; ++j) background.data[j] = Vector2.right;
                }

                //label
                if (chartOptions.label.enable)
                {
                    if (chartOptions.plotOptions.inverted)
                    {
                        float offsetPos = 0.0f;
                        float unit = chartGrid.unitWidth;
                        if (!chartOptions.xAxis.reversed)
                        {
                            unit *= -1;
                            offsetPos = chartSize.y;
                        }
                        offsetPos += offset[i] + unit * 0.5f;

                        for (int j = 0; j < chartData.categories.Count; ++j)
                        {
                            if (!IsValid(i, j)) continue;
                            float pos = offsetPos + unit * j;
                            float h = chartSize.x * (barList[i].data[j].y + barList[i].data[j].x * chartOptions.label.anchoredPosition) + 
                                chartOptions.label.offset * Mathf.Sign(barList[i].data[j].x);
                            var label = Instantiate(labelTemp, seriesLabelRect);
                            label.text = GetFormattedPointText(i, j, chartOptions.label.format, 1);
                            label.rectTransform.anchoredPosition = new Vector2(h, pos);
                        }
                    }
                    else
                    {
                        float offsetPos = 0.0f;
                        float unit = chartGrid.unitWidth;
                        if (chartOptions.xAxis.reversed)
                        {
                            unit *= -1;
                            offsetPos = chartSize.x;
                        }
                        offsetPos += offset[i] + unit * 0.5f;

                        for (int j = 0; j < chartData.categories.Count; ++j)
                        {
                            if (!IsValid(i, j)) continue;
                            float pos = offsetPos + unit * j;
                            float h = chartSize.y * (barList[i].data[j].y + barList[i].data[j].x * chartOptions.label.anchoredPosition) + 
                                chartOptions.label.offset * Mathf.Sign(barList[i].data[j].x);
                            var label = Instantiate(labelTemp, seriesLabelRect);
                            label.text = GetFormattedPointText(i, j, chartOptions.label.format, 1);
                            label.rectTransform.anchoredPosition = new Vector2(pos, h);
                        }
                    }
                }
            }
            ChartHelper.Destroy(labelTemp.gameObject);
        }
    }
}