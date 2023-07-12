using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CommonToastPopup : MonoBehaviour
{
    public string msg;
    public TMP_Text text_msg;
    public float showTime = 3;

    // Start is called before the first frame update
    void Start()
    {
        Destroy(gameObject, showTime);
    }

    public void Init()
    {
        text_msg.text = msg;
    }

}
