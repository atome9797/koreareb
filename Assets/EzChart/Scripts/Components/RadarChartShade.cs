using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChartUtil
{
    public class RadarChartShade : ChartGraphic
    {
        public Vector2[] data = null; //ratio, starting
        public bool reverse = false;
        public int seriesIndex = 0;
        public RadarChart chart;
        public RadarChartPoint point;

        Vector2[] pBuffer = null;

        protected override void Init()
        {
            if (data == null || data.Length == 0 || chart == null) return;

            float radius = rectTransform.rect.size.x < rectTransform.rect.size.y ? rectTransform.rect.size.x * 0.5f : rectTransform.rect.size.y * 0.5f;

            int index = 0;
            //float angleOffset = 360.0f / data.Length * 0.5f;
            //Vector2[] cossin = GetCosSin(data.Length, angleOffset, 360.0f, false);
            pBuffer = point.PointsBuffer;
            Vector2[] points = new Vector2[4];
            Vector2[] uvs = new Vector2[2];
            uvs[0] = new Vector2(0.0f, 0.0f);
            uvs[1] = new Vector2(1.0f, 0.0f);

            //Vector2 p = (reverse ? RotateCCW(Vector2.up, cossin[0]) : RotateCW(Vector2.up, cossin[0])) * radius * data[0];
            Vector2 pStart = pBuffer[0] * radius * data[0].y;
            Vector2 p = pBuffer[0] * radius * (data[0].y + data[0].x);
            for (int i = 1; i < data.Length; ++i)
            {
                if (!chart.IsValid(seriesIndex, i)) continue;
                Vector2 lastStart = pStart;
                Vector2 lastPoint = p;
                //p = reverse ? RotateCCW(Vector2.up, cossin[i]) : RotateCW(Vector2.up, cossin[i]) * radius * data[i];
                pStart = pBuffer[i] * radius * data[i].y;
                p = pBuffer[i] * radius * (data[i].y + data[i].x);
                if (!chart.IsValid(seriesIndex, i - 1)) continue;

                points[0] = lastStart;
                points[1] = lastPoint;
                points[2] = p;
                points[3] = pStart;

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

            {
                if (!chart.IsValid(seriesIndex, 0)) return;
                Vector2 lastStart = pStart;
                Vector2 lastPoint = p;
                //p = (reverse ? RotateCCW(Vector2.up, cossin[0]) : RotateCW(Vector2.up, cossin[0])) * radius * data[0];
                pStart = pBuffer[0] * radius * data[0].y;
                p = pBuffer[0] * radius * (data[0].y + data[0].x);
                if (!chart.IsValid(seriesIndex, data.Length - 1)) return;

                points[0] = lastStart;
                points[1] = lastPoint;
                points[2] = p;
                points[3] = pStart;

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

            pBuffer = null;
        }
    }
}