using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Data;

public class ViewMyInfoEdit : BaseView
{
    public MainManager mainManager;
    public ViewMyInfo viewMyInfo;

    public string MenuName = "내정보변경";

    public ServerManager serverManager;

    private DataTable memberTable;

    public GameObject UI_LoginPWChange;
    public Transform parent;

    public TMP_InputField input_myname;
    public TMP_Text text_myID;
    public TMP_InputField input_mydepart;
    public TMP_InputField input_mytitle;

    public TMP_InputField input_mywork;

    public TMP_Text text_mypermission;


    public Button input_myname_btn_del;
    public Button input_mydepart_btn_del;
    public Button input_mytitle_btn_del;


    public Button btn_save;
    public Button btn_close;

    public Button btn_passwordChange;

    public MemberData member;
    public TMP_InputField[] inputs;

    // 비밀번호 변경 
    public TMP_InputField reset_password_input_CPW;
    public TMP_InputField reset_password_input_PW;
    public TMP_InputField reset_password_input_PW2;

    public Button reset_password_btn_del_CPW;
    public Button reset_password_btn_del_PW;
    public Button reset_password_btn_del_PW2;
    public Button reset_password_save_btn;
    public Button reset_password_cancel_btn;

    public TMP_Text UI_reset_password_input_CPW_Guide;
    public TMP_Text UI_reset_password_input_PW_Guide;
    public TMP_Text UI_reset_password_input_PW2_Guide;


