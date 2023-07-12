using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChartUtil
{
    public class LineChartLineCurve : LineChartLine
    {
        protected override void Init()
        {
            if (data == null || data.Length == 0 || chart == null) return;

            float halfWidth = width * 0.5f;
            Vector2 size = rectTransform.rect.size;
            Vector2 m_offset = -size * 0.5f;

            int index = 0;
            Vector2[] points = new Vector2[4];
            Vector2[] uvs = new Vector2[2];
            uvs[0] = new Vector2(0.0f, 0.0f);
            uvs[1] = new Vector2(1.0f, 0.0f);

            if (inverted)
            {
                float unit = size.y / data.Length;
                if (reverse)
                {
                    unit *= -1;
                    m_offset.y *= -1;
                }
                m_offset.y += unit * 0.5f;

                Vector2 p1 = new Vector2(m_offset.x + size.x * (data[0].y + data[0].x), m_offset.y);
                Vector2 t1 = new Vector2();
                for (int i = 1; i < data.Length; ++i)
                {
                    if (!chart.IsValid(seriesIndex, i)) continue;
                    Vector2 p0 = p1;
                    Vector2 t0 = -t1;
                    p1 = new Vector2(m_offset.x + size.x * (data[i].y + data[i].x), m_offset.y + unit * i);
                    if (chart.IsValid(seriesIndex, i + 1))
                    {
                        Vector2 p2 = new Vector2(m_offset.x + size.x * (data[i + 1].y + data[i + 1].x), m_offset.y + unit * (i + 1));
                        t1 = p0 - p2;
                        t1 *= CURVATURE;
                    }
                    else
                    {
                        t1 = new Vector2();
                    }
                    if (!chart.IsValid(seriesIndex, i - 1)) continue;

                    int stepCount = Mathf.CeilToInt((p1 - p0).magnitude / STEP_SIZE);
                    float stepSize = 1.0f / stepCount;
                    Vector2 p = p0;
                    for (int j = 1; j <= stepCount; ++j)
                    {
                        float t = stepSize * j;
                        Vector2 pLast = p;
                        p = BerzierCurve(t, p, p0 + t0, p1 + t1, p1);

                        Vector2 dir = p - pLast;
                        points = CreateRect(dir, halfWidth);

                        UIVertex[] v = new UIVertex[4];
                        v[0].position = points[0] + pLast;
                        v[1].position = points[1] + pLast;
                        v[2].position = points[2] + pLast;
                        v[3].position = points[3] + pLast;
                        v[0].color = color;
                        v[1].color = color;
                        v[2].color = color;
                        v[3].color = color;
                        v[0].uv0 = uvs[0];
                        v[1].uv0 = uvs[0];
                        v[2].uv0 = uvs[1];
                        v[3].uv0 = uvs[1];
                        vertices.AddRange(v);

                        int[] tri = new int[] { index, index + 1, index + 2, index + 2, index + 3, index };
                        indices.AddRange(tri);
                        if (j < stepCount)
                        {
                            tri = new int[] { index + 1, index + 4, index + 7, index + 7, index + 2, index + 1 };
                            indices.AddRange(tri);
                        }
                        index += 4;
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

                Vector2 p1 = new Vector2(m_offset.x, m_offset.y + size.y * (data[0].y + data[0].x));
                Vector2 t1 = new Vector2();
                for (int i = 1; i < data.Length; ++i)
                {
                    if (!chart.IsValid(seriesIndex, i)) continue;
                    Vector2 p0 = p1;
                    Vector2 t0 = -t1;
                    p1 = new Vector2(m_offset.x + unit * i, m_offset.y + size.y * (data[i].y + data[i].x));
                    if (chart.IsValid(seriesIndex, i + 1))
                    {
                        Vector2 p2 = new Vector2(m_offset.x + unit * (i + 1), m_offset.y + size.y * (data[i + 1].y + data[i + 1].x));
                        t1 = p0 - p2;
                        t1 *= CURVATURE;
                    }
                    else
                    {
                        t1 = new Vector2();
                    }
                    if (!chart.IsValid(seriesIndex, i - 1)) continue;

                    int stepCount = Mathf.CeilToInt((p1 - p0).magnitude / STEP_SIZE);
                    float stepSize = 1.0f / stepCount;
                    Vector2 p = p0;
                    for (int j = 1; j <= stepCount; ++j)
                    {
                        float t = stepSize * j;
                        Vector2 pLast = p;
                        p = BerzierCurve(t, p, p0 + t0, p1 + t1, p1);

                        Vector2 dir = p - pLast;
                        points = CreateRect(dir, halfWidth);

                        UIVertex[] v = new UIVertex[4];
                        v[0].position = points[0] + pLast;
                        v[1].position = points[1] + pLast;
                        v[2].position = points[2] + pLast;
                        v[3].position = points[3] + pLast;
                        v[0].color = color;
                        v[1].color = color;
                        v[2].color = color;
                        v[3].color = color;
                        v[0].uv0 = uvs[0];
                        v[1].uv0 = uvs[0];
                        v[2].uv0 = uvs[1];
                        v[3].uv0 = uvs[1];
                        vertices.AddRange(v);

                        int[] tri = new int[] { index, index + 1, index + 2, index + 2, index + 3, index };
                        indices.AddRange(tri);
                        if (j < stepCount)
                        {
                            tri = new int[] { index + 1, index + 4, index + 7, index + 7, index + 2, index + 1 };
                            indices.AddRange(tri);
                        }
                        index += 4;
                    }
                }
            }
        }

        Vector2 BerzierCurve(float t, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return Mathf.Pow(1 - t, 3) * p0 + 3 * Mathf.Pow(1 - t, 2) * t * p1 + 3 * (1 - t) * t * t * p2 + Mathf.Pow(t, 3) * p3;
        }
    }
}