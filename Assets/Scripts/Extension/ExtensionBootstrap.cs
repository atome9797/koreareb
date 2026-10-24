using System;
using UnityEngine;

/// <summary>
/// 확장 메뉴(시장분석, 일본점검)를 기존 GNB 에 붙인다. GnbViewControl.Start() 가 부른다.
/// 씬 파일을 고치지 않고 실행할 때 붙인다:
///  - GNB 마지막 메뉴 버튼(계정관리)을 복제해 id 4 '시장분석', id 5 '일본점검' 버튼을 만든다.
///  - 화면은 계정 관리 화면(UI_Menu04) 바로 뒤 형제로 같은 위치·크기에 만든다 (팝업·내정보 창보다 아래).
///  - 다른 메뉴로 바뀌면(GnbViewControl.MenuChanging) 확장 화면을 닫는다.
/// 실패해도 기존 화면은 그대로 쓸 수 있게 예외를 잡는다.
/// </summary>
public static class ExtensionBootstrap
{
    public const int MarketMenuId = 4;
    public const int JapanMenuId = 5;

    public static void Attach(GnbViewControl gnb)
    {
        if (gnb == null || gnb.GetComponent<ExtensionHost>() != null)
        {
            return;
        }
        try
        {
            gnb.gameObject.AddComponent<ExtensionHost>().Init(gnb);
        }
        catch (Exception e)
        {
            Debug.LogError("[확장] 시장분석·일본점검 메뉴를 붙이지 못했습니다: " + e.Message);
            Debug.LogException(e);
        }
    }
}
