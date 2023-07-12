using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectMenu : MonoBehaviour
{
    int[] CityId = new int[] { 0, 11, 26, 27, 28, 29, 30, 31, 36, 41, 42, 43, 44, 45, 46, 47, 48, 50 };

    public DateText dateText;
    public Toggle[] periodToggle;

    //검색구분
    public TMP_Dropdown SeacrhDropdown;//드롭 다운

    //추가된 사항 2023-05-26 김영훈
    public TMP_InputField input_search;
    public string searchText = "";

    // 적발내용 rfxg type
    public ToggleGroup CheckTypeGroup;
    public Toggle[] toggle_checkType;

    // RF XG rfxg
    public ToggleGroup ProbGroup;
    public Toggle[] toggle_prob;

    // 전체 상위 50%, 25%, 10% /rfxg percent
    public ToggleGroup RankGroup;
    public Toggle[] toggle_rank;

    //editor에서 설정
    public List<CityToggle> cityToggles = new List<CityToggle>();

    public void ResetData()
    {
        //기간 초기화 2023.06.11 => 초기화 1년으로
        periodToggle[1].isOn = true;
        PriodChange(periodToggle[1], 1);

        //검색어 초기화
        SeacrhDropdown.value = 0;
        input_search.text = "";

        //도시 초기화
        cityToggles[0].toggle.isOn = true;

        MenuChangedEvent(true);

        //적발내용 초기화
        toggle_checkType[0].isOn = true;
        toggle_prob[0].isOn = true;
        toggle_rank[0].isOn = true;
    }

    private void Awake()
    {
        cityToggles[0].toggle.onValueChanged.AddListener(MenuChangedEvent); //전체 토글 클릭시 이벤트
        for(int i = 0; i < 4; i++)
        {
            int num = i;
            periodToggle[i].onValueChanged.AddListener(_=>PriodChange(periodToggle[num], num));//기간 토글 클릭시 이벤트
        }

        periodToggle[1].isOn = true;
        PriodChange(periodToggle[1], 1);
    }

    /// <summary>
    /// 기간 라디오 버튼 이벤트
    /// </summary>
    public void PriodToggleEvent(bool check = false)
    {
        for (int i = 0; i < periodToggle.Length; i++)
        {
            periodToggle[i].isOn = check;
        }
    }

    /// <summary>
    /// 도시 체크 박스 이벤트
    /// </summary>
    /// <param name="check"></param>
    public void MenuChangedEvent(bool check)
    {   
        for (int i = 1; i < cityToggles.Count; i++)
        {
            cityToggles[i].toggle.isOn = check;
        }
    }

    /// <summary>
    /// 도시 하나만 체크 이벤트
    /// </summary>
    /// <param name="SelectMenu"></param>
    public void MenuCheckEvent(int SelectMenu)
    {
        for (int i = 0; i < cityToggles.Count; i++)
        {
            cityToggles[i].toggle.isOn = false;

            if (SelectMenu == CityId[i])
            {
                cityToggles[i].toggle.isOn = true;
            }
        }
    }

    /// <summary>
    /// 도시 체크 박스 리스트 배열 생성
    /// </summary>
    /// <param name="cityToggles"></param>
    /// <returns></returns>
    public bool [] CheckCityToggle(List<CityToggle> cityToggles)
    {
        bool[] cityMenu = new bool[18];

        for(int i = 0; i < cityToggles.Count; i++)
        {
            if(cityToggles[i].toggle.isOn)
            {
                cityMenu[i] = true;
            }
        }

        return cityMenu;
    }

    /// <summary>
    /// rfxg 라디오 버튼 체크
    /// </summary>
    /// <param name="_rfxgType"></param>
    /// <returns></returns>
    public bool[] CheckRfxgType(bool [] _rfxgType)
    {
        
        for (int i = 0; i < toggle_prob.Length; i++)
        {
            _rfxgType[i] = false;

            if (toggle_prob[i].isOn)
            {
                _rfxgType[i] = true;
            }
        }

        return _rfxgType;
    }

    /// <summary>
    /// rfxg 퍼센트 라디오 버튼 체크
    /// </summary>
    /// <param name="_rfxPerType"></param>
    /// <returns></returns>
    public bool[] CheckRfxgPerType(bool[] _rfxPerType)
    {
        
        for (int i = 0; i < toggle_rank.Length; i++)
        {
            _rfxPerType[i] = false;

            if (toggle_rank[i].isOn)
            {
                _rfxPerType[i] = true;
            }
        }

        return _rfxPerType;
    }

    /// <summary>
    /// rfxg 타입 라디오 이벤트
    /// </summary>
    /// <param name="_rfxPerType"></param>
    /// <returns></returns>
    public bool[] CheckDFType(bool[] _rfxgDFType)
    {
        
        for (int i = 0; i < toggle_checkType.Length; i++)
        {
            _rfxgDFType[i] = false;

            if (toggle_checkType[i].isOn)
            {
                _rfxgDFType[i] = true;
            }
        }

        return _rfxgDFType;
    }

    /// <summary>
    /// 검색 구분 토글 
    /// </summary>
    /// <param name="_dropdownType"></param>
    /// <returns></returns>
    public bool[] DropdownType(bool[] _dropdownType)
    {
        for (int i = 0; i < SeacrhDropdown.options.Count; i++)
        {
            _dropdownType[i] = false;
        }
        _dropdownType[SeacrhDropdown.value] = true;

        return _dropdownType;
    }

    public void PriodChange(Toggle toggle , int priod)
    {
        if(toggle.isOn)
        {
            dateText.ChangeCalendarPriod(priod);
        }
    }

    /// <summary>
    /// 검색 버튼 활성화 여부
    /// </summary>
    /// <param name="cityToggles"></param>
    /// <param name="button"></param>
    /// <returns></returns>
    public Button SeacrhButtonActiveCheck(List<CityToggle> cityToggles, Button button)
    {
        bool check = false;

        for(int i = 0; i < cityToggles.Count; i++)
        {
            if(cityToggles[i].toggle.isOn)
            {
                check = true;
            }
        }

        button.interactable = check;

        return button;
    }
}
