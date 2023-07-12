using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class CommonErrorPopup : BaseView
{
    //[SerializeField]
    //private TextMeshProUGUI pTitle;

    [SerializeField]
    private TextMeshProUGUI pContent;

    //[SerializeField]
    //private Button leftButton;

    [SerializeField]
    private Button rightButton;

    public void ShowPopup_OneButton(string content, string rigthBtnName, UnityAction rightBtnEvent = null )
    {
        //pTitle.text = title;
        pContent.text = content;

        //leftButton.transform.GetComponentInChildren<TextMeshProUGUI>().text = leftBtnName;
        rightButton.transform.GetComponentInChildren<TextMeshProUGUI>().text = rigthBtnName;

        //confirmButton.gameObject.SetActive(false);

        //leftButton.gameObject.SetActive(true);
        rightButton.gameObject.SetActive(true);

        //leftButton.onClick.AddListener(leftBtnEvent);
        rightButton.onClick.AddListener(rightBtnEvent);

        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)pContent.transform);

        Open();
    }

}
