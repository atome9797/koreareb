using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if CHART_TMPRO
using TMPro;
#endif

namespace ChartUtil
{
    public class Gauge : ChartBase
    {
        ChartGridCircleInverted chartGrid;
        float ringWidth;
        Material ringMat;
        Image pointer;

        private void OnDestroy()
        {
            if (ringMat != null) ChartHelper.Destroy(ringMat);
        }

        public override void UpdateChart()
        {
            chartGrid = new ChartGridCircleInverted();
            chartGrid.chart = this;
            chartGrid.activeCount = 1;
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
        }

        protected override int FindCategory()
        {
            return -1;
        }

        protected override int FindSeries()
        {
            return -1;
        }

        protected override void HighlightCurrentCategory()
        {

        }

        protected override void UnhighlightCurrentCategory()
        {

        }

        protected override void HighlightCurrentSeries()
        {

        }

        protected override void UnhighlightCurrentSeries()
        {

        }

        protected override void UpdateTooltip()
        {
            tooltip.tooltipText.text = GetFormattedPointText(chartOptions.tooltip.headerFormat, 0);
            if (tooltip.tooltipText.text.Length > 0) tooltip.tooltipText.text += "\n";
            tooltip.tooltipText.text += GetFormattedPointText(chartOptions.tooltip.pointFormat, 0);
            tooltip.background.rectTransform.sizeDelta = new Vector2(tooltip.tooltipText.preferredWidth + 16.0f, tooltip.tooltipText.preferredHeight + 6.0f);
        }

        public bool IsValid()
        {
            return chartData.series[0].show && chartData.series[0].data.Count > 0 && chartData.series[0].data[0].show;
        }

        string GetFormattedHeaderText(int cateIndex, string format)
        {
            format = format.Replace("{category}", chartData.categories[cateIndex]);
            return format;
        }

        string GetFormattedPointText(string format, int type)
        {
            string f = type == 0 ? chartOptions.tooltip.pointNumericFormat : chartOptions.label.numericFormat;
            format = format.Replace("\\n", "\n");
            format = format.Replace("{category}", "");
            format = format.Replace("{series.name}", chartData.series[0].name);
            format = format.Replace("{data.value}", GetValueString(chartData.series[0].data[0].value, f));
            format = format.Replace("{data.percentage}", GetPercentageString(chartData.series[0].data[0].value / chartGrid.yAxisInfo.max * 100, f));
            return format;
        }

        void UpdateItems()
        {
            if (!IsValid()) return;

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
            ringMat = new Material(Resources.Load<Material>("Materials/Chart_VBlur"));
            ringMat.SetFloat("_Smoothness", Mathf.Clamp01(3.0f / ringWidth));

            //band
            if (chartOptions.plotOptions.gaugeOption.bands != null)
            {
                float smoothness = Mathf.Clamp01(3.0f / chartOptions.plotOptions.gaugeOption.bandWidth);
                float gridWidth = chartOptions.plotOptions.gaugeOption.bandWidth * (1 + smoothness);
                foreach (ChartOptions.BandOptions bandOpt in chartOptions.plotOptions.gaugeOption.bands)
                {
                    ChartGraphicRing band = ChartHelper.CreateEmptyRect("Band", chartGrid.gridRect).gameObject.AddComponent<ChartGraphicRing>();
                    band.transform.SetAsFirstSibling();
                    band.gameObject.AddComponent<ChartMaterialHandler>().Load("Materials/Chart_VBlur");
                    band.rectTransform.sizeDelta = new Vector2(chartSize.x - gridWidth, chartSize.x - gridWidth);
                    band.color = bandOpt.color;
                    band.width = gridWidth;
                    band.startAngle = Mathf.Lerp(chartGrid.startAngle, chartGrid.endAngle, bandOpt.from);
                    band.endAngle = Mathf.Lerp(chartGrid.startAngle, chartGrid.endAngle, bandOpt.to);
                    band.material.SetFloat("_Smoothness", smoothness);
                }
            }

            //pointer
            {
                float r = (chartData.series[0].data[0].value - chartGrid.yAxisInfo.min) / chartGrid.yAxisInfo.span;
                float angle = Mathf.Lerp(chartGrid.startAngle, chartGrid.endAngle, r);
                Vector2 pSize = new Vector2(chartOptions.plotOptions.gaugeOption.pointerWidth, chartSize.x * 0.5f * chartOptions.plotOptions.gaugeOption.pointerLengthScale);

                Image background = ChartHelper.CreateImage("Pointer", chartGrid.gridRect);
                background.sprite = Resources.Load<Sprite>("Images/Chart_Circle_128x128");
                background.color = chartOptions.plotOptions.gaugeOption.pointerColor;
                background.rectTransform.sizeDelta = new Vector2(pSize.x, pSize.x) * 2.0f;

                pointer = ChartHelper.CreateImage("Pointer", dataRect);
                pointer.sprite = Resources.Load<Sprite>("Images/Chart_Pointer");
                pointer.type = Image.Type.Sliced;
                pointer.color = chartOptions.plotOptions.gaugeOption.pointerColor;
                pointer.rectTransform.sizeDelta = pSize;
                pointer.rectTransform.pivot = new Vector2(0.5f, 0.0f);
                pointer.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, -angle);
            }
            
            //label
            if (chartOptions.label.enable)
            {
                var label = Instantiate(labelTemp, labelRect);
                label.text = GetFormattedPointText(chartOptions.label.format, 1);
                label.rectTransform.anchoredPosition = new Vector2(0.0f, chartOptions.label.offset);
            }

            ChartHelper.Destroy(labelTemp.gameObject);
        }
    }
}