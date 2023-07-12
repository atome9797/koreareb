using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if CHART_TMPRO
using TMPro;
#endif

namespace ChartUtil
{
    public class ChartGridCircleInverted : ChartGridCircle
    {
        [HideInInspector] public int activeCount = 0;
        [HideInInspector] public float startAngle = 0.0f;
        [HideInInspector] public float endAngle = 360.0f;
        [HideInInspector] public bool semicircle = false;
        [HideInInspector] public Vector2 centerOffset;

        public override int GetItemIndex(Vector2 pos)
        {
            int index = -1;
            return index;
        }

        protected override void ComputeChartSize()
        {
            float tmp = semicircle ? 2.0f : 1.0f;
            chart.chartSize.x = (chart.chartSize.x < chart.chartSize.y * tmp ? chart.chartSize.x : chart.chartSize.y * tmp) * Mathf.Clamp01(outerSize);

            float labelSize = 0.0f;
            if (chart.chartOptions.yAxis.enableLabel)
                labelSize += chart.chartOptions.yAxis.labelOption.customizedText == null ? 
                    chart.chartOptions.yAxis.labelOption.fontSize : chart.chartOptions.yAxis.labelOption.customizedText.fontSize;
            chart.chartSize.x -= (labelSize + yTickSize.y + innerBorderWidth * 0.5f) * 2.0f * tmp;
            chart.chartSize.y = chart.chartSize.x * Mathf.Clamp(innerSize, 0.0f, outerSize);

            if (semicircle)
            {
                startAngle = Mathf.Clamp(startAngle, -90.0f, 90.0f);
                endAngle = Mathf.Clamp(endAngle, -90.0f, 90.0f);
                centerOffset.y = -chart.chartSize.x * 0.25f;
            }
            gridRect.anchoredPosition = centerOffset;
            labelRect.anchoredPosition = centerOffset;
        }

        protected override void UpdateBackground()
        {
            Image background = ChartHelper.CreateImage("Background", chart.transform);
            background.transform.SetAsFirstSibling();
            background.sprite = Resources.Load<Sprite>("Images/Chart_Circle_512x512");
            background.color = chart.chartOptions.plotOptions.backgroundColor;
            float bSize = chart.chartSize.x + yTickSize.y * 2.0f + outerBorderWidth;
            background.rectTransform.sizeDelta = new Vector2(bSize, bSize);
            background.rectTransform.anchoredPosition = centerOffset;
            if (semicircle)
            {
                background.type = Image.Type.Filled;
                background.fillMethod = Image.FillMethod.Radial360;
                background.fillOrigin = 3;
                background.fillAmount = 0.5f;
            }
        }

        protected override void UpdateInnerBorder()
        {
            float smoothness = Mathf.Clamp01(3.0f / innerBorderWidth);
            float gridWidth = innerBorderWidth * (1 + smoothness);
            ChartGraphicRing innerBorder = ChartHelper.CreateEmptyRect("InnerRing", gridRect).gameObject.AddComponent<ChartGraphicRing>();
            innerBorder.gameObject.AddComponent<ChartMaterialHandler>().Load("Materials/Chart_VBlur");
            innerBorder.rectTransform.sizeDelta = new Vector2(chart.chartSize.y, chart.chartSize.y);
            innerBorder.color = chart.chartOptions.pane.innerBorderColor;
            innerBorder.width = gridWidth;
            innerBorder.mid = midGrid;
            innerBorder.side = activeCount;
            innerBorder.startAngle = startAngle;
            innerBorder.endAngle = endAngle;
            innerBorder.isCircular = circularGrid;
            innerBorder.material.SetFloat("_Smoothness", smoothness);
        }

        protected override void UpdateOuterBorder()
        {
            float smoothness = Mathf.Clamp01(3.0f / outerBorderWidth);
            float gridWidth = outerBorderWidth * (1 + smoothness);
            ChartGraphicRing outerBorder = ChartHelper.CreateEmptyRect("OuterRing", gridRect).gameObject.AddComponent<ChartGraphicRing>();
            outerBorder.gameObject.AddComponent<ChartMaterialHandler>().Load("Materials/Chart_VBlur");
            outerBorder.rectTransform.sizeDelta = new Vector2(chart.chartSize.x, chart.chartSize.x);
            outerBorder.color = chart.chartOptions.pane.outerBorderColor;
            outerBorder.width = gridWidth;
            outerBorder.mid = midGrid;
            outerBorder.side = activeCount;
            outerBorder.startAngle = startAngle;
            outerBorder.endAngle = endAngle;
            outerBorder.isCircular = circularGrid;
            outerBorder.material.SetFloat("_Smoothness", smoothness);
        }

