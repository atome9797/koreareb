﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChartUtil
{
    public class ChartGraphicRadialLine : ChartGraphic
    {
        public float startAngle = 0.0f;
        public float endAngle = 360.0f;
        public int side = 8;
        public float width = 1.0f;
        public float innerSize = 0.0f;
        public bool mid = false;
        public int interval = 1;

        protected override void Init()
        {
            if (side < 1) side = 1;
            innerSize = Mathf.Clamp01(innerSize);
            float radius = rectTransform.rect.size.x < rectTransform.rect.size.y ? rectTransform.rect.size.x * 0.5f : rectTransform.rect.size.y * 0.5f;
            float offset = radius * innerSize;
            float angleOffset = startAngle;
            float total = Mathf.Repeat(endAngle - startAngle, 360.0001f);
            if (mid) angleOffset += total / side * 0.5f;

            int index = 0;
            Vector2[] rect = new Vector2[4];
            rect[0] = new Vector2(-width * 0.5f, offset);
            rect[1] = new Vector2(-width * 0.5f, radius);
            rect[2] = new Vector2(width * 0.5f, radius);
            rect[3] = new Vector2(width * 0.5f, offset);
            Vector2[] points = new Vector2[4];
            Vector2[] cossin = GetCosSin(side, angleOffset, total, total < 359.9f);
            Vector2[] uvs = new Vector2[2];
            uvs[0] = new Vector2(0.0f, 0.0f);
            uvs[1] = new Vector2(1.0f, 0.0f);

            for (int i = 0; i < cossin.Length; i += interval)
            {
                points[0] = RotateCW(rect[0], cossin[i]);
                points[1] = RotateCW(rect[1], cossin[i]);
                points[2] = RotateCW(rect[2], cossin[i]);
                points[3] = RotateCW(rect[3], cossin[i]);

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
                v[1].uv0 = uvs[0];
                v[2].uv0 = uvs[1];
                v[3].uv0 = uvs[1];
                vertices.AddRange(v);

                int[] tri = new int[] { index, index + 1, index + 2, index + 2, index + 3, index };
                indices.AddRange(tri);
                index += 4;
            }
        }
    }
}