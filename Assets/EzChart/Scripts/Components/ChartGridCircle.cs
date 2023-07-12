using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if CHART_TMPRO
using TMPro;
#endif

namespace ChartUtil
{
    public class ChartGridCircle : ChartGrid
    {
        [HideInInspector] public bool circularGrid = true;
        [HideInInspector] public float innerSize = 0.0f;
        [HideInInspector] public float outerSize = 1.0f;
        [HideInInspector] public float mouseAngle;
        [HideInInspector] public List<float> positiveSum = new List<float>();

        protected MaskableGraphic highlight;
        protected Vector2 yTickSize, xTickSize;
        protected float outerBorderWidth, innerBorderWidth;

        public override int GetItemIndex(Vector2 pos)
        {
            if (pos.sqrMagnitude > 0.25f * chart.chartSize.x * chart.chartSize.x || pos.sqrMagnitude < 0.25f * chart.chartSize.y * chart.chartSize.y) return -1;
            int index = -1;
            mouseAngle = Mathf.Repeat(-Vector2.SignedAngle(new Vector2(0.0f, 1.0f), pos), 360.0001f);
            index = Mathf.FloorToInt(mouseAngle / unitWidth) % chart.chartData.categories.Count;
            if (chart.chartOptions.xAxis.reversed) index = chart.chartData.categories.Count - index - 1;
            return index;
        }

        public override void HighlightItem(int index)
        {
            if (highlight == null) return;
            if (chart.chartOptions.xAxis.reversed) index = chart.chartData.categories.Count - index - 1;
            highlight.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, -unitWidth * index);
            highlight.gameObject.SetActive(true);
        }

        public override void UnhighlightItem(int index)
        {
            if (highlight == null) return;
            highlight.gameObject.SetActive(false);
        }

        public override void UpdateGrid()
        {
            yTickSize = chart.chartOptions.yAxis.enableTick ? chart.chartOptions.yAxis.tickSize : new Vector2();
            xTickSize = chart.chartOptions.xAxis.enableTick ? chart.chartOptions.xAxis.tickSize : new Vector2();
            outerBorderWidth = chart.chartOptions.pane.enableOuterBorder ? chart.chartOptions.pane.outerBorderWidth : 0.0f;
            innerBorderWidth = chart.chartOptions.pane.enableInnerBorder ? chart.chartOptions.pane.innerBorderWidth : 0.0f;

            gridRect = ChartHelper.CreateEmptyRect("GridRect", chart.transform, true);
            labelRect = ChartHelper.CreateEmptyRect("GridLabelRect", chart.transform, true);

            ComputeChartSize();

            ComputeYAxis();
            if (chart.chartOptions.yAxis.enableGridLine) UpdateYAxisGrid();
            if (chart.chartOptions.yAxis.enableTick) UpdateYAxisTick();
            if (chart.chartOptions.yAxis.enableLabel) UpdateYAxisLabel();

            ComputeXAxis();
            if (chart.chartOptions.xAxis.enableGridLine) UpdateXAxisGrid();
            if (chart.chartOptions.xAxis.enableTick) UpdateXAxisTick();
            if (chart.chartOptions.xAxis.enableLabel) UpdateXAxisLabel();

            UpdateHighlight();
            if (chart.chartOptions.plotOptions.enableBackground) UpdateBackground();
            if (chart.chartOptions.pane.enableInnerBorder) UpdateInnerBorder();
            if (chart.chartOptions.pane.enableInnerBorder) UpdateOuterBorder();
        }
        
        protected virtual void ComputeChartSize()
        {
            chart.chartSize.x = (chart.chartSize.x < chart.chartSize.y ? chart.chartSize.x : chart.chartSize.y) * Mathf.Clamp01(outerSize);

            float labelSize = 0.0f;
            if (chart.chartOptions.xAxis.enableLabel)
                labelSize += chart.chartOptions.xAxis.labelOption.customizedText == null ?
                    chart.chartOptions.xAxis.labelOption.fontSize : chart.chartOptions.xAxis.labelOption.customizedText.fontSize;
            chart.chartSize.x -= (labelSize + xTickSize.y + innerBorderWidth * 0.5f) * 2.0f;
            chart.chartSize.y = chart.chartSize.x * Mathf.Clamp(innerSize, 0.0f, outerSize);
        }

        protected virtual void UpdateHighlight()
        {
            Image highlight = ChartHelper.CreateImage("Highlight", chart.transform);
            highlight.color = chart.chartOptions.plotOptions.itemHighlightColor;
            highlight.sprite = Resources.Load<Sprite>("Images/Chart_Circle_512x512");
            highlight.type = Image.Type.Filled;
            highlight.fillMethod = Image.FillMethod.Radial360;
            highlight.fillOrigin = (int)Image.Origin360.Top;
            highlight.fillAmount = Mathf.Clamp01(1.0f / chart.chartData.categories.Count);
            highlight.rectTransform.sizeDelta = new Vector2(chart.chartSize.x, chart.chartSize.x);
            highlight.gameObject.SetActive(false);
            this.highlight = highlight;
        }

