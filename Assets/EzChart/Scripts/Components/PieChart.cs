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
    public class PieChart : ChartBase
    {
        //Image highlight;
        Image background;
        PieChartCircle[] pieList;
        Vector2 circleSize;
        Material pieMat;
        Material pieMatFade;
        float startAngle, endAngle;
        [HideInInspector] public Vector2 centerOffset;

        private void OnDestroy()
        {
            if (pieMat != null) ChartHelper.Destroy(pieMat);
            if (pieMatFade != null) ChartHelper.Destroy(pieMatFade);
        }

        public override void UpdateChart()
        {
            RectTransform gridReck = ChartHelper.CreateEmptyRect("GridRect", transform, true);
            dataRect = ChartHelper.CreateEmptyRect("DataRect", transform, true);
            labelRect = ChartHelper.CreateEmptyRect("LabelRect", transform, true);

            float tmp = chartOptions.pane.semicircle ? 2.0f : 1.0f;
            circleSize.x = (chartSize.x < chartSize.y * tmp ? chartSize.x : chartSize.y * tmp) * Mathf.Clamp01(chartOptions.pane.outerSize) * 0.9f;
            startAngle = chartOptions.pane.startAngle;
            endAngle = chartOptions.pane.endAngle;
            if (chartOptions.pane.semicircle)
            {
                startAngle = Mathf.Clamp(startAngle, -90.0f, 90.0f);
                endAngle = Mathf.Clamp(endAngle, -90.0f, 90.0f);
                centerOffset.y = -circleSize.x * 0.25f;
            }
            gridReck.anchoredPosition = centerOffset;
            dataRect.anchoredPosition = centerOffset;
            labelRect.anchoredPosition = centerOffset;

            if (chartOptions.plotOptions.enableBackground)
            {
                background = ChartHelper.CreateImage("Background", gridReck);
                background.sprite = Resources.Load<Sprite>("Images/Chart_Circle_512x512");
                background.color = chartOptions.plotOptions.backgroundColor;
                background.rectTransform.sizeDelta = new Vector2(circleSize.x, circleSize.x);
                if (chartOptions.pane.semicircle)
                {
                    background.type = Image.Type.Filled;
                    background.fillMethod = Image.FillMethod.Radial360;
                    background.fillOrigin = 3;
                    background.fillAmount = 0.5f;
                }
            }

            //highlight = ChartHelper.CreateImage("Highlight", gridReck);
            //highlight.sprite = Resources.Load<Sprite>("Images/Chart_Circle_512x512");
            //highlight.color = chartOptions.plotOptions.itemHighlightColor;
            //highlight.rectTransform.sizeDelta = new Vector2(circleSize.x, circleSize.x);
            //if (chartOptions.pane.semicircle)
            //{
            //    highlight.type = Image.Type.Filled;
            //    highlight.fillMethod = Image.FillMethod.Radial360;
            //    highlight.fillOrigin = 3;
            //    highlight.fillAmount = 0.5f;
            //}
            //highlight.gameObject.SetActive(false);

            circleSize.x *= 0.95f;
            circleSize.y = circleSize.x * Mathf.Clamp(chartOptions.pane.innerSize, 0.0f, chartOptions.pane.outerSize);

            UpdateItems();
        }

        protected override int FindCategory()
        {
            int index = -1;
            Vector2 dir = localMousePosition - centerOffset;
            if (dir.sqrMagnitude > 0.25f * circleSize.x * circleSize.x || dir.sqrMagnitude < 0.25f * circleSize.y * circleSize.y) return -1;
            for (int i = 0; i < chartData.series.Count; ++i)
            {
                if (!IsValid(i)) continue;
                float tmp = pieList[i].angle * 0.5f;
                Vector2 piedir = ChartGraphic.RotateCW(Vector2.up, pieList[i].cosSin);
                if (Vector2.Angle(dir, piedir) <= tmp) { index = i; break; }
            }
            return index;
        }

        protected override int FindSeries()
        {
            return currCate;
        }

        protected override void HighlightCurrentCategory()
        {
            //highlight.gameObject.SetActive(true);
            pieList[currCate].rectTransform.localScale /= 0.95f;
            for (int i = 0; i < pieList.Length; ++i)
            {
                if (pieList[i] == null || i == currCate) continue;
                pieList[i].material = pieMatFade;
            }
        }

        protected override void UnhighlightCurrentCategory()
        {
            //highlight.gameObject.SetActive(false);
            pieList[currCate].rectTransform.localScale = Vector3.one;
            for (int i = 0; i < pieList.Length; ++i)
            {
                if (pieList[i] == null || i == currCate) continue;
                pieList[i].material = pieMat;
            }
        }

        protected override void HighlightCurrentSeries()
        {
            HighlightCurrentCategory();
        }

        protected override void UnhighlightCurrentSeries()
        {
            UnhighlightCurrentCategory();
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
            //templates
#if CHART_TMPRO
            TextMeshProUGUI labelTemp;
            TextMeshProUGUI[] labelList = chartOptions.label.enable ? new TextMeshProUGUI[chartData.series.Count] : null;
#else
            Text labelTemp;
            Text[] labelList = chartOptions.label.enable ? new Text[chartData.series.Count] : null;
#endif
            labelTemp = ChartHelper.CreateText("Label", transform, chartOptions.label.textOption, chartOptions.plotOptions.generalFont);
            labelTemp.rectTransform.sizeDelta = Vector2.zero;
            labelTemp.rectTransform.localRotation = Quaternion.Euler(0.0f, 0.0f, chartOptions.label.rotation);

            Image lineTemp = ChartHelper.CreateImage("Line", transform);
            lineTemp.sprite = Resources.Load<Sprite>("Images/Chart_Line");
            lineTemp.type = Image.Type.Sliced;
            lineTemp.rectTransform.pivot = new Vector2(0.5f, 0.0f);
            Image[] lineList = chartOptions.label.enable ? new Image[chartData.series.Count] : null;

            //material
            float smoothness = Mathf.Clamp01(4.0f / (circleSize.x - circleSize.y));
            pieMat = new Material(Resources.Load<Material>("Materials/Chart_VBlur"));
            pieMat.SetFloat("_Smoothness", smoothness);
            pieMatFade = new Material(pieMat);
            pieMatFade.color = new Color(1.0f, 1.0f, 1.0f, fadeValue);

            //item
            float labelDist = Mathf.Lerp(circleSize.y * 0.5f, circleSize.x * 0.5f, chartOptions.label.anchoredPosition) + chartOptions.label.offset;
            pieList = new PieChartCircle[chartData.series.Count];
            float stack = startAngle;
            float total = Mathf.Repeat(endAngle - startAngle, 360.0001f);
            for (int i = 0; i < chartData.series.Count; ++i)
            {
                RectTransform seriesRect = ChartHelper.CreateEmptyRect(chartData.series[i].name, dataRect);
                seriesRect.sizeDelta = new Vector2(circleSize.x, circleSize.x);
                seriesRect.SetAsFirstSibling();
                RectTransform seriesLabelRect = ChartHelper.CreateEmptyRect(chartData.series[i].name, labelRect, true);
                //seriesLabelRect.sizeDelta = new Vector2(circleSize.x, circleSize.x);
                seriesLabelRect.SetAsFirstSibling();
                if (!IsValid(i)) continue;

                //pie
                pieList[i] = ChartHelper.CreateEmptyRect("Pie", seriesRect, true).gameObject.AddComponent<PieChartCircle>();
                pieList[i].material = pieMat;
                pieList[i].color = chartOptions.plotOptions.dataColor[i % chartOptions.plotOptions.dataColor.Length];
                pieList[i].angle = total * chartData.series[i].data[0].value / chartDataInfo.maxSum;
                //if (chartOptions.xAxis.reversed)
                //{
                //    pieList[i].center = stack - pieList[i].angle * 0.5f + 360.0f;
                //    stack -= pieList[i].angle;
                //}
                //else
                {
                    pieList[i].center = stack + pieList[i].angle * 0.5f;
                    stack += pieList[i].angle;
                }
                pieList[i].cosSin = new Vector2(Mathf.Cos(pieList[i].center * Mathf.Deg2Rad), Mathf.Sin(pieList[i].center * Mathf.Deg2Rad));
                pieList[i].innerSize = chartOptions.pane.innerSize;
                pieList[i].separation = chartOptions.plotOptions.pieChartOption.itemSeparation;

                //label
                if (chartOptions.label.enable)
                {
                    Vector2 dir = ChartGraphic.RotateCW(Vector2.up, pieList[i].cosSin);
                    labelList[i] = Instantiate(labelTemp, seriesLabelRect);
                    labelList[i].rectTransform.pivot = dir.x > 0.0f ? new Vector2(0.0f, 0.5f) : new Vector2(1.0f, 0.5f);
                    labelList[i].text = GetFormattedPointText(i, chartOptions.label.format, 1);
                    labelList[i].rectTransform.anchoredPosition = dir * labelDist;
                    if (chartOptions.label.offset > 0.0f) lineList[i] = Instantiate(lineTemp, seriesLabelRect);
                    //labelList[i].gameObject.SetActive(circle.fillAmount > 0.01f);
                }
            }

            //adjust label position
            if (chartOptions.label.enable && chartOptions.label.offset > 0.0f)
            {
                float height = labelTemp.fontSize * 1.2f;
                List<int> label_right = new List<int>();
                List<int> label_left = new List<int>();

                for (int i = 0; i < labelList.Length; ++i)
                {
                    if (labelList[i] == null || !labelList[i].gameObject.activeSelf) continue;
                    if (labelList[i].rectTransform.anchoredPosition.x > 0.0f) label_right.Add(i);
                    else label_left.Add(i);
                }
                label_left.Reverse();

                //right
                float y = 99999.0f;
                foreach (int i in label_right)
                {
                    if (labelList[i].rectTransform.anchoredPosition.y < y - height)
                    {
                        y = labelList[i].rectTransform.anchoredPosition.y;
                    }
                    else
                    {
                        y -= height;
                        if (y < -labelDist) break;
                        float x = Mathf.Sqrt(labelDist * labelDist - y * y);
                        labelList[i].rectTransform.anchoredPosition = new Vector2(x, y);
                    }
                }

                //reverse right
                y = -99999.0f;
                label_right.Reverse();
                foreach (int i in label_right)
                {
                    if (labelList[i].rectTransform.anchoredPosition.y > y + height)
                    {
                        y = labelList[i].rectTransform.anchoredPosition.y;
                        labelList[i].rectTransform.anchoredPosition = new Vector2(labelList[i].rectTransform.anchoredPosition.x + chartOptions.label.offset * 0.5f, y);
                    }
                    else
                    {
                        y += height;
                        if (y > labelDist) break;
                        float x = Mathf.Sqrt(labelDist * labelDist - y * y);
                        labelList[i].rectTransform.anchoredPosition = new Vector2(x + chartOptions.label.offset * 0.5f, y);
                    }
                }

                //left
                y = 99999.0f;
                foreach (int i in label_left)
                {
                    if (labelList[i].rectTransform.anchoredPosition.y < y - height)
                    {
                        y = labelList[i].rectTransform.anchoredPosition.y;
                    }
                    else
                    {
                        y -= height;
                        if (y <= -labelDist) break;
                        float x = -Mathf.Sqrt(labelDist * labelDist - y * y);
                        labelList[i].rectTransform.anchoredPosition = new Vector2(x, y);
                    }
                }

                //reverse left
                y = -99999.0f;
                label_left.Reverse();
                foreach (int i in label_left)
                {
                    if (labelList[i].rectTransform.anchoredPosition.y > y + height)
                    {
                        y = labelList[i].rectTransform.anchoredPosition.y;
                        labelList[i].rectTransform.anchoredPosition = new Vector2(labelList[i].rectTransform.anchoredPosition.x - chartOptions.label.offset * 0.5f, y);
                    }
                    else
                    {
                        y += height;
                        if (y > labelDist) break;
                        float x = -Mathf.Sqrt(labelDist * labelDist - y * y);
                        labelList[i].rectTransform.anchoredPosition = new Vector2(x - chartOptions.label.offset * 0.5f, y);
                    }
                }

                //find max delta
                float delta_xMax = 0.0f, delta_yMax = 0.0f;
                for (int i = 0; i < labelList.Length; ++i)
                {
                    if (labelList[i] == null || !labelList[i].gameObject.activeSelf) continue;

                    float wLimit = chartOptions.label.bestFit ? chartSize.x * 0.3f : Mathf.Clamp(chartSize.x * 0.5f - Mathf.Abs(labelList[i].rectTransform.anchoredPosition.x), 0.0f, chartSize.x * 0.3f);
                    float width = labelList[i].preferredWidth;
                    if (width > wLimit) { width = wLimit; ChartHelper.TruncateText(labelList[i], wLimit); }
                    labelList[i].rectTransform.sizeDelta = new Vector2(width, height);

                    float delta_x = Mathf.Abs(labelList[i].rectTransform.anchoredPosition.x) + width - chartSize.x * 0.5f;
                    if (delta_x > delta_xMax) delta_xMax = delta_x;
                    float delta_y = Mathf.Abs(labelList[i].rectTransform.anchoredPosition.y) + height * 0.5f - chartSize.y * 0.5f + centerOffset.y;
                    if (delta_y > delta_yMax) delta_yMax = delta_y;
                }
                delta_xMax = Mathf.Clamp(delta_xMax, 0.0f, chartSize.x * 0.3f);
                delta_yMax = Mathf.Clamp(delta_yMax, 0.0f, chartSize.y * 0.3f);

                float delta = delta_xMax > delta_yMax ? delta_xMax : delta_yMax;
                float ratio = (circleSize.x * 0.5f - delta) / (circleSize.x * 0.5f);
                if (background != null) background.rectTransform.sizeDelta *= ratio;
                //highlight.rectTransform.sizeDelta *= ratio;
                for (int i = 0; i < chartData.series.Count; ++i)
                {
                    if (pieList[i] == null) continue;
                    pieList[i].rectTransform.sizeDelta *= ratio;
                    labelList[i].rectTransform.anchoredPosition *= ratio;
                }

                //update line
                for (int i = 0; i < lineList.Length; ++i)
                {
                    if (lineList[i] == null || !lineList[i].gameObject.activeSelf) continue;

                    Vector2 dir = ChartGraphic.RotateCW(Vector2.up, pieList[i].cosSin);
                    Vector2 p1 = dir * circleSize.x * 0.5f * ratio;
                    Vector2 p2 = labelList[i].rectTransform.anchoredPosition;
                    Vector2 dif = p2 - p1;

                    lineList[i].color = pieList[i].color;
                    lineList[i].rectTransform.anchoredPosition = p1;
                    lineList[i].rectTransform.sizeDelta = new Vector2(labelTemp.fontSize / 6.0f, dif.magnitude);
                    lineList[i].rectTransform.localRotation = Quaternion.FromToRotation(Vector2.up, dif);
                }
            }

            ChartHelper.Destroy(labelTemp.gameObject);
            ChartHelper.Destroy(lineTemp.gameObject);
        }
    }
}
