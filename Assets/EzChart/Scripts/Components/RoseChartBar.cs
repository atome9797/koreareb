using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChartUtil
{
    public class RoseChartBar : ChartGraphic
    {
        public Vector2[] data = null; //ratio, starting
        public float offset = 0.0f;
        public float width = 5.0f;
        public float innerSize = 0.0f;
        public float innerExtend = 0.0f;    //fill gap
        public bool reverse = false;
        public int seriesIndex = 0;
        public RoseChart chart;

        protected override void Init()
        {
            if (data == null || data.Length == 0 || chart == null) return;

            innerSize = Mathf.Clamp01(innerSize);
            float radius = rectTransform.rect.size.x < rectTransform.rect.size.y ? rectTransform.rect.size.x * 0.5f : rectTransform.rect.size.y * 0.5f;
            float radiusInner = radius * innerSize;
            float range = radius - radiusInner;

            int barSide = Mathf.RoundToInt(width / 360.0f * CosSin.Length);
            Vector2[] cossinBar = GetCosSin(barSide, 90.0f - width * 0.5f, width, true);

            float direction = reverse ? -360.0f : 360.0f;
            float angleOffset = direction / data.Length * 0.5f + offset;
            int index = 0;
            Vector2[] cossin = GetCosSin(data.Length, angleOffset, direction, false);
            Vector2[] points = new Vector2[4];
            Vector2[] uvs = new Vector2[2];
            uvs[0] = new Vector2(0.0f, 0.0f);
            uvs[1] = new Vector2(0.0f, 1.0f);
            Color[] colors = chart.chartOptions.plotOptions.roseChartOption.colorByCategories ? chart.chartOptions.plotOptions.dataColor : new Color[] { color };

            for (int i = 0; i < data.Length; ++i)
            {
                if (!chart.IsValid(seriesIndex, i)) continue;
                float rStart = radiusInner + range * data[i].y;
                float r = rStart + range * data[i].x;
                rStart -= innerExtend;
                if (rStart < 0.0f) rStart = 0.0f;
                int colorIndex = i % colors.Length;

                uvs[0].y = 1.0f - (data[i].y + data[i].x);
                if (uvs[0].y < 0.5f) uvs[0].y = 0.5f;

                for (int j = 0; j < cossinBar.Length - 1; ++j)
                {
                    points[0] = cossinBar[j] * rStart;
                    points[1] = cossinBar[j] * r;
                    points[2] = cossinBar[j + 1] * r;
                    points[3] = cossinBar[j + 1] * rStart;

                    points[0] = RotateCW(points[0], cossin[i]);
                    points[1] = RotateCW(points[1], cossin[i]);
                    points[2] = RotateCW(points[2], cossin[i]);
                    points[3] = RotateCW(points[3], cossin[i]);

                    UIVertex[] v = new UIVertex[4];
                    v[0].position = points[0];
                    v[1].position = points[1];
                    v[2].position = points[2];
                    v[3].position = points[3];
                    v[0].color = colors[colorIndex];
                    v[1].color = colors[colorIndex];
                    v[2].color = colors[colorIndex];
                    v[3].color = colors[colorIndex];
                    v[0].uv0 = uvs[0];
                    v[1].uv0 = uvs[1];
                    v[2].uv0 = uvs[1];
                    v[3].uv0 = uvs[0];
                    vertices.AddRange(v);

                    int[] tri = new int[] { index, index + 1, index + 2, index + 2, index + 3, index };
                    indices.AddRange(tri);
                    index += 4;
                }
            }
        }
    }
}