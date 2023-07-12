using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GnbViewControl : MonoBehaviour
{
    public MainManager mainManager;

    // 메인 메뉴들
    public ViewMain viewMain;

    public MenuButton[] menuButtons;
    public MenuButton selectMenuButton;

    public GameObject UI_MenuBox;
    public GameObject[] UI_MenuBoxs;
            
    public ViewDashBoard UI_DashBoard;

    public GameObject[] UI_Menu01;
    public GameObject[] UI_Menu02;
    public GameObject[] UI_Menu03;
    public GameObject UI_Menu04;

    public TMP_Text[] m01_menus;
    public TMP_Text[] m02_menus;
    public TMP_Text[] m03_menus;

    public int setNum = -1;
    public int lastNum = -1;

    // 내 GNB 아이콘
    public Button myInfoIcon;

    public Button myNameClick;

    public GameObject UI_MyInfoblob;
    public bool isMyInfoTab;

    // 로그아웃
    public Button btn_MyInfoView;

    // 배경 버튼
    public Button btn_background;

    // GNB 하단 내정보
    public TextMeshProUGUI Text_myTitle;
    public TextMeshProUGUI Text_myName;
    public TextMeshProUGUI Text_myID;

    // 내정보 변경
    public bool isMyinfoChanging;

    public Color menus_onColor;
    public Color menus_offColor;



    public void Start()
    {
        // 내 아이콘
        //myInfoIcon.onClick.AddListener(Click_MyInfoIcon);
        myInfoIcon.onClick.AddListener(Click_MyInfoView);

        // 이름 클릭
        myNameClick.onClick.AddListener(Click_MyInfoIcon);


        // 로그아웃
        btn_MyInfoView.onClick.AddListener(Click_LogOut);

        // 배경 클릭
        btn_background.onClick.AddListener(Click_Background);
    }

    // 배경 클릭
    void Click_Background()
    {
        isMyInfoTab = false;

        UI_MenuBox.SetActive(isMyInfoTab);
        btn_background.gameObject.SetActive(isMyInfoTab);

        if (isMyInfoTab == false)
        {
            if (lastNum == -1)
            {
                unLoad_Menu();

            }
            else
            {
                if (lastNum != setNum)
                {
                    if (lastNum < 3)
                    {
                        unLoad_Menu();

                        // Debug.Log($"<color=yellow>[GnbViewControl][menuButton][{menuButtons[lastNum].id}]</color>");

                        menuButtons[lastNum].SelectOn.SetActive(true);

                        menuButtons[lastNum].ButtonLabel.color = menuButtons[lastNum].OnColor;

                    }
                }
            }

        }

    }

    
    // 내정보 탭 클릭
    public void Click_MyInfoIcon()
    {
        Debug.Log("Click_MyInfoIcon Click");

        

        isMyInfoTab = !isMyInfoTab;

        UI_MenuBox.SetActive(isMyInfoTab);
        btn_background.gameObject.SetActive(isMyInfoTab);

        if (isMyInfoTab == false)
        {
            unLoad_MenuBox();

            if(lastNum == -1)
            {
                unLoad_Menu();
            }
            else
            {
                if (lastNum != setNum)
                {
                    if (lastNum < 3)
                    {
                        unLoad_Menu();

                        // Debug.Log($"<color=yellow>[GnbViewControl][menuButton][{menuButtons[lastNum].id}]</color>");

                        menuButtons[lastNum].SelectOn.SetActive(true);

                        menuButtons[lastNum].ButtonLabel.color = menuButtons[lastNum].OnColor;

                    }
                }
            }
            
        }
    }

    public void Click_LogOut()
    {
        CommonPopup commonPopup = Instantiate(viewMain.mainManager.commonPopup, viewMain.mainManager.contents);

        commonPopup.ShowPopup_TwoButton(
            "로그아웃 하시겠습니까?",
            "아니오",
            "네",
            () =>
            {
                // 아니오 삭제

                commonPopup.CloseDestroy();
            },

            () =>
            {
                // 네
                // ResetButton(true);
                SceneManager.LoadScene(0);

                commonPopup.CloseDestroy();
            });
    }

    //

    public void Click_MyInfoView()
    {
        viewMain.viewMyInfo.gameObject.SetActive(true);

        viewMain.viewMyInfo.Init();

        unLoad_MenuBox();
    }

    public void Load_MyInfo()
    {
        
    }

    public void unLoad_MyInfo()
    {

    }
    
    public void unLoad_Menu()
    {
        for (int i = 0; i < menuButtons.Length; i++)
        {
            menuButtons[i].SelectOn.SetActive(false);

            menuButtons[i].ButtonLabel.color = menuButtons[0].OffColor;
        }
    }
    // GNB 메뉴 선택
    public void SelectMenu(MenuButton menuButton)
    {
        if(isMyinfoChanging)
        {
            CommonPopup commonPopup = Instantiate(mainManager.commonPopup, mainManager.contents);

            commonPopup.ShowPopup_TwoButton(
                "현재 입력된 정보가 저장되지 않습니다.\n" +
                "이동하시겠습니까 ? ",
                "아니오",
                "네",
                () =>
                {
                    // 아니오 삭제
                    commonPopup.CloseDestroy();
                },

                () =>
                {
                    // 네                    
                    commonPopup.CloseDestroy();

                    // 내정보보기 수정 닫기

                    viewMain.viewMyInfoEdit.Close();

                    viewMain.viewMyInfo.Close();

                    isMyinfoChanging = false;

                    unLoad_Menu();

                    Debug.Log($"<color=yellow>[GnbViewControl][menuButton][{menuButton}]</color>");

                    menuButton.SelectOn.SetActive(true);

                    menuButton.ButtonLabel.color = menuButton.OnColor;

                    selectMenuButton = menuButton;

                    SelectMenuBox(selectMenuButton.id);

                });
        }
        else
        {
            viewMain.viewMyInfo.Close();

            unLoad_Menu();

            // Debug.Log($"<color=yellow>[GnbViewControl][menuButton][{menuButton}]</color>");

            menuButton.SelectOn.SetActive(true);

            menuButton.ButtonLabel.color = menuButton.OnColor;

            selectMenuButton = menuButton;

            SelectMenuBox(selectMenuButton.id);
        }
        
    }

    // 메뉴별 박스
    public void SelectMenuBox(int selectNum)
    {
        if (selectNum < 3)
        {
            unLoad_MenuBox();

            UI_MenuBox.SetActive(true);

            isMyInfoTab = true;

            btn_background.gameObject.SetActive(isMyInfoTab);

            UI_MenuBoxs[selectNum].SetActive(true);
        }
        else
        {
            UI_MenuBox.SetActive(false);

            isMyInfoTab = false;

            btn_background.gameObject.SetActive(isMyInfoTab);

            unLoad_MenuBox();
        }

        setNum = selectNum;
    }

    public void Load_DashBoard()
    {        
        if(isMyinfoChanging)
        {
            CommonPopup commonPopup = Instantiate(mainManager.commonPopup, mainManager.contents);

            commonPopup.ShowPopup_TwoButton(
                "현재 입력된 정보가 저장되지 않습니다.\n" +
                "이동하시겠습니까 ? ",
                "아니오",
                "네",
                () =>
                {
                    // 아니오 삭제
                    commonPopup.CloseDestroy();
                },

                () =>
                {
                    // 네                    
                    commonPopup.CloseDestroy();

                    // 내정보보기 수정 닫기

                    viewMain.viewMyInfoEdit.Close();

                    isMyinfoChanging = false;

                    viewMain.viewMyInfo.Close();

                    UI_DashBoard.Open();

                    UI_DashBoard.Init();

                    unLoad_Menu();

                    unLoad_MenuBox();

                    unLoad_Menu_01();
                    unLoad_Menu_02();
                    unLoad_Menu_03();
                    unLoad_Menu_04();

                    setNum = -1;
                    lastNum = -1;
                });
        }
        else
        {
            viewMain.viewMyInfo.Close();

            UI_DashBoard.Open();

            UI_DashBoard.Init();

            unLoad_Menu();

            unLoad_MenuBox();

            unLoad_Menu_01();
            unLoad_Menu_02();
            unLoad_Menu_03();
            unLoad_Menu_04();

            setNum = -1;
            lastNum = -1;
        }



    }

    public void unLoad_DashBoard()
    {
        UI_DashBoard.Close();
    }

    public void unLoad_MenuBox()
    {
        UI_MenuBox.SetActive(false);

        isMyInfoTab = false;

        btn_background.gameObject.SetActive(isMyInfoTab);

        for (int i = 0; i < UI_MenuBoxs.Length; i++)
        {
            UI_MenuBoxs[i].SetActive(false);
        }
    }

    public void unLoad_Menu_01()
    {
        for (int i = 0; i < UI_Menu01.Length; i++)
        {
            UI_Menu01[i].SetActive(false);

            m01_menus[i].color = menus_offColor;
        }
    }

    public void unLoad_Menu_02()
    {
        for (int i = 0; i < UI_Menu02.Length; i++)
        {
            UI_Menu02[i].SetActive(false);

            m02_menus[i].color = menus_offColor;
        }
    }

    public void unLoad_Menu_03()
    {
        for (int i = 0; i < UI_Menu03.Length; i++)
        {
            UI_Menu03[i].SetActive(false);

            m03_menus[i].color = menus_offColor;
        }
    }

    public void unLoad_Menu_04()
    {
        UI_Menu04.SetActive(false);
        unLoad_MenuBox();
    }

    // 주택청약
    public void SelectMenu_01(int selectNum)
    {
        unLoad_Menu_01();

        UI_Menu01[selectNum].SetActive(true);

        m01_menus[selectNum].color = menus_onColor;

        if(selectNum == 3)
        {
            viewMain.viewExecute.Open();
        }
        else if(selectNum == 2)
        {
            viewMain.viewDanji.Action_Search();
        }

        lastNum = setNum;

        unLoad_Menu_02();
        unLoad_Menu_03();
        unLoad_Menu_04();
    }

    // 통계
    public void SelectMenu_02(int selectNum)
    {
        unLoad_Menu_02();
        UI_Menu02[selectNum].SetActive(true);

        m02_menus[selectNum].color = menus_onColor;

        lastNum = setNum;

        unLoad_Menu_01();
        unLoad_Menu_03();
        unLoad_Menu_04();
    }

    // 이력관리
    public void SelectMenu_03(int selectNum)
    {
        unLoad_Menu_03();
        UI_Menu03[selectNum].SetActive(true);

        m03_menus[selectNum].color = menus_onColor;

        lastNum = setNum;

        unLoad_Menu_01();
        unLoad_Menu_02();
        unLoad_Menu_04();
    }

    // 계정 관리
    public void SelectMenu_04(int selectNum)
    {
        unLoad_Menu_04();
        UI_Menu04.SetActive(true);

        lastNum = setNum;

        unLoad_Menu_01();
        unLoad_Menu_02();
        unLoad_Menu_03();
    }
}
