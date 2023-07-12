using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Data;

public class ViewMyInfo : BaseView
{
    public MainManager mainManager;

    public string MenuName = "내정보";

    public ServerManager serverManager;

    private DataTable memberTable;

    // 내정보 수정
    public ViewMyInfoEdit viewMyInfoEdit;

    public GameObject UI_MyPagePWChange;
    public Transform parent;

    public TMP_Text text_myname;
    public TMP_Text text_myID;
    public TMP_Text text_mydepart;
    public TMP_Text text_mytitle;
    public TMP_Text text_mywork;
    
    public TMP_Text text_mypermission;

    public Button btn_edit;
    public Button btn_close;

    public Button btn_passwordChange;


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

    public RectTransform[] viewupdate;

    // Start is called before the first frame update
    void Start()
    {
        // 나가기 
        btn_close.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
        });

        // 비밀번호 수정 
        btn_passwordChange.onClick.AddListener(OpenPasswordChange);            
            
        // 내정보 수정
        btn_edit.onClick.AddListener(OpenMyInfoEdit);


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
            UI_MyPagePWChange.SetActive(false);

            reset_password_btn_del_CPW.gameObject.SetActive(false);
            reset_password_btn_del_PW.gameObject.SetActive(false);
            reset_password_btn_del_PW2.gameObject.SetActive(false);
            reset_password_input_CPW.text = "";
            reset_password_input_PW.text = "";
            reset_password_input_PW2.text = "";

        });
    }

    private void Update()
    {
        if (reset_password_input_CPW.isFocused || reset_password_input_PW.isFocused || reset_password_input_PW2.isFocused) // password 에 포커스가 되어있을때
        {
            Input.imeCompositionMode = IMECompositionMode.Off;
        }
        else
        {
            Input.imeCompositionMode = IMECompositionMode.On;
        }
    }


    public void Init()
    {
        memberTable = new DataTable();
        memberTable = serverManager.GetMemberDataLogin(mainManager.myData.mem_user_id);

        text_myname.text = mainManager.myData.mem_name;
        text_myID.text = mainManager.myData.mem_user_id;
        text_mydepart.text = mainManager.myData.mem_depart;
        text_mytitle.text = mainManager.myData.mem_title;

        text_mypermission.text = mainManager.myData.mem_auth_id;

        text_mywork.text = mainManager.myData.mem_work;


    }

    // 내정보 변경
    public void OpenMyInfoEdit()
    {
        Debug.Log("OpenMyInfoEdit");
        Debug.Log("mainManager.gnbViewControl.isMyinfoChanging : " + mainManager.gnbViewControl.isMyinfoChanging);

        mainManager.gnbViewControl.isMyinfoChanging = true;

        viewMyInfoEdit.gameObject.SetActive(true);

        viewMyInfoEdit.Init();
    }

    // 비밀번호 변경
    public void OpenPasswordChange()
    {
        Init();
        UI_MyPagePWChange.SetActive(true);
    }

    private void UpdatePassword()
    {
        if (reset_password_input_CPW.text.Length > 0 && reset_password_input_PW.text.Length > 0 && reset_password_input_PW2.text.Length > 0)
        {
            // 패스워드 재설정            
            ActionUpdatePassword(reset_password_input_CPW.text, reset_password_input_PW.text, reset_password_input_PW2.text);
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

    //비밀번호 재설정
    public void ActionUpdatePassword(string currentPasseword, string newPassword, string newPasswordCheck)
    {
        //현재 비밀번호가 틀린경우
        bool checkCurrentPasswordError = false;
        //비밀번호가 기존 비밀번호와 같으면 오류
        bool checkNewPasswordError = false;
        //새 비밀번호와 새비밀번호 확인이 다른 경우 오류
        bool checkNewPassword2Error = false;

        string userId = "";


        UI_reset_password_input_CPW_Guide.gameObject.SetActive(false);
        UI_reset_password_input_PW_Guide.gameObject.SetActive(false);
        UI_reset_password_input_PW2_Guide.gameObject.SetActive(false);

        foreach (DataRow row in memberTable.Rows)
        {
            userId = row["mem_userId"].ToString();

            if (currentPasseword != mainManager.Decode(row["mem_password"].ToString()))
            {
                checkCurrentPasswordError = true;
                UI_reset_password_input_CPW_Guide.text = "현재 비밀번호가 올바르지 않습니다.";
                UI_reset_password_input_CPW_Guide.gameObject.SetActive(true);
            }

            if (newPassword == currentPasseword)
            {
                checkNewPasswordError = true;
                UI_reset_password_input_PW_Guide.text = "이전 비밀번호와 같습니다.";
                UI_reset_password_input_PW_Guide.gameObject.SetActive(true);
            }
            else if (!mainManager.CheckingString(newPassword)) //비밀번호 정규식 아니면 에러
            {
                checkNewPasswordError = true;
                UI_reset_password_input_PW_Guide.text = "영문대문자 + 영문소문자 + 숫자 + 특수문자, 3가지 이상을 조합하여 8~16자로 입력해주세요.";
                UI_reset_password_input_PW_Guide.gameObject.SetActive(true);
            }

            if (newPasswordCheck != newPassword)
            {
                checkNewPassword2Error = true;
                UI_reset_password_input_PW2_Guide.text = "새 비밀번호와 새 비밀번호 확인이 일치하지 않습니다.";
                UI_reset_password_input_PW2_Guide.gameObject.SetActive(true);
            }
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)parent);
        LayoutRebuilder.ForceRebuildLayoutImmediate(viewupdate[0]);
        LayoutRebuilder.ForceRebuildLayoutImmediate(viewupdate[1]);
        LayoutRebuilder.ForceRebuildLayoutImmediate(viewupdate[2]);


        //비밀번호 변경
        if (!checkCurrentPasswordError && !checkNewPasswordError && !checkNewPassword2Error)
        {
            serverManager.UpdateUserPassword(userId, mainManager.Encode(newPassword));

            //대시보드로 이동
            UI_reset_password_input_CPW_Guide.text = "";
            UI_reset_password_input_CPW_Guide.gameObject.SetActive(false);
            UI_reset_password_input_PW_Guide.text = "";
            UI_reset_password_input_PW_Guide.gameObject.SetActive(false);
            UI_reset_password_input_PW2_Guide.text = "";
            UI_reset_password_input_PW2_Guide.gameObject.SetActive(false);
            //UI_DashBoard.SetActive(true); 
            UI_MyPagePWChange.SetActive(false);

            reset_password_btn_del_CPW.gameObject.SetActive(false);
            reset_password_btn_del_PW.gameObject.SetActive(false);
            reset_password_btn_del_PW2.gameObject.SetActive(false);

            reset_password_input_CPW.text = "";
            reset_password_input_PW.text = "";
            reset_password_input_PW2.text = "";
        }
    }
}