        protected override void UpdateYAxisGrid()
        {
            float smoothness = Mathf.Clamp01(3.0f / chart.chartOptions.yAxis.gridLineWidth);
            float gridWidth = chart.chartOptions.yAxis.gridLineWidth * (1 + smoothness);
            ChartGraphicCircle yGrid = ChartHelper.CreateEmptyRect("XGrid", gridRect).gameObject.AddComponent<ChartGraphicCircle>();
            yGrid.gameObject.AddComponent<ChartMaterialHandler>().Load("Materials/Chart_VBlur");
            yGrid.rectTransform.sizeDelta = new Vector2(chart.chartSize.x, chart.chartSize.x);
            yGrid.color = chart.chartOptions.yAxis.gridLineColor;
            yGrid.width = gridWidth;
            yGrid.num = activeCount;
            yGrid.mid = midGrid;
            yGrid.side = (int)yAxisInfo.count;
            yGrid.innerSize = innerSize;
            yGrid.startAngle = startAngle;
            yGrid.endAngle = endAngle;
            yGrid.isCircular = circularGrid;
            yGrid.material.SetFloat("_Smoothness", smoothness);
        }

        protected override void UpdateYAxisTick()
        {
            float smoothness = Mathf.Clamp01(3.0f / yTickSize.x);
            float gridWidth = yTickSize.x * (1 + smoothness);
            ChartGraphicRadialLine yTicks = ChartHelper.CreateEmptyRect("XTicks", gridRect).gameObject.AddComponent<ChartGraphicRadialLine>();
            yTicks.gameObject.AddComponent<ChartMaterialHandler>().Load("Materials/Chart_UBlur");
            float bSize = chart.chartSize.x + yTickSize.y * 2.0f + outerBorderWidth;
            yTicks.rectTransform.sizeDelta = new Vector2(bSize, bSize);
            yTicks.color = chart.chartOptions.yAxis.tickColor;
            yTicks.width = gridWidth;
            yTicks.innerSize = chart.chartSize.x / yTicks.rectTransform.sizeDelta.x;
            yTicks.startAngle = startAngle;
            yTicks.endAngle = endAngle;
            yTicks.mid = midGrid;
            yTicks.side = (int)yAxisInfo.count;
            yTicks.material.SetFloat("_Smoothness", smoothness);
        }

        protected override void UpdateYAxisLabel()
        {
#if CHART_TMPRO
            TextMeshProUGUI labelTemp;
#else
            Text labelTemp;
#endif
            labelTemp = ChartHelper.CreateText("YGridLabel", chart.transform, chart.chartOptions.yAxis.labelOption, chart.chartOptions.plotOptions.generalFont);
            labelTemp.rectTransform.sizeDelta = Vector2.zero;

            float total = Mathf.Repeat(endAngle - startAngle, 360.0001f);
            float dist = chart.chartSize.x * 0.5f + labelTemp.fontSize * 0.5f + yTickSize.y + innerBorderWidth * 0.5f;
            float steps = total > 359.0f ? yAxisInfo.count - 1 : yAxisInfo.count;
            for (int i = 0; i <= steps; ++i)
            {
                var label = GameObject.Instantiate(labelTemp, labelRect);
                label.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, -startAngle - total / yAxisInfo.count * i);
                label.rectTransform.anchoredPosition = label.transform.up * dist;
                label.text = (yAxisInfo.min + yAxisInfo.interval * i).ToString(yAxisInfo.labelFormat);
            }

            ChartHelper.Destroy(labelTemp.gameObject);
        }

        protected override void ComputeXAxis()
        {
            unitWidth = (chart.chartSize.x - chart.chartSize.y) * 0.5f / activeCount;
        }

        protected override void UpdateXAxisGrid()
        {
            float smoothness = Mathf.Clamp01(3.0f / chart.chartOptions.xAxis.gridLineWidth);
            float gridWidth = chart.chartOptions.xAxis.gridLineWidth * (1 + smoothness);
            ChartGraphicRadialLine xGrid = ChartHelper.CreateEmptyRect("XGrid", gridRect).gameObject.AddComponent<ChartGraphicRadialLine>();
            if (!midGrid) xGrid.transform.SetAsFirstSibling();
            xGrid.gameObject.AddComponent<ChartMaterialHandler>().Load("Materials/Chart_UBlur");
            xGrid.rectTransform.sizeDelta = new Vector2(chart.chartSize.x, chart.chartSize.x);
            xGrid.color = chart.chartOptions.xAxis.gridLineColor;
            xGrid.width = gridWidth;
            xGrid.innerSize = innerSize;
            xGrid.startAngle = startAngle;
            xGrid.endAngle = endAngle;
            xGrid.mid = midGrid;
            xGrid.side = (int)yAxisInfo.count;
            xGrid.material.SetFloat("_Smoothness", smoothness);
        }

        protected override void UpdateXAxisTick()
        {

        }

        protected override void UpdateXAxisLabel()
        {

        }
    }
}