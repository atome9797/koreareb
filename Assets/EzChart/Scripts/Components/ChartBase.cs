using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ChartUtil
{
    public class ChartBase : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        protected const float fadeValue = 0.4f;

        [SerializeField] public Vector2 chartSize;
        [HideInInspector] public ChartOptions chartOptions;
        [HideInInspector] public ChartData chartData;
        [HideInInspector] public ChartDataInfo chartDataInfo;
        [HideInInspector] public ChartEvents chartEvents;
        [HideInInspector] public ChartTooltip tooltip;
        [HideInInspector] public List<int> skipSeries = new List<int>();

        protected int currCate = -1;
        protected int currSeries = -1;
        protected Vector2 localMousePosition;
        protected RectTransform dataRect;
        protected RectTransform labelRect;

        bool mouseOver = false;
        Camera eventCam;
        RectTransform rectTransform;
        public RectTransform m;

        public static string GetValueString(float value, string format)
        {
            if (format == "") return ChartHelper.FloatToString(value);
            else return value.ToString(format);
        }

        public static string GetPercentageString(float value, string format)
        {
            if (format == "") return value.ToString("f0") + "%";
            else return value.ToString(format) + "%";
        }

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        public virtual void UpdateChart() { }

        protected virtual int FindCategory() { return -1; }

        protected virtual int FindSeries() { return -1; }

        protected virtual void HighlightCurrentCategory() { }

        protected virtual void UnhighlightCurrentCategory() { }

        protected virtual void HighlightCurrentSeries() { }

        protected virtual void UnhighlightCurrentSeries() { }

        protected virtual void UpdateTooltip() { }

        void Update()
        {
            if (mouseOver) DoMouseTracking();
            if (tooltip != null && tooltip.gameObject.activeSelf && !tooltip.isFading)
            {
                tooltip.transform.localPosition = localMousePosition + rectTransform.anchoredPosition;
                tooltip.ValidatePosition();
            }
        }

        void ShowTooltip()
        {
            if (tooltip == null) return;
            tooltip.gameObject.SetActive(true);
            tooltip.ResetFade();
            UpdateTooltip();
        }

        void HideTooltip()
        {
            if (tooltip == null) return;
            tooltip.FadeOut();
        }

        void DoMouseTracking()
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, Input.mousePosition, eventCam, out localMousePosition)) return;

            if (chartOptions.tooltip.share)
            {
                int cate = FindCategory();
                if (cate != currCate)
                {
                    if (currCate >= 0) UnhighlightCurrentCategory();
                    currCate = cate;
                    if (currCate >= 0)
                    {
                        HighlightCurrentCategory();
                        ShowTooltip();
                    }
                    else
                    {
                        HideTooltip();
                    }
                }
            }
            else
            {
                int cate = FindCategory();
                bool cateChanged = false;
                if (cate != currCate)
                {
                    cateChanged = true;
                    if (currCate >= 0) UnhighlightCurrentCategory();
                    currCate = cate;
                    if (currCate >= 0) HighlightCurrentCategory();
                }
                if (currCate >= 0)
                {
                    int seri = FindSeries();
                    if (seri != currSeries)
                    {
                        if (currSeries >= 0) UnhighlightCurrentSeries();
                        currSeries = seri;
                        if (currSeries >= 0)
                        {
                            HighlightCurrentSeries();
                            ShowTooltip();
                        }
                        else
                        {
                            HideTooltip();
                        }
                    }
                    else if(currSeries >= 0 && cateChanged)
                    {
                        UpdateTooltip();
                    }
                }
                else if (currSeries >= 0)
                {
                    UnhighlightCurrentSeries();
                    currSeries = -1;
                    HideTooltip();
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!chartOptions.plotOptions.mouseTracking) return;
            mouseOver = true;
            eventCam = eventData.enterEventCamera;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!chartOptions.plotOptions.mouseTracking) return;
            mouseOver = false;
            if (currCate >= 0)
            {
                UnhighlightCurrentCategory();
                currCate = -1;
            }
            if (currSeries >= 0)
            {
                UnhighlightCurrentSeries();
                currSeries = -1;
            }
            HideTooltip();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (chartEvents == null) return;
            currCate = FindCategory();
            currSeries = FindSeries();
            chartEvents.itemClickEvent.Invoke(currCate, currSeries);
        }
    }
}