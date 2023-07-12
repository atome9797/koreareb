using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ExecuteData 
{
    // filePath
    public string _path;

    // SelectMenu
    // 검색 구분
    public int SeacrhDropdown;

    // 입력 필드 
    public string searchText;

    // 검색 기간
    public DateTime? fromDate;
    public DateTime? endDate;

    // 선택 년도
    public int periodNum;

    // RFXG
    public int toggleprobNum;

    // 선택 랭크
    public int toggleRank;

    // 선택 지역
    public bool[] cityToggle = new bool[18];

}
