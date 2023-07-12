using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI.ProceduralImage;
using TMPro;
using UnityEngine.UI;

public class MapEvent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public ViewDashBoard viewDashBoard;

    private Image proceduralImage;
    private Outline outline;
    public Color originalColor;
    public Color originalTextColor;

    public Color disableColor_d;
    public Color disableColor_w;

    private RectTransform rectTransform;
    private float originalY;
    private float targetY;
    private bool isMoving;

    public int PosId;

    public GameObject labelMapObject;
    // 라벨 말풍선
    public ProceduralImage labelMapImage;

    private TMP_Text labelMapPoint;
    private TMP_Text labelMapName;

    private void Awake()
    {
        PosId = transform.GetSiblingIndex();


        proceduralImage = GetComponent<Image>();
        outline = GetComponent<Outline>();
        //originalColor = proceduralImage.color;
        rectTransform = GetComponent<RectTransform>();
        originalY = rectTransform.anchoredPosition.y;
        targetY = originalY;
        isMoving = false;

        labelMapImage = labelMapObject.GetComponent<ProceduralImage>();
        labelMapPoint = labelMapObject.transform.GetChild(1).GetComponent<TMP_Text>();
        labelMapName = labelMapObject.transform.GetChild(0).GetComponent<TMP_Text>();
        //ColorUtility.TryParseHtmlString("#00cb94", out onColor);
        //ColorUtility.TryParseHtmlString("#166c8c", out targetTextColor);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetY = originalY + 4f;
        isMoving = true;

        labelMapImage.enabled = true;

        outline.enabled = true;
        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, targetY);

        transform.SetAsLastSibling();

        // 테마가 다크 일떄
        if (viewDashBoard.mainManager.thememode == MainManager.Thememode.dark)
        {
            proceduralImage.color = viewDashBoard.colorMapHover_d;

            outline.effectColor = viewDashBoard.colorOutline_d;

            labelMapImage.color = viewDashBoard.colorBubbleSelect_d;

            labelMapPoint.color = viewDashBoard.colorTextHover_d;
            labelMapName.color = viewDashBoard.colorTextHover_d;
        }
        else
        {

            proceduralImage.color = viewDashBoard.colorMapHover_w;

            outline.effectColor = viewDashBoard.colorOutline_w;

            labelMapImage.color = viewDashBoard.colorBubbleSelect_w;

            labelMapPoint.color = viewDashBoard.colorTextHover_w;
            labelMapName.color = viewDashBoard.colorTextHover_w;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetY = originalY;
        isMoving = true;

        labelMapImage.enabled = false;

        outline.enabled = false;
        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, targetY);

        transform.SetSiblingIndex(PosId);

        // 테마가 다크 일떄
        if (viewDashBoard.mainManager.thememode == MainManager.Thememode.dark)
        {
            proceduralImage.color = originalColor;

            outline.effectColor = viewDashBoard.colorOutline_d;

            labelMapPoint.color = originalTextColor;
            labelMapName.color = originalTextColor;
        }
        else
        {

            proceduralImage.color = originalColor;

            outline.effectColor = viewDashBoard.colorOutline_w;

            labelMapPoint.color = originalTextColor;
            labelMapName.color = originalTextColor;
        }

    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Handle pointer down event if needed
        //proceduralImage.color = onColor;
        //targetY = originalY + 4f;
        //isMoving = true;

        //labelMapImage.enabled = true;

        //labelMapPoint.color = targetTextColor;
        //labelMapName.color = targetTextColor;

        //outline.enabled = true;
        //outline.effectColor = new Color(outline.effectColor.r, outline.effectColor.g, outline.effectColor.b, 1f);

        labelMapImage.enabled = false;

        // 테마가 다크 일떄
        if (viewDashBoard.mainManager.thememode == MainManager.Thememode.dark)
        {
            proceduralImage.color = viewDashBoard.colorMapSelect_d;

            outline.effectColor = viewDashBoard.colorOutline_d;

            labelMapPoint.color = originalTextColor;
            labelMapName.color = originalTextColor;
        }
        else
        {

            proceduralImage.color = viewDashBoard.colorMapSelect_w;

            outline.effectColor = viewDashBoard.colorOutline_w;

            labelMapPoint.color = originalTextColor;
            labelMapName.color = originalTextColor;
        }

    }

    public void OnPointerUp(PointerEventData eventData)
    {
        targetY = originalY;
        isMoving = true;

        labelMapImage.enabled = false;

        outline.enabled = false;
        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, targetY);

        transform.SetSiblingIndex(PosId);

        // 테마가 다크 일떄
        if (viewDashBoard.mainManager.thememode == MainManager.Thememode.dark)
        {
            proceduralImage.color = originalColor;

            labelMapPoint.color = viewDashBoard.colorTextDeFault_d;
            labelMapName.color = viewDashBoard.colorTextDeFault_d;
        }
        else
        {

            proceduralImage.color = originalColor;

            labelMapPoint.color = viewDashBoard.colorTextDeFault_w;
            labelMapName.color = viewDashBoard.colorTextDeFault_w;
        }

    }

}