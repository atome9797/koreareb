using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if CHART_TMPRO
using TMPro;
#endif

namespace ChartUtil
{
    public class SolidGauge : ChartBase
    {
        ChartGridCircleInverted chartGrid;
        float ringWidth;
        ChartGraphicRing[] ringList;
        Material ringMat;
        Material ringMatFade;
        Material backgroundMat;

        private void OnDestroy()
        {
            if (ringMat != null) ChartHelper.Destroy(ringMat);
            if (ringMatFade != null) ChartHelper.Destroy(ringMatFade);
            if (backgroundMat != null) ChartHelper.Destroy(backgroundMat);
        }

        public override void UpdateChart()
        {
            int activeCount = 0;
            for (int i = 0; i < chartData.series.Count; ++i)
            {
                if (!chartData.series[i].show) continue;
                activeCount++;
            }
            
            chartGrid = new ChartGridCircleInverted();
            chartGrid.chart = this;
            chartGrid.activeCount = activeCount;
            chartGrid.midGrid = false;
            chartGrid.circularGrid = true;
            chartGrid.semicircle = chartOptions.pane.semicircle;
            chartGrid.startAngle = chartOptions.pane.startAngle;
            chartGrid.endAngle = chartOptions.pane.endAngle;
            chartGrid.innerSize = chartOptions.pane.innerSize;
            chartGrid.outerSize = chartOptions.pane.outerSize;
            chartGrid.UpdateGrid();

            dataRect = ChartHelper.CreateEmptyRect("DataRect", transform, true);
            labelRect = ChartHelper.CreateEmptyRect("LabelRect", transform, true);
            dataRect.anchoredPosition = chartGrid.centerOffset;
            labelRect.anchoredPosition = chartGrid.centerOffset;

            UpdateItems();
            chartGrid.labelRect.SetSiblingIndex(labelRect.GetSiblingIndex() - 1);
            if (chartOptions.plotOptions.frontGrid) chartGrid.gridRect.SetSiblingIndex(dataRect.GetSiblingIndex() + 1);
        }

        protected override int FindCategory()
        {
            int index = -1;
            Vector2 dir = localMousePosition - chartGrid.centerOffset;
            if (chartGrid.semicircle && dir.y < 0.0f) return -1; 
            if (dir.sqrMagnitude > 0.25f * chartSize.x * chartSize.x || dir.sqrMagnitude < 0.25f * chartSize.y * chartSize.y) return -1;
            int counter = 0;
            for (int i = 0; i < chartData.series.Count; ++i)
            {
                if (!IsValid(i)) continue;
                counter++;
                float dist = chartSize.y * 0.5f + chartGrid.unitWidth * counter;
                if (dir.sqrMagnitude < dist * dist) { index = i; break; }
            }
            return index;
        }

        protected override int FindSeries()
        {
            return currCate;
        }

        protected override void HighlightCurrentCategory()
        {
            for (int i = 0; i < ringList.Length; ++i)
            {
                if (ringList[i] == null || i == currCate) continue;
                ringList[i].material = ringMatFade;
            }
        }

        protected override void UnhighlightCurrentCategory()
        {
            for (int i = 0; i < ringList.Length; ++i)
            {
                if (ringList[i] == null || i == currCate) continue;
                ringList[i].material = ringMat;
            }
        }

        protected override void HighlightCurrentSeries()
        {

        }

        protected override void UnhighlightCurrentSeries()
        {

        }

        protected override void UpdateTooltip()
        {
            tooltip.tooltipText.text = GetFormattedPointText(currCate, chartOptions.tooltip.headerFormat, 0);
            if (tooltip.tooltipText.text.Length > 0) tooltip.tooltipText.text += "\n";
            tooltip.tooltipText.text += GetFormattedPointText(currCate, chartOptions.tooltip.pointFormat, 0);
            tooltip.background.rectTransform.sizeDelta = new Vector2(tooltip.tooltipText.preferredWidth + 16.0f, tooltip.tooltipText.preferredHeight + 6.0f);
        }

        public bool IsValid(int seriesIndex)
        {
            return chartData.series[seriesIndex].show && chartData.series[seriesIndex].data.Count > 0 && chartData.series[seriesIndex].data[0].show && chartData.series[seriesIndex].data[0].value > 0.0f;
        }