    // Start is called before the first frame update
    void Start()
    {
        // 나가기 
        btn_close.onClick.AddListener(() =>
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
                    mainManager.gnbViewControl.isMyinfoChanging = false;

                    commonPopup.CloseDestroy();
                    gameObject.SetActive(false);
                });

        });

        // 비밀번호 수정 
        btn_passwordChange.onClick.AddListener(OpenPasswordChange);

        // 내정보 저장
        btn_save.onClick.AddListener(updateInfo);

        input_myname_btn_del.onClick.AddListener(() => { input_myname.text = ""; });
        input_mydepart_btn_del.onClick.AddListener(() => { input_mydepart.text = ""; });
        input_mytitle_btn_del.onClick.AddListener(() => { input_mytitle.text = ""; });

        input_myname.onValueChanged.AddListener(delegate { CheckInputMyname(input_myname); });
        input_mydepart.onValueChanged.AddListener(delegate { CheckInputMydepart(input_mydepart); });
        input_mytitle.onValueChanged.AddListener(delegate { CheckInputMyTitle(input_mytitle); });


        //사용자 입력후 엔터 클릭시 발생하는 이벤트
        reset_password_input_CPW.onSubmit.AddListener(delegate { CheckInputResetCPW(reset_password_input_CPW); });
        reset_password_input_PW.onSubmit.AddListener(delegate { CheckInputResetPW(reset_password_input_PW); });
        reset_password_input_PW2.onSubmit.AddListener(delegate { CheckInputResetPW2(reset_password_input_PW2); });

        reset_password_btn_del_CPW.onClick.AddListener(() => { reset_password_input_CPW.text = ""; });
        reset_password_btn_del_PW.onClick.AddListener(() => { reset_password_input_PW.text = ""; });
        reset_password_btn_del_PW2.onClick.AddListener(() => { reset_password_input_PW2.text = ""; });

        //직접 비밀번호 변경 저장 버튼 눌렀을때
        reset_password_save_btn.onClick.AddListener(UpdatePassword);
        //취소 버튼 누르면 팝업 종료
        reset_password_cancel_btn.onClick.AddListener(() => {
            UI_reset_password_input_CPW_Guide.text = "";
            UI_reset_password_input_CPW_Guide.gameObject.SetActive(false);
            UI_reset_password_input_PW_Guide.text = "";
            UI_reset_password_input_PW_Guide.gameObject.SetActive(false);
            UI_reset_password_input_PW2_Guide.text = "";
            UI_reset_password_input_PW2_Guide.gameObject.SetActive(false);
            //UI_DashBoard.SetActive(true);
            UI_LoginPWChange.SetActive(false);

            reset_password_btn_del_CPW.gameObject.SetActive(false);
            reset_password_btn_del_PW.gameObject.SetActive(false);
            reset_password_btn_del_PW2.gameObject.SetActive(false);
            reset_password_input_CPW.text = "";
            reset_password_input_PW.text = "";
            reset_password_input_PW2.text = "";

        });
    }

    public void Init()
    {
        memberTable = new DataTable();
        memberTable = serverManager.GetMemberDataLogin(mainManager.myData.mem_user_id);
        
        member = mainManager.myData;

        //input_myname.placeholder.GetComponent<TMP_Text>().text = member.mem_name;
        //input_mytitle.placeholder.GetComponent<TMP_Text>().text = member.mem_title;
        //input_mydepart.placeholder.GetComponent<TMP_Text>().text = member.mem_depart;

        input_myname.text = member.mem_name;
        input_mytitle.text = member.mem_title;
        input_mydepart.text = member.mem_depart;

        input_mywork.text = member.mem_work;

        //input_myname.text = "";
        //input_mytitle.text = "";
        //input_mydepart.text = "";

        text_myID.text = member.mem_user_id;
        text_mypermission.text = member.mem_auth_id;

        btn_save.interactable = false;
    }

    // 내정보 변경
    public void updateInfo()
    {
        CommonPopup commonPopup = Instantiate(mainManager.commonPopup, mainManager.contents);

        commonPopup.ShowPopup_TwoButton(
            "입력하신 정보로 저장하시겠습니까?",
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
                member.mem_name = input_myname.text;
                member.mem_title = input_mytitle.text;
                member.mem_depart = input_mydepart.text;
                member.mem_work = input_mywork.text;
                                
                serverManager.AccountUpdate(member);

                mainManager.myData = member;

                gameObject.SetActive(false);

                mainManager.viewMain.viewMyInfo.Init();

                commonPopup.CloseDestroy();
                
                string name = mainManager.myData.mem_name;

                mainManager.gnbViewControl.Text_myName.GetComponent<RectTransform>().sizeDelta = new Vector2(mainManager.gnbViewControl.Text_myName.GetComponent<RectTransform>().sizeDelta.x, 19);

                //mainManager.gnbViewControl.Text_myID.GetComponent<RectTransform>().anchoredPosition = new Vector2(mainManager.gnbViewControl.Text_myID.GetComponent<RectTransform>().anchoredPosition.x, -47.3f);

                if (mainManager.myData.mem_name.Length > 5)
                {
                    string firstName = mainManager.myData.mem_name.Substring(0, 5);
                    string secendName = mainManager.myData.mem_name.Substring(5);
                    if (secendName.Length > 3)
                    {
                        secendName = mainManager.myData.mem_name.Substring(0, 3) + "...";
                    }
                    name = $"{firstName}\n{secendName}";

                    mainManager.gnbViewControl.Text_myName.GetComponent<RectTransform>().sizeDelta = new Vector2(mainManager.gnbViewControl.Text_myName.GetComponent<RectTransform>().sizeDelta.x, 38);

                    //mainManager.gnbViewControl.Text_myID.GetComponent<RectTransform>().anchoredPosition = new Vector2(mainManager.gnbViewControl.Text_myID.GetComponent<RectTransform>().anchoredPosition.x, -66.3f);
                }

                mainManager.gnbViewControl.Text_myName.text = $"{name}";

                mainManager.gnbViewControl.Text_myID.text = $"({mainManager.myData.mem_user_id})";

                mainManager.gnbViewControl.isMyinfoChanging = false;

                
            });
    }


    public void CheckInputMyname(TMP_InputField _input)
    {
        btn_save.interactable = AllCheckInput();
    }

    public void CheckInputMydepart(TMP_InputField _input)
    {
        btn_save.interactable = AllCheckInput();
    }

    public void CheckInputMyTitle(TMP_InputField _input)
    {
        btn_save.interactable = AllCheckInput();
    }

    public bool AllCheckInput()
    {
        bool allInputOn = true;

        foreach (var item in inputs)
        {
            if(item.text.Length == 0)
            {
                allInputOn = false;
                break;
            }
        }
        return allInputOn;
    }


    // 비밀번호 변경
    public void OpenPasswordChange()
    {
        UI_LoginPWChange.SetActive(true);

    }

    private void UpdatePassword()
    {
        if (reset_password_input_CPW.text.Length > 0 && reset_password_input_PW.text.Length > 0 && reset_password_input_PW2.text.Length > 0)
        {
            // 패스워드 재설정            
            viewMyInfo.ActionUpdatePassword(reset_password_input_CPW.text, reset_password_input_PW.text, reset_password_input_PW2.text);
        }
    }


    public void CheckInputResetCPW(TMP_InputField _input)
    {
        if (_input.text.Length > 0)
        {
            reset_password_input_PW.ActivateInputField();
            reset_password_btn_del_CPW.gameObject.SetActive(false);
        }
    }

    public void CheckInputResetPW(TMP_InputField _input)
    {
        if (_input.text.Length > 0)
        {
            reset_password_input_PW2.ActivateInputField();
            reset_password_btn_del_PW.gameObject.SetActive(false);
        }
    }

    public void CheckInputResetPW2(TMP_InputField _input)
    {
        if (_input.text.Length > 0)
        {
            // 비밀번호 재설정 함수 호출
            UpdatePassword();
        }
    }



}
