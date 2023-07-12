using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ViewLogin : MonoBehaviour
{

    public MainManager mainManager;
    public ServerManager serverManager;
    public DataSet ds;

    DataTable memberTable;

    public ViewMain viewMain;

    // UI
    public TMP_InputField input_ID;
    public TMP_InputField input_PW;

    public Button btn_del_ID;
    public Button btn_del_PW;
    public Button btn_login;

    public TMP_InputField reset_password_input_CPW;
    public TMP_InputField reset_password_input_PW;
    public TMP_InputField reset_password_input_PW2;

    public Button reset_password_btn_del_CPW;
    public Button reset_password_btn_del_PW;
    public Button reset_password_btn_del_PW2;
    public Button reset_password_save_btn;
    public Button reset_password_cancel_btn;

    public TMP_Text UI_LoginGuide;
    public TMP_Text UI_reset_password_input_CPW_Guide;
    public TMP_Text UI_reset_password_input_PW_Guide;
    public TMP_Text UI_reset_password_input_PW2_Guide;

    public string[] guides;

    // GNB 하단 내정보
    public TextMeshProUGUI Text_myTitle;
    public TextMeshProUGUI Text_myName;
    public TextMeshProUGUI Text_myID;

    public GameObject UI_DashBoard;
    public GameObject UI_LoginPWChange;
    public Transform parent;

    public int failCount;

    public RectTransform[] viewupdate;

    EventSystem system;

    // 계정 관리 버튼
    // 비번초기화
    public Button btn_resetPassword;
    // 계정정지
    public Button btn_memberStop;

    // 계정추가
    public Button btn_memberAdd;

    private void Awake()
    {
        system = EventSystem.current;
    }

    // Start is called before the first frame update
    void Start()
    {
        // 
        input_ID.Select();

        //사용자 입력후 엔터 클릭시 발생하는 이벤트
        input_ID.onSubmit.AddListener(delegate { CheckInputID(input_ID); });
        input_PW.onSubmit.AddListener(delegate { CheckInputPW(input_PW); });

        //사용자 입력후 엔터 클릭시 발생하는 이벤트
        reset_password_input_CPW.onSubmit.AddListener(delegate { CheckInputResetCPW(reset_password_input_CPW); });
        reset_password_input_PW.onSubmit.AddListener(delegate { CheckInputResetPW(reset_password_input_PW); });
        reset_password_input_PW2.onSubmit.AddListener(delegate { CheckInputResetPW2(reset_password_input_PW2); });

        reset_password_btn_del_CPW.onClick.AddListener(() => { reset_password_input_CPW.text = "";  });
        reset_password_btn_del_PW.onClick.AddListener(() => { reset_password_input_PW.text = "";  });
        reset_password_btn_del_PW2.onClick.AddListener(() => { reset_password_input_PW2.text = "";  });

        //직접 비밀번호 변경 저장 버튼 눌렀을때
        reset_password_save_btn.onClick.AddListener(UpdatePassword);
        //취소 버튼 누르면 팝업 종료
        reset_password_cancel_btn.onClick.AddListener(() => {
            UI_LoginGuide.text = "";
            UI_LoginGuide.gameObject.SetActive(false);
            UI_reset_password_input_CPW_Guide.text = "";
            UI_reset_password_input_CPW_Guide.gameObject.SetActive(false);
            UI_reset_password_input_PW_Guide.text = "";
            UI_reset_password_input_PW_Guide.gameObject.SetActive(false);
            UI_reset_password_input_PW2_Guide.text = "";
            UI_reset_password_input_PW2_Guide.gameObject.SetActive(false);
            UI_DashBoard.SetActive(true);
            UI_LoginPWChange.SetActive(false);

            reset_password_btn_del_CPW.gameObject.SetActive(false);
            reset_password_btn_del_PW.gameObject.SetActive(false);
            reset_password_btn_del_PW2.gameObject.SetActive(false);

            reset_password_input_CPW.text = "";
            reset_password_input_PW.text = "";
            reset_password_input_PW2.text = "";

        });

        //삭제 버튼
        btn_del_ID.onClick.AddListener(() => {
            input_ID.text = "";
        });

        //삭제 버튼
        btn_del_PW.onClick.AddListener(() => {
            input_PW.text = "";
        });

        //직접 로그인 버튼 눌렀을때 처리
        btn_login.onClick.AddListener(Login);

        Init();

        //비밀번호 초기화 되었을때 재설정
    }

    void Init()
    {
        // 맴버 정보 획득
        //serverManager.GetMemberData();       

        input_ID.Select();
    }

    private void Update()
    {
        
        if (input_PW.isFocused || reset_password_input_CPW.isFocused || reset_password_input_PW.isFocused || reset_password_input_PW2.isFocused) // password 에 포커스가 되어있을때
        {
            Input.imeCompositionMode = IMECompositionMode.Off;
        }
        else
        {
            Input.imeCompositionMode = IMECompositionMode.On;
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            // Tab + LeftShift는 위의 Selectable 객체를 선택

            if(system.currentSelectedGameObject != null)
            {
                Selectable next = system.currentSelectedGameObject.GetComponent<Selectable>().FindSelectableOnDown();

                if (next != null)
                {
                    next.Select();
                }
                else
                {
                    input_ID.Select();
                }
            }
            else
            {
                input_ID.Select();
            }

        }
        else if (Input.GetKeyDown(KeyCode.Return))
        {
            // 엔터키를 치면 로그인 (제출) 버튼을 클릭

            if(system.currentSelectedGameObject == btn_login)
            {
                Debug.Log("system.currentSelectedGameObject : " + system.currentSelectedGameObject.name);

                btn_login.onClick.Invoke();
                Debug.Log("Button pressed!");

            }
        }
    }

    private void Login()
    {
        Debug.Log("여기");

        if(input_ID.text.Length > 0 && input_PW.text.Length > 0)
        {
            // 로그인 시작

            UI_LoginGuide.text = "";
            UI_LoginGuide.gameObject.SetActive(false);
            
            ActionLogin(input_ID.text, input_PW.text);
        }
    }


    private void UpdatePassword()
    {
        if (reset_password_input_CPW.text.Length > 0 && reset_password_input_PW.text.Length > 0 && reset_password_input_PW2.text.Length > 0)
        {
            // 패스워드 재설정

            UI_LoginGuide.text = "";
            UI_LoginGuide.gameObject.SetActive(false);

            ActionUpdatePassword(reset_password_input_CPW.text, reset_password_input_PW.text, reset_password_input_PW2.text);
        }
    }


    public void CheckInputID(TMP_InputField _input)
    {
        if (_input.text.Length > 0)
        {
            Debug.Log("CheckInputID has been entered");

            //input_PW.ActivateInputField();
            //btn_del_ID.gameObject.SetActive(false);
            input_PW.Select();

        }
        else if (_input.text.Length == 0)
        {
            Debug.Log("CheckInputID Empty");
            input_ID.Select();
        }
    }

    public void CheckInputPW(TMP_InputField _input)
    {
        if (_input.text.Length > 0)
        {
            Debug.Log("CheckInputPW has been entered");

            // 로그인 시작
            Login();
        }
        else if (_input.text.Length == 0)
        {
            Debug.Log("CheckInputPW Empty");
            input_PW.Select();
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

            if (currentPasseword != row["mem_password"].ToString())
            {
                checkCurrentPasswordError = true;
                UI_reset_password_input_CPW_Guide.text = "현재 비밀번호가 올바르지 않습니다.";
                UI_reset_password_input_CPW_Guide.gameObject.SetActive(true);
            }

            if(newPassword == currentPasseword)
            {
                checkNewPasswordError = true;
                UI_reset_password_input_PW_Guide.text = "이전 비밀번호와 같습니다.";
                UI_reset_password_input_PW_Guide.gameObject.SetActive(true);
            }
            else if(!mainManager.CheckingString(newPassword)) //비밀번호 정규식 아니면 에러
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
            serverManager.UpdateUserPassword(userId, newPassword);

            //대시보드로 이동
            UI_LoginGuide.text = "";
            UI_LoginGuide.gameObject.SetActive(false);
            UI_reset_password_input_CPW_Guide.text = "";
            UI_reset_password_input_CPW_Guide.gameObject.SetActive(false);
            UI_reset_password_input_PW_Guide.text = "";
            UI_reset_password_input_PW_Guide.gameObject.SetActive(false);
            UI_reset_password_input_PW2_Guide.text = "";
            UI_reset_password_input_PW2_Guide.gameObject.SetActive(false);
            UI_DashBoard.SetActive(true);
            LoadDashBoard();

            gameObject.SetActive(false);
            UI_LoginPWChange.SetActive(false);

            reset_password_btn_del_CPW.gameObject.SetActive(false);
            reset_password_btn_del_PW.gameObject.SetActive(false);
            reset_password_btn_del_PW2.gameObject.SetActive(false);

            reset_password_input_CPW.text = "";
            reset_password_input_PW.text = "";
            reset_password_input_PW2.text = "";

            //타이머 시작
            //mainManager.StartTimer();
        }
    }

    public void ActionLogin(string userID, string userPW)
    {
        Debug.Log("ds: " + serverManager.ds_member.DataSetName);
        Debug.Log($"user Login : {userID}, {userPW}");
        userPW = mainManager.Encode(userPW);
        memberTable = new DataTable();
        memberTable = serverManager.GetMemberDataLogin(userID);

        bool loginCheck = false;
        bool IdCheck = false;


        foreach (DataRow row in memberTable.Rows)
        {
            IdCheck = true;
            
            if (userID == row["mem_userId"].ToString() && userPW == row["mem_password"].ToString())
            {
                // 로그인이 성공하면 맴버 데이터를 받아서 처리한다. 
                loginCheck = true;

                //로그인 성공한 맴버의 정보를 저장
                SetLoginInfo(row);
            }
            else
            {

                if (int.Parse(row["mem_failcount"].ToString()) >= 4) 
                {
                    UI_LoginGuide.text = "계정이 잠겼습니다. 최고관리자에게 문의하여 임시비밀번호를 발급받으세요.";
                    //계정 잠금 업데이트
                    serverManager.GetMemberStatusDataUpdate(userID);
                }
                else if (row["mem_failcount"].ToString() == "0")
                {
                    //실패 카운트 업데이트
                    serverManager.GetMemberDataUpdate(userID, int.Parse(row["mem_failcount"].ToString()));

                    UI_LoginGuide.text = "비밀번호가 일치하지 않습니다. 5회틀릴 경우 계정이 잠깁니다.";
                }
                else //1회 이상일때
                {
                    //실패 카운트 업데이트
                    serverManager.GetMemberDataUpdate(userID, int.Parse(row["mem_failcount"].ToString()));

                    UI_LoginGuide.text = $"비밀번호가 일치하지 않습니다. ({5 - int.Parse(row["mem_failcount"].ToString()) - 1}회 남음).";
                }
                UI_LoginGuide.gameObject.SetActive(true);
            }


            switch (row["mem_status"].ToString())
            {
                //case "0": UI_LoginGuide.text = "비밀번호 초기화된 상태입니다."; UI_LoginGuide.gameObject.SetActive(true);  break;
                case "2": UI_LoginGuide.text = "계정이 중지상태 입니다."; loginCheck = false; UI_LoginGuide.gameObject.SetActive(true); break;
                case "3": UI_LoginGuide.text = "계정이 잠겼습니다. 최고관리자에게 문의하여 임시비밀번호를 발급받으세요."; loginCheck = false; UI_LoginGuide.gameObject.SetActive(true); break;
            }

        }

        if(!IdCheck)
        {
            UI_LoginGuide.text = "아이디가 올바르지 않습니다. 다시 확인해주세요.";
            UI_LoginGuide.gameObject.SetActive(true);
        }

        //로그인 성공했을때 바로 넘어감
        if(loginCheck)
        {
            //접속 로그 추가
            mainManager.InsertConnectionLog();

            //비밀번호 실패 카운트 초기화
            serverManager.UpdateUserFailCount(mainManager.myData.mem_user_id);

            input_ID.text = "";
            input_PW.text = "";

            /*
            if (UI_LoginGuide.text == "비밀번호 초기화된 상태입니다.")
            {
                UI_LoginPWChange.SetActive(true);

                reset_password_btn_del_CPW.gameObject.SetActive(false);
                reset_password_btn_del_PW.gameObject.SetActive(false);
                reset_password_btn_del_PW2.gameObject.SetActive(false);

                reset_password_input_CPW.text = "";
                reset_password_input_PW.text = "";
                reset_password_input_PW2.text = "";
            }
            else
            {*/
                UI_DashBoard.SetActive(true);
                gameObject.SetActive(false);
                LoadDashBoard();

                //타이머 시작
                //mainManager.StartTimer();
            //}
        }
    }


    public void LoadDashBoard()
    {
        viewMain.gnbViewControl.Load_DashBoard();
    }

    public void SetLoginInfo(DataRow _row)
    {
        mainManager.myData = new MemberData(_row);

        //mainManager.myData.ToString();
        Text_myTitle.text = mainManager.myData.mem_auth_id;
        Text_myName.text = mainManager.myData.mem_name;

        Text_myName.GetComponent<RectTransform>().sizeDelta = new Vector2(Text_myName.GetComponent<RectTransform>().sizeDelta.x, 19);

        //Text_myID.GetComponent<RectTransform>().offsetMin = new Vector2(Text_myID.GetComponent<RectTransform>().offsetMin.x, -47.3f);

        if (mainManager.myData.mem_name.Length > 5)
        {
            string firstName = mainManager.myData.mem_name.Substring(0, 5);
            string secendName = mainManager.myData.mem_name.Substring(5);
            if(secendName.Length > 3)
            {
                secendName = mainManager.myData.mem_name.Substring(0, 3) + "...";
            }
            Text_myName.text = $"{firstName}\n{secendName}";

            Text_myName.GetComponent<RectTransform>().sizeDelta = new Vector2(Text_myName.GetComponent<RectTransform>().sizeDelta.x, 38);

            //Text_myID.GetComponent<RectTransform>().anchoredPosition = new Vector2(Text_myID.GetComponent<RectTransform>().anchoredPosition.x, -66.3f);
        }
        Text_myID.text = $"({mainManager.myData.mem_user_id})";

        // 권한에 따른 계정 관리 버튼 활성화 비활성화

        if( mainManager.myData.mem_auth_id == "일반관리자")
        {
            btn_resetPassword.gameObject.SetActive(false);
            btn_memberStop.gameObject.SetActive(false);
            btn_memberAdd.gameObject.SetActive(false);
        }
        else
        {
            // 최고 관리자 일때만 버튼 활성화
            btn_resetPassword.gameObject.SetActive(true);
            btn_memberStop.gameObject.SetActive(true);
            btn_memberAdd.gameObject.SetActive(true);
        }
    }
    
}
