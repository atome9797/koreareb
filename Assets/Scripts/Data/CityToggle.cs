using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CityToggle : MonoBehaviour
{
    public Toggle toggle;
    public TextMeshProUGUI title;

    private void Awake()
    {
        toggle = gameObject.GetComponent<Toggle>();
        title = gameObject.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
    }
}
