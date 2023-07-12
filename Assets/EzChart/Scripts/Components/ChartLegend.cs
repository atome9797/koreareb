using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if CHART_TMPRO
using TMPro;
#endif

namespace ChartUtil
{
    [ExecuteInEditMode]
    public class ChartLegend : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
#if CHART_TMPRO
        public TextMeshProUGUI text;
#else
        public Text text;
#endif
        public Image icon;
        public Image background;
        int index;
        Chart chart;
        Color textColor;

        public void Init(int i, Chart c)
        {
            index = i;
            chart = c;
            textColor = text.color;
            if (icon != null)
                icon.sprite = chart.chartOptions.legend.iconImage == null || chart.chartOptions.legend.iconImage.Length == 0 ?
                    null : chart.chartOptions.legend.iconImage[index % chart.chartOptions.legend.iconImage.Length];
        }

        public void SetStatus(bool isOn)
        {
            if (isOn)
            {
                if (icon != null) icon.color = chart.chartOptions.plotOptions.dataColor[index % chart.chartOptions.plotOptions.dataColor.Length];
                text.color = textColor;
            }
            else
            {
                if (icon != null) icon.color = chart.chartOptions.legend.dimmedColor;
                text.color = chart.chartOptions.legend.dimmedColor;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            chart.ToggleSeries(index);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            background.color = chart.chartOptions.legend.highlightColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            background.color = chart.chartOptions.legend.backgroundColor;
        }
    }
}