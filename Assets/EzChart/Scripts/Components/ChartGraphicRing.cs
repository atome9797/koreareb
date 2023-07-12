﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChartUtil
{
    public class ChartGraphicRing : ChartGraphic
    {
        public float startAngle = 0.0f;
        public float endAngle = 360.0f;
        public int side = 8;
        public float width = 1.0f;
        public bool isCircular = true;
        public bool mid = false;

        protected override void Init()
        {
            if (side < 3) side = 3;
            float radius = rectTransform.rect.size.x < rectTransform.rect.size.y ? rectTransform.rect.size.x * 0.5f : rectTransform.rect.size.y * 0.5f;
            float angleOffset = -startAngle;
            float total = Mathf.Repeat(endAngle - startAngle, 360.0001f);
            if (mid) angleOffset -= total / side * 0.5f;
            int sideCircular = Mathf.RoundToInt(total / 360.0f * CosSin.Length);

            int index = 0;
            Vector2[] points = new Vector2[4];
            Vector2[] cossin = isCircular ? (total < 359.9f ? GetCosSin(sideCircular, angleOffset + 90.0f, -total) : CosSin) : GetCosSin(side, angleOffset + 90.0f, -total);
            Vector2[] uvs = new Vector2[2];
            uvs[0] = new Vector2(0.0f, 0.0f);
            uvs[1] = new Vector2(0.0f, 1.0f);

            float r = radius + width * 0.5f;
            for (int j = 0; j < cossin.Length - 1; ++j)
            {
                points[0] = cossin[j] * (r - width);
                points[1] = cossin[j] * (r);
                points[2] = cossin[j + 1] * (r);
                points[3] = cossin[j + 1] * (r - width);

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