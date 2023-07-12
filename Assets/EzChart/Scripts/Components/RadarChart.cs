using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if CHART_TMPRO
using TMPro;
#endif

namespace ChartUtil
{
    public class RadarChart : ChartBase
    {
        ChartGridCircle chartGrid;
        RadarChartPoint[] pointList;
        RadarChartLine[] lineList;
        RadarChartShade[] shadeList;
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
            chartGrid = new ChartGridCircle();
            chartGrid.chart = this;
            chartGrid.midGrid = true;
            chartGrid.circularGrid = chartOptions.plotOptions.radarChartOption.circularGrid;
            chartGrid.innerSize = 0.0f;
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
            float min = float.PositiveInfinity;
            float radius = chartSize.x * 0.5f;

            float posx = chartGrid.unitWidth * (currCate + 0.5f);
            float posx2 = 0.0f;
            int nextCate = 0;
            if (chartOptions.xAxis.reversed)
            {
                posx = 360.0f - posx;
                if (chartGrid.mouseAngle > posx)
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
                if (chartGrid.mouseAngle > posx)
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
            nextCate = (int)Mathf.Repeat(nextCate, chartData.categories.Count);

            for (int i = 0; i < chartData.series.Count; ++i)
            {
                if (!chartData.series[i].show || !IsValid(i, nextCate)) continue;
                float r = (chartGrid.mouseAngle - posx) / (posx2 - posx);
                float size = pointList[i].data[currCate].x * radius;
                float size2 = pointList[i].data[nextCate].x * radius;
                float pos = (pointList[i].data[currCate].y + pointList[i].data[currCate].x * 0.5f) * radius;
                float pos2 = (pointList[i].data[nextCate].y + pointList[i].data[nextCate].x * 0.5f) * radius;
                size = Mathf.Lerp(size, size2, r);
                pos = Mathf.Lerp(pos, pos2, r);
                float dir = localMousePosition.magnitude - pos;
                if (chartOptions.plotOptions.columnStacking == ColumnStacking.None)
                {
                    if (Mathf.Abs(dir) < size * 0.5f && size < min) { index = i; min = size; }
                }
                else
                {
                    if (Mathf.Abs(dir) < size * 0.5f) { index = i; break; }
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
                if (chartOptions.plotOptions.radarChartOption.enableLine) lineList[i].material = lineMatFade;
                if (chartOptions.plotOptions.radarChartOption.enableShade) shadeList[i].material = shadeMatFade;
            }
        }

        protected override void UnhighlightCurrentSeries()
        {
            for (int i = 0; i < pointList.Length; ++i)
            {
                if (pointList[i] == null || i == currSeries) continue;
                pointList[i].material = pointMat;
                if (chartOptions.plotOptions.radarChartOption.enableLine) lineList[i].material = lineMat;
                if (chartOptions.plotOptions.radarChartOption.enableShade) shadeList[i].material = null;
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

        void UpdateItems()
        {
#if CHART_TMPRO
            TextMeshProUGUI labelTemp;
#else
            Text labelTemp;
#endif
            labelTemp = ChartHelper.CreateText("Label", transform, chartOptions.label.textOption, chartOptions.plotOptions.generalFont);
            labelTemp.rectTransform.sizeDelta = Vector2.zero;

            //material
            if (chartOptions.plotOptions.radarChartOption.enablePointOutline)
            {
                pointMat = new Material(Resources.Load<Material>("Materials/Chart_OutlineCircle"));
                pointMat.SetFloat("_Smoothness", Mathf.Clamp01(2.0f / chartOptions.plotOptions.radarChartOption.pointSize));
                pointMat.SetFloat("_OutlineWidth", Mathf.Clamp01(chartOptions.plotOptions.radarChartOption.pointOutlineWidth * 2.0f / chartOptions.plotOptions.radarChartOption.pointSize));
                pointMat.SetColor("_OutlineColor", chartOptions.plotOptions.radarChartOption.pointOutlineColor);
                pointMatFade = new Material(pointMat);
                pointMatFade.color = new Color(1.0f, 1.0f, 1.0f, fadeValue);
                pointMatFade.SetColor("_OutlineColor", chartOptions.plotOptions.radarChartOption.pointOutlineColor * fadeValue);
            }
            else
            {
                pointMat = new Material(Resources.Load<Material>("Materials/Chart_Circle"));
                pointMat.SetFloat("_Smoothness", Mathf.Clamp01(3.0f / chartOptions.plotOptions.radarChartOption.pointSize));
                pointMatFade = new Material(pointMat);
                pointMatFade.color = new Color(1.0f, 1.0f, 1.0f, fadeValue);
            }
            if (chartOptions.plotOptions.radarChartOption.enableLine)
            {
                lineMat = new Material(Resources.Load<Material>("Materials/Chart_UBlur"));
                lineMat.SetFloat("_Smoothness", Mathf.Clamp01(3.0f / chartOptions.plotOptions.radarChartOption.lineWidth));
                lineMatFade = new Material(lineMat);
                lineMatFade.color = new Color(1.0f, 1.0f, 1.0f, fadeValue);
            }
            if (chartOptions.plotOptions.radarChartOption.enableShade)
            {
                shadeMatFade = new Material(Resources.Load<Material>("Materials/Chart_UI"));
                shadeMatFade.color = new Color(1.0f, 1.0f, 1.0f, fadeValue);
            }

            //item
            pointList = new RadarChartPoint[chartData.series.Count];
            if (chartOptions.plotOptions.radarChartOption.enableLine) lineList = new RadarChartLine[chartData.series.Count];
            if (chartOptions.plotOptions.radarChartOption.enableShade) shadeList = new RadarChartShade[chartData.series.Count];
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

                //point
                pointList[i] = ChartHelper.CreateEmptyRect("Point", seriesRect, true).gameObject.AddComponent<RadarChartPoint>();
                pointList[i].material = pointMat;
                pointList[i].color = chartOptions.plotOptions.dataColor[i % chartOptions.plotOptions.dataColor.Length];
                pointList[i].diameter = chartOptions.plotOptions.radarChartOption.pointSize;
                pointList[i].reverse = chartOptions.xAxis.reversed;
                pointList[i].data = new Vector2[chartData.categories.Count];
                pointList[i].seriesIndex = i;
                pointList[i].chart = this;
                pointList[i].CaculateBuffer();
                for (int j = 0; j < chartData.categories.Count; ++j) pointList[i].data[j] = GetDataRatio(i, j, stackValueList);

                //line
                if (chartOptions.plotOptions.radarChartOption.enableLine)
                {
                    lineList[i] = ChartHelper.CreateEmptyRect("Line", seriesRect, true).gameObject.AddComponent<RadarChartLine>();
                    lineList[i].transform.SetAsFirstSibling();
                    lineList[i].material = lineMat;
                    lineList[i].color = pointList[i].color;
                    lineList[i].width = chartOptions.plotOptions.radarChartOption.lineWidth;
                    lineList[i].reverse = pointList[i].reverse;
                    lineList[i].data = pointList[i].data;
                    lineList[i].point = pointList[i];
                    lineList[i].seriesIndex = i;
                    lineList[i].chart = this;
                }

                //shade
                if (chartOptions.plotOptions.radarChartOption.enableShade)
                {
                    shadeList[i] = ChartHelper.CreateEmptyRect("Shade", seriesRect, true).gameObject.AddComponent<RadarChartShade>();
                    shadeList[i].transform.SetAsFirstSibling();
                    shadeList[i].color = new Color(pointList[i].color.r, pointList[i].color.g, pointList[i].color.b, chartOptions.plotOptions.radarChartOption.shadeOpacity);
                    shadeList[i].reverse = pointList[i].reverse;
                    shadeList[i].data = pointList[i].data;
                    shadeList[i].point = pointList[i];
                    shadeList[i].seriesIndex = i;
                    shadeList[i].chart = this;
                }

                //label
                if (chartOptions.label.enable)
                {
                    for (int j = 0; j < chartData.categories.Count; ++j)
                    {
                        if (!IsValid(i, j)) continue;
                        float dist = chartSize.x * 0.5f * (pointList[i].data[j].y + pointList[i].data[j].x * chartOptions.label.anchoredPosition) + chartOptions.label.offset;
                        int posIndex = chartOptions.xAxis.reversed ? chartData.categories.Count - j - 1 : j;
                        float pos = chartGrid.unitWidth * posIndex + chartGrid.unitWidth * 0.5f;
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

