using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChartUtil
{
    public class LineChartShade : ChartGraphic
    {
        public Vector2[] data = null; //ratio, starting
        public bool inverted = false;
        public bool reverse = false;
        public int seriesIndex = 0;
        public LineChart chart;

        protected override void Init()
        {
            if (data == null || data.Length == 0 || chart == null) return;

            Vector2 size = rectTransform.rect.size;
            Vector2 m_offset = -size * 0.5f;

            int index = 0;
            Vector2[] points = new Vector2[4];

            if (inverted)
            {
                float unit = size.y / data.Length;
                if (reverse)
                {
                    unit *= -1;
                    m_offset.y *= -1;
                }
                m_offset.y += unit * 0.5f;

                Vector2 pStart = new Vector2(m_offset.x + size.x * data[0].y, m_offset.y);
                Vector2 p = pStart + new Vector2(size.x * data[0].x, 0.0f);
                for (int i = 1; i < data.Length; ++i)
                {
                    if (!chart.IsValid(seriesIndex, i)) continue;
                    Vector2 pStartLast = pStart;
                    Vector2 pLast = p;
                    pStart = new Vector2(m_offset.x + size.x * data[i].y, m_offset.y + unit * i);
                    p = pStart + new Vector2(size.x * data[i].x, 0.0f);
                    if (!chart.IsValid(seriesIndex, i - 1)) continue;

                    points[0] = pStartLast;
                    points[1] = pLast;
                    points[2] = p;
                    points[3] = pStart;

                    if (Vector2.Dot(points[1] - points[0], points[2] - points[3]) >= 0.0f)
                    {
                        UIVertex[] v = new UIVertex[4];
                        v[0].position = points[0];
                        v[1].position = points[1];
                        v[2].position = points[2];
                        v[3].position = points[3];
                        v[0].color = color;
                        v[1].color = color;
                        v[2].color = color;
                        v[3].color = color;
                        v[0].uv0 = Vector2.zero;
                        v[1].uv0 = Vector2.zero;
                        v[2].uv0 = Vector2.zero;
                        v[3].uv0 = Vector2.zero;
                        vertices.AddRange(v);

                        int[] tri = new int[] { index, index + 1, index + 2, index + 2, index + 3, index };
                        indices.AddRange(tri);
                        index += 4;
                    }
                    else
                    {
                        float t = (points[1].x - points[0].x) * (points[0].y - points[3].y) - (points[0].x - points[3].x) * (points[1].y - points[0].y);
                        t /= (points[1].x - points[2].x) * (points[0].y - points[3].y) - (points[0].x - points[3].x) * (points[1].y - points[2].y);
                        Vector2 pZero = points[1] + (points[2] - points[1]) * t;

                        UIVertex[] v = new UIVertex[5];
                        v[0].position = points[0];
                        v[1].position = points[1];
                        v[2].position = points[2];
                        v[3].position = points[3];
                        v[4].position = pZero;
                        v[0].color = color;
                        v[1].color = color;
                        v[2].color = color;
                        v[3].color = color;
                        v[4].color = color;
                        v[0].uv0 = Vector2.zero;
                        v[1].uv0 = Vector2.zero;
                        v[2].uv0 = Vector2.zero;
                        v[3].uv0 = Vector2.zero;
                        v[4].uv0 = Vector2.zero;
                        vertices.AddRange(v);

                        int[] tri = new int[] { index, index + 1, index + 4, index + 4, index + 3, index + 2 };
                        indices.AddRange(tri);
                        index += 5;
                    }
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

                Vector2 pStart = new Vector2(m_offset.x, m_offset.y + size.y * data[0].y);
                Vector2 p = pStart + new Vector2(0.0f, size.y * data[0].x);
                for (int i = 1; i < data.Length; ++i)
                {
                    if (!chart.IsValid(seriesIndex, i)) continue;
                    Vector2 pStartLast = pStart;
                    Vector2 pLast = p;
                    pStart = new Vector2(m_offset.x + unit * i, m_offset.y + size.y * data[i].y);
                    p = pStart + new Vector2(0.0f, size.y * data[i].x);
                    if (!chart.IsValid(seriesIndex, i - 1)) continue;

                    points[0] = pStartLast;
                    points[1] = pLast;
                    points[2] = p;
                    points[3] = pStart;

                    if (Vector2.Dot(points[1] - points[0], points[2] - points[3]) >= 0.0f)
                    {
                        UIVertex[] v = new UIVertex[4];
                        v[0].position = points[0];
                        v[1].position = points[1];
                        v[2].position = points[2];
                        v[3].position = points[3];
                        v[0].color = color;
                        v[1].color = color;
                        v[2].color = color;
                        v[3].color = color;
                        v[0].uv0 = Vector2.zero;
                        v[1].uv0 = Vector2.zero;
                        v[2].uv0 = Vector2.zero;
                        v[3].uv0 = Vector2.zero;
                        vertices.AddRange(v);

                        int[] tri = new int[] { index, index + 1, index + 2, index + 2, index + 3, index };
                        indices.AddRange(tri);
                        index += 4;
                    }
                    else
                    {
                        float t = (points[1].x - points[0].x) * (points[0].y - points[3].y) - (points[0].x - points[3].x) * (points[1].y - points[0].y);
                        t /= (points[1].x - points[2].x) * (points[0].y - points[3].y) - (points[0].x - points[3].x) * (points[1].y - points[2].y);
                        Vector2 pZero = points[1] + (points[2] - points[1]) * t;

                        UIVertex[] v = new UIVertex[5];
                        v[0].position = points[0];
                        v[1].position = points[1];
                        v[2].position = points[2];
                        v[3].position = points[3];
                        v[4].position = pZero;
                        v[0].color = color;
                        v[1].color = color;
                        v[2].color = color;
                        v[3].color = color;
                        v[4].color = color;
                        v[0].uv0 = Vector2.zero;
                        v[1].uv0 = Vector2.zero;
                        v[2].uv0 = Vector2.zero;
                        v[3].uv0 = Vector2.zero;
                        v[4].uv0 = Vector2.zero;
                        vertices.AddRange(v);

                        int[] tri = new int[] { index, index + 1, index + 4, index + 4, index + 3, index + 2 };
                        indices.AddRange(tri);
                        index += 5;
                    }
                }
            }
        }
    }
}