        string GetFormattedHeaderText(int cateIndex, string format)
        {
            format = format.Replace("{category}", chartData.categories[cateIndex]);
            return format;
        }

        string GetFormattedPointText(int seriesIndex, string format, int type)
        {
            string f = type == 0 ? chartOptions.tooltip.pointNumericFormat : chartOptions.label.numericFormat;
            format = format.Replace("\\n", "\n");
            format = format.Replace("{category}", "");
            format = format.Replace("{series.name}", chartData.series[seriesIndex].name);
            format = format.Replace("{data.value}", GetValueString(chartData.series[seriesIndex].data[0].value, f));
            format = format.Replace("{data.percentage}", GetPercentageString(chartData.series[seriesIndex].data[0].value / chartDataInfo.maxSum * 100, f));
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
            labelTemp.rectTransform.sizeDelta = Vector2.zero;

            //material
            float ringWidth = Mathf.Clamp(chartOptions.plotOptions.solidGaugeOption.barWidth, 0.0f, chartGrid.unitWidth);
            float backgroundWidth = Mathf.Clamp(chartOptions.plotOptions.solidGaugeOption.barBackgroundWidth, 0.0f, chartGrid.unitWidth);
            ringMat = new Material(Resources.Load<Material>("Materials/Chart_VBlur"));
            ringMat.SetFloat("_Smoothness", Mathf.Clamp01(3.0f / ringWidth));
            ringMatFade = new Material(ringMat);
            ringMatFade.color = new Color(1.0f, 1.0f, 1.0f, fadeValue);
            if (chartOptions.plotOptions.solidGaugeOption.enableBarBackground)
            {
                backgroundMat = new Material(Resources.Load<Material>("Materials/Chart_VBlur"));
                backgroundMat.SetFloat("_Smoothness", Mathf.Clamp01(3.0f / backgroundWidth));
            }

            //item
            ringList = new ChartGraphicRing[chartData.series.Count];
            int counter = 0;
            for (int i = 0; i < chartData.series.Count; ++i)
            {
                RectTransform seriesRect = ChartHelper.CreateEmptyRect(chartData.series[i].name, dataRect);
                seriesRect.sizeDelta = new Vector2(chartSize.x, chartSize.x);
                seriesRect.SetAsFirstSibling();
                RectTransform seriesLabelRect = ChartHelper.CreateEmptyRect(chartData.series[i].name, labelRect, true);
                seriesLabelRect.SetAsFirstSibling();
                if (!IsValid(i)) continue;

                //ring
                float rSize = chartSize.y + chartGrid.unitWidth * (counter + 0.5f) * 2.0f;
                float r = (chartData.series[i].data[0].value - chartGrid.yAxisInfo.min) / chartGrid.yAxisInfo.span;
                ringList[i] = ChartHelper.CreateEmptyRect("Ring", seriesRect).gameObject.AddComponent<ChartGraphicRing>();
                ringList[i].material = ringMat;
                ringList[i].rectTransform.sizeDelta = new Vector2(rSize, rSize);
                ringList[i].color = chartOptions.plotOptions.dataColor[i % chartOptions.plotOptions.dataColor.Length];
                ringList[i].width = ringWidth + 1.5f;   //fill gap
                ringList[i].startAngle = chartGrid.startAngle;
                ringList[i].endAngle = Mathf.Lerp(chartGrid.startAngle, chartGrid.endAngle, r);

                //background
                if (chartOptions.plotOptions.solidGaugeOption.enableBarBackground)
                {
                    ChartGraphicRing background = ChartHelper.CreateEmptyRect("Background", seriesRect).gameObject.AddComponent<ChartGraphicRing>();
                    background.transform.SetAsFirstSibling();
                    background.material = backgroundMat;
                    background.rectTransform.sizeDelta = new Vector2(rSize, rSize);
                    background.color = chartOptions.plotOptions.solidGaugeOption.barBackgroundColor;
                    background.width = backgroundWidth + 1.5f;   //fill gap
                    background.startAngle = chartGrid.startAngle;
                    background.endAngle = chartGrid.endAngle;
                }

                counter++;
            }

            ChartHelper.Destroy(labelTemp.gameObject);
        }
    }
}