using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChartUtil
{
    public class LineChartPoint : ChartGraphic
    {
        public Vector2[] data = null; //ratio, starting
        public float diameter = 2.0f;
        public bool inverted = false;
        public bool reverse = false;
        public int seriesIndex = 0;
        public LineChart chart;

        protected override void Init()
        {
            if (data == null || data.Length == 0 || chart == null) return;

            Vector2 size = rectTransform.rect.size;
            Vector2 m_offset = -size * 0.5f;
            float radius = diameter * 0.5f;

            int index = 0;
            Vector2[] points = new Vector2[4];
            Vector2[] uvs = new Vector2[4];
            uvs[0] = new Vector2(0.0f, 0.0f);
            uvs[1] = new Vector2(0.0f, 1.0f);
            uvs[2] = new Vector2(1.0f, 1.0f);
            uvs[3] = new Vector2(1.0f, 0.0f);

            if (inverted)
            {
                float unit = size.y / data.Length;
                if (reverse)
                {
                    unit *= -1;
                    m_offset.y *= -1;
                }
                m_offset.y += unit * 0.5f;

                for (int i = 0; i < data.Length; ++i)
                {
                    if (!chart.IsValid(seriesIndex, i)) continue;
                    float y = m_offset.y + unit * i;
                    float x = m_offset.x + size.x * (data[i].y + data[i].x);

                    points[0] = new Vector2(x - radius, y - radius);
                    points[1] = new Vector2(x - radius, y + radius);
                    points[2] = new Vector2(x + radius, y + radius);
                    points[3] = new Vector2(x + radius, y - radius);

                    UIVertex[] v = new UIVertex[4];
                    v[0].position = points[0];
                    v[1].position = points[1];
                    v[2].position = points[2];
                    v[3].position = points[3];
                    v[0].color = color;
                    v[1].color = color;
                    v[2].color = color;
                    v[3].color = color;
                    v[0].uv0 = uvs[0];
                    v[1].uv0 = uvs[1];
                    v[2].uv0 = uvs[2];
                    v[3].uv0 = uvs[3];
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
                m_offset.x += unit * 0.5f;

                for (int i = 0; i < data.Length; ++i)
                {
                    if (!chart.IsValid(seriesIndex, i)) continue;
                    float x = m_offset.x + unit * i;
                    float y = m_offset.y + size.y * (data[i].y + data[i].x);

                    points[0] = new Vector2(x - radius, y - radius);
                    points[1] = new Vector2(x - radius, y + radius);
                    points[2] = new Vector2(x + radius, y + radius);
                    points[3] = new Vector2(x + radius, y - radius);

                    UIVertex[] v = new UIVertex[4];
                    v[0].position = points[0];
                    v[1].position = points[1];
                    v[2].position = points[2];
                    v[3].position = points[3];
                    v[0].color = color;
                    v[1].color = color;
                    v[2].color = color;
                    v[3].color = color;
                    v[0].uv0 = uvs[0];
                    v[1].uv0 = uvs[1];
                    v[2].uv0 = uvs[2];
                    v[3].uv0 = uvs[3];
                    vertices.AddRange(v);

                    int[] tri = new int[] { index, index + 1, index + 2, index + 2, index + 3, index };
                    indices.AddRange(tri);
                    index += 4;
                }
            }
        }
    }
}