using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 확장 메뉴 버튼과 화면을 만들고 메뉴 전환을 맡는다 (ExtensionBootstrap.Attach 가 GNB 에 붙인다).
/// MonoBehaviour 라서 파일 이름과 클래스 이름을 맞춘다.
/// </summary>
public sealed class ExtensionHost : MonoBehaviour
{
    private GnbViewControl gnb;
    private MainManager main;
    private ExtSettings settings;
    private readonly List<ExtScreen> screens = new List<ExtScreen>();

    public void Init(GnbViewControl gnb)
    {
        this.gnb = gnb;
        main = gnb.mainManager;
        if (main == null || gnb.UI_Menu04 == null || gnb.menuButtons == null || gnb.menuButtons.Length == 0)
        {
            Debug.LogWarning("[확장] GNB 구성이 예상과 달라 확장 메뉴를 붙이지 않습니다.");
            return;
        }

        SetupFonts();
        settings = ExtSettings.Load();

        MarketAnalysisScreen market = CreateScreen<MarketAnalysisScreen>("UI_Menu_Ext_Market");
        JapanInspectionScreen japan = CreateScreen<JapanInspectionScreen>("UI_Menu_Ext_Japan");

        // 아이콘: 시장분석은 통계 메뉴, 일본점검은 주택청약 메뉴 아이콘을 쓴다
        AddMenuButton(ExtensionBootstrap.MarketMenuId, "시장분석", 1, market);
        AddMenuButton(ExtensionBootstrap.JapanMenuId, "일본점검", 0, japan);

        gnb.MenuChanging += HideAll;
    }

    private void OnDestroy()
    {
        if (gnb != null)
        {
            gnb.MenuChanging -= HideAll;
        }
    }

    private void SetupFonts()
    {
        // Pretendard 는 TextMesh Pro/Resources 폴더에 있어 이름으로 읽을 수 있다. 없으면 GNB 글꼴을 쓴다.
        TMP_FontAsset regular = Resources.Load<TMP_FontAsset>("Fonts & Materials/Pretendard/Pretendard-Regular SDF");
        TMP_FontAsset strong = Resources.Load<TMP_FontAsset>("Fonts & Materials/Pretendard/Pretendard-SemiBold SDF");
        if (regular == null && gnb.m01_menus != null && gnb.m01_menus.Length > 0 && gnb.m01_menus[0] != null)
        {
            regular = gnb.m01_menus[0].font;
        }
        if (regular == null && gnb.Text_myName != null)
        {
            regular = gnb.Text_myName.font;
        }
        if (strong == null && gnb.Text_myName != null)
        {
            strong = gnb.Text_myName.font;
        }
        ExtFonts.EnsureJapaneseFallback(regular);
        ExtUi.Init(regular, strong, gnb.UI_Menu04.layer);
    }

    private T CreateScreen<T>(string name) where T : ExtScreen
    {
        RectTransform template = (RectTransform)gnb.UI_Menu04.transform;
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = gnb.UI_Menu04.layer;
        go.SetActive(false);
        go.transform.SetParent(template.parent, false);
        go.transform.SetSiblingIndex(template.GetSiblingIndex() + 1);

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = template.anchorMin;
        rt.anchorMax = template.anchorMax;
        rt.pivot = template.pivot;
        rt.anchoredPosition = template.anchoredPosition;
        rt.sizeDelta = template.sizeDelta;
        rt.localScale = Vector3.one;

        Image back = go.AddComponent<Image>();
        back.color = ExtUi.Background;
        back.raycastTarget = true;         // 뒤의 화면이 눌리지 않게

        T screen = go.AddComponent<T>();
        screen.Init(main, gnb, settings);
        screens.Add(screen);
        return screen;
    }

    private void AddMenuButton(int id, string label, int iconFrom, ExtScreen screen)
    {
        MenuButton template = gnb.menuButtons[gnb.menuButtons.Length - 1];
        MenuButton clone = Instantiate(template, template.transform.parent);
        clone.name = "BtnMenu_Ext_" + id;
        clone.id = id;
        clone.transform.SetAsLastSibling();
        if (clone.SelectOn != null)
        {
            clone.SelectOn.SetActive(false);
        }
        if (clone.ButtonLabel != null)
        {
            clone.ButtonLabel.text = label;
            clone.ButtonLabel.color = clone.OffColor;
        }

        Image icon = FindIcon(clone);
        Image source = iconFrom < gnb.menuButtons.Length ? FindIcon(gnb.menuButtons[iconFrom]) : null;
        if (icon != null && source != null)
        {
            icon.sprite = source.sprite;
        }

        Button button = clone.GetComponent<Button>();
        if (button != null)
        {
            button.onClick = new Button.ButtonClickedEvent();       // 복제한 계정관리 버튼의 씬 연결을 지운다
            button.onClick.AddListener(() => Activate(clone, screen));
        }

        var list = new List<MenuButton>(gnb.menuButtons);
        list.Add(clone);
        gnb.menuButtons = list.ToArray();
    }

    private static Image FindIcon(MenuButton button)
    {
        if (button == null)
        {
            return null;
        }
        Transform icon = button.transform.Find("img_button");
        return icon != null ? icon.GetComponent<Image>() : null;
    }

    private void Activate(MenuButton button, ExtScreen screen)
    {
        if (gnb.isMyinfoChanging)
        {
            // GnbViewControl.SelectMenu 와 같은 확인 (내정보 수정 중)
            CommonPopup popup = Instantiate(main.commonPopup, main.contents);
            popup.ShowPopup_TwoButton(
                "현재 입력된 정보가 저장되지 않습니다.\n이동하시겠습니까 ? ",
                "아니오",
                "네",
                () => popup.CloseDestroy(),
                () =>
                {
                    popup.CloseDestroy();
                    gnb.viewMain.viewMyInfoEdit.Close();
                    gnb.viewMain.viewMyInfo.Close();
                    gnb.isMyinfoChanging = false;
                    Open(button, screen);
                });
            return;
        }
        Open(button, screen);
    }

    private void Open(MenuButton button, ExtScreen screen)
    {
        gnb.SelectMenu(button);          // 버튼 강조, 내정보 닫기, 메뉴 상자 닫기, setNum = id
        gnb.unLoad_Menu_01();
        gnb.unLoad_Menu_02();
        gnb.unLoad_Menu_03();
        gnb.UI_Menu04.SetActive(false);
        gnb.unLoad_DashBoard();
        HideAll();
        screen.Show();
        gnb.lastNum = gnb.setNum;
    }

    private void HideAll()
    {
        foreach (ExtScreen screen in screens)
        {
            if (screen != null)
            {
                screen.Hide();
            }
        }
    }
}
