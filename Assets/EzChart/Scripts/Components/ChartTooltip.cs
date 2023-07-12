using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if CHART_TMPRO
using TMPro;
#endif

namespace ChartUtil
{
    public class ChartTooltip : MonoBehaviour
    {
#if CHART_TMPRO
        public TextMeshProUGUI tooltipText;
#else
        public Text tooltipText;
#endif
        public Image background;
        [SerializeField] public Vector2 chartSize;
        [SerializeField] public Vector2 parentPivot;
        public bool isFading { get { return fadingTimer > 0.0f; } }

        float fadingTimer;
        float backgroundAlpha;
        float textAlpha;

        private void Update()
        {
            if (fadingTimer > 0.0f)
            {
                fadingTimer -= Time.deltaTime;
                if (fadingTimer <= 0.2f)
                {
                    SetAlpha(fadingTimer / 0.2f);
                }
                if (fadingTimer <= 0.0f)
                {
                    gameObject.SetActive(false);
                }
            }
        }

        public void Init()
        {
            backgroundAlpha = background.color.a;
            textAlpha = tooltipText.color.a;
        }

        public void ValidatePosition()
        {
            Vector3 pos = transform.localPosition;
            pos.x = Mathf.Clamp(pos.x, -chartSize.x * parentPivot.x + background.rectTransform.sizeDelta.x * 0.5f, 
                chartSize.x * (1.0f - parentPivot.x) - background.rectTransform.sizeDelta.x * 0.5f);
            pos.y = Mathf.Clamp(pos.y, -chartSize.y * parentPivot.y, 
                chartSize.y * (1.0f - parentPivot.y) - background.rectTransform.sizeDelta.y);
            transform.localPosition = pos;
        }

        public void FadeOut()
        {
            fadingTimer = 0.6f;
        }

        public void ResetFade()
        {
            fadingTimer = 0.0f;
            SetAlpha(1.0f);
        }

        void SetAlpha(float a)
        {
            Color c = background.color;
            c.a = backgroundAlpha * a;
            background.color = c;
            c = tooltipText.color;
            c.a = textAlpha;
            tooltipText.color = c;
        }
    }
}
