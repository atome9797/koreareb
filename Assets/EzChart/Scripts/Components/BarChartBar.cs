using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChartUtil
{
    public class BarChartBar : ChartGraphic
    {
        public Vector2[] data = null; //ratio, starting
        public float offset = 0.0f;
        public float width = 5.0f;
        public bool inverted = false;
        public bool reverse = false;
        public bool isBackground = false;
        public int seriesIndex = 0;
        public BarChart chart;

        protected override void Init()
        {
            if (data == null || data.Length == 0 || chart == null) return;

            Vector2 size = rectTransform.rect.size;
            Vector2 m_offset = -size * 0.5f;

            int index = 0;
            Vector2[] points = new Vector2[4];
            Color[] colors = chart.chartOptions.plotOptions.barChartOption.colorByCategories && !isBackground ? chart.chartOptions.plotOptions.dataColor : new Color[] { color };

            if (inverted)
            {
                float unit = size.y / data.Length;
                if (reverse)
                {
                    unit *= -1;
                    m_offset.y *= -1;
                }
                m_offset.y += offset + unit * 0.5f - width * 0.5f;

                for (int i = 0; i < data.Length; ++i)
                {
                    if (!chart.IsValid(seriesIndex, i)) continue;
                    float pos = m_offset.y + unit * i;
                    float h = size.x * data[i].x;
                    float hStart = m_offset.x + size.x * data[i].y;
                    int colorIndex = i % colors.Length;

                    points[0] = new Vector2(hStart, pos);
                    points[1] = new Vector2(hStart + h, pos);
                    points[2] = new Vector2(hStart + h, pos + width);
                    points[3] = new Vector2(hStart, pos + width);

                    UIVertex[] v = new UIVertex[4];
                    v[0].position = points[0];
                    v[1].position = points[1];
                    v[2].position = points[2];
                    v[3].position = points[3];
                    v[0].color = colors[0];
                    v[1].color = colors[1];
                    v[2].color = colors[1];
                    v[3].color = colors[0];

                    v[0].uv0 = Vector2.zero;
                    v[1].uv0 = Vector2.zero;
                    v[2].uv0 = Vector2.zero;
                    v[3].uv0 = Vector2.zero;
                    vertices.AddRange(v);

                    int[] tri = new int[] { index, index + 1, index + 2, index + 2, index + 3, index };
                    indices.AddRange(tri);
                    index += 4;
                }
            }
            else
            {
                float unit = size.x / data.Length;
                if (reverse)
                {
                    unit *= -1;
                    m_offset.x *= -1;
                }
                m_offset.x += offset + unit * 0.5f - width * 0.5f;

                for (int i = 0; i < data.Length; ++i)
                {
                    if (!chart.IsValid(seriesIndex, i)) continue;
                    float pos = m_offset.x + unit * i;
                    float h = size.y * data[i].x;
                    float hStart = m_offset.y + size.y * data[i].y;
                    int colorIndex = i % colors.Length;

                    points[0] = new Vector2(pos, hStart);
                    points[1] = new Vector2(pos, hStart + h);
                    points[2] = new Vector2(pos + width, hStart + h);
                    points[3] = new Vector2(pos + width, hStart);

                    UIVertex[] v = new UIVertex[4];
                    v[0].position = points[0];
                    v[1].position = points[1];
                    v[2].position = points[2];
                    v[3].position = points[3];
                    //v[0].color = colors[colorIndex];
                    //v[1].color = colors[colorIndex];
                    //v[2].color = colors[colorIndex];
                    //v[3].color = colors[colorIndex];
                    v[0].color = colors[0];
                    v[1].color = colors[1];
                    v[2].color = colors[1];
                    v[3].color = colors[0];

                    v[0].uv0 = Vector2.zero;
                    v[1].uv0 = Vector2.zero;
                    v[2].uv0 = Vector2.zero;
                    v[3].uv0 = Vector2.zero;
                    vertices.AddRange(v);

                    int[] tri = new int[] { index, index + 1, index + 2, index + 2, index + 3, index };
                    indices.AddRange(tri);
                    index += 4;
                }
            }
        }
    }
}