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
    public class LineChart : ChartBase
    {
        [HideInInspector] public ChartGridRect chartGrid;
        LineChartPoint[] pointList;
        LineChartLine[] lineList;
        LineChartShade[] shadeList;
        Material pointMat;
        Material lineMat;
        Material pointMatFade;
        Material lineMatFade;
        Material shadeMatFade;

        private void OnDestroy()
        {
            if (pointMat != null) ChartHelper.Destroy(pointMat);
            if (lineMat != null) ChartHelper.Destroy(lineMat);
            if (pointMatFade != null) ChartHelper.Destroy(pointMatFade);
            if (lineMatFade != null) ChartHelper.Destroy(lineMatFade);
            if (shadeMatFade != null) ChartHelper.Destroy(shadeMatFade);
        }

        public override void UpdateChart()
        {
            if (chartGrid == null)
            {
                if (chartOptions.plotOptions.inverted) chartGrid = new ChartGridRectInverted();
                else chartGrid = new ChartGridRect();
                chartGrid.chart = this;
                chartGrid.midGrid = true;
                chartGrid.UpdateGrid();
            }
            dataRect = ChartHelper.CreateEmptyRect("DataRect", transform, true);
            labelRect = ChartHelper.CreateEmptyRect("LabelRect", transform, true);
            dataRect.pivot = Vector2.zero;
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
            float min = float.PositiveInfinity;
            Vector2 mousePos = localMousePosition + chartSize * 0.5f;
            if (chartOptions.plotOptions.inverted)
            {
                float posx = chartGrid.unitWidth * (currCate + 0.5f);
                float posx2 = 0.0f;
                int nextCate = 0;
                if (!chartOptions.xAxis.reversed)
                {
                    posx = chartSize.y - posx;
                    if (mousePos.y > posx)
                    {
                        nextCate = currCate - 1;
                        posx2 = posx + chartGrid.unitWidth;
                    }
                    else
                    {
                        nextCate = currCate + 1;
                        posx2 = posx - chartGrid.unitWidth;
                    }
                }
                else
                {
                    if (mousePos.y > posx)
                    {
                        nextCate = currCate + 1;
                        posx2 = posx + chartGrid.unitWidth;
                    }
                    else
                    {
                        nextCate = currCate - 1;
                        posx2 = posx - chartGrid.unitWidth;
                    }
                }
                nextCate = Mathf.Clamp(nextCate, 0, chartData.categories.Count);

                for (int i = 0; i < chartData.series.Count; ++i)
                {
                    if (!chartData.series[i].show || !IsValid(i, nextCate)) continue;
                    float r = (mousePos.y - posx) / (posx2 - posx);
                    float size = Mathf.Abs(pointList[i].data[currCate].x) * chartSize.x;
                    float size2 = Mathf.Abs(pointList[i].data[nextCate].x) * chartSize.x;
                    float pos = (pointList[i].data[currCate].y + pointList[i].data[currCate].x * 0.5f) * chartSize.x;
                    float pos2 = (pointList[i].data[nextCate].y + pointList[i].data[nextCate].x * 0.5f) * chartSize.x;
                    size = Mathf.Lerp(size, size2, r);
                    pos = Mathf.Lerp(pos, pos2, r);
                    float dir = mousePos.x - pos;
                    if (chartOptions.plotOptions.columnStacking == ColumnStacking.None)
                    {
                        if (Mathf.Abs(dir) < size * 0.5f && size < min) { index = i; min = size; }
                    }
                    else
                    {
                        if (Mathf.Abs(dir) < size * 0.5f) { index = i; break; }
                    }
                }
            }
            else
            {
                float posx = chartGrid.unitWidth * (currCate + 0.5f);
                float posx2 = 0.0f;
                int nextCate = 0;
                if (chartOptions.xAxis.reversed)
                {
                    posx = chartSize.x - posx;
                    if (mousePos.x > posx)
                    {
                        nextCate = currCate - 1;
                        posx2 = posx + chartGrid.unitWidth;
                    }
                    else
                    {
                        nextCate = currCate + 1;
                        posx2 = posx - chartGrid.unitWidth;
                    }
                }
                else
                {
                    if (mousePos.x > posx)
                    {
                        nextCate = currCate + 1;
                        posx2 = posx + chartGrid.unitWidth;
                    }
                    else
                    {
                        nextCate = currCate - 1;
                        posx2 = posx - chartGrid.unitWidth;
                    }
                }
                nextCate = Mathf.Clamp(nextCate, 0, chartData.categories.Count);

                for (int i = 0; i < chartData.series.Count; ++i)
                {
                    if (!chartData.series[i].show || !IsValid(i, nextCate)) continue;
                    float r = (mousePos.x - posx) / (posx2 - posx);
                    float size = Mathf.Abs(pointList[i].data[currCate].x) * chartSize.y;
                    float size2 = Mathf.Abs(pointList[i].data[nextCate].x) * chartSize.y;
                    float pos = (pointList[i].data[currCate].y + pointList[i].data[currCate].x * 0.5f) * chartSize.y;
                    float pos2 = (pointList[i].data[nextCate].y + pointList[i].data[nextCate].x * 0.5f) * chartSize.y;
                    size = Mathf.Lerp(size, size2, r);
                    pos = Mathf.Lerp(pos, pos2, r);
                    float dir = mousePos.y - pos;
                    if (chartOptions.plotOptions.columnStacking == ColumnStacking.None)
                    {
                        if (Mathf.Abs(dir) < size * 0.5f && size < min) { index = i; min = size; }
                    }
                    else
                    {
                        if (Mathf.Abs(dir) < size * 0.5f) { index = i; break; }
                    }
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
            for (int i = 0; i < pointList.Length; ++i)
            {
                if (pointList[i] == null || i == currSeries) continue;
                pointList[i].material = pointMatFade;
                if (chartOptions.plotOptions.lineChartOption.enableLine) lineList[i].material = lineMatFade;
                if (chartOptions.plotOptions.lineChartOption.enableShade) shadeList[i].material = shadeMatFade;
            }
        }

        protected override void UnhighlightCurrentSeries()
        {
            for (int i = 0; i < pointList.Length; ++i)
            {
                if (pointList[i] == null || i == currSeries) continue;
                pointList[i].material = pointMat;
                if (chartOptions.plotOptions.lineChartOption.enableLine) lineList[i].material = lineMat;
                if (chartOptions.plotOptions.lineChartOption.enableShade) shadeList[i].material = null;
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

        void UpdateItemsLinear()
        {

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
            if (chartOptions.plotOptions.lineChartOption.enablePointOutline)
            {
                pointMat = new Material(Resources.Load<Material>("Materials/Chart_OutlineCircle"));
                pointMat.SetFloat("_Smoothness", Mathf.Clamp01(2.0f / chartOptions.plotOptions.lineChartOption.pointSize));
                pointMat.SetFloat("_OutlineWidth", Mathf.Clamp01(chartOptions.plotOptions.lineChartOption.pointOutlineWidth * 2.0f / chartOptions.plotOptions.lineChartOption.pointSize));
                pointMat.SetColor("_OutlineColor", chartOptions.plotOptions.lineChartOption.pointOutlineColor);
                pointMatFade = new Material(pointMat);
                pointMatFade.color = new Color(1.0f, 1.0f, 1.0f, fadeValue);
                pointMatFade.SetColor("_OutlineColor", chartOptions.plotOptions.lineChartOption.pointOutlineColor * fadeValue);
            }
            else
            {
                pointMat = new Material(Resources.Load<Material>("Materials/Chart_Circle"));
                pointMat.SetFloat("_Smoothness", Mathf.Clamp01(3.0f / chartOptions.plotOptions.lineChartOption.pointSize));
                pointMatFade = new Material(pointMat);
                pointMatFade.color = new Color(1.0f, 1.0f, 1.0f, fadeValue);
            }
            if (chartOptions.plotOptions.lineChartOption.enableLine)
            {
                lineMat = new Material(Resources.Load<Material>("Materials/Chart_UBlur"));
                lineMat.SetFloat("_Smoothness", Mathf.Clamp01(3.0f / chartOptions.plotOptions.lineChartOption.lineWidth));
                lineMatFade = new Material(lineMat);
                lineMatFade.color = new Color(1.0f, 1.0f, 1.0f, fadeValue);
            }
            if (chartOptions.plotOptions.lineChartOption.enableShade)
            {
                shadeMatFade = new Material(Resources.Load<Material>("Materials/Chart_UI"));
                shadeMatFade.color = new Color(1.0f, 1.0f, 1.0f, fadeValue);
            }

            //item
            pointList = new LineChartPoint[chartData.series.Count];
            if (chartOptions.plotOptions.lineChartOption.enableLine) lineList = chartOptions.plotOptions.lineChartOption.splineCurve ? new LineChartLineCurve[chartData.series.Count] : new LineChartLine[chartData.series.Count];
            if (chartOptions.plotOptions.lineChartOption.enableShade) shadeList = chartOptions.plotOptions.lineChartOption.splineCurve ? new LineChartShadeCurve[chartData.series.Count] : new LineChartShade[chartData.series.Count];
            float[] stackValueList = chartOptions.plotOptions.columnStacking == ColumnStacking.None ? null : new float[chartData.categories.Count];
            float[] stackValueListNeg = chartOptions.plotOptions.columnStacking == ColumnStacking.None ? null : new float[chartData.categories.Count];
            for (int i = 0; i < chartData.series.Count; ++i)
            {
                RectTransform seriesRect = ChartHelper.CreateEmptyRect(chartData.series[i].name, dataRect, true);
                seriesRect.SetAsFirstSibling();
                RectTransform seriesLabelRect = ChartHelper.CreateEmptyRect(chartData.series[i].name, labelRect, true);
                seriesLabelRect.SetAsFirstSibling();
                if (skipSeries.Contains(i) || !chartData.series[i].show) continue;

                //point
                pointList[i] = ChartHelper.CreateEmptyRect("Point", seriesRect, true).gameObject.AddComponent<LineChartPoint>();
                pointList[i].material = pointMat;
                pointList[i].color = chartOptions.plotOptions.dataColor[i % chartOptions.plotOptions.dataColor.Length];
                pointList[i].diameter = chartOptions.plotOptions.lineChartOption.pointSize;
                pointList[i].inverted = chartOptions.plotOptions.inverted;
                pointList[i].reverse = chartOptions.xAxis.reversed ^ chartOptions.plotOptions.inverted;
                pointList[i].data = new Vector2[chartData.categories.Count];
                pointList[i].seriesIndex = i;
                pointList[i].chart = this;
                for (int j = 0; j < chartData.categories.Count; ++j) pointList[i].data[j] = GetDataRatio(i, j, stackValueList, stackValueListNeg);

                //line
                if (chartOptions.plotOptions.lineChartOption.enableLine)
                {
                    lineList[i] = chartOptions.plotOptions.lineChartOption.splineCurve ?
                        ChartHelper.CreateEmptyRect("Line", seriesRect, true).gameObject.AddComponent<LineChartLineCurve>() :
                        ChartHelper.CreateEmptyRect("Line", seriesRect, true).gameObject.AddComponent<LineChartLine>();
                    lineList[i].transform.SetAsFirstSibling();
                    lineList[i].material = lineMat;
                    lineList[i].color = pointList[i].color;
                    lineList[i].width = chartOptions.plotOptions.lineChartOption.lineWidth;
                    lineList[i].inverted = pointList[i].inverted;
                    lineList[i].reverse = pointList[i].reverse;
                    lineList[i].data = pointList[i].data;
                    lineList[i].seriesIndex = i;
                    lineList[i].chart = this;
                }

                //shade
                if (chartOptions.plotOptions.lineChartOption.enableShade)
                {
                    shadeList[i] = chartOptions.plotOptions.lineChartOption.splineCurve ?
                        ChartHelper.CreateEmptyRect("Shade", seriesRect, true).gameObject.AddComponent<LineChartShadeCurve>() :
                        ChartHelper.CreateEmptyRect("Shade", seriesRect, true).gameObject.AddComponent<LineChartShade>();
                    shadeList[i].transform.SetAsFirstSibling();
                    shadeList[i].color = new Color(pointList[i].color.r, pointList[i].color.g, pointList[i].color.b, chartOptions.plotOptions.lineChartOption.shadeOpacity);
                    shadeList[i].inverted = pointList[i].inverted;
                    shadeList[i].reverse = pointList[i].reverse;
                    shadeList[i].data = pointList[i].data;
                    shadeList[i].seriesIndex = i;
                    shadeList[i].chart = this;
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
                        offsetPos += unit * 0.5f;

                        for (int j = 0; j < chartData.categories.Count; ++j)
                        {
                            if (!IsValid(i, j)) continue;
                            float pos = offsetPos + unit * j;
                            float h = chartSize.x * (pointList[i].data[j].y + pointList[i].data[j].x * chartOptions.label.anchoredPosition) +
                                chartOptions.label.offset * Mathf.Sign(pointList[i].data[j].x);
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
                        offsetPos += unit * 0.5f;

                        for (int j = 0; j < chartData.categories.Count; ++j)
                        {
                            if (!IsValid(i, j)) continue;
                            float pos = offsetPos + unit * j;
                            float h = chartSize.y * (pointList[i].data[j].y + pointList[i].data[j].x * chartOptions.label.anchoredPosition) +
                                chartOptions.label.offset * Mathf.Sign(pointList[i].data[j].x);
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
