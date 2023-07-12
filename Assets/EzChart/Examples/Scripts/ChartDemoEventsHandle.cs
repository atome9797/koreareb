using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChartUtil.Demo
{
    public class ChartDemoEventsHandle : MonoBehaviour
    {
        [SerializeField] ChartData chartData;
        [SerializeField] Text chartEventText;

        public void OnItemClick(int cateIndex, int seriesIndex)
        {
            string cateStr = cateIndex >= 0 ?
                "Category \"" + chartData.categories[cateIndex] + "\" has been clicked" :
                "No category has been  clicked";
            string seriesStr = seriesIndex >= 0 ?
                "Series \"" + chartData.series[seriesIndex].name + "\" has been  clicked" :
                "No series has been  clicked";
            chartEventText.text = "Item Click Event: " + cateStr + ", " + seriesStr;
        }

        public void OnSeriesToggle(int seriesIndex, bool isOn)
        {
            string toggleStr = isOn ? "On" : "Off";
            chartEventText.text = "Series Toggle Event: Series \"" + chartData.series[seriesIndex].name + "\" has been turned " + toggleStr;
        }
    }
}