        protected virtual void UpdateBackground()
        {
            Image background = ChartHelper.CreateImage("Background", chart.transform);
            background.transform.SetAsFirstSibling();
            background.sprite = Resources.Load<Sprite>("Images/Chart_Circle_512x512");
            background.color = chart.chartOptions.plotOptions.backgroundColor;
            float bSize = chart.chartSize.x + xTickSize.y * 2.0f + innerBorderWidth;
            background.rectTransform.sizeDelta = new Vector2(bSize, bSize);
        }

        protected virtual void UpdateInnerBorder()
        {
            if (chart.chartSize.y < 0.01f) return;
            float smoothness = Mathf.Clamp01(3.0f / innerBorderWidth);
            float gridWidth = innerBorderWidth * (1 + smoothness);
            ChartGraphicRing innerBorder = ChartHelper.CreateEmptyRect("InnerRing", gridRect).gameObject.AddComponent<ChartGraphicRing>();
            innerBorder.gameObject.AddComponent<ChartMaterialHandler>().Load("Materials/Chart_VBlur");
            innerBorder.rectTransform.sizeDelta = new Vector2(chart.chartSize.y, chart.chartSize.y);
            innerBorder.color = chart.chartOptions.pane.innerBorderColor;
            innerBorder.width = gridWidth;
            innerBorder.mid = midGrid;
            innerBorder.side = chart.chartData.categories.Count;
            innerBorder.isCircular = circularGrid;
            innerBorder.material.SetFloat("_Smoothness", smoothness);
        }

        protected virtual void UpdateOuterBorder()
        {
            float smoothness = Mathf.Clamp01(3.0f / outerBorderWidth);
            float gridWidth = outerBorderWidth * (1 + smoothness);
            ChartGraphicRing outerBorder = ChartHelper.CreateEmptyRect("OuterRing", gridRect).gameObject.AddComponent<ChartGraphicRing>();
            outerBorder.gameObject.AddComponent<ChartMaterialHandler>().Load("Materials/Chart_VBlur");
            outerBorder.rectTransform.sizeDelta = new Vector2(chart.chartSize.x, chart.chartSize.x);
            outerBorder.color = chart.chartOptions.pane.outerBorderColor;
            outerBorder.width = gridWidth;
            outerBorder.mid = midGrid;
            outerBorder.side = chart.chartData.categories.Count;
            outerBorder.isCircular = circularGrid;
            outerBorder.material.SetFloat("_Smoothness", smoothness);
        }

        protected virtual void ComputeYAxis()
        {
            yAxisInfo = new AxisInfo();
            switch (chart.chartOptions.plotOptions.columnStacking)
            {
                case ColumnStacking.None:
                    if (chart.chartOptions.yAxis.autoAxisValues)
                    {
                        yAxisInfo.Compute(chart.chartDataInfo.min > 0.0f ? chart.chartDataInfo.min : 0.0f, chart.chartDataInfo.max, chart.chartOptions.yAxis.axisDivision, chart.chartOptions.yAxis.startFromZero);
                    }
                    else
                    {
                        yAxisInfo.Compute(chart.chartOptions.yAxis.min > 0.0f ? chart.chartOptions.yAxis.min : 0.0f, chart.chartOptions.yAxis.max, chart.chartOptions.yAxis.axisDivision);
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

        protected virtual void UpdateYAxisGrid()
        {
            int interval = Mathf.RoundToInt(chart.chartOptions.xAxis.interval);
            if (interval < 1) interval = 1;
            float smoothness = Mathf.Clamp01(3.0f / chart.chartOptions.yAxis.gridLineWidth);
            float gridWidth = chart.chartOptions.yAxis.gridLineWidth * (1 + smoothness);
            ChartGraphicRadialLine yGrid = ChartHelper.CreateEmptyRect("YGrid", gridRect).gameObject.AddComponent<ChartGraphicRadialLine>();
            if (!midGrid) yGrid.transform.SetAsFirstSibling();
            yGrid.gameObject.AddComponent<ChartMaterialHandler>().Load("Materials/Chart_UBlur");
            yGrid.rectTransform.sizeDelta = new Vector2(chart.chartSize.x, chart.chartSize.x);
            yGrid.color = chart.chartOptions.yAxis.gridLineColor;
            yGrid.width = gridWidth;
            yGrid.innerSize = innerSize;
            yGrid.mid = midGrid;
            yGrid.side = chart.chartData.categories.Count;
            yGrid.interval = interval;
            yGrid.material.SetFloat("_Smoothness", smoothness);
        }

        protected virtual void UpdateYAxisTick()
        {
            //float circularGridRatio = circularGrid ? 1.0f : Mathf.Sin((90.0f - 360.0f / chart.chartData.categories.Count * 0.5f) * Mathf.Deg2Rad);
            //ChartGraphicRect yTicks = ChartHelper.CreateEmptyRect("YTicks", gridRect).gameObject.AddComponent<ChartGraphicRect>();
            //yTicks.color = chart.chartOptions.yAxis.tickColor;
            //yTicks.width = yTickSize.y;
            //yTicks.num = yAxisInfo.count;
            //yTicks.inverted = true;
            //yTicks.rectTransform.sizeDelta = new Vector2(yTickSize.x, (chart.chartSize.x - chart.chartSize.y) * 0.5f * circularGridRatio);
            //yTicks.rectTransform.anchoredPosition = new Vector2(0.0f, yTicks.rectTransform.sizeDelta.y * 0.5f + chart.chartOptions.xAxis.gridLineWidth * 0.5f + yTickSize.y * 0.5f);
        }

        protected virtual void UpdateYAxisLabel()
        {
#if CHART_TMPRO
            TextMeshProUGUI labelTemp;
#else
            Text labelTemp;
#endif
            labelTemp = ChartHelper.CreateText("YGridLabel", chart.transform, chart.chartOptions.yAxis.labelOption, chart.chartOptions.plotOptions.generalFont, TextAnchor.LowerCenter);
            labelTemp.rectTransform.sizeDelta = Vector2.zero;

            float circularGridRatio = circularGrid ? 1.0f : Mathf.Sin((90.0f - 360.0f / chart.chartData.categories.Count * 0.5f) * Mathf.Deg2Rad);
            float spacing = (chart.chartSize.x - chart.chartSize.y) * circularGridRatio * 0.5f / yAxisInfo.count;
            float offset = chart.chartSize.y * 0.5f + chart.chartOptions.xAxis.gridLineWidth * 0.5f;
            for (int i = 0; i <= yAxisInfo.count; ++i)
            {
                float h = offset + spacing * i;
                var label = GameObject.Instantiate(labelTemp, labelRect);
                label.text = (yAxisInfo.min + yAxisInfo.interval * i).ToString(yAxisInfo.labelFormat);
                label.transform.localPosition = new Vector2(0.0f, h);
            }

            ChartHelper.Destroy(labelTemp.gameObject);
        }

        protected virtual void ComputeXAxis()
        {
            unitWidth = 360.0f / chart.chartData.categories.Count;
        }

        protected virtual void UpdateXAxisGrid()
        {
            float smoothness = Mathf.Clamp01(3.0f / chart.chartOptions.xAxis.gridLineWidth);
            float gridWidth = chart.chartOptions.xAxis.gridLineWidth * (1 + smoothness);
            ChartGraphicCircle xGrid = ChartHelper.CreateEmptyRect("XGrid", gridRect).gameObject.AddComponent<ChartGraphicCircle>();
            xGrid.gameObject.AddComponent<ChartMaterialHandler>().Load("Materials/Chart_VBlur");
            xGrid.rectTransform.sizeDelta = new Vector2(chart.chartSize.x, chart.chartSize.x);
            xGrid.color = chart.chartOptions.xAxis.gridLineColor;
            xGrid.width = gridWidth;
            xGrid.num = yAxisInfo.count;
            xGrid.mid = midGrid;
            xGrid.side = chart.chartData.categories.Count;
            xGrid.innerSize = innerSize;
            xGrid.isCircular = circularGrid;
            xGrid.material.SetFloat("_Smoothness", smoothness);
        }

        protected virtual void UpdateXAxisTick()
        {
            int interval = Mathf.RoundToInt(chart.chartOptions.xAxis.interval);
            if (interval < 1) interval = 1;
            float smoothness = Mathf.Clamp01(3.0f / xTickSize.x);
            float gridWidth = xTickSize.x * (1 + smoothness);
            ChartGraphicRadialLine xTicks = ChartHelper.CreateEmptyRect("XTicks", gridRect).gameObject.AddComponent<ChartGraphicRadialLine>();
            xTicks.gameObject.AddComponent<ChartMaterialHandler>().Load("Materials/Chart_UBlur");
            float bSize = chart.chartSize.x + xTickSize.y * 2.0f + innerBorderWidth;
            xTicks.rectTransform.sizeDelta = new Vector2(bSize, bSize);
            xTicks.color = chart.chartOptions.xAxis.tickColor;
            xTicks.width = gridWidth;
            xTicks.innerSize = chart.chartSize.x / xTicks.rectTransform.sizeDelta.x;
            xTicks.mid = midGrid;
            xTicks.side = chart.chartData.categories.Count;
            xTicks.interval = interval;
            xTicks.material.SetFloat("_Smoothness", smoothness);
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

            int interval = Mathf.RoundToInt(chart.chartOptions.xAxis.interval);
            if (interval < 1) interval = 1;
            float dist = chart.chartSize.x * 0.5f + labelTemp.fontSize * 0.5f + xTickSize.y + innerBorderWidth * 0.5f;
            for (int i = 0; i < chart.chartData.categories.Count; i += interval)
            {
                int posIndex = chart.chartOptions.xAxis.reversed ? chart.chartData.categories.Count - i - 1 : i;
                var label = GameObject.Instantiate(labelTemp, labelRect);
                label.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, -unitWidth * posIndex - unitWidth * 0.5f);
                label.rectTransform.anchoredPosition = label.transform.up * dist;
                label.text = chart.chartData.categories[i];
            }

            ChartHelper.Destroy(labelTemp.gameObject);
        }
    }
}