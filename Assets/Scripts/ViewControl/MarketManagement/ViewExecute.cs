using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SFB;
using System.IO;
using System.Text.RegularExpressions;
using ExcelDataReader;
using System.Text;
using System.Data;
using System.Linq;
using System;
using UnityEngine.UI.ProceduralImage;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class ViewExecute : MonoBehaviour
{
    public string MenuName = "실시 · 등록";

    public MainManager mainManager;

    public ServerManager serverManager;

    public Item_Check item_Check;

    public GameObject item_Empty;

    public Transform contents;

    public Transform parent;

    public RectTransform[] viewupdate;

    // UI
    public RectTransform menuContent;
    public RectTransform searchContent;

    public Vector2 openSize;
    public Vector2 closeSize;

    public GameObject[] UI_Search;

    public bool isMenuOpen;

    public Button btn_menuOpen;
    public Button btn_menuClose;

    public GameObject UI_Fileitem;

    // 점검 여부 처리 
    public GameObject UI_DFCheck_Popup;

    public string[] checkCode = {"", "Z", "N", "Y"};

    // 점검 Toggle
    public ToggleGroup DFCheckGroup;

    public Button btn_DFCheckCancel;
    public Button btn_DFCheckOK;

    // 부정청약 결과 처리
    public GameObject UI_DFResult_Popup;
    public ToggleGroup DFResultGroup;

    // 기타 의견
    public TMP_InputField input_ResultOthers;
    public Button btn_DFResultCancel;
    public Button btn_DFResultOK;

    // Text
    public TextMeshProUGUI Text_FileName;

    // 전체항목
    public TextMeshProUGUI Text_totalCount;

    // 미점검
    public TextMeshProUGUI Text_checkCount;

    // DropDown
    public TMP_Dropdown dropdown_check;
    public TMP_Dropdown dropdown_count;

    public bool ignoreValueChanged;

    // 찾아보기
    public Button btn_FileLoad;

    // 
    public Button btn_DeleteFile;

    // 점검여부 처리 
    public Button btn_checkProcess;

    // 부정청약결과 처리
    public Button btn_checkResultProcess;

    // 출력
    public Button btn_Print;

    // 초기화
    public Button btn_Reset;

    // 조회
    public Button btn_Search;

    // 엑셀 다운로드

    // 결과저장
    public Button btn_ResultSave;

    // 삭제
    public Button btn_Delete;

    // 전체선택
    public Button btn_AllSelect;
    public Image btn_On;

    public string _path;
    public string _savePath = @"C:\korea_rebs\SendResult.csv";

    public string filename;

    public DataTable OpenTable;
    public DataTable CheckTable;
    public DataTable listTable;
    public DataTable resultTable;

    bool[] _dropdownType = new bool[4];//검색구분
    bool[] BustType = new bool[3];//적발내용
    bool[] RfxgType = new bool[3];//rfxg 타입
    bool[] PrecentType = new bool[4];//위험도 퍼센트
    bool[] CityMenu = new bool[18];
    float[] percent = { 1, 0.5f, 0.25f, 0.1f }; //세대수 퍼센트
    int index = 0;


    //검색 조건
    public SelectMenu selectMenu;

    // 검색 구분
    public string[] searchMode = { "*", "apt_name", "apt_number", "apt_sub_pos" };

    public int selectSearchMode = 0;

    public Toggle[] toggle_searchDate;
    public bool[] searchDates;

    [SerializeField]
    List<Item_Check> list_Check = new List<Item_Check>();

    [SerializeField]
    public List<Toggle> list_Toggles = new List<Toggle>();


    public string selectCheck = "";
    public string selectCheckResult = "";
    
    public int listCount;
    public int listMode;

    public int[] listCounts = { 10, 20, 50, 100 };

    public int selectrowCount;

    public List<SupportType> supportTypes;

    // 전체항목
    public int totalCount;

    // 미점검
    public int unCheckCount;

    // 해당없음
    public int checkPassCount;

    // 점검완료
    public int checkCount;

    // 엑셀 다운로드
    public Button btn_Excel;

    public string[] col = { };
    public string[] colname = { "apt_number", "apt_type", "apt_name", "apt_dong", "apt_hosu", "apt_sup_type", "apt_sp_sup", "apt_sup_pos", "apt_size", "apt_gen_sup", "apt_sp_sup1", "apt_sup_amount",
        "user_birth", "user_name", "user_age", "user_relationship", "user_admin_change", "user_change_date", "user_spouse", "user_sp_note", "user_admin_address", "user_addr_match","user_addr_dup",
        "user_sp_simul", "user_sub_num", "user_housemember", "user_hp_housemember", "user_phone_dup", "user_ip_dub_win", "user_ip_dub_app", "user_phone", "user_ip_1",  "user_ip_2", "user_ip_3",
        "user_ip_4", "ad_ip_red_2", "ad_ip_red_3", "ad_ip_red_4", "receipt_date", "receipt_time", "receipt_medium", "app_residence_class", "win_residence_class", "residenct_area", "app_address",
        "app_detail_address", "addpoint_app_status", "addpoint_win_status", "addpoint_sum", "dependent_number", "save_sub_period", "homeless_period", "home_owner", "long_soldier", "i_nh_nohome_period",
        "j_save_type", "sub_open_date", "sub_pay_number", "sub_pay_amount", "sub_expiry_period", "multichild_number", "multichild_homeless_period", "multichild_residenct_period", "multichild_subsave_period",
        "multichild_totalscore", "multichild_minor_number", "new_public_act", "new_revision_date", "new_income_sep", "new_app_rank", "new_underaged_number", "new_fetus_number",
        "new_income_ratio", "new_income_category", "ad_new_income_sep", "new_avgincome_allo", "new_minor_childscore", "new_minor_childnumber", "new_residence_allo", "new_residence_period",
        "new_marriageperiod_allo", "new_marriage_period", "new_singleparentchildage_allo", "new_singleparentchildage", "new_constructionsaveperiod_allo", "new_construction_saveperiod", "new_totalscore",
        "user_recom_type", "sub_rewinlimit_status", "sub_marrysingle_status", "householder_status", "last5year_win_status", "homeless_number", "income_base", "incometax_income_base",
        "gen_compose", "homeless_householder", "base_assets", "elderlyparent_status", "minorchild_threemore", "marriage_sevenyear", "ad_addressmatch_status", "inspection_status",
        "unauth_sub_decision","unauth_sub_type", "unauth_regidence_maintain", "unauth_address_maintain", "unauth_disguise_transfer", "unauth_forgery_qualifi", "unauth_savecerti_sale",
        "unauth_disguise_divorce", "unauth_other", "rec_anno_date", "win_anno_date", "limittransfer_date", "first_requirement", "total_suphouse_number", "saleprice_cap_status", "overcrow_area_status", "first_internet_date",
        "second_internet_date", "ad_change_date", "ad_total_supply", "ad_namebirth_dup", "ad_namebirthphone_dup", "ad_administrative_change", "ad_reception_time", "ad_appwinresimatch_status", "ad_dependent_number", "ad_save_period",
        "ad_homeless_period", "ad_subpay_number", "ad_subpay_amount", "ad_sub_period", "ad_app_type", "ad_multichild_infantnumber", "ad_multichildhomeless_period", "ad_multichild_residenceperiod", "ad_multichild_residencesavesubperiod", "ad_multichild_totalscore",
        "ad_multichildminor_number", "ad_newminor_number", "ad_newfetus_number", "ad_minor_number", "ad_total_points", "ad_changetime2",
        "unauth_rf_prob", "unauth_rf_per50", "unauth_rf_per25", "unauth_rf_per10", "unauth_xgb_prob","unauth_xgb_per50", "unauth_xgb_per25","unauth_xgb_per10",
        "fake_rf_prob", "fake_rf_per50", "fake_rf_per25", "fake_rf_per10", "fake_xgb_prob", "fake_xgb_per50", "fake_xgb_per25", "fake_xgb_per10",
        "sale_rf_prob", "sale_rf_per50", "sale_rf_per25", "sale_rf_per10", "sale_xgb_prob" ,"sale_xgb_per50", "sale_xgb_per25", "sale_xgb_per10", "inspection_status_result"};

    private string[] korcolname = {"data_no","주택관리번호", "민영국민", "주택명", "동", "호수", "공급유형", "특별공급종류", "공급위치_시군구코드", "크기", "일반공급", "특별공급", "공급금액", "생년", "성명",
"연령","세대주관계", "행정변경", "변경일자", "배우자", "특이사항", "행정주소", "주소일치여부_부동산원", "주소중복횟수", "특일동시여부", "2년청약건수", "세대원수", "분리세대원수", "폰중복횟수_부동산원",
"IP중복당첨횟수_부동산원", "IP중복신청횟수_부동산원", "폰번호", "IP_1", "IP_2", "IP_3", "IP_4", "ad_IP중복_2자리", "ad_IP중복_3자리", "ad_IP중복_4자리", "접수일자", "접수시간", "접수매체", "신청거주구분",
"당첨거주구분", "거주지역", "신청주소", "상세신청주소", "가점신청여부", "가점당첨여부", "가점합계", "부양가족수", "저축가입기간", "무주택기간", "주택소유구분", "장기복무군인", "I_국민주택무주택기간",
"J_저축종류","청약통장개설일자",  "청약납부회차", "청약납부금액", "청약경과기간", "다자녀_영유아자녀수", "다자녀_무주택기간", "다자녀_해당지역거주기간", "다자녀_입주자저축가입기간", "다자녀_총점", "다자녀_미성년자녀수",
"신혼_공특법", "신혼_개정시행일자", "신혼미공특_소득구분",  "신혼미공특_신청순위", "신혼미공특_미성년자녀수", "신혼미공특_태아수", "신혼미공특_소득비율", "신혼미공특_소득구분명", "ad_신혼미공특소득구분", "신혼공특_월평균소득배점",
"신혼공특_미성년자녀수배점", "신혼공특_미성년자녀수", "신혼공특_해당지역거주기간배점", "신혼공특_해당지역거주기간",  "신혼공특_혼인기간배점","신혼공특_혼인기간",  "신혼공특_한부모가족자녀나이배점",
"신혼공특_한부모가족자녀나이", "신혼공특_입주자저축가입기간배점", "신혼공특_입주자저축가입기간", "신혼공특_총점","기관추천종류", "청약_재당첨제한여부", "청약_혼인및한부모가족", "세대주여부",
"과거5년이내당첨", "무주택세대구성원", "소득기준", "소득(소득세 5개년도 납부)기준", "세대구성", "무주택세대주", "자산기준", "노부모부양", "3명이상미성년자녀", "혼인기간7년이내", "ad_주소일치여부","검사여부",
"부정청약판정", "부정청약유형", "부정_거주지유지", "부정_주소지유지", "부정_위장전입", "부정_자격위조", "부정_입주자저축증서매매", "부정_위장이혼", "부정_기타", "모집공고일", "당첨자발표일", "해당전입제한일",
"1순위요건", "총공급가구수", "분양가상한제여부", "과밀성장권역여부", "1순위인터넷접수일", "2순위인터넷접수일시", "ad_변경일자", "ad_총공급", "ad_성명생년중복", "ad_성명생년전화중복", "ad_행정변경시점",
"ad_접수시간", "ad_신청당첨거주일치여부", "ad_부양가족수", "ad_저축가입기간", "ad_무주택기간", "ad_청약납부회차", "ad_청약납부금액", "ad_청약경과기간", "ad_신청유형", "ad_다자녀영유아자녀수",
"ad_다자녀무주택기간", "ad_다자녀해당지역거주기간", "ad_다자녀입주자저축가입기간", "ad_다자녀총점", "ad_다자녀미성년자녀수", "ad_신혼미공특미성년자녀수", "ad_신혼미공특태아수", "ad_미성년자녀수",
"ad_총점", "ad_변경시점2", "부정청약_rf_prob", "부정청약_rf_0.5",  "부정청약_rf_0.25", "부정청약_rf_0.1", "부정청약_xgb_prob", "부정청약_xgb_0.5", "부정청약_xgb_0.25", "부정청약_xgb_0.1","위장전입_rf_prob",
"위장전입_rf_0.5", "위장전입_rf_0.25", "위장전입_rf_0.1", "위장전입_xgb_prob", "위장전입_xgb_0.5", "위장전입_xgb_0.25", "위장전입_xgb_0.1", "매매_rf_prob", "매매_rf_0.5", "매매_rf_0.25","매매_rf_0.1",
"매매_xgb_prob", "매매_xgb_0.5", "매매_xgb_0.25", "매매_xgb_0.1", "inspection_status_result"};

    public string currentTime = "";
    public bool isSelect;
    public bool isAll;
    public bool isFirstOpen = true;

    // 정렬 버튼 
    public SortButton[] sortButtons;

    public Sprite Img_ASC; // 오름차순 이미지
    public Sprite Img_DESC; // 내림 차순 이미지

    public string SortTarget; // 정렬 기준

    int intValue = 0;
    double doubleValue = 0;

    // 페이지

    // 페이지별 아이템 수
    public int itemsPerPage = 10;
    public List<GameObject> itemUIList; // 아이템을 표시할 UI 요소 리스트

    public Button prevButton; // 이전 10 페이지로 이동하는 버튼
    public Button nextButton; // 다음 10 페이지로 이동하는 버튼

    public List<Button> pageButtons; // 페이지 번호를 나타내는 버튼 리스트

    public Button firstPageButton; // 첫 페이지로 이동하는 버튼
    public Button lastPageButton; // 마지막 페이지로 이동하는 버튼

    public int currentPage = 1; // 현재 페이지
    public RectTransform pageContents;

    // 출력부분
    public GameObject printCanvas;
    public GameObject printCamBox;

    public ScreenShot[] printCamera;

    public Item_CheckPrint[] printItems;

    public GameObject[] menuTitles;

    // 점검 결과에서 기타항목의 내용은 부정청약유형에 

    // 세부 유형 타입

    //inspection_status_result

    [SerializeField]
    public ExecuteData EXD = new ExecuteData();


    private void Awake()
    {
        selectMenu = gameObject.GetComponent<SelectMenu>();//메뉴 조건

        savePath = Application.streamingAssetsPath + "/lastdata.json";

    }

    // Start is called before the first frame update
    void Start()
    {
        currentPage = 1;

        listCount = 10;
        itemsPerPage = listCount;

        totalCount = 0;
        unCheckCount = 0;
        checkPassCount = 0;
        checkCount = 0;

        dropdown_check.value = 0;
        dropdown_count.value = 0;

        for (int i = 0; i < selectMenu.cityToggles.Count; i++)
        {
            selectMenu.cityToggles[i].toggle.onValueChanged.AddListener(SearchButtonActive);//기간 토글 클릭시 이벤트
        }
        // 페이지 리스너 등록
        prevButton.onClick.AddListener(Previous10Pages);
        nextButton.onClick.AddListener(Next10Pages);
        firstPageButton.onClick.AddListener(GoToFirstPage);
        lastPageButton.onClick.AddListener(GoToLastPage);

        for (int i = 0; i < pageButtons.Count; i++)
        {
            int pageNumber = ((currentPage - 1) / 10) * 10 + i + 1;
            pageButtons[i].onClick.AddListener(() => GoToPage(pageNumber));
        }

        //Reset();

        // 메뉴 펼치기
        btn_menuOpen.onClick.AddListener(Action_Open);

        // 메뉴 접기
        btn_menuClose.onClick.AddListener(Action_Close);

        // Init();

        // 찾아보기
        btn_FileLoad.onClick.AddListener(Action_FileLoad);

        btn_DeleteFile.onClick.AddListener(Action_DeleteFileItem);

        // 조회
        btn_Search.onClick.AddListener(Action_Search);

        // 초기화
        btn_Reset.onClick.AddListener(Action_Reset);

        // 출력
        btn_Print.onClick.AddListener(Action_Print);

        // 전체 선택
        btn_AllSelect.onClick.AddListener(All_Select);

        // 부정청약점검여부 선택
        btn_checkProcess.onClick.AddListener(Open_CheckPopup);

        // 부정청약 결과 처리
        btn_checkResultProcess.onClick.AddListener(Open_CheckResultPopup);

        // 결과 저장
        btn_ResultSave.onClick.AddListener(Action_SaveResult);

        // 부정청약 점검 처리

        // 엑셀 다운로드
        btn_Excel.onClick.AddListener(Action_Excel);

        //점검 OK
        btn_DFCheckOK.onClick.AddListener(Set_DFCheck);

        // 점검 여부 토글 
        foreach (Toggle toggle in DFCheckGroup.GetComponentsInChildren<Toggle>())
        {
            toggle.onValueChanged.AddListener(OnCheckToggleValueChanged);
        }
        
        // 점검 Cancel
        btn_DFCheckCancel.onClick.AddListener(Close_CheckPopup);

        // 부정청약 점검 결과 처리 

        // 결과처리 토글 
        foreach (Toggle toggle in DFResultGroup.GetComponentsInChildren<Toggle>())
        {
            toggle.onValueChanged.AddListener(OnCheckResultToggleValueChanged);
        }

        // 점검 결과 OK
        btn_DFResultOK.onClick.AddListener(Set_DFCheckResult);

        // 점검 Cancel
        btn_DFResultCancel.onClick.AddListener(Close_CheckResultPopup);

        // 항목 삭제
        btn_Delete.onClick.AddListener(Click_Delete);

    }

    public void Click_Delete()
    {
        CommonPopup commonPopup = Instantiate(mainManager.commonPopup, mainManager.contents);

        commonPopup.ShowPopup_TwoButton(
        "선택한 항목을 삭체 처리 하시겠습니까?.",
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

            if (resultTable != null)
            {
                Action_Delete();
            }
        });
    }
    public async void Action_Delete()
    {
        mainManager.viewLoading.Open();

        await Delete_Data();

        //string names = Path.GetFileNameWithoutExtension(_path);

        //mainManager.ChangeLogInsert(MenuName, $"{names} 결과 저장");

        mainManager.viewLoading.Close();

        //ResetButton(true);
        DeleteReset();

        if (_path.Length > 0)
        {
            // Action_Check();

            // Debug.Log("OpenTable : " + OpenTable.Rows.Count);

            if (OpenTable == null)
            {
                Debug.Log("OpenTable is null");

                Action_Check();
            }
            else
            {
                Debug.Log("OpenTable : " + OpenTable.Rows.Count);

                // 테이블이 이미 있는 경우에는
                Action_ServerLoad();
            }

        }
        else
        {
            // 파일이 없는경우에는 서버에 있는 데이터만 조회해서 보여준다
            Action_ReLoad();
        }
        
    }

    //삭제
    public async Task Delete_Data()
    {
        // 

        DataRow[] deleteRows = resultTable.Select("isSelect = true");

        Debug.Log("Delete_Data Count : " + deleteRows.Length);


        if (deleteRows.Length > 0)
        {
            foreach (DataRow rows in deleteRows)
            {
                //Debug.Log("rows.0 : " + rows["data_no"].ToString());

                await Task.Delay(1);

                serverManager.DeleteRiskData("tb_riskdata", "data_no", rows["data_no"].ToString());
            }
        }
    }

    public void SearchButtonActive(bool check)
    {
        selectMenu.SeacrhButtonActiveCheck(selectMenu.cityToggles, btn_Search);
    }

    // 결과 저장

    public void Action_SaveResult()
    {
        var vcount = resultTable.Select("isSelect = True");

        if (vcount.Length > 0)
        {
            CommonPopup commonPopup = Instantiate(mainManager.commonPopup, mainManager.contents);

            commonPopup.ShowPopup_TwoButton(
            "모든 점검여부 및 부정쳥약 결과 처리가 완료되었습니다.\n" +
            "데이터를 서버에 저장 하시겠습니까?",
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

                if (resultTable != null)
                {
                    Action_ResultSave();
                }
            });
        }
        else
        {
            CommonErrorPopup commonErrorPopup = Instantiate(mainManager.commonErrorPopup, mainManager.contents);

            commonErrorPopup.ShowPopup_OneButton(
            "선택한 항목이 없습니다.",
            "확인",
            () =>
            {
                commonErrorPopup.CloseDestroy();
            }
            );
        }

    }


    // 점검여부

    // 점검여부를 저장한다
    public void Set_DFCheck()
    {
        Debug.Log("Set_DFCheck");
        Debug.Log("selectCheck : " + selectCheck);

        // 선택한 행만 가져온다

        DataRow[] rows = resultTable.Select("isSelect = True");
        Debug.Log("selectCheck rows Count : " + rows.Length);
        string cellValue = "";

        if (rows.Length > 0)
        {
            foreach (DataRow dataRow in rows)
            {
                //cellValue = dataRow["inspection_status"].ToString();
                //Debug.Log($"검사여부 : {cellValue}");
                //cellValue = dataRow["inspection_status_result"].ToString();
                //Debug.Log($"점검여부 : {cellValue}");
                //cellValue = dataRow["점검여부"].ToString();
                //Debug.Log($"점검여부2 : {cellValue}");

                // 점검 여부에선 점검을 했냐 안했냐만 판단한다
                if (selectCheck == "Y")
                {
                    // 점검 완료
                    // inspection_status = 1, ispection_status_result = Y", 
                    dataRow["inspection_status"] = 1;
                    dataRow["inspection_status_result"] = "Y";

                    
                    dataRow["점검여부"] = "Y";
                }
                else if(selectCheck == "Z")
                {
                    // 해당없음
                    dataRow["inspection_status"] = 0;
                    dataRow["inspection_status_result"] = "Z";


                    dataRow["점검여부"] = "Z";
                }
                else if(selectCheck == "N")
                {
                    // 미처리
                    dataRow["inspection_status"] = 0;
                    dataRow["inspection_status_result"] = "N";


                    dataRow["점검여부"] = "N";
                }

                // dataRow["isChange"] = "true";

                //cellValue = dataRow["inspection_status"].ToString();
                //Debug.Log($"검사여부 END : {cellValue}");
                //cellValue = dataRow["inspection_status_result"].ToString();
                //Debug.Log($"점검여부 END: {cellValue}");
                //cellValue = dataRow["점검여부"].ToString();
                //Debug.Log($"점검여부2 END: {cellValue}");
            }
        }

        UI_DFCheck_Popup.SetActive(false);

        // 점검여부 드롭메뉴 초기화 안함
        //ignoreValueChanged = true;

        //dropdown_check.value = 0;

        //ignoreValueChanged = false;

        // UnSelect();

        // 리스트 새로 그리기

        UpdateUI();

        Debug.Log("AreAllResultPass() : " + AreAllResultPass());

        if (AreAllStatusY())
        {
            // 선택한것이 모두 점검 완료일 경우엔
            btn_checkResultProcess.interactable = true;
        }
        else
        {
            btn_checkResultProcess.interactable = false;
        }

    }

    private void OnCheckToggleValueChanged(bool isOn)
    {
        if (isOn)
        {
            Toggle selectedToggle = DFCheckGroup.ActiveToggles().FirstOrDefault();

            if (selectedToggle != null)
            {
                // 선택된 토글을 사용합니다.
                // 원하는 작업을 수행하세요.
                string labelText = selectedToggle.GetComponentInChildren<TextMeshProUGUI>().text;

                Debug.Log("선택된 토글: " + selectedToggle.name);
                Debug.Log("선택된 토글의 라벨 텍스트: " + labelText);

                // 미점검 N, 점검완료 Y,  해당없음 Z
                if(labelText == "미점검")
                {
                    selectCheck = "N";
                }
                else if(labelText == "점검완료")
                {
                    selectCheck = "Y";
                }
                else if(labelText == "해당없음")
                {
                    selectCheck = "Z";
                }
                else
                {

                }
            }
        }
    }

    // 점검여부 팝업 열기
    public void Open_CheckPopup()
    { 
        UI_DFCheck_Popup.SetActive(true);

        // ToggleGroup의 첫 번째 토글 가져오기
        Toggle firstToggle = DFCheckGroup.GetComponentsInChildren<Toggle>(true)[0];

        // 첫 번째 토글의 isOn 속성을 true로 설정
        firstToggle.isOn = true;

        selectCheck = "N";
        //DFCheckGroup.ActiveToggles().First().isOn = true;

        input_ResultOthers.text = "";
    }

    // 점검여부 팝업 닫기
    public void Close_CheckPopup()
    {
        //Debug.Log("Close_CheckPopup");

        // ToggleGroup의 첫 번째 토글 가져오기
        Toggle firstToggle = DFCheckGroup.GetComponentsInChildren<Toggle>(true)[0];

        // 첫 번째 토글의 isOn 속성을 true로 설정
        firstToggle.isOn = true;

        //DFCheckGroup.ActiveToggles().First().isOn = true;
        input_ResultOthers.text = "";

        UI_DFCheck_Popup.SetActive(false);
    }


    public void Set_DFCheckResult()
    {
        // 점검 결과를 저장한다
        Debug.Log("Set_DFCheckResult");

        DataRow[] rows = resultTable.Select("isSelect = True");

        if (rows.Length > 0)
        {
            foreach (DataRow dataRow in rows)
            {
                // 통과, 위장전입, 통장매매, 기타(15자)

                if (selectCheckResult == "통과")
                {
                    // 부정청약판정
                    dataRow["unauth_sub_decision"] = 0;
                    dataRow["부정청약결과"] = "통과";

                    // 부정청약유형
                    // 부정청약유형을 빈값으로 변경한다
                    dataRow["unauth_sub_type"] = "무";

                    // 부정_위장전입 = 0
                    dataRow["unauth_disguise_transfer"] = 0;

                    // 부정_통장매매 = 0
                    dataRow["unauth_savecerti_sale"] = 0;

                    // 기타 = 0
                    dataRow["unauth_other"] = 0;
                }
                else if (selectCheckResult == "위장전입")
                {
                    // 부정청약판정
                    dataRow["unauth_sub_decision"] = 1;
                    dataRow["부정청약결과"] = "위장전입";

                    // 부정청약유형
                    dataRow["unauth_sub_type"] = selectCheckResult;

                    // 부정_위장전입
                    dataRow["unauth_disguise_transfer"] = 1;
                    
                    // 부정_통장매매 = 0
                    dataRow["unauth_savecerti_sale"] = 0;

                    // 기타 = 0
                    dataRow["unauth_other"] = 0;
                }
                else if (selectCheckResult == "통장매매")
                {
                    // 부정청약판정
                    dataRow["unauth_sub_decision"] = 1;
                    dataRow["부정청약결과"] = "통장매매";

                    // 부정청약유형
                    dataRow["unauth_sub_type"] = selectCheckResult;

                    // 부정_통장매매
                    dataRow["unauth_savecerti_sale"] = 1;

                    // 부정_위장전입 = 0
                    dataRow["unauth_disguise_transfer"] = 0;

                    // 기타 = 0
                    dataRow["unauth_other"] = 0;
                }
                else if (selectCheckResult == "기타")
                {
                    // 부정청약판정
                    dataRow["unauth_sub_decision"] = 1;
                    dataRow["부정청약결과"] = input_ResultOthers.text.Trim();

                    string comment = input_ResultOthers.text.Trim();

                    if (!string.IsNullOrEmpty(comment) && comment.Length > 0)
                    {
                        // 빈값이이 아니고
                        // 부정청약유형
                        dataRow["unauth_sub_type"] = comment;
                    }
                    else
                    {
                        comment = "무";
                        // 부정청약유형
                        dataRow["unauth_sub_type"] = comment;
                    }
                    
                    // 기타
                    dataRow["unauth_other"] = 1;

                    // 부정_위장전입 = 0
                    dataRow["unauth_disguise_transfer"] = 0;

                    // 부정_통장매매 = 0
                    dataRow["unauth_savecerti_sale"] = 0;
                }

                //dataRow["isChange"] = "true";
            }
        }

        UI_DFResult_Popup.SetActive(false);

        //ignoreValueChanged = true;

        //dropdown_check.value = 0;

        //ignoreValueChanged = false;

        //UnSelect();

        // 리스트 새로 그리기
        UpdateUI();
        
        // 전부 통과면 활성화 중에 적발이 있으면 비활성화
        if(AreAllResultPass())
        {
            btn_checkProcess.interactable = true;
        }
        else
        {
            btn_checkProcess.interactable = false;
        }
    }

    private void OnCheckResultToggleValueChanged(bool isOn)
    {
        if (isOn)
        {
            Toggle selectedToggle = DFResultGroup.ActiveToggles().FirstOrDefault();

            if (selectedToggle != null)
            {
                // 선택된 토글을 사용합니다.
                // 원하는 작업을 수행하세요.
                string labelText = selectedToggle.GetComponentInChildren<TextMeshProUGUI>().text;

                Debug.Log("선택된 토글: " + selectedToggle.name);
                Debug.Log("선택된 토글의 라벨 텍스트: " + labelText);

                // 통과, 위장전입, 통장매매, 기타
                selectCheckResult = labelText;
            }
        }
    }

    // 점검결과처리 팝업 열기
    public void Open_CheckResultPopup()
    {
        UI_DFResult_Popup.SetActive(true);

        // ToggleGroup의 첫 번째 토글 가져오기
        Toggle firstToggle = DFResultGroup.GetComponentsInChildren<Toggle>(true)[0];

        // 첫 번째 토글의 isOn 속성을 true로 설정
        firstToggle.isOn = true;
        
        selectCheckResult = "통과";


    }

    // 점검결과처리 팝업 닫기
    public void Close_CheckResultPopup()
    {
        //Debug.Log("Close_CheckResultPopup");

        // ToggleGroup의 첫 번째 토글 가져오기
        Toggle firstToggle = DFResultGroup.GetComponentsInChildren<Toggle>(true)[0];

        // 첫 번째 토글의 isOn 속성을 true로 설정
        firstToggle.isOn = true;

        //DFResultGroup.ActiveToggles().First().isOn = true;
        input_ResultOthers.text = "";

        UI_DFResult_Popup.SetActive(false);
    }

    public void Action_Excel()
    {
        if(resultTable == null)
        {
            return;
        }

        //string[] cols = new string[CheckTable.Columns.Count];

        string[] fileTypes = new string[] { "CSV Files", "csv" };

        StandaloneFileBrowser.SaveFilePanelAsync("엑셀 다운로드", "", "", "csv", (string path) =>
        {
            if (!string.IsNullOrEmpty(path))
            {
                // 파일 저장 로직을 구현하세요
                // 예를 들어, 파일을 생성하고 내용을 저장할 수 있습니다.
                // File.WriteAllText(path, "Hello, World!");
                DataTable saveTable = resultTable.Copy();

                string[] columnsToRemove = { "isSelect", "NO", "data_idx", "create_date", "change_date"}; // 삭제할 칼럼들의 이름 배열

                foreach (string columnName in columnsToRemove)
                {
                    if (saveTable.Columns.Contains(columnName))
                    {
                        saveTable.Columns.Remove(columnName);
                    }
                }

                SaveDataTableToCsv(saveTable, path, korcolname);


                //엑셀 이력 추가
                Debug.Log("File saved at: " + path);

                if (!string.IsNullOrEmpty(_path))
                {
                    //string originalFileName = Path.GetFileNameWithoutExtension(_path); // 기존 파일명 가져오기
                    //mainManager.DownloadLodInsert(MenuName,  $"{selectMenu.dateText.Canlendar.text} {originalFileName} 엑셀다운로드");

                    string names = Path.GetFileNameWithoutExtension(path);
                    mainManager.DownloadLodInsert(MenuName, $"{names} 엑셀다운로드");

                }

            }
            else
            {
                Debug.Log("Save file operation cancelled.");
            }
        });
    }


    public void SaveDataTableToCsv(DataTable dataTable, string filePath, string[] columname)
    {
        // StreamWriter를 이용하여 CSV 파일 생성
        using (StreamWriter streamWriter = new StreamWriter(filePath, false, Encoding.GetEncoding("EUC-KR")))
        {
            /*List<string> headers = new List<string>();
            foreach (DataColumn column in dataTable.Columns)
            {
                headers.Add(column.ColumnName);
            }
            streamWriter.WriteLine(string.Join(",", headers));*/

            // 헤더 쓰기
            streamWriter.WriteLine(string.Join(",", columname));

            string cellValue = "";

            
            // 데이터 쓰기
            foreach (DataRow row in dataTable.Rows)
            {
                List<string> values = new List<string>();
                //Debug.Log("dataTable.Columns : " + dataTable.Columns.Count);

                
                for (int columnIndex = 0; columnIndex < columname.Length; columnIndex++)
                {
                    DataColumn column = dataTable.Columns[columnIndex];

                    //Debug.Log($"COLUMN columnIndex {columnIndex}: " + column.ColumnName);


                    if (row[column].ToString().Contains(","))
                    {
                        //Debug.Log("쉼표가 있어" + row[column].ToString());

                        row[column] = $"\"{row[column]}\"";
                    }

                    values.Add(row[column].ToString());
                }
                /*
                foreach (DataColumn column in dataTable.Columns)
                {
                    if (row[column].ToString().Contains(","))
                    {
                        //Debug.Log("쉼표가 있어" + row[column].ToString());

                        row[column] = $"\"{row[column]}\"";
                    }

                    values.Add(row[column].ToString());
                }
                */

                streamWriter.WriteLine(string.Join(",", values));
            }
        }
    }
    
    // 실시등록 저장
    public void SaveExecute()
    {
        CheckSearchDate(searchDates); // 검색기간

        selectMenu.CheckRfxgType(RfxgType); //rfxg
        selectMenu.CheckRfxgPerType(PrecentType); //퍼센트

        ExecuteData ED = new ExecuteData();
        //ED._path = _path;
        ED.SeacrhDropdown = selectMenu.SeacrhDropdown.value;
        ED.searchText = selectMenu.input_search.text;

        ED.fromDate = DateTime.ParseExact(selectMenu.dateText.FromDate.ToString("00000000"), "yyyyMMdd", null);

        ED.endDate = DateTime.ParseExact(selectMenu.dateText.ToDate.ToString("00000000"), "yyyyMMdd", null);

        ED.periodNum = Array.IndexOf<bool>(searchDates, true);
        ED.toggleprobNum = Array.IndexOf<bool>(RfxgType, true);
        ED.toggleRank = Array.IndexOf<bool>(PrecentType, true);

        ED.cityToggle = selectMenu.CheckCityToggle(selectMenu.cityToggles);

        string jsonData = JsonConvert.SerializeObject(ED);

        // 파일 저장
        File.WriteAllText(savePath, jsonData);
    }

    public void ErrorLoadPopup()
    {
        // 저장된 파일이 없습니다. 다음 경로에 파일을 확인해 주세요.

        string originalFileName = Path.GetFileNameWithoutExtension(_path); // 선택 파일명 가져오기
        string extension = Path.GetExtension(_path); // 확장자 가져오기

        filename = "";

        Text_FileName.text = filename;

        UI_Fileitem.SetActive(false);

        CommonErrorPopup commonErrorPopup = Instantiate(mainManager.commonErrorPopup, mainManager.contents);

        commonErrorPopup.ShowPopup_OneButton(
            "불러올 파일이 없습니다.",
            "확인",
            () =>
            {
                commonErrorPopup.CloseDestroy();
            }
        );
    }

    // 실시등록 저장파일 로드
    public void LoadExecute()
    {
       
        string jsonData = File.ReadAllText(savePath);

        EXD = JsonConvert.DeserializeObject<ExecuteData>(jsonData);

        // 검색 구분 호출
        selectMenu.SeacrhDropdown.value = EXD.SeacrhDropdown;
        selectMenu.input_search.text = EXD.searchText;

        // 검색기간 설정은 1년으로 한다

        selectMenu.periodToggle[1].isOn = true;
        selectMenu.PriodChange(selectMenu.periodToggle[1], 1);

        /*
        if (EXD.periodNum == -1)
        {
            // 선택된게 없으니 기간 호출인경우이므로
            selectMenu.dateText.SetCalenderUpdate(EXD.fromDate, EXD.endDate);
        }
        else
        {
            selectMenu.periodToggle[EXD.periodNum].isOn = true;
            selectMenu.PriodChange(selectMenu.periodToggle[EXD.periodNum], EXD.periodNum);
        }*/

        // RFXG
        selectMenu.toggle_prob[EXD.toggleprobNum].isOn = true;

        // Rank
        selectMenu.toggle_rank[EXD.toggleRank].isOn = true;


        // City
        selectMenu.cityToggles[0].toggle.isOn = EXD.cityToggle[0];
        for (int i = 1; i < selectMenu.cityToggles.Count; i++)
        {
            selectMenu.cityToggles[i].toggle.isOn = EXD.cityToggle[i];
        }

        Action_ReLoad();

        /*
        _path = EXD._path;

        if(_path.Length > 0)
        {
            string originalFileName = Path.GetFileNameWithoutExtension(_path); // 선택 파일명 가져오기
            string extension = Path.GetExtension(_path); // 확장자 가져오기

            // filename = $"{originalFileName}_위험도산출{extension}";
            filename = $"{originalFileName}{extension}";

            Text_FileName.text = filename;

            UI_Fileitem.SetActive(true);

            // 검색 구분 호출
            selectMenu.SeacrhDropdown.value = EXD.SeacrhDropdown;
            selectMenu.input_search.text = EXD.searchText;

            // 검색기간 설정

            if (EXD.periodNum == -1)
            {
                // 선택된게 없으니 기간 호출인경우이므로

                selectMenu.dateText.SetCalenderUpdate(EXD.fromDate, EXD.endDate);
            }
            else
            {
                selectMenu.periodToggle[EXD.periodNum].isOn = true;
                selectMenu.PriodChange(selectMenu.periodToggle[EXD.periodNum], EXD.periodNum);
            }

            // RFXG
            selectMenu.toggle_prob[EXD.toggleprobNum].isOn = true;

            // Rank
            selectMenu.toggle_rank[EXD.toggleRank].isOn = true;


            // City
            selectMenu.cityToggles[0].toggle.isOn = EXD.cityToggle[0];
            for (int i = 1; i < selectMenu.cityToggles.Count; i++)
            {
                selectMenu.cityToggles[i].toggle.isOn = EXD.cityToggle[i];
            }


            if (CheckTable != null)
            {
                CheckTable.Clear();
                CheckTable = null;
            }

            if (resultTable != null)
            {
                resultTable.Clear();
                resultTable = null;
            }

            if (OpenTable != null)
            {
                OpenTable.Clear();
                OpenTable = null;
            }


            Action_Check();
        }
        else
        {
            Action_ReLoad();
        }
        */


    }

    // 실시등록 기록 삭제 
    public void DeleteExecute()
    {
        Debug.Log("DeleteExecute");

        // 파일이 존재하는지 확인 후 삭제
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log("File deleted: " + savePath);
        }
        else
        {
            Debug.Log("File not found: " + savePath);
        }
    }

    public void Open()
    {
        // Open일때

        //Debug.Log("ViewExecute OPEN");
        //Debug.Log("ViewExecute OPEN " + isFirstOpen);

        if (isFirstOpen)
        {
            // 프로그램이 처음 켜진 경우에는 마지막 실시등록을 호출한다.
            //Debug.Log("ViewExecute File.Exists(savePath) " + File.Exists(savePath));

            // 실시등록 파일 체크
            if (File.Exists(savePath))
            {
                // 파일이 있는 경우에는 파일을 불러온다
                LoadExecute();

            }
            else
            {
                // 없는 경우에는 아무것도 안하쥬
                // ErrorLoadPopup();

                Action_ReLoad();
            }
            isFirstOpen = false;
        }
        else
        {
            // 아닐 경우엔 아무것도 안하쥬

            if (resultTable == null)
            {
                Debug.Log("reusltTable  NULL");
                Action_ReLoad();
            }
            else 
            {
                Debug.Log("reusltTable NOT NULL");
            }
        }

    }

    // 산출 결과 전송시 초기화    
    public void SendInit()
    {
        Debug.Log("<color=yellow>[ViewExecute][SendInit]</color>");

        Debug.Log("<color=yellow>Init _path : " + _path + "</color>");

        // 저장된 파일 기록 삭제
        DeleteExecute();

        Reset();

        ClearPrintList();
        
        ClearListView();

        /*
        if (!string.IsNullOrEmpty(_path))
        {
            string originalFileName = Path.GetFileNameWithoutExtension(_path); // 기존 파일명 가져오기
            string extension = Path.GetExtension(_path); // 확장자 가져오기

            filename = $"{originalFileName}{extension}";

            Text_FileName.text = filename;

            UI_Fileitem.SetActive(true);

            // 조회 버튼 활성화
            btn_Search.interactable = true;

            btn_Search.GetComponentInChildren<TextMeshProUGUI>().color = mainManager.OnTextColor;
        }
        */

        // 바로 조회 하게 한다?
        // Action_Check();
    }

    // start 초기화
    public void Init()
    {
        Debug.Log("<color=yellow>[ViewExecute][Init]</color>");

        Debug.Log("<color=yellow>Init _path : " + _path + "</color>");

        currentPage = 1;

        listCount = 10;
        itemsPerPage = listCount;

        totalCount = 0;
        unCheckCount = 0;
        checkPassCount = 0;
        checkCount = 0;

        dropdown_check.value = 0;
        dropdown_count.value = 0;

        if (!string.IsNullOrEmpty(_path))
        {
            string originalFileName = Path.GetFileNameWithoutExtension(_path); // 기존 파일명 가져오기
            string extension = Path.GetExtension(_path); // 확장자 가져오기

            filename = $"{originalFileName}{extension}";

            Text_FileName.text = filename;

            UI_Fileitem.SetActive(true);

            // 조회 버튼 활성화
            btn_Search.interactable = true;

            btn_Search.GetComponentInChildren<TextMeshProUGUI>().color = mainManager.OnTextColor;
        }
        else
        {
            Text_FileName.text = "";

            UI_Fileitem.SetActive(false);

            // 조회 버튼 활성화
            // btn_Search.interactable = false;
            // btn_Search.GetComponentInChildren<TextMeshProUGUI>().color = mainManager.OffTextColor;
        }

        Reset();

        ClearPrintList();
    }

    public void getCount()
    {
        
        unCheckCount = CountRowsWithCondition(resultTable, "inspection_status_result", "N");

        checkPassCount = CountRowsWithCondition(resultTable, "inspection_status_result", "Z");

        checkCount = CountRowsWithCondition(resultTable, "inspection_status_result", "Y");

        Text_totalCount.text = "전체항목(" + totalCount + ")";

        Text_checkCount.text = $"미점검(<style=accent>{unCheckCount}</style>)," +
                                $"해당없음({checkPassCount})," +
                                $"점검완료({checkCount})";

        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)Text_totalCount.gameObject.transform.parent);
    }

    public void Action_FileLoad()
    {
        var paths = StandaloneFileBrowser.OpenFilePanel("Load File", "", "csv", false);

        // WriteResult(paths);

        Action_FileItem(paths);
    }


    public void Action_FileItem(string[] paths)
    {
        if (paths.Length == 0)
        {
            return;
        }

        UnSelect();

        Reset();

        if (contents.childCount > 0)
        {
            ClearListView();
        }

        _path = "";

        _path = paths[0];
        string originalFileName = Path.GetFileNameWithoutExtension(_path); // 선택 파일명 가져오기
        string extension = Path.GetExtension(_path); // 확장자 가져오기

        // filename = $"{originalFileName}_위험도산출{extension}";
        filename = $"{originalFileName}{extension}";

        Text_FileName.text = filename;

        UI_Fileitem.SetActive(true);

        if (CheckTable != null)
        {
            CheckTable.Clear();            
            CheckTable = null;
        }

        if (resultTable != null)
        {
            resultTable.Clear();        
            resultTable = null;
        }

        if(OpenTable != null)
        {
            OpenTable.Clear();
            OpenTable = null;
        }

        // btn_Search.interactable = true;
        // btn_Search.GetComponentInChildren<TextMeshProUGUI>().color = mainManager.OnTextColor;
    }

    public void Action_DeleteFileItem()
    {
        _path = "";

        Text_FileName.text = "";

        UI_Fileitem.SetActive(false);
        
        // 파일 삭제시 조회버튼 비활성
        // btn_Search.interactable = false;
        // btn_Search.GetComponentInChildren<TextMeshProUGUI>().color = mainManager.OffTextColor;

        if (CheckTable != null)
        {            
            CheckTable = null;
        }

        if (resultTable != null)
        {
            resultTable = null;
        }


        if (OpenTable != null)
        {
            OpenTable = null;
        }

        if (contents.childCount > 0)
        {
            ClearListView();
        }

        Reset();


        UnSelect();

        ResetButton(true);
    }


    // 결과 저장
    async void Action_ResultSave()
    {
        mainManager.viewLoading.Open();

        // 산출 파일 저장용
        //string extension = Path.GetExtension(_savePath);
        //string sendPath = Path.Combine(Path.GetDirectoryName(_savePath), $"SendResult{extension}");

        Debug.Log("Save_Copy");

        DataTable saveTable = resultTable.Copy();

        string[] columnsToRemove = { "NO", "data_idx", "create_date", "change_date", "isChange" }; // 삭제할 칼럼들의 이름 배열

        foreach (string columnName in columnsToRemove)
        {
            if (saveTable.Columns.Contains(columnName))
            {
                saveTable.Columns.Remove(columnName);
            }
        }

        //DataRow[] changeRows = saveTable.Select("isChange = true");

        //Debug.Log("changeRows Count : " + changeRows.Length);

        for (int i = saveTable.Columns.Count - 2; i > 171; i--)
        {
            // Debug.Log("I : " + i);

            saveTable.Columns.RemoveAt(i);
        }


        DataRow[] changeRows = saveTable.Select("isSelect = true");
        
        //if(changeRows.Length <= 0)
        //{
        //    // return;
        //    CommonErrorPopup commonErrorPopup = Instantiate(mainManager.commonErrorPopup, mainManager.contents);

        //    commonErrorPopup.ShowPopup_OneButton(
        //    "선택한 항목이 없습니다.",
        //    "확인",
        //    () =>
        //    {
        //        commonErrorPopup.CloseDestroy();
        //    }
        //    );

        //    saveTable = null;
        //    mainManager.viewLoading.Close();
        //    return;
        //}

        SaveDataTableToCsv(changeRows.CopyToDataTable(), _savePath, korcolname);


        Debug.Log("전송시작");

        // 결과 저장 유니티
        //await Save_Data();

        if (File.Exists(_savePath))
        {
            mainManager.viewLoading.Open();

            mainManager.isPython = true;

            mainManager.loadMsg.text = "결과 저장 Processing... ";

            var result = await mainManager.RunProcessAsync("python", "SaveResult.py");

            Debug.Log("Result: " + result.ToString());

            mainManager.viewLoading.Close();

            mainManager.isPython = false;

            if (result == 0)
            {
                // 저장 성공시

                Debug.Log("전송끝");

                string names = Path.GetFileNameWithoutExtension(_path);

                mainManager.ChangeLogInsert(MenuName, $"{names} 결과 저장");

                UnSelectAll();

                UnSelect();

            }
            else
            {
                // 저장 실패 알림
            }

        }

        mainManager.viewLoading.Close();

    }

    // 결과 저장용
    public async Task Save_Data()
    {
        // CheckTable 전송

        //Debug.Log("CheckTable ROWS COUNT : " + CheckTable.Rows.Count);

        Debug.Log("Save_Copy");

        DataTable saveTable = resultTable.Copy();

        string[] columnsToRemove = { "NO", "data_idx", "create_date", "change_date", "isChange"}; // 삭제할 칼럼들의 이름 배열

        foreach (string columnName in columnsToRemove)
        {
            if (saveTable.Columns.Contains(columnName))
            {
                saveTable.Columns.Remove(columnName);
            }
        }

        //DataRow[] changeRows = saveTable.Select("isChange = true");

        //Debug.Log("changeRows Count : " + changeRows.Length);

        for (int i = saveTable.Columns.Count - 2; i > 171; i--)
        {
            // Debug.Log("I : " + i);

            saveTable.Columns.RemoveAt(i);
        }

        int saveCount = 0;
        
        Debug.Log("SA");

        DataRow[] changeRows = saveTable.Select("isSelect = true");

        Debug.Log("changeRows Count 2 : " + changeRows.Length);

        if (changeRows.Length > 0)
        {
            // 선택한 경우만 가져와서 저장시도
            foreach (DataRow rows in changeRows)
            {
                //Debug.Log("rows.0 : " + rows[0].ToString());
                //Debug.Log("rows.3 : " + rows[3].ToString());
                //Debug.Log("rows.4 : " + rows[4].ToString());
                //Debug.Log("rows.5 : " + rows[5].ToString());
                //Debug.Log("rows.6 : " + rows[6].ToString());
                //Debug.Log("rows.12 : " + rows[12].ToString());
                //Debug.Log("rows.13 : " + rows[13].ToString());

                await Task.Delay(1);

                serverManager.UpdateRiskData("tb_riskdata", colname, checkNames, rows.ItemArray);


                if(OpenTable != null)
                {
                    string userName = rows.Field<string>("user_name");
                    Int64 aptNumber = rows.Field<Int64>("apt_number");
                    string aptDong = rows.Field<string>("apt_dong");
                    string aptHosu = rows.Field<string>("apt_hosu");
                    Int64 aptSupType = rows.Field<Int64>("apt_sup_type");
                    Int64 aptSpSup = rows.Field<Int64>("apt_sp_sup");
                    Int64 userBirth = rows.Field<Int64>("user_birth");

                    //string rowString = $"UserName: {userName}, AptNumber: {aptNumber}, AptDong: {aptDong}, AptHosu: {aptHosu}, AptSupType: {aptSupType}, AptSpSup: {aptSpSup}, UserBirth: {userBirth}";


                    //Debug.Log("ROWSTRING : " + rowString);

                    DataRow[] matchingRows = OpenTable.Select(
                        $"성명 = '{userName}' AND " +
                        $"주택관리번호 = '{aptNumber}' AND " +
                        $"동 = '{aptDong}' AND " +
                        $"호수 = '{aptHosu}' AND " +
                        $"공급유형 = '{aptSupType}' AND " +
                        $"특별공급종류 = '{aptSpSup}' AND " +
                        $"생년 = '{userBirth}'"
                    );

                    matchingRows[0][getNames[0]] = rows[setNames[0]];
                    matchingRows[0][getNames[1]] = rows[setNames[1]];
                    matchingRows[0][getNames[2]] = rows[setNames[2]];
                    matchingRows[0][getNames[3]] = rows[setNames[3]];
                    matchingRows[0][getNames[4]] = rows[setNames[4]];
                    matchingRows[0][getNames[5]] = rows[setNames[5]];

                    // inspection_status_result, 점검여부
                    matchingRows[0][getNames[6]] = rows[setNames[6]];
                }

                saveCount += 1;

                mainManager.loadMsg.text = saveCount + " / " + changeRows.Length;
            }
        }

        /*

        foreach (DataRow rows in saveTable.Rows)
        {
            //Debug.Log("rows.0 : " + rows[0].ToString());
            //Debug.Log("rows.3 : " + rows[3].ToString());
            //Debug.Log("rows.4 : " + rows[4].ToString());
            //Debug.Log("rows.5 : " + rows[5].ToString());
            //Debug.Log("rows.6 : " + rows[6].ToString());
            //Debug.Log("rows.12 : " + rows[12].ToString());
            //Debug.Log("rows.13 : " + rows[13].ToString());

            await Task.Delay(1);

            //serverManager.UpdateRiskData("tb_riskdata", colname, checkNames, rows.ItemArray);

            saveCount += 1;

            mainManager.loadMsg.text = saveCount + " / " + saveTable.Rows.Count;
        }*/


    }

    public void All_Select()
    {
        if (!isAll)
        {
            isAll = true;

            btn_On.gameObject.SetActive(true); // 전체 이미지 선택 온
        }
        else
        {
            isAll = false;
            btn_On.gameObject.SetActive(false);
        }

        foreach (Item_Check item in list_Check)
        {
            if (item.toggle.interactable)
            {
                item.toggle.isOn = isAll;
            }
        }

        /*
        foreach (Toggle item in list_Toggles)
        {
            item.isOn = isAll;
        }*/
    }
    public void UnSelect()
    {
        Debug.Log("UNSELECT");

        isAll = false;
        btn_On.gameObject.SetActive(false);

        btn_checkProcess.interactable = false;
        btn_checkResultProcess.interactable = false;

        foreach (Item_Check item in list_Check)
        {
            //Debug.Log($"{item.id} interactable : " + item.toggle.interactable);

            if (item.toggle.interactable)
            {
                //Debug.Log($"item.toggle {item.id} isOn : " + item.toggle.isOn);

                item.toggle.isOn = isAll;

                //Debug.Log($"item.toggle {item.id} isOn : " + item.toggle.isOn);
            }
        }
    }

    public void UnSelectAll()
    {
        Debug.Log("UNSELECT");

        isAll = false;
        btn_On.gameObject.SetActive(false);

        btn_checkProcess.interactable = false;
        btn_checkResultProcess.interactable = false;

        foreach(DataRow rows in resultTable.Rows)
        {
            rows["isSelect"] = "False";
        }

    }

    // 정렬 버튼
    public void Reset_Sort(SortButton sortButton)
    {
        // 정렬 초기화
        for (int i = 0; i < sortButtons.Length; i++)
        {
            if (sortButtons[i] != sortButton)
            {
                // sortButtons[i].Img_SortMode.gameObject.SetActive(false);
                // sortButtons[i].Img_SortMode.sprite = Img_DESC;

                sortButtons[i].SortStatus = 0;
            }
        }
    }

    public void Reset()
    {
        Debug.Log("ViewExecute Reset");

        currentPage = 1;

        listCount = 10;
        itemsPerPage = listCount;

        totalCount = 0;
        unCheckCount = 0;
        checkPassCount = 0;
        checkCount = 0;

        dropdown_check.value = 0;
        dropdown_count.value = 0;

        Text_totalCount.text = "전체항목(" + totalCount + ")";

        Text_checkCount.text = $"미점검(<style=accent>{unCheckCount}</style>)," +
                                $"해당없음({checkPassCount})," +

                                $"점검완료({checkCount})";


        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)Text_totalCount.gameObject.transform.parent);

    }

    public void Clear_Sort()
    {
        Debug.Log("Clear Sort");

        // NO로 정렬 초기화
        for (int i = 0; i < sortButtons.Length; i++)
        {
            // sortButtons[i].Img_SortMode.gameObject.SetActive(false);
            // sortButtons[i].Img_SortMode.sprite = Img_DESC;

            sortButtons[i].SortStatus = 0;
        }

        sortButtons[0].SortStatus = 1;

        SortTarget = "NO ASC";
    }

    public void Set_Sort(SortButton sortButton)
    {
        UnSelect();

        Reset_Sort(sortButton);

        if (sortButton.SortStatus == 0)
        {
            // 오름 차순으로
            // sortButton.Img_SortMode.gameObject.SetActive(true);

            // sortButton.Img_SortMode.sprite = Img_ASC;

            sortButton.SortStatus = 1;

            SortTarget = sortButton.SortTarget + " ASC";
        }
        else if (sortButton.SortStatus == 1)
        {
            // 내림 차순으로

            // sortButton.Img_SortMode.gameObject.SetActive(true);

            // sortButton.Img_SortMode.sprite = Img_DESC;

            sortButton.SortStatus = 2;

            SortTarget = sortButton.SortTarget + " DESC";
        }
        else
        {
            // 2일땐

            // 오름 차순으로
            // sortButton.Img_SortMode.gameObject.SetActive(true);

            // sortButton.Img_SortMode.sprite = Img_ASC;
            sortButton.SortStatus = 1;

            SortTarget = sortButton.SortTarget + " ASC";
        }

        // 기본으로
        //sortButton.Img_SortMode.gameObject.SetActive(false);

        //sortButton.SortStatus = 0;

        //SortTarget = "NO ASC";
        // sortButton.Img_SortMode.sprite = Img_ASC;

        if (resultTable != null)
        {
            UpdateUI();
        }

        Debug.Log(" SORT " + SortTarget);
    }

    // 이전 10 페이지로 이동하는 함수
    void Previous10Pages()
    {
        if (resultTable != null)
        {
            currentPage -= 10;
            if (currentPage < 1)
                currentPage = 1;

            UpdateUI();
        }
    }

    // 다음 10 페이지로 이동하는 함수
    void Next10Pages()
    {
        if (resultTable != null)
        {
            currentPage += 10;
            int maxPage = Mathf.CeilToInt((float)resultTable.Rows.Count / itemsPerPage);
            if (currentPage > maxPage)
                currentPage = maxPage;

            UpdateUI();
        }
    }

    // 첫 페이지로 이동하는 함수
    void GoToFirstPage()
    {
        if (resultTable != null)
        {
            currentPage = 1;
            UpdateUI();
        }
    }

    // 마지막 페이지로 이동하는 함수
    void GoToLastPage()
    {
        if (resultTable != null)
        {
            int maxPage = Mathf.CeilToInt((float)resultTable.Rows.Count / itemsPerPage);
            currentPage = maxPage;
            UpdateUI();
        }
    }

    // 특정 페이지로 이동하는 함수
    void GoToPage(int pageNumber)
    {
        if (resultTable != null)
        {
            //Debug.Log(" GOTOPAGE :" + pageNumber);

            int maxPage = Mathf.CeilToInt((float)resultTable.Rows.Count / itemsPerPage);
            int buttonOffset = (currentPage - 1) / 10 * 10; // Calculate the offset for the page buttons

            int targetPage = buttonOffset + pageNumber; // Calculate the target page based on the current offset

            if (targetPage < 1)
                targetPage = 1;

            if (targetPage > maxPage)
                targetPage = maxPage;

            currentPage = targetPage;

            UpdateUI();
        }
    }

    void SetUpdateUI()
    {
    
        // 페이지 번호 버튼 업데이트
        int maxPage = Mathf.CeilToInt((float)selectrowCount / itemsPerPage);

        //Debug.Log("maxPage :" + maxPage);

        int buttonOffset = (currentPage - 1) / 10 * 10; // Calculate the offset for the page buttons

        for (int i = 0; i < pageButtons.Count; i++)
        {
            Button pageButton = pageButtons[i];
            int pageNumber = buttonOffset + i + 1; // Apply the offset to the page number

            if (pageNumber <= maxPage)
            {
                pageButton.gameObject.SetActive(true);
                TextMeshProUGUI buttonText = pageButton.GetComponentInChildren<TextMeshProUGUI>();
                buttonText.text = mainManager.FormatNumberWithSymbol(pageNumber);

                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)pageButton.gameObject.transform);

                // 현재 페이지 버튼 강조 표시
                if (pageNumber == currentPage)
                {
                    ColorBlock colors = pageButton.colors;
                    colors.normalColor = mainManager.PageOnColor;
                    // colors.highlightedColor = Color.blue;
                    pageButton.colors = colors;
                    buttonText.color = mainManager.PageTextOnColor;
                }
                else
                {
                    ColorBlock colors = pageButton.colors;
                    colors.normalColor = mainManager.PageOffColor;
                    // colors.highlightedColor = Color.white;
                    pageButton.colors = colors;
                    buttonText.color = mainManager.PageTextOffColor;
                }
            }
            else
            {
                pageButton.gameObject.SetActive(false);
            }
        }

        // 이전 10 페이지 및 다음 10 페이지 버튼 활성/비활성 설정   
        prevButton.interactable = currentPage > 10;
        nextButton.interactable = buttonOffset + 10 <= maxPage;

        // 첫 페이지 및 마지막 페이지 버튼 활성/비활성 설정
        firstPageButton.interactable = currentPage > 1;
        lastPageButton.interactable = currentPage < maxPage;

        LayoutRebuilder.ForceRebuildLayoutImmediate(pageContents);
    }

    // UI 업데이트 함수
    void UpdateUI()
    {
        /*
        Debug.Log("startIndex : " + startIndex);
        Debug.Log("endIndex : " + endIndex);

        for (int i = 0; i < itemUIList.Count; i++)
        {
            GameObject itemUI = itemUIList[i];
            if (i < endIndex - startIndex)
            {
                itemUI.SetActive(true);
                // 데이터 표시
                DataItem dataItem = allData[startIndex + i];
                Text itemText = itemUI.GetComponentInChildren<Text>();
                itemText.text = dataItem.name;
            }
            else
            {
                itemUI.SetActive(false);
            }
        }
        */
        /*
        // 이전 페이지 및 다음 페이지 버튼 활성/비활성 설정
        prevButton.interactable = currentPage > 1;
        nextButton.interactable = currentPage < Mathf.CeilToInt((float)allData.Count / itemsPerPage);
        */

        // 정렬 적용

        // 페이지 번호 버튼 업데이트
        int maxPage = Mathf.CeilToInt((float)resultTable.Rows.Count / itemsPerPage);

        //Debug.Log("maxPage :" + maxPage);

        int buttonOffset = (currentPage - 1) / 10 * 10; // Calculate the offset for the page buttons

        for (int i = 0; i < pageButtons.Count; i++)
        {
            Button pageButton = pageButtons[i];
            int pageNumber = buttonOffset + i + 1; // Apply the offset to the page number

            if (pageNumber <= maxPage)
            {
                pageButton.gameObject.SetActive(true);
                TextMeshProUGUI buttonText = pageButton.GetComponentInChildren<TextMeshProUGUI>();
                buttonText.text = mainManager.FormatNumberWithSymbol(pageNumber);

                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)pageButton.gameObject.transform);

                // 현재 페이지 버튼 강조 표시
                if (pageNumber == currentPage)
                {
                    ColorBlock colors = pageButton.colors;
                    colors.normalColor = mainManager.PageOnColor;
                    // colors.highlightedColor = Color.blue;
                    pageButton.colors = colors;
                    buttonText.color = mainManager.PageTextOnColor;
                }
                else
                {
                    ColorBlock colors = pageButton.colors;
                    colors.normalColor = mainManager.PageOffColor;
                    // colors.highlightedColor = Color.white;
                    pageButton.colors = colors;
                    buttonText.color = mainManager.PageTextOffColor;
                }
            }
            else
            {
                pageButton.gameObject.SetActive(false);
            }
        }

        // 이전 10 페이지 및 다음 10 페이지 버튼 활성/비활성 설정   
        prevButton.interactable = currentPage > 10;
        nextButton.interactable = buttonOffset + 10 <= maxPage;

        // 첫 페이지 및 마지막 페이지 버튼 활성/비활성 설정
        firstPageButton.interactable = currentPage > 1;
        lastPageButton.interactable = currentPage < maxPage;

        LayoutRebuilder.ForceRebuildLayoutImmediate(pageContents);

        // 데이터 변경
        if (resultTable != null)
        {
            UpdateView();
        }
    }

    public void ResetView()
    {
        for (int i = 0; i < viewupdate.Length; i++)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(viewupdate[i]);
        }

        parent.gameObject.SetActive(false);

        parent.gameObject.SetActive(true);
    }

    public void ClearListView()
    {
        //Debug.Log("ClearListView");

        var children = contents.transform.GetComponentsInChildren<Item_Check>(true);

        foreach (var child in children)
        {
            //if (child == contents) continue;
            //child.parent = null;

            Destroy(child.gameObject);
        }

        list_Check.Clear();
        list_Toggles.Clear();
    }

    public void CheckDropdownValueChanged(TMP_Dropdown change)
    {
        Debug.Log("Check : " + change.value);


        listMode = change.value;

        // 전체 선택 해제
        UnSelect();

        if (ignoreValueChanged)
        return;

        if (resultTable != null)
        {
            // UpdateListView();
            // 필터 변경시 리스트 처음으로
            GoToFirstPage();
        }
    }


    public void ListCountDropdownValueChanged(TMP_Dropdown change)
    {
        Debug.Log("New Value : " + change.value);

        listCount = listCounts[change.value];

        itemsPerPage = listCount;

        // 전체 선택 해제
        UnSelect();

        // 파일 아이템이 있을 경우에만 새로 그린다
        if (resultTable != null)
        {
            // 리스트 갯수 변경시 리스트 처음으로
            GoToFirstPage();

            //UpdateListView();
        }
    }



    // 검색 조회
    public void Action_Search()
    {
        UnSelect();

        ClearListView();

        if (_path.Length > 0)
        {
            currentTime = GetCurrentDate();

            // Action_Check();

            // Debug.Log("OpenTable : " + OpenTable.Rows.Count);

            if (OpenTable == null)
            {
                Debug.Log("OpenTable is null");

                Action_Check();
            }
            else
            {
                Debug.Log("OpenTable : " + OpenTable.Rows.Count);

                // 테이블이 이미 있는 경우에는
                Action_ServerLoad();
            }

        }
        else
        {
            // 파일이 없는경우에는 서버에 있는 데이터만 조회해서 보여준다
            Action_ReLoad();
        }

        // 마지막 검색 저장
        SaveExecute();


        //// 테이블이 없을경우
        //if (OpenTable == null)
        //{
        //    // Action_CSVLoad();
        //    Action_ReLoad();
        //}
        //else
        //{
        //    // 테이블이 있는 경우엔 조회
        //  in  Action_ReLoad();
        //}
    }

    // 테이블 있는 경우
    public async void Action_ReLoad()
    {
        Debug.Log("Action_Reload");

        CheckTable = null;

        CheckTable = new DataTable();

        selectMenu.DropdownType(_dropdownType); //검색구분
        selectMenu.CheckDFType(BustType); //적발내용
        selectMenu.CheckRfxgType(RfxgType); //rfxg
        selectMenu.CheckRfxgPerType(PrecentType); //퍼센트
        CityMenu = selectMenu.CheckCityToggle(selectMenu.cityToggles);

        mainManager.viewLoading.Open();

        mainManager.loadMsg.text = "";


        CheckTable = await serverManager.GetMarketManageByImplementationAndRegistration(selectMenu.dateText.FromDate, selectMenu.dateText.ToDate, _dropdownType, selectMenu.input_search.text, CityMenu, BustType, RfxgType, PrecentType);

        //for (int i = 0; i < 4; i++)
        //{
        //    if (PrecentType[i])
        //    {
        //        index = i;
        //    }
        //}

        Debug.Log("CheckTable : COUNT " + CheckTable.Rows.Count);

        // 새로운 컬럼 추가        
        //CheckTable.Columns.Add(new DataColumn("inspection_status_result", typeof(string)));
        CheckTable.Columns.Add(new DataColumn("isSelect", typeof(string)));
        CheckTable.Columns.Add(new DataColumn("isChange", typeof(string)));

        // CheckTable.Columns.Add(new DataColumn("Error", typeof(int)));

        // CheckTable.Columns.Add(new DataColumn("NO", typeof(int)));
        resultTable = null;

        resultTable = CheckTable.Copy();

        mainManager.viewLoading.Close();

        SetListView();
    }


    // 테이블 없는 경우 
    public void Action_CSVLoad()
    {
        // 한글 깨짐 현상 해결 가능
        var config = new ExcelReaderConfiguration();

        //config.FallbackEncoding = System.Text.Encoding.GetEncoding("ks_c_5601-1987");
        // config.FallbackEncoding = Encoding.GetEncoding("51949");

        //config.FallbackEncoding = Encoding.GetEncoding("cp949");
        config.FallbackEncoding = Encoding.GetEncoding("EUC-KR");

        OpenTable = new DataTable();

        var streamer = new FileStream(_path, FileMode.Open, FileAccess.Read);
        using (var reader = ExcelReaderFactory.CreateCsvReader(streamer, config))
        {
            // 항상 하나의 시트만 관리된다.
            OpenTable = reader.AsDataSet().Tables[0];

            if (reader.Read())
            {
                Debug.Log("FieldCount : " + reader.FieldCount);

                col = new string[reader.FieldCount];

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string columnName = reader.GetString(i);
                    //string columnName = reader.AsDataSet().Tables[0].Columns[i].ColumnName;

                    // 칼럼명 저장하기
                    col[i] = columnName;

                    OpenTable.Columns[i].ColumnName = columnName;
                }
            }

            // 칼럼 이름으로 검색하여 해당 칼럼을 가져옴
            DataColumn inspectionStatusColumn = OpenTable.Columns["inspection_status_result"];

            // inspectionStatusColumn이 null이 아니라면 해당 칼럼이 존재하는 것
            if (inspectionStatusColumn != null)
            {
                // inspection_status_result 칼럼이 존재하는 경우
                Debug.Log("inspection_status_result 칼럼이 존재합니다.");
            }
            else
            {
                // inspection_status_result 칼럼이 존재하지 않는 경우
                Debug.Log("inspection_status_result 칼럼이 존재하지 않습니다.");
                DataColumn dc = new DataColumn("inspection_status_result", typeof(string));
                dc.DefaultValue = "N";

                OpenTable.Columns.Add(dc);
            }

            OpenTable.Rows.RemoveAt(0);

            reader.Dispose();
            reader.Close();
        }

        serverManager.DeleteRiskTable("tb_checkresult");

        Action_Save();
    }

    public async void Action_SCVLoad()
    {
        Debug.Log("[ViewExecute][Action_SCVLoad]");

        mainManager.viewLoading.Open();

        mainManager.loadMsg.text = "";

        // 한글 깨짐 현상 해결 가능
        var config = new ExcelReaderConfiguration();

        //config.FallbackEncoding = System.Text.Encoding.GetEncoding("ks_c_5601-1987");
        // config.FallbackEncoding = Encoding.GetEncoding("51949");

        //config.FallbackEncoding = Encoding.GetEncoding("cp949");
        config.FallbackEncoding = Encoding.GetEncoding("EUC-KR");
        OpenTable = null;

        OpenTable = new DataTable();

        var streamer = new FileStream(_path, FileMode.Open, FileAccess.Read);
        using (var reader = ExcelReaderFactory.CreateCsvReader(streamer, config))
        {
            // 항상 하나의 시트만 관리된다.
            //OpenTable = reader.AsDataSet().Tables[0];
            OpenTable = await ReadCsvAsync(reader);


            if (reader.Read())
            {
                Debug.Log("FieldCount : " + reader.FieldCount);

                col = new string[reader.FieldCount];

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string columnName = reader.GetString(i);
                    //string columnName = reader.AsDataSet().Tables[0].Columns[i].ColumnName;

                    // 칼럼명 저장하기
                    col[i] = columnName;

                    OpenTable.Columns[i].ColumnName = columnName;
                }
            }

            // 칼럼 이름으로 검색하여 해당 칼럼을 가져옴
            DataColumn inspectionStatusColumn = OpenTable.Columns["inspection_status_result"];

            // inspectionStatusColumn이 null이 아니라면 해당 칼럼이 존재하는 것
            if (inspectionStatusColumn != null)
            {
                // inspection_status_result 칼럼이 존재하는 경우
                Debug.Log("inspection_status_result 칼럼이 존재합니다.");
            }
            else
            {
                // inspection_status_result 칼럼이 존재하지 않는 경우
                Debug.Log("inspection_status_result 칼럼이 존재하지 않습니다.");
                DataColumn dc = new DataColumn("inspection_status_result", typeof(string));
                dc.DefaultValue = "N";

                OpenTable.Columns.Add(dc);
            }

            OpenTable.Rows.RemoveAt(0);

            reader.Dispose();
            reader.Close();
        }

        Debug.Log("OpenTable : " + OpenTable.Rows.Count);
        
    }

    // 비동기로 CSV 파일을 읽는 메서드를 추가합니다.
    private async Task<DataTable> ReadCsvAsync(IExcelDataReader reader)
    {
        var dataSet = await Task.Run(() => reader.AsDataSet());
        return dataSet.Tables[0];
    }

    public async void Action_ServerLoad()
    {
        Debug.Log("[ViewExecute][Action_ServerLoad]");

        mainManager.viewLoading.Open();

        mainManager.loadMsg.text = "";

        // 서버에 저장된 데이터 호출
        CheckTable = null;

        CheckTable = new DataTable();

        selectMenu.DropdownType(_dropdownType); //검색구분
        selectMenu.CheckDFType(BustType); //적발내용
        selectMenu.CheckRfxgType(RfxgType); //rfxg
        selectMenu.CheckRfxgPerType(PrecentType); //퍼센트
        CityMenu = selectMenu.CheckCityToggle(selectMenu.cityToggles);

        //Debug.Log("selectMenu.dateText.FromDate" + selectMenu.dateText.FromDate);
        //Debug.Log("selectMenu.dateText.ToDate" + selectMenu.dateText.ToDate);
        //Debug.Log("_dropdownType" + RfxgType);
        //Debug.Log("_dropdownType" + PrecentType);
        //Debug.Log("_dropdownType" + CityMenu);

        
        CheckTable = await serverManager.GetMarketManageByImplementationAndRegistration(
            selectMenu.dateText.FromDate, selectMenu.dateText.ToDate, _dropdownType,
            selectMenu.input_search.text, CityMenu, BustType, RfxgType, PrecentType);

        //Debug.Log("CHEKCTABLE : COUNT " + CheckTable.Rows.Count);

        // 새로운 컬럼 추가        
        //CheckTable.Columns.Add(new DataColumn("inspection_status_result", typeof(string)));
        CheckTable.Columns.Add(new DataColumn("isSelect", typeof(string)));
        CheckTable.Columns.Add(new DataColumn("isChange", typeof(string)));



        // 두 테이블 비교
        var innerJoin = (from tb1 in CheckTable.AsEnumerable()
                         join tb2 in OpenTable.AsEnumerable()
                         on new
                         {
                             col1 = tb1.Field<string>("user_name"),
                             col2 = tb1.Field<Int64>("apt_number").ToString(),
                             col3 = tb1.Field<string>("apt_dong").ToString(),
                             col4 = tb1.Field<string>("apt_hosu").ToString(),
                             col5 = tb1.Field<Int64>("apt_sup_type").ToString(),
                             col6 = tb1.Field<Int64>("apt_sp_sup").ToString(),
                             col7 = tb1.Field<Int64>("user_birth").ToString()
                         } equals
                         new
                         {
                             col1 = tb2.Field<string>("성명"),
                             col2 = tb2.Field<string>("주택관리번호"),
                             col3 = tb2.Field<string>("동"),
                             col4 = tb2.Field<string>("호수"),
                             col5 = tb2.Field<string>("공급유형"),
                             col6 = tb2.Field<string>("특별공급종류"),
                             col7 = tb2.Field<string>("생년")
                         }
                         select tb1);

        if (innerJoin.Count<DataRow>() > 0 == false)
        {
            Debug.Log("조회 결과가 없습니다.");
            item_Empty.SetActive(true);

            mainManager.loadMsg.text = "";

            mainManager.viewLoading.Close();

            return;
        }
        else
        {
            resultTable = innerJoin.CopyToDataTable();

            //Debug.Log("resultTable : " + resultTable.Rows.Count);

            foreach (DataRow row in resultTable.Rows)
            {
                string userName = row.Field<string>("user_name");
                Int64 aptNumber = row.Field<Int64>("apt_number");
                string aptDong = row.Field<string>("apt_dong");
                string aptHosu = row.Field<string>("apt_hosu");
                Int64 aptSupType = row.Field<Int64>("apt_sup_type");
                Int64 aptSpSup = row.Field<Int64>("apt_sp_sup");
                Int64 userBirth = row.Field<Int64>("user_birth");

                //string rowString = $"UserName: {userName}, AptNumber: {aptNumber}, AptDong: {aptDong}, AptHosu: {aptHosu}, AptSupType: {aptSupType}, AptSpSup: {aptSpSup}, UserBirth: {userBirth}";


                //Debug.Log("ROWSTRING : " + rowString);

                DataRow[] matchingRows = OpenTable.Select(
                    $"성명 = '{userName}' AND " +
                    $"주택관리번호 = '{aptNumber}' AND " +
                    $"동 = '{aptDong}' AND " +
                    $"호수 = '{aptHosu}' AND " +
                    $"공급유형 = '{aptSupType}' AND " +
                    $"특별공급종류 = '{aptSpSup}' AND " +
                    $"생년 = '{userBirth}'"
                );

                //Debug.Log("matchingRows Length : " + matchingRows.Length);

                //string inspection_status = matchingRows[0].Field<string>("검사여부");
                //string unauth_sub_decision = matchingRows[0].Field<string>("부정청약판정");
                //string unauth_sub_type = matchingRows[0].Field<string>("부정청약유형");
                //string unauth_regidence_maintain = matchingRows[0].Field<string>("부정_거주지유지");
                //string unauth_address_maintain = matchingRows[0].Field<string>("부정_주소지유지");
                //string unauth_disguise_transfer = matchingRows[0].Field<string>("부정_위장전입");
                //string unauth_forgery_qualifi = matchingRows[0].Field<string>("부정_자격위조");
                //string unauth_savecerti_sale = matchingRows[0].Field<string>("부정_입주자저축증서매매");
                //string unauth_disguise_divorce = matchingRows[0].Field<string>("부정_위장이혼");
                //string unauth_other = matchingRows[0].Field<string>("부정_기타");
                //string inspection_status_result = matchingRows[0].Field<string>("inspection_status_result");

                //string fieldsString = $"검사여부: {inspection_status}, 부정청약판정: {unauth_sub_decision}," +
                //    $"부정청약유형: {unauth_sub_type}, 부정_거주지유지: {unauth_regidence_maintain}," +
                //    $"부정_주소지유지: {unauth_address_maintain}, 부정_위장전입: {unauth_disguise_transfer}," +
                //    $"부정_자격위조: {unauth_forgery_qualifi}, 부정_입주자저축증서매매: {unauth_savecerti_sale}, " +
                //    $"부정_위장이혼: {unauth_disguise_divorce}, 부정_기타: {unauth_other}, 점검여부: {inspection_status_result}";

                //Debug.Log("fieldsString : <color=yellow>" + fieldsString + "</color>");


                // 데이터 저장

                //Debug.Log($"row : {row[setNames[2]]}, matchingRows : {matchingRows[0][getNames[2]]}");
                //Debug.Log($"row inspection_status_result : {row[setNames[6]]},  row 점검여부 : {row["점검여부"]},  matchingRows 점검여부 : {matchingRows[0][getNames[6]]}");

                // 검사여부, 부정청약판정, 부정청약유형(string), 부정_위장전입, 부정_입주자저축증서매매, 부정_기타, 점검여부(string)
                // inspection_status unauth_sub_decision unauth_sub_type unauth_disguise_transfer unauth_savecerti_sale unauth_other inspection_status_result

                // CASE WHEN inner_data.inspection_status_result = 'Z' THEN 'Z' ELSE inner_data.inspection_status_result END as '점검여부', 
                // CASE WHEN inner_data.unauth_sub_decision = 0 THEN '통과' ELSE unauth_sub_type  END as '부정청약결과',    

                row[setNames[0]] = matchingRows[0][getNames[0]];
                row[setNames[1]] = matchingRows[0][getNames[1]];
                row[setNames[2]] = matchingRows[0][getNames[2]];
                row[setNames[3]] = matchingRows[0][getNames[3]];
                row[setNames[4]] = matchingRows[0][getNames[4]];
                row[setNames[5]] = matchingRows[0][getNames[5]];

                //Debug.Log($"2222222222222 row inspection_status_result : {matchingRows[0][getNames[6]]}");

                string originalString = (string)matchingRows[0][getNames[6]];
                string uppercaseString = originalString.ToUpper();

                //Debug.Log($"2222222222222 row uppercaseString : {uppercaseString}");

                // inspection_status_result, 점검여부
                row[setNames[6]] = uppercaseString;
                row["점검여부"] = uppercaseString;

                // unauth_sub_decision 부정청약판정
                if (matchingRows[0][getNames[1]].ToString() == "0")
                {
                    row["부정청약결과"] = "통과";
                }
                else
                {
                    row["부정청약결과"] = row[setNames[2]];
                }

                //Debug.Log($"2222222222222 row : {row[setNames[2]]}, matchingRows : {matchingRows[0][getNames[2]]} , {row["부정청약결과"]}" );
                //Debug.Log($"2222222222222 row inspection_status_result : {row[setNames[6]]},  row 점검여부 : {row["점검여부"]},  matchingRows 점검여부 : {matchingRows[0][getNames[6]]}");
            }


            mainManager.loadMsg.text = "";

            mainManager.viewLoading.Close();

            if (resultTable.Rows.Count == 0)
            {
                item_Empty.SetActive(true);
            }
            else
            {
                item_Empty.SetActive(false);

                SetListView2();
            }

        }

    }

    string[] checkNames = { "apt_number", "apt_dong", "apt_hosu", "apt_sup_type", "apt_sp_sup", "user_birth", "user_name" };
    string[] openNames = { "주택관리번호", "동", "호수", "공급유형", "특별공급종류", "생년", "성명" };

    string[] getNames = { "검사여부","부정청약판정","부정청약유형","부정_위장전입","부정_입주자저축증서매매","부정_기타","inspection_status_result" };
    string[] setNames = { "inspection_status","unauth_sub_decision","unauth_sub_type","unauth_disguise_transfer","unauth_savecerti_sale","unauth_other","inspection_status_result" };
    private string savePath;

    public async void Action_Check()
    {
        Debug.Log("[ViewExecute][Action_Check]");

        mainManager.viewLoading.Open();

        mainManager.loadMsg.text = "";

        // 파일 호출 

        // 한글 깨짐 현상 해결 가능
        var config = new ExcelReaderConfiguration();

        //config.FallbackEncoding = System.Text.Encoding.GetEncoding("ks_c_5601-1987");
        // config.FallbackEncoding = Encoding.GetEncoding("51949");

        //config.FallbackEncoding = Encoding.GetEncoding("cp949");
        config.FallbackEncoding = Encoding.GetEncoding("EUC-KR");

        OpenTable = null;

        OpenTable = new DataTable();

        var streamer = new FileStream(_path, FileMode.Open, FileAccess.Read);
        using (var reader = ExcelReaderFactory.CreateCsvReader(streamer, config))
        {
            // 항상 하나의 시트만 관리된다.
            //OpenTable = reader.AsDataSet().Tables[0];
            OpenTable = await ReadCsvAsync(reader);


            if (reader.Read())
            {
                Debug.Log("FieldCount : " + reader.FieldCount);

                col = new string[reader.FieldCount];

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string columnName = reader.GetString(i);
                    //string columnName = reader.AsDataSet().Tables[0].Columns[i].ColumnName;

                    // 칼럼명 저장하기
                    col[i] = columnName;

                    OpenTable.Columns[i].ColumnName = columnName;
                }
            }

            // 칼럼 이름으로 검색하여 해당 칼럼을 가져옴
            DataColumn inspectionStatusColumn = OpenTable.Columns["inspection_status_result"];

            // inspectionStatusColumn이 null이 아니라면 해당 칼럼이 존재하는 것
            if (inspectionStatusColumn != null)
            {
                // inspection_status_result 칼럼이 존재하는 경우
                Debug.Log("inspection_status_result 칼럼이 존재합니다.");
            }
            else
            {
                // inspection_status_result 칼럼이 존재하지 않는 경우
                Debug.Log("inspection_status_result 칼럼이 존재하지 않습니다.");
                DataColumn dc = new DataColumn("inspection_status_result", typeof(string));
                dc.DefaultValue = "N";

                OpenTable.Columns.Add(dc);
            }

            OpenTable.Rows.RemoveAt(0);

            reader.Dispose();
            reader.Close();
        }

        Debug.Log("OpenTable : " + OpenTable.Rows.Count);

        // 서버에 저장된 데이터 호출

        Debug.Log("[ViewExecute][Action_ServerLoad]");

        CheckTable = null;

        CheckTable = new DataTable();

        selectMenu.DropdownType(_dropdownType); //검색구분
        selectMenu.CheckDFType(BustType); //적발내용
        selectMenu.CheckRfxgType(RfxgType); //rfxg
        selectMenu.CheckRfxgPerType(PrecentType); //퍼센트
        CityMenu = selectMenu.CheckCityToggle(selectMenu.cityToggles);

        //Debug.Log("selectMenu.dateText.FromDate" + selectMenu.dateText.FromDate);
        //Debug.Log("selectMenu.dateText.ToDate" + selectMenu.dateText.ToDate);
        //Debug.Log("_dropdownType" + RfxgType);
        //Debug.Log("_dropdownType" + PrecentType);
        //Debug.Log("_dropdownType" + CityMenu);

        CheckTable = await serverManager.GetMarketManageByImplementationAndRegistration(
            selectMenu.dateText.FromDate, selectMenu.dateText.ToDate, _dropdownType,
            selectMenu.input_search.text, CityMenu, BustType, RfxgType, PrecentType);

        //Debug.Log("CHEKCTABLE : COUNT " + CheckTable.Rows.Count);

        // 새로운 컬럼 추가        
        //CheckTable.Columns.Add(new DataColumn("inspection_status_result", typeof(string)));
        CheckTable.Columns.Add(new DataColumn("isSelect", typeof(string)));
        CheckTable.Columns.Add(new DataColumn("isChange", typeof(string)));


        // 두 테이블 비교
        var innerJoin = (from tb1 in CheckTable.AsEnumerable()
                         join tb2 in OpenTable.AsEnumerable()
                         on new
                         {
                             col1 = tb1.Field<string>("user_name"),
                             col2 = tb1.Field<Int64>("apt_number").ToString(),
                             col3 = tb1.Field<string>("apt_dong").ToString(),
                             col4 = tb1.Field<string>("apt_hosu").ToString(),
                             col5 = tb1.Field<Int64>("apt_sup_type").ToString(),
                             col6 = tb1.Field<Int64>("apt_sp_sup").ToString(),
                             col7 = tb1.Field<Int64>("user_birth").ToString()
                         } equals
                         new
                         {
                             col1 = tb2.Field<string>("성명"),
                             col2 = tb2.Field<string>("주택관리번호"),
                             col3 = tb2.Field<string>("동"),
                             col4 = tb2.Field<string>("호수"),
                             col5 = tb2.Field<string>("공급유형"),
                             col6 = tb2.Field<string>("특별공급종류"),
                             col7 = tb2.Field<string>("생년")
                         }
                         select tb1);

        if (innerJoin.Count<DataRow>() > 0 == false)
        {
            Debug.Log("조회 결과가 없습니다.");
            item_Empty.SetActive(true);

            mainManager.loadMsg.text = "";

            mainManager.viewLoading.Close();

            return;
        }
        else
        {
            resultTable = innerJoin.CopyToDataTable();

            Debug.Log("resultTable : " + resultTable.Rows.Count);

            foreach (DataRow row in resultTable.Rows)
            {
                string userName = row.Field<string>("user_name");
                Int64 aptNumber = row.Field<Int64>("apt_number");
                string aptDong = row.Field<string>("apt_dong");
                string aptHosu = row.Field<string>("apt_hosu");
                Int64 aptSupType = row.Field<Int64>("apt_sup_type");
                Int64 aptSpSup = row.Field<Int64>("apt_sp_sup");
                Int64 userBirth = row.Field<Int64>("user_birth");

                //string rowString = $"UserName: {userName}, AptNumber: {aptNumber}, AptDong: {aptDong}, AptHosu: {aptHosu}, AptSupType: {aptSupType}, AptSpSup: {aptSpSup}, UserBirth: {userBirth}";


                //Debug.Log("ROWSTRING : " + rowString);

                DataRow[] matchingRows = OpenTable.Select(
                    $"성명 = '{userName}' AND " +
                    $"주택관리번호 = '{aptNumber}' AND " +
                    $"동 = '{aptDong}' AND " +
                    $"호수 = '{aptHosu}' AND " +
                    $"공급유형 = '{aptSupType}' AND " +
                    $"특별공급종류 = '{aptSpSup}' AND " +
                    $"생년 = '{userBirth}'"
                );

                //Debug.Log("matchingRows Length : " + matchingRows.Length);

                //string inspection_status = matchingRows[0].Field<string>("검사여부");
                //string unauth_sub_decision = matchingRows[0].Field<string>("부정청약판정");
                //string unauth_sub_type = matchingRows[0].Field<string>("부정청약유형");
                //string unauth_regidence_maintain = matchingRows[0].Field<string>("부정_거주지유지");
                //string unauth_address_maintain = matchingRows[0].Field<string>("부정_주소지유지");
                //string unauth_disguise_transfer = matchingRows[0].Field<string>("부정_위장전입");
                //string unauth_forgery_qualifi = matchingRows[0].Field<string>("부정_자격위조");
                //string unauth_savecerti_sale = matchingRows[0].Field<string>("부정_입주자저축증서매매");
                //string unauth_disguise_divorce = matchingRows[0].Field<string>("부정_위장이혼");
                //string unauth_other = matchingRows[0].Field<string>("부정_기타");
                //string inspection_status_result = matchingRows[0].Field<string>("inspection_status_result");

                //string fieldsString = $"검사여부: {inspection_status}, 부정청약판정: {unauth_sub_decision}," +
                //    $"부정청약유형: {unauth_sub_type}, 부정_거주지유지: {unauth_regidence_maintain}," +
                //    $"부정_주소지유지: {unauth_address_maintain}, 부정_위장전입: {unauth_disguise_transfer}," +
                //    $"부정_자격위조: {unauth_forgery_qualifi}, 부정_입주자저축증서매매: {unauth_savecerti_sale}, " +
                //    $"부정_위장이혼: {unauth_disguise_divorce}, 부정_기타: {unauth_other}, 점검여부: {inspection_status_result}";

                //Debug.Log("fieldsString : <color=yellow>" + fieldsString + "</color>");


                // 데이터 저장

                //Debug.Log($"row : {row[setNames[2]]}, matchingRows : {matchingRows[0][getNames[2]]}");
                //Debug.Log($"row inspection_status_result : {row[setNames[6]]},  row 점검여부 : {row["점검여부"]},  matchingRows 점검여부 : {matchingRows[0][getNames[6]]}");

                // 검사여부, 부정청약판정, 부정청약유형(string), 부정_위장전입, 부정_입주자저축증서매매, 부정_기타, 점검여부(string)
                // inspection_status unauth_sub_decision unauth_sub_type unauth_disguise_transfer unauth_savecerti_sale unauth_other inspection_status_result

                // CASE WHEN inner_data.inspection_status_result = 'Z' THEN 'Z' ELSE inner_data.inspection_status_result END as '점검여부', 
                // CASE WHEN inner_data.unauth_sub_decision = 0 THEN '통과' ELSE unauth_sub_type  END as '부정청약결과',    

                row[setNames[0]] = matchingRows[0][getNames[0]];
                row[setNames[1]] = matchingRows[0][getNames[1]];
                row[setNames[2]] = matchingRows[0][getNames[2]];
                row[setNames[3]] = matchingRows[0][getNames[3]];
                row[setNames[4]] = matchingRows[0][getNames[4]];
                row[setNames[5]] = matchingRows[0][getNames[5]];

                string originalString = (string)matchingRows[0][getNames[6]];
                string uppercaseString = originalString.ToUpper();

                // inspection_status_result, 점검여부
                row[setNames[6]] = uppercaseString;
                row["점검여부"] = uppercaseString;

                // inspection_status_result, 점검여부
                //row[setNames[6]] = matchingRows[0][getNames[6]];
                //row["점검여부"] = matchingRows[0][getNames[6]];

                // unauth_sub_decision 부정청약판정
                if (matchingRows[0][getNames[1]].ToString() == "0")
                {
                    row["부정청약결과"] = "통과";
                }
                else
                {
                    row["부정청약결과"] = row[setNames[2]];
                }

                //Debug.Log($"2222222222222 row : {row[setNames[2]]}, matchingRows : {matchingRows[0][getNames[2]]} , {row["부정청약결과"]}" );
                //Debug.Log($"2222222222222 row inspection_status_result : {row[setNames[6]]},  row 점검여부 : {row["점검여부"]},  matchingRows 점검여부 : {matchingRows[0][getNames[6]]}");
            }


            mainManager.loadMsg.text = "";

            mainManager.viewLoading.Close();

            if (resultTable.Rows.Count == 0)
            {
                item_Empty.SetActive(true);
            }
            else
            {
                item_Empty.SetActive(false);

                SetListView2();
            }
        }


    }
    public async void Action_EmptyCheck()
    {  
        Action_ServerLoad();

        if (resultTable.Rows.Count == 0)
        {
            item_Empty.SetActive(true);
        }
        else
        {
            item_Empty.SetActive(false);

            SetListView2();
        }

        // Debug.Log(CheckTable.Rows.Count());


    }


    public async void Action_Save()
    {
        mainManager.viewLoading.Open();

        mainManager.loadMsg.text = "";
        Debug.Log("전송시작");

        // 체인으로 대기
        await Send_Data();

        Debug.Log("전송끝");

        CheckTable = null;

        CheckTable = new DataTable();

        selectMenu.DropdownType(_dropdownType); //검색구분
        selectMenu.CheckDFType(BustType); //적발내용
        selectMenu.CheckRfxgType(RfxgType); //rfxg
        selectMenu.CheckRfxgPerType(PrecentType); //퍼센트
        CityMenu = selectMenu.CheckCityToggle(selectMenu.cityToggles);


        CheckTable = await serverManager.GetMarketManageByImplementationAndRegistration(
            selectMenu.dateText.FromDate, selectMenu.dateText.ToDate, _dropdownType, 
            selectMenu.input_search.text, CityMenu, BustType, RfxgType, PrecentType);

        //for (int i = 0; i < 4; i++)
        //{
        //    if (PrecentType[i])
        //    {
        //        index = i;
        //    }
        //}

        Debug.Log("CHEKCTABLE : COUNT " + CheckTable.Rows.Count);

        // 새로운 컬럼 추가        
        //CheckTable.Columns.Add(new DataColumn("inspection_status_result", typeof(string)));
        CheckTable.Columns.Add(new DataColumn("isSelect", typeof(string)));
        CheckTable.Columns.Add(new DataColumn("isChange", typeof(string)));

        // CheckTable.Columns.Add(new DataColumn("Error", typeof(int)));

        // CheckTable.Columns.Add(new DataColumn("NO", typeof(int)));

        SetListView();

        mainManager.viewLoading.Close();
    }

    public async Task Send_Data()
    {
        // riskTable 전송

        Debug.Log("ROWS COUNT : " + OpenTable.Rows.Count);

        int saveCount = 0;

        foreach (DataRow rows in OpenTable.Rows)
        {
            //Debug.Log("rows name : " + rows["성명"].ToString());

            await Task.Delay(1);

            serverManager.InsertRiskData("tb_checkresult", colname, rows.ItemArray);

            saveCount += 1;

            mainManager.loadMsg.text = saveCount + " / " + OpenTable.Rows.Count;
        }
    }

    //public void Update()
    //{
    //    if(Input.GetKeyDown(KeyCode.Space))
    //    {
    //        var vcount = resultTable.Select("isSelect = True");

    //        Debug.Log("선택한 갯수 : " + vcount.Length);
    //    }
    //}

    public void UpdateView()
    {

        Debug.Log("UPDATE VIEW");

        ClearListView();

        // 현재 정렬 버튼으로

        resultTable.DefaultView.Sort = SortTarget;

        listTable = null;

        listTable = resultTable.DefaultView.ToTable();

        //재생성 함수 들어갈곳
        string inspection_status = "";


        switch (listMode)
        {
            case 1: inspection_status = "점검여부 = 'Z'"; break;
            case 2: inspection_status = "점검여부 = 'N'"; break;
            case 3: inspection_status = "점검여부 = 'Y'"; break;
        }

        DataRow[] selectedRows = listTable.Select(inspection_status);
        //  Debug.Log("selectedRows : " + selectedRows.Length) ;

        selectrowCount = selectedRows.Count();

        int startIndex = (currentPage - 1) * itemsPerPage;
        int endIndex = Mathf.Min(startIndex + itemsPerPage, selectedRows.Length);

        string cellValue = "";

        for (var rowIndex = startIndex; rowIndex < endIndex; rowIndex++)
        {
            var row = selectedRows[rowIndex];

            Item_Check item = Instantiate(item_Check, contents);

            item.toggle.isOn = bool.Parse(row["isSelect"].ToString());

            item.id = row["data_no"].ToString();


            //item.no.text = row["NO"].ToString();
            item.no.text = (rowIndex + 1).ToString();

            cellValue = row["점검여부"].ToString();

            // Debug.Log("점검여부 : " + cellValue);

            if (cellValue == "N")
            {
                cellValue = "<color=#ef8674>" + cellValue + "</color>";
            }
            else if (cellValue == "Y")
            {
                cellValue = "<color=#2dbbbc>" + cellValue + "</color>";

            }
            else
            {
                cellValue = "해당없음";
            }

            item.check.text = cellValue;

            cellValue = row["부정청약결과"].ToString();

            // Debug.Log("부정청약결과 : " + cellValue);

            if (cellValue == "통과")
            {
                cellValue = "<color=#2dbbbc>" + cellValue + "</color>";
            }
            else
            {
                // cellValue = row["unauth_sub_type"].ToString();

                cellValue = "<color=#ef8674>" + cellValue + "</color>";
            }

            item.checkResult.text = cellValue;

            item.aptNum.text = row["주택관리번호"].ToString();

            item.aptType.text = row["민영국민"].ToString();
            item.aptName.text = row["단지명"].ToString();

            item.aptDong.text = row["동"].ToString();

            item.aptHosu.text = row["호수"].ToString();

            item.userName.text = row["성명"].ToString();

            item.userBirth.text = row["생년월일"].ToString().PadLeft(6, '0');

            item.userAge.text = row["연령"].ToString();

            if (row["공급유형"].ToString() == "10")
            {
                item.supType.text = "일반";
                item.detailType.text = checkDeatilType("0");
            }
            else
            {
                item.supType.text = "특별";

                item.detailType.text = checkDeatilType(row["세부유형타입"].ToString());
            }

            item.relation.text = row["세대주관계"].ToString();

            item.userchangeDate.text = mainManager.DateFormat(row["변경일자"].ToString());

            item.userspouse.text = row["배우자"].ToString();

            item.housemember.text = row["세대원수"].ToString();

            item.hp_housememeber.text = row["분리세대"].ToString();

            item.phonedup.text = row["폰중복"].ToString();

            item.ipdupwin.text = row["ip중복당첨"].ToString();

            item.ipdupapp.text = row["ip중복신청"].ToString();

            item.appclass.text = row["신청거주구분"].ToString();

            item.winclass.text = row["당첨거주구분"].ToString();

            item.addressmatch.text = row["주소일치여부"].ToString();

            item.adminaddress.text = row["등본주소"].ToString();

            item.inputaddress.text = row["입력주소"].ToString();

            item.recomType.text = row["기관추천"].ToString();

            item.uploadDate.text = row["업로드일시"].ToString();

            //double resultrisk = (double)row["위험도"] * 100;

            double resultrisk = (double.Parse(row["위험도"].ToString()) * 100);

            item.risk.text = (Mathf.Floor(float.Parse(resultrisk.ToString()) * 10f) / 10f).ToString("N1");

            item.viewExecute = this;

            item.Init();

            list_Check.Add(item);
            list_Toggles.Add(item.toggle);
        }

        if (list_Check.Count == 0)
        {
            item_Empty.SetActive(true);
        }
        else
        {
            item_Empty.SetActive(false);
        }

        totalCount = resultTable.Rows.Count;

        ResetView();

        getCount();

        SetUpdateUI();

        if (list_Check.Count > 0)
        {
            if (AreAllTogglesOn())
            {
                isAll = true;
                btn_On.gameObject.SetActive(true);
            }
            else
            {
                isAll = false;
                btn_On.gameObject.SetActive(false);
            }
        }    
    }
    public void SetListView2()
    {
        Debug.Log("SetListView2");

        ClearListView();

        // NO로 정렬 지정
        Clear_Sort();

        unCheckCount = 0;

        for (var rowIndex = 0; rowIndex < resultTable.Rows.Count; rowIndex++)
        {
            // 행 가져오기
            var slot = resultTable.Rows[rowIndex];

            slot["isSelect"] = "false";
            slot["isChange"] = "false";

            // 기본은 미점검? // Z ="해당없음", N:미점검, Y:점검완료
            //slot["inspection_status_result"] = "N";

            // 위험도 데이터 추가 

        }

        resultTable.DefaultView.Sort = SortTarget;

        listTable = null;

        listTable = resultTable.DefaultView.ToTable();

        int endIndex = Mathf.Min(listCount, listTable.Rows.Count);

        selectrowCount = endIndex;

        string cellValue = "";

        for (var rowIndex = 0; rowIndex < endIndex; rowIndex++)
        {
            var slot = listTable.Rows[rowIndex];

            Item_Check item = Instantiate(item_Check, contents);

            item.toggle.isOn = false;

            item.id = slot["data_no"].ToString();
            //item.no.text = slot["NO"].ToString();
            item.no.text = (rowIndex + 1).ToString();

            cellValue = slot["점검여부"].ToString();

            if (cellValue == "N")
            {
                cellValue = "<color=#ef8674>" + cellValue + "</color>";
            }
            else if (cellValue == "Y")
            {
                cellValue = "<color=#2dbbbc>" + cellValue + "</color>";

            }
            else
            {
                cellValue = "해당없음";

            }

            item.check.text = cellValue;

            cellValue = slot["부정청약결과"].ToString();

            if (cellValue == "통과")
            {
                cellValue = "<color=#2dbbbc>" + cellValue + "</color>";
            }
            else
            {
                // cellValue = slot["부정청약결과"].ToString();

                cellValue = "<color=#ef8674>" + cellValue + "</color>";


            }

            item.checkResult.text = cellValue;


            item.aptNum.text = slot["주택관리번호"].ToString();

            item.aptType.text = slot["민영국민"].ToString();
            item.aptName.text = slot["단지명"].ToString();

            item.aptDong.text = slot["동"].ToString();

            item.aptHosu.text = slot["호수"].ToString();

            item.userName.text = slot["성명"].ToString();

            item.userBirth.text = slot["생년월일"].ToString().PadLeft(6, '0');

            item.userAge.text = slot["연령"].ToString();

            if (slot["공급유형"].ToString() == "10")
            {
                item.supType.text = "일반";
                item.detailType.text = checkDeatilType("0");
            }
            else
            {
                item.supType.text = "특별";

                item.detailType.text = checkDeatilType(slot["세부유형타입"].ToString());
            }

            item.relation.text = slot["세대주관계"].ToString();

            item.userchangeDate.text = mainManager.DateFormat(slot["변경일자"].ToString());

            item.userspouse.text = slot["배우자"].ToString();

            item.housemember.text = slot["세대원수"].ToString();

            item.hp_housememeber.text = slot["분리세대"].ToString();

            item.phonedup.text = slot["폰중복"].ToString();

            item.ipdupwin.text = slot["ip중복당첨"].ToString();

            item.ipdupapp.text = slot["ip중복신청"].ToString();

            item.appclass.text = slot["신청거주구분"].ToString();

            item.winclass.text = slot["당첨거주구분"].ToString();

            item.addressmatch.text = slot["주소일치여부"].ToString();

            item.adminaddress.text = slot["등본주소"].ToString();

            item.inputaddress.text = slot["입력주소"].ToString();

            item.recomType.text = slot["기관추천"].ToString();

            item.uploadDate.text = slot["업로드일시"].ToString();

            //double resultrisk = (double)slot["위험도"] * 100;

            double resultrisk = (double.Parse(slot["위험도"].ToString()) * 100);

            item.risk.text = (Mathf.Floor(float.Parse(resultrisk.ToString()) * 10f) / 10f).ToString("N1");

            item.viewExecute = this;

            item.Init();

            list_Check.Add(item);
            list_Toggles.Add(item.toggle);
        }

        if (list_Check.Count == 0)
        {
            item_Empty.SetActive(true);
        }
        else
        {
            item_Empty.SetActive(false);
        }

        totalCount = resultTable.Rows.Count;

        ResetView();

        getCount();

        // 처음으로 세팅
        GoToFirstPage();

        SetUpdateUI();
        if (list_Check.Count > 0)
        {
            if (AreAllTogglesOn())
            {
                isAll = true;
                btn_On.gameObject.SetActive(true);
            }
            else
            {
                isAll = false;
                btn_On.gameObject.SetActive(false);
            }
        }
    }


    public void SetListView()
    {
        ClearListView();

        // NO로 정렬 지정
        Clear_Sort();

        unCheckCount = 0;

        for (var rowIndex = 0; rowIndex < resultTable.Rows.Count; rowIndex++)
        {
            // 행 가져오기
            var slot = resultTable.Rows[rowIndex];

            slot["isSelect"] = "false";
            slot["isChange"] = "false";

            // 기본은 미점검? // Z ="해당없음", N:미점검, Y:점검완료
            //slot["inspection_status_result"] = "N";

            // 위험도 데이터 추가 

        }

        resultTable.DefaultView.Sort = SortTarget;

        listTable = null;

        listTable = resultTable.DefaultView.ToTable();

        int endIndex = Mathf.Min(listCount, listTable.Rows.Count);

        selectrowCount = endIndex;
        string cellValue = "";

        for (var rowIndex = 0; rowIndex < endIndex; rowIndex++)
        {
            var slot = listTable.Rows[rowIndex];

            Item_Check item = Instantiate(item_Check, contents);

            item.toggle.isOn = false;

            item.id = slot["data_no"].ToString();
            //item.no.text = slot["NO"].ToString();
            item.no.text = (rowIndex + 1).ToString();

            cellValue = slot["점검여부"].ToString();

            if (cellValue == "N")
            {
                cellValue = "<color=#ef8674>" + cellValue + "</color>";
            }
            else if (cellValue == "Y")
            {
                cellValue = "<color=#2dbbbc>" + cellValue + "</color>";

            }
            else
            {
                cellValue = "해당없음";

            }

            item.check.text = cellValue;

            cellValue = slot["부정청약결과"].ToString();

            if (cellValue == "통과")
            {
                cellValue = "<color=#2dbbbc>" + cellValue + "</color>";
            }
            else
            {
                // cellValue = slot["부정청약결과"].ToString();

                cellValue = "<color=#ef8674>" + cellValue + "</color>";


            }

            item.checkResult.text = cellValue;


            item.aptNum.text = slot["주택관리번호"].ToString();

            item.aptType.text = slot["민영국민"].ToString();
            item.aptName.text = slot["단지명"].ToString();

            item.aptDong.text = slot["동"].ToString();

            item.aptHosu.text = slot["호수"].ToString();

            item.userName.text = slot["성명"].ToString();

            item.userBirth.text = slot["생년월일"].ToString().PadLeft(6, '0');

            item.userAge.text = slot["연령"].ToString();

            if(slot["공급유형"].ToString() == "10")
            {
                item.supType.text = "일반";
                item.detailType.text = checkDeatilType("0");
            }
            else
            {
                item.supType.text = "특별";

                item.detailType.text = checkDeatilType(slot["세부유형타입"].ToString());
            }

            item.relation.text = slot["세대주관계"].ToString();

            item.userchangeDate.text = mainManager.DateFormat(slot["변경일자"].ToString());

            item.userspouse.text = slot["배우자"].ToString();

            item.housemember.text = slot["세대원수"].ToString();

            item.hp_housememeber.text = slot["분리세대"].ToString();

            item.phonedup.text = slot["폰중복"].ToString();

            item.ipdupwin.text = slot["ip중복당첨"].ToString();

            item.ipdupapp.text = slot["ip중복신청"].ToString();

            item.appclass.text = slot["신청거주구분"].ToString();

            item.winclass.text = slot["당첨거주구분"].ToString();

            item.addressmatch.text = slot["주소일치여부"].ToString();

            item.adminaddress.text = slot["등본주소"].ToString();

            item.inputaddress.text = slot["입력주소"].ToString();

            item.recomType.text = slot["기관추천"].ToString();

            item.uploadDate.text = slot["업로드일시"].ToString();

            //double resultrisk = (double)slot["위험도"] * 100;

            double resultrisk = (double.Parse(slot["위험도"].ToString()) * 100);

            item.risk.text = (Mathf.Floor(float.Parse(resultrisk.ToString()) * 10f) / 10f).ToString("N1");

            item.viewExecute = this;

            item.Init();

            list_Check.Add(item);
            list_Toggles.Add(item.toggle);
        }

        if (list_Check.Count == 0)
        {
            item_Empty.SetActive(true);
        }
        else
        {
            item_Empty.SetActive(false);
        }

        totalCount = resultTable.Rows.Count;
        
        ResetView();

        getCount();

        // 처음으로 세팅
        GoToFirstPage();

        SetUpdateUI();

        if (AreAllTogglesOn())
        {
            isAll = true;
            btn_On.gameObject.SetActive(true);
        }
        else
        {
            isAll = false;
            btn_On.gameObject.SetActive(false);
        }

    }

    // 특별공급 유형 변환
    public string checkDeatilType(string typeData)
    {
        string codeName = "";

        if(typeData == "0")
        {
            codeName = "일반";
        }
        else if (typeData == "1")
        {
            codeName = "다자녀";
        }
        else if (typeData == "2")
        {
            codeName = "신혼부부";
        }
        else if (typeData == "3")
        {
            codeName = "생애최초";
        }
        else if (typeData == "4")
        {
            codeName = "노부모";
        }
        else if (typeData == "5")
        {
            codeName = "기관추천";
        }
        else if (typeData == "6")
        {
            codeName = "이전종사자";
        }
        else if (typeData == "7")
        {
            codeName = "청년";
        }
        else if (typeData == "13")
        {
            codeName = "신혼희망타운";
        }
        else
        {
            // 미분류시
            codeName = typeData;
        }

        return codeName;
    }

    // 초기화
    public void Action_Reset()
    {

        CommonPopup commonPopup = Instantiate(mainManager.commonPopup, mainManager.contents);

        commonPopup.ShowPopup_TwoButton(
            "모든 조회설정이 초기화 됩니다.\n" +
            "진행하시겠습니까 ? ",
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
                ResetButton(true);

                commonPopup.CloseDestroy();
            });
    }


    public bool AreAllTogglesOn()
    {
        bool allTogglesOn = true;

        foreach (Toggle toggle in list_Toggles)
        {
            if (!toggle.isOn)
            {
                allTogglesOn = false;
                break;
            }
        }

        return allTogglesOn;
    }
    public bool AreAllTogglesOff()
    {
        bool allTogglesOff = true;

        foreach (Toggle toggle in list_Toggles)
        {
            if (toggle.isOn)
            {
                allTogglesOff = false;
                break;
            }
        }

        return allTogglesOff;
    }

    // 선택한것들의 결과처리가 모두 통과일 경우에만 점검여부를 바꿀수 있다고?
    public bool AreAllResultPass()
    {
        bool allPass = true;

        DataRow[] rows = resultTable.Select("isSelect = True");

        // Debug.Log("rows : " + rows.Length);

        if(rows.Length > 0)
        {
            foreach (DataRow row in rows)
            {
                //Debug.Log(" 점검여부 Check = " + row["점검여부"].ToString());
                //Debug.Log(" 부정청약결과 처리 = " + row["부정청약결과"].ToString());
                //Debug.Log(" unauth_sub_decision 처리 = " + row["unauth_sub_decision"].ToString());
                //Debug.Log(" inspection_status 처리 = " + row["inspection_status"].ToString());

                if (row["unauth_sub_decision"].ToString() != "0" && row["inspection_status"].ToString() == "1")
                {
                    allPass = false;
                    break;
                }
            }

        }
        else
        {

            // 선택한것이 없으므로 false;
            allPass = false;
        }



        return allPass;


    }
    
    // 선택한것이 점검여부가 모두 Y인지 체크
    public bool AreAllStatusY()
    {
        bool allY = true;

        DataRow[] rows = resultTable.Select("isSelect = True");

        // Debug.Log("rows : " + rows.Length);

        if(rows.Length > 0)
        {
            foreach (DataRow row in rows)
            {
                // Debug.Log(" AreAllStatusY 점검여부 = " + row["점검여부"].ToString());

                if (row["점검여부"].ToString() == "N" || row["점검여부"].ToString() == "Z")
                {
                    allY = false;
                    break;
                }
            }
        }
        else
        {
            // 선택한것이 없으므로 false;
            allY = false;
        }

        return allY;

    }

    public void Check_SelectItem()
    {
        // 전부 선택시
        if (AreAllTogglesOn())
        {
            isAll = true;
            btn_On.gameObject.SetActive(true);
        }
                
        if (AreAllTogglesOff())
        {
            btn_checkProcess.interactable = false;
            btn_checkResultProcess.interactable = false;
        }

        // 선택한것중에 점검완료가 아닌게 있는지 체크

        // Debug.Log("AreAllStatusY() : " + AreAllStatusY());

        if (AreAllStatusY())
        {
            // 선택한것이 모두 점검 완료일 경우엔
            btn_checkResultProcess.interactable = true;
        }
        else
        {
            btn_checkResultProcess.interactable = false;
        }

        // Debug.Log("AreAllResultPass() : " + AreAllResultPass());

        // 통과일 경우에 점검여부를 처리하고 통과가 아닐떄는 점검여부버튼 비활성화
        if (AreAllResultPass())
        {
            btn_checkProcess.interactable = true;
        }
        else
        {
            btn_checkProcess.interactable = false;
        }

    }


    public void ResetButton(bool check)
    {
        //true일때 초기화 아니면 setActive false
        if (check)
        {
            Reset();
            dropdown_check.value = 0;
            dropdown_count.value = 0;

            isAll = false;
            btn_On.gameObject.SetActive(false);

            btn_checkProcess.interactable = false;
            btn_checkResultProcess.interactable = false;

            ClearListView();

            // NO로 정렬 지정
            Clear_Sort();

            // 기본 차순 NO로 오름 차순 정렬한다.
            CheckTable = null;
            listTable = null;
            OpenTable = null;
            resultTable = null;

            item_Empty.SetActive(true);

            //selectMenu reset
            selectMenu.ResetData();
        }

        //ResetModal.SetActive(false);
    }

    public void DeleteReset()
    {
        Reset();
        dropdown_check.value = 0;
        dropdown_count.value = 0;

        isAll = false;
        btn_On.gameObject.SetActive(false);

        btn_checkProcess.interactable = false;
        btn_checkResultProcess.interactable = false;

        ClearListView();

        // NO로 정렬 지정
        Clear_Sort();

        // 기본 차순 NO로 오름 차순 정렬한다.
        CheckTable = null;
        listTable = null;
        OpenTable = null;
        resultTable = null;
    }

    // 출력
    public void Action_Print()
    {
        ClearPrintList();

        Debug.Log("Action_Print");

        //for (int i = 0; i < menuTitles.Length; i++)
        //{
        //    Debug.Log($"menuTitles index {i} : " + menuTitles[i].transform.GetSiblingIndex());
        //}

        printCanvas.SetActive(true);
        printCamBox.SetActive(true);
        
        int requiredCameraCount = Mathf.Clamp((list_Check.Count - 1) / 25 + 1, 1, 4);
        int maxCameraCount = Mathf.Min(requiredCameraCount, printCamera.Length) ;

        Debug.Log("maxCameraCount : " + maxCameraCount);

        for (int i = 0; i < maxCameraCount; i++)
        {
            printCamera[i].gameObject.SetActive(true);
            menuTitles[i].gameObject.SetActive(true);
        }
    
        // 출력 아이템 켜고

        // 출력 아이템 생성하고 
        SetPrintView();

        // StandaloneFileBrowser.SaveFilePanel("엑셀다운로드", "", "", "");

        //string path =  StandaloneFileBrowser.SaveFilePanel("엑셀다운로드", "", "", "");

        //Debug.Log("Select Path : " + path);
        //string basefilename = $"{MenuName}";

        string path = StandaloneFileBrowser.SaveFilePanel("출력 저장", "", MenuName, "");

        if (!string.IsNullOrEmpty(path))
        {
            string currentTime = DateTime.Now.ToString("yyyyMMdd_HHmmss"); // 현재 시간을 yyyyMMdd_HHmmss 형식으로 가져옴
            string originalFileName = Path.GetFileNameWithoutExtension(path); // 기존 파일명 가져오기
            string extension = Path.GetExtension(path); // 확장자 가져오기
            string directoryPath = Path.GetDirectoryName(path); // 새로운 파일 경로

            Debug.Log("Select Path : " + path);
            Debug.Log("Select directoryPath : " + directoryPath);
            Debug.Log("Select currentTime : " + currentTime);
            Debug.Log("Select originalFileName : " + originalFileName);
            Debug.Log("Select extension : " + extension);

            for (int i = 0; i < maxCameraCount; i++)
            {
                printCamera[i].path = directoryPath;
                printCamera[i].filename = currentTime + "_" + MenuName;
                printCamera[i].init();

                printCamera[i].ClickScreenShot();
            }

            printCanvas.SetActive(false);
            printCamBox.SetActive(false);

        }
        /*
        StandaloneFileBrowser.SaveFilePanelAsync("출력 저장", "", basefilename, "", (string path) =>
        {
            if(!string.IsNullOrEmpty(path))
            {
                string currentTime = DateTime.Now.ToString("yyyyMMdd_HHmmss"); // 현재 시간을 yyyyMMdd_HHmmss 형식으로 가져옴
                string originalFileName = Path.GetFileNameWithoutExtension(path); // 기존 파일명 가져오기
                string extension = Path.GetExtension(path); // 확장자 가져오기
                string directoryPath = Path.GetDirectoryName(path); // 새로운 파일 경로

                Debug.Log("Select Path : " + path);
                Debug.Log("Select directoryPath : " + directoryPath);
                Debug.Log("Select currentTime : " + currentTime);
                Debug.Log("Select originalFileName : " + originalFileName);
                Debug.Log("Select extension : " + extension);

                for (int i = 0; i < maxCameraCount; i++)
                {
                    printCamera[i].path = directoryPath;
                    printCamera[i].filename = currentTime + "_" + basefilename;
                    printCamera[i].init();

                    printCamera[i].ClickScreenShot();
                }
            }
        });*/

    }

    public void ClearPrintList()
    {
        Debug.Log("ClearPrintList");

        for (int i = 0; i < printItems.Length; i++)
        {
            printItems[i].Close();
        }

        for (int i = 0; i < menuTitles.Length; i++)
        {
            menuTitles[i].SetActive(false);
            printCamera[i].gameObject.SetActive(false);
        }
    }

    // 출력용 아이템 생성
    public void SetPrintView()
    {
        Debug.Log("SetPrintView");

        Debug.Log("list_Check.Count : " + list_Check.Count);

        for (int i = 0; i < list_Check.Count; i++)
        {
            printItems[i].item_Check = list_Check[i];

            printItems[i].Open();

            printItems[i].Init();
            
        }
        //for (int i = 0; i < list_Check.Count; i++)
        //{
        //    Item_CheckPrint item = Instantiate(item_CheckPrint, printContents);

        //    item.item_Check = list_Check[i];

        //    item.Init();
        //}
    }

    // 메뉴 펼치기
    public void Action_Open()
    {
        //for (int i = 0; i < UI_Search.Length; i++)
        //{
        //    UI_Search[i].SetActive(true);ㅂ
        //}

        searchContent.sizeDelta = openSize;

        btn_menuOpen.gameObject.SetActive(false);
        btn_menuClose.gameObject.SetActive(true);

        isMenuOpen = true;

        LayoutRebuilder.ForceRebuildLayoutImmediate(menuContent);
        LayoutRebuilder.ForceRebuildLayoutImmediate(searchContent);

        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)parent);

    }

    // 메뉴 접기
    public void Action_Close()
    {
        //for (int i = 0; i < UI_Search.Length; i++)
        //{
        //    UI_Search[i].SetActive(false);
        //}

        searchContent.sizeDelta = closeSize;

        btn_menuOpen.gameObject.SetActive(true);
        btn_menuClose.gameObject.SetActive(false);

        isMenuOpen = false;

        LayoutRebuilder.ForceRebuildLayoutImmediate(menuContent);
        LayoutRebuilder.ForceRebuildLayoutImmediate(searchContent);

        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)parent);

    }

    // 특정값 칼럼 갯수 구하기
    public int CountRowsWithCondition(DataTable table, string columnName, string value)
    {
        string filterExpression = $"{columnName} = '{value}'";
        DataRow[] filteredRows = table.Select(filterExpression);
        int count = filteredRows.Length;

        return count;
    }

    public string GetCurrentDate()
    {
        return DateTime.Now.ToString(("yyyy.MM.dd HH:mm:ss"));
    }

    public bool[] CheckSearchDate(bool[] _searchDate)
    {

        for (int i = 0; i < toggle_searchDate.Length; i++)
        {
            _searchDate[i] = false;

            if (toggle_searchDate[i].isOn)
            {
                _searchDate[i] = true;
            }
        }

        return _searchDate;

    }
}
