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

public class ViewDangerous : MonoBehaviour
{
    public string MenuName = "위험도 산출";

    public MainManager mainManager;

    public ServerManager serverManager;

    public ViewExecute viewExecute;

    public Item_RiskData item_RiskData;

    public GameObject item_Empty;

    public Transform contents;

    public Transform parent;

    public RectTransform[] viewupdate;

    // UI
    public GameObject UI_Fileitem;

    // Text
    public TextMeshProUGUI Text_FileName;

    // 전체항목
    public TextMeshProUGUI Text_totalCount;
    
    // Buttons
    public Button btn_FileLoad;

    public Button btn_DeleteFile;

    // 위험도 산출
    public Button btn_ActionLauncher;

    // 산출 결과 전송
    public Button btn_SendRiskData;

    // 엑셀 다운로드
    public Button btn_Excel;


    // DropDown
    public TMP_Dropdown dropdown_count;

    public string _path;

    public string _selectPath;

    public string _resultPath = @"C:\korea_rebs\Result.csv";

    public string _sendPath = @"C:\korea_rebs\SendResult.csv";

    public string filename;

    public DataTable riskTable;
    public DataTable listTable;

    [SerializeField]
    List<Item_RiskData> list_risk = new List<Item_RiskData>();
    
    public int listCount;    

    public int[] listCounts = { 10, 20, 50, 100 };

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
        "sale_rf_prob", "sale_rf_per50", "sale_rf_per25", "sale_rf_per10", "sale_xgb_prob" ,"sale_xgb_per50", "sale_xgb_per25", "sale_xgb_per10"};

    public string[] korcolname = {"주택관리번호", "민영국민", "주택명", "동", "호수", "공급유형", "특별공급종류", "공급위치_시군구코드", "크기", "일반공급", "특별공급", "공급금액", "생년", "성명",
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
"ad_총점", "ad_변경시점2", 
        "부정청약_rf_prob", "부정청약_rf_0.5", "부정청약_rf_0.25", "부정청약_rf_0.1", "부정청약_xgb_prob", "부정청약_xgb_0.5", "부정청약_xgb_0.25", "부정청약_xgb_0.1",
        "위장전입_rf_prob", "위장전입_rf_0.5", "위장전입_rf_0.25", "위장전입_rf_0.1", "위장전입_xgb_prob", "위장전입_xgb_0.5", "위장전입_xgb_0.25", "위장전입_xgb_0.1", 
        "매매_rf_prob", "매매_rf_0.5", "매매_rf_0.25","매매_rf_0.1","매매_xgb_prob", "매매_xgb_0.5", "매매_xgb_0.25", "매매_xgb_0.1"};


    private string[] savecolname = {"data_no", "주택관리번호", "민영국민", "주택명", "동", "호수", "공급유형", "특별공급종류", "공급위치_시군구코드", "크기", "일반공급", "특별공급", "공급금액", "생년", "성명",
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
"ad_총점", "ad_변경시점2", "부정청약_rf_prob", "부정청약_rf_0.5", "부정청약_rf_0.25", "부정청약_rf_0.1", "부정청약_xgb_prob", "부정청약_xgb_0.5", "부정청약_xgb_0.25", "부정청약_xgb_0.1","위장전입_rf_prob",
"위장전입_rf_0.5", "위장전입_rf_0.25", "위장전입_rf_0.1", "위장전입_xgb_prob", "위장전입_xgb_0.5", "위장전입_xgb_0.25", "위장전입_xgb_0.1", "매매_rf_prob", "매매_rf_0.5", "매매_rf_0.25","매매_rf_0.1",
"매매_xgb_prob", "매매_xgb_0.5", "매매_xgb_0.25", "매매_xgb_0.1", "inspection_status_result"};

    // 전체항목
    public int totalCount;

    public string[] col;
    public string[] cols;

    public string currentTime = "";


    // 정렬 버튼 
    public SortButton[] sortButtons;

    public Sprite Img_ASC; // 오름차순 이미지
    public Sprite Img_DESC; // 내림 차순 이미지

    public string SortTarget; // 정렬 기준


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

    // Start is called before the first frame update
    void Start()
    {
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

        Init();

        btn_FileLoad.onClick.AddListener(() =>
        {
            // 파일로드
            Action_FileLoad();
        });

        btn_DeleteFile.onClick.AddListener(() =>
        {
            // 파일 경로 삭제
            Action_DeleteFileItem();
        });

        btn_ActionLauncher.onClick.AddListener(() =>
        {
            // 위험도 실행
            Action_Launcher();
        });

        // 산출 결과 전송
        btn_SendRiskData.onClick.AddListener(() =>
        {
            CommonPopup commonPopup = Instantiate(mainManager.commonPopup, mainManager.contents);

            commonPopup.ShowPopup_TwoButton(
            "위험도 산출 완료된 파일을 실시 · 등록 메뉴로 \n" +
            "전송하시겠습니까?",
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

                // 데이터 서버 저장 
                Action_SendData();

                //TaskOnPrepare(); 
            });


        });


        // 엑셀 다운로드
        btn_Excel.onClick.AddListener(Action_Excel);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    string[] checkNames = { "apt_number", "apt_dong", "apt_hosu", "apt_sup_type", "apt_sp_sup", "user_birth", "user_name" };

    public async Task Send_Data()
    {
        // riskTable 전송

        Debug.Log("ROWS COUNT : " + riskTable.Rows.Count);

        int saveCount = 0;
        foreach (DataRow rows in riskTable.Rows)
        {
            //Debug.Log("rows name : " + rows["성명"].ToString());
            await Task.Delay(1);

            //Debug.Log("rows.0 : " + rows[0].ToString());
            //Debug.Log("rows.3 : " + rows[3].ToString());
            //Debug.Log("rows.4 : " + rows[4].ToString());
            //Debug.Log("rows.5 : " + rows[5].ToString());
            //Debug.Log("rows.6 : " + rows[6].ToString());
            //Debug.Log("rows.12 : " + rows[12].ToString());
            //Debug.Log("rows.13 : " + rows[13].ToString());


            //serverManager.InsertRiskData("tb_risk", colname, rows.ItemArray);
            serverManager.UpdateRiskData("tb_checkresult", colname, checkNames, rows.ItemArray);

            saveCount += 1;

            mainManager.loadMsg.text = saveCount + " / " + riskTable.Rows.Count;

        }

        //for (int i = 0; i < riskTable.Rows.Count; i++)
        //{
        //    await Task.Delay(1);

        //    serverManager.UpdateRiskData("tb_riskdata", colname, checkNames, riskTable.Rows[i].ItemArray);

        //    Debug.Log($"{i} / {riskTable.Rows.Count}"); 
        //}
    }


    // 엑셀 다운로드
    public void Action_Excel()
    {
        if (riskTable == null)
        {
            return;
        }
        else
        {
            if (riskTable.Rows.Count == 0)
            {
                return;
            }
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
                DataTable saveTable = riskTable.Copy();

                string[] columnsToRemove = { "NO", "AverageProb" }; // 삭제할 칼럼들의 이름 배열

                foreach (string columnName in columnsToRemove)
                {
                    if (saveTable.Columns.Contains(columnName))
                    {
                        saveTable.Columns.Remove(columnName);
                    }
                }

                SaveDataTableToCsv(saveTable, path);

                Debug.Log("File saved at: " + path);

                if (!string.IsNullOrEmpty(_path))
                {
                    //string originalFileName = Path.GetFileNameWithoutExtension(_path); // 기존 파일명 가져오기
                    //mainManager.DownloadLodInsert(MenuName, originalFileName + " 엑셀다운로드");

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

    private void SaveDataTableToCsv(DataTable dataTable, string filePath)
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
            streamWriter.WriteLine(string.Join(",", savecolname));

            // 데이터 쓰기
            foreach (DataRow row in dataTable.Rows)
            {
                List<string> values = new List<string>();
                foreach (DataColumn column in dataTable.Columns)
                {
                    if (row[column].ToString().Contains(","))
                    {
                        //Debug.Log("쉼표가 있어" + row[column].ToString());

                        row[column] = $"\"{row[column]}\"";

                        //Debug.Log("쉼표가 있어쮸" + row[column].ToString());

                        // row[column] = $"\"{row[column]}\"";

                        //try
                        //{
                        //    row[column] = $"\"{row[column]}\"";
                        //    Debug.Log("쉼표가 있어서 추가 했어 " + row[column].ToString());

                        //}
                        //catch (Exception e)
                        //{
                        //    Debug.Log("쉼표가 있어" + e.ToString());
                        //}
                    }

                    values.Add(row[column].ToString());
                }
                streamWriter.WriteLine(string.Join(",", values));
            }
        }
    }


    public void Set_Sort(SortButton sortButton)
    {
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

        if (riskTable != null)
        {
            UpdateUI();
        }

        Debug.Log(" SORT " + SortTarget);
    }


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

    public void Clear_Sort()
    {
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

    // 이전 페이지로 이동하는 함수
    void PreviousPage()
    {
        currentPage--;
        if (currentPage < 1)
            currentPage = 1;

        UpdateUI();
    }

    // 다음 페이지로 이동하는 함수
    void NextPage()
    {
        currentPage++;
        int maxPage = Mathf.CeilToInt((float)riskTable.Rows.Count / itemsPerPage);
        if (currentPage > maxPage)
            currentPage = maxPage;

        UpdateUI();
    }

    // 이전 10 페이지로 이동하는 함수
    void Previous10Pages()
    {
        if (riskTable != null)
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
        if (riskTable != null)
        {
            currentPage += 10;
            int maxPage = Mathf.CeilToInt((float)riskTable.Rows.Count / itemsPerPage);
            if (currentPage > maxPage)
                currentPage = maxPage;

            UpdateUI();
        }
    }

    // 첫 페이지로 이동하는 함수
    void GoToFirstPage()
    {
        if (riskTable != null)
        {
            currentPage = 1;
            UpdateUI();
        }
    }

    // 마지막 페이지로 이동하는 함수
    void GoToLastPage()
    {
        if (riskTable != null)
        {
            int maxPage = Mathf.CeilToInt((float)riskTable.Rows.Count / itemsPerPage);
            currentPage = maxPage;
            UpdateUI();
        }
    }

    // 특정 페이지로 이동하는 함수
    void GoToPage(int pageNumber)
    {
        Debug.Log(" GOTOPAGE :" + pageNumber);

        int maxPage = Mathf.CeilToInt((float)riskTable.Rows.Count / itemsPerPage);
        int buttonOffset = (currentPage - 1) / 10 * 10; // Calculate the offset for the page buttons

        int targetPage = buttonOffset + pageNumber; // Calculate the target page based on the current offset

        if (targetPage < 1)
            targetPage = 1;

        if (targetPage > maxPage)
            targetPage = maxPage;

        currentPage = targetPage;

        UpdateUI();
    }

    void SetUpdateUI()
    {
        // 페이지 번호 버튼 업데이트
        int maxPage = Mathf.CeilToInt((float)riskTable.Rows.Count / itemsPerPage);

        Debug.Log("maxPage :" + maxPage);

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
        int maxPage = Mathf.CeilToInt((float)riskTable.Rows.Count / itemsPerPage);

        Debug.Log("maxPage :" + maxPage);
        Debug.Log("currentPage :" + currentPage);

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
        if (riskTable != null)
        {
            UpdateView();
        }
    }

    // Init
    public void Init()
    {
        totalCount = 0;

        dropdown_count.value = 0;

        //_path = @"C:\korea_rebs\dataframe_add.csv";

        //_selectPath = _path;
        _selectPath = @"C:\korea_rebs\dataframe_add.csv";

        _resultPath = @"C:\korea_rebs\Result.csv";


        // Debug.Log("<color=yellow>IsNullOrEmpty : " + string.IsNullOrEmpty(_path) + "</color>");

        if (!string.IsNullOrEmpty(_path))
        {
            string originalFileName = Path.GetFileNameWithoutExtension(_path); // 기존 파일명 가져오기
            string extension = Path.GetExtension(_path); // 확장자 가져오기

            filename = $"{originalFileName}{extension}";

            Text_FileName.text = filename;

            UI_Fileitem.SetActive(true);

            // 위험도 산출 버튼 활성화
            btn_ActionLauncher.interactable = true;

            btn_ActionLauncher.GetComponentInChildren<TextMeshProUGUI>().color = mainManager.OnTextColor;
        }
        else
        {
            Text_FileName.text = "";

            UI_Fileitem.SetActive(false);

            // 위험도 산출 버튼 활성화
            btn_ActionLauncher.interactable = false;

            btn_ActionLauncher.GetComponentInChildren<TextMeshProUGUI>().color = mainManager.OffTextColor;
        }
    
    }

    public void getCount()
    {
        Text_totalCount.text = "전체항목(" + totalCount + ")";
    }

    // 위험도 산출 결과 로드
    public void Action_ResultCSVLoad()
    {
        // var path = AssetDatabase.GetAssetPath(csv);
        // System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        // Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // 한글 깨짐 현상 해결 가능
        var config = new ExcelReaderConfiguration();

        //config.FallbackEncoding = System.Text.Encoding.GetEncoding("ks_c_5601-1987");
        // config.FallbackEncoding = Encoding.GetEncoding("51949");

        //config.FallbackEncoding = Encoding.GetEncoding("cp949");
        config.FallbackEncoding = Encoding.GetEncoding("EUC-KR");


        var streamer = new FileStream(_resultPath, FileMode.Open, FileAccess.Read);

        using (var reader = ExcelReaderFactory.CreateCsvReader(streamer, config))
        {
            // 항상 하나의 시트만 관리된다.
            riskTable = reader.AsDataSet().Tables[0].Copy();

            // 시트 이름
            //Debug.Log($"riskTable Name: {riskTable.TableName}");
            
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

                    riskTable.Columns[i].ColumnName = columnName;
                }
            }

            // 칼럼열 삭제
            riskTable.Rows.RemoveAt(0);

            /*
            for (var rowIndex = 0; rowIndex < prepareTable.Rows.Count; rowIndex++)
            {
                // 행 가져오기
                var slot = riskTable.Rows[rowIndex];
                
                for (var columnIndex = 0; columnIndex < slot.ItemArray.Length; columnIndex++)
                {
                    // 열 가져오기
                    var item = slot.ItemArray[columnIndex];
                    // Debug.Log($"slot[{rowIndex}][{columnIndex}] : {item}");
                }
            }
            */
            reader.Dispose();
            reader.Close();
        }

        // 칼럼명 저장하기
        //for (int i = 0; i < riskTable.Rows[0].ItemArray.Length; i++)
        //{
        //    riskTable.Columns[i].ColumnName = riskTable.Rows[0].ItemArray[i].ToString();
        //}

        //col = riskTable.Columns.Cast<DataColumn>()
        //                   .Select(x => x.ColumnName)
        //                   .ToArray();

        riskTable.Columns.Add(new DataColumn("NO", typeof(int)));

        // 위험도 평균 칼럼 추가
        DataColumn averageColumn = new DataColumn("AverageProb", typeof(double));
        //averageColumn.Expression = "(부정청약_rf_prob + 부정청약_xgb_prob) / 2 * 100";
        averageColumn.DefaultValue = 0;

        riskTable.Columns.Add(averageColumn);

        // 데이터 index 칼럼 추가
        DataColumn noColum = new DataColumn("data_no", typeof(string));        
        noColum.Expression = "주택관리번호 + 동 + 호수 + 공급유형 + 특별공급종류 + 생년+ 성명";   
        
        riskTable.Columns.Add(noColum);

        riskTable.Columns["data_no"].SetOrdinal(0);
        riskTable.Columns["data_no"].ColumnName = "data_no";
        Debug.Log("data_no : " + riskTable.Columns["data_no"]);

        // 점검여부열 추가
        DataColumn dc = new DataColumn("inspection_status_result", typeof(string));
        dc.ColumnName = "inspection_status_result";
        dc.DefaultValue = "N";

        riskTable.Columns.Add(dc);

        //for (int i = 0; i < riskTable.Rows[0].ItemArray.Length; i++)
        //{
        //    Debug.Log(riskTable.Rows[0][i].ToString());
        //}

        // Debug.Log("riskTable.Rows[0] : " + riskTable.Rows[0].ToString());

        // 서버에 저장 후 ? 

        SetListView();

        btn_SendRiskData.interactable = true;

    }

    // 산출결과 전송 
    async void Action_SendData()
    {
        mainManager.viewLoading.Open();

        Debug.Log("전송시작");

        string originalFileName = Path.GetFileNameWithoutExtension(_path); // 기존 파일명 가져오기
        string extension = Path.GetExtension(_path); // 확장자 가져오기

        string searchString = "_전처리";

        int startIndex = originalFileName.IndexOf(searchString);

        //Debug.Log("startIndex : " + startIndex);

        if (startIndex != -1)
        {
            originalFileName = originalFileName.Substring(0, startIndex);
        }

        string[] files = Directory.GetFiles(Path.GetDirectoryName(_path), "*.csv");
        string baseFileName = $"{originalFileName}_위험도산출";


        int count = 0;
        foreach (string filePath in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            // 파일 이름이 지정된 기본 파일 이름과 유사한 경우 개수 증가
            if (fileName.StartsWith(baseFileName))
            {
                count++;
            }
        }

        Debug.Log(" 비슷한 파일 갯수는 :" + count);
        string copyFileName = $"{originalFileName}_위험도산출{(count+1)}{extension}"; // 현재 시간을 포함한 새로운 파일명 생성


        //string copyPath = Path.Combine(Path.GetDirectoryName(_path), Path.GetFileNameWithoutExtension(_path) + "_전처리.csv"));
        string copyPath = Path.Combine(Path.GetDirectoryName(_path), copyFileName); // 새로운 파일 경로 생성

        // 산출 파일 저장용
        string sendPath = Path.Combine(Path.GetDirectoryName(_resultPath), $"SendResult{extension}");


        Debug.Log("SEND PATH : " + sendPath);

        //Debug.Log("COPY PATH : " + copyPath);

        // CopyFile(_resultPath, copyPath);

        // 산출 파일 전송

        DataTable saveTable = riskTable.Copy();

        string[] columnsToRemove = { "NO", "AverageProb" }; // 삭제할 칼럼들의 이름 배열

        foreach (string columnName in columnsToRemove)
        {
            if (saveTable.Columns.Contains(columnName))
            {
                saveTable.Columns.Remove(columnName);
            }
        }

        // 위험도 산출 파일 백업
        SaveDataTableToCsv(saveTable, copyPath);

        // 위험도 산출 저장 파일 복사cccc  
        CopyFile(copyPath, sendPath);
        
        if(File.Exists(_sendPath))
        {
            mainManager.viewLoading.Open();

            mainManager.isPython = true;

            mainManager.loadMsg.text = "산출 데이터 저장 Processing... ";

            var result = await mainManager.RunProcessAsync("python", "SendInsertUpdate.py");

            Debug.Log("Result: " + result.ToString());

            mainManager.viewLoading.Close();            
            
            mainManager.isPython = false;

            if (result == 0)
            {
                // 산출 성공시

                // 실시 등록에 경로 복사
                // viewExecute._path = copyPath;

                //string names = Path.GetFileNameWithoutExtension(_path);

                //mainManager.ChangeLogInsert(MenuName, $"{names} 위험도 산출");

                // viewExecute.Action_DeleteFileItem();

                viewExecute.SendInit();

                mainManager.gnbViewControl.SelectMenu_01(2);
            }
            else
            {
                // 산출 실패 알림

            }

        }
    }


    // 위험도 산출 
    async void Action_Launcher()
    {
        //Action_ResultCSVLoad();


        // 선택한 파일 복사한다
        //if(File.Exists(_selectPath) == false)
        //{
        //    CopyFile(_path, _selectPath);
        //}

        CopyFile(_path, _selectPath);

        if (riskTable != null)
        {
            riskTable.Clear();
            riskTable.Dispose();
            riskTable = null;
        }

        if (File.Exists(_selectPath))
        {
            mainManager.viewLoading.Open();

            mainManager.isPython = true;

            Debug.Log("Processing...");

            mainManager.loadMsg.text = "위험도 산출 Processing... ";

            var result = await mainManager.RunProcessAsync("python", "Launcher.py");

            Debug.Log("Result: " + result.ToString());

            mainManager.viewLoading.Close();

            mainManager.isPython = false;

            // 위험도 산출 할때 시간 저장
            currentTime = GetCurrentDate();

            if (result == 0)
            {
                // 산출 성공시
                Action_ResultCSVLoad();

                string names = Path.GetFileNameWithoutExtension(_path);

                mainManager.ChangeLogInsert(MenuName, $"{names} 위험도 산출");

            }
            else
            {
                // 산출 실패 알림
                
            }
        }

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

        if (contents.childCount > 0)
        {
            ClearListView();
        }

        _path = "";

        _path = paths[0];

        string originalFileName = Path.GetFileNameWithoutExtension(_path); // 선택 파일명 가져오기
        string extension = Path.GetExtension(_path); // 확장자 가져오기
        string searchString = "_전처리";

        int startIndex = originalFileName.IndexOf(searchString);
        
        //Debug.Log("startIndex : " + startIndex);

        if (startIndex != -1)
        {
            originalFileName = originalFileName.Substring(0, startIndex);
        }            

        //originalFileName = originalFileName.Replace("_전처리", "");

        filename = $"{originalFileName}{extension}";

        Text_FileName.text = filename;

        UI_Fileitem.SetActive(true);

        if (riskTable != null)
        {
            riskTable.Clear();
            riskTable.Dispose();
            riskTable = null;
        }

        btn_ActionLauncher.interactable = true;

        btn_ActionLauncher.GetComponentInChildren<TextMeshProUGUI>().color = mainManager.OnTextColor;
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
        var children = contents.transform.GetComponentsInChildren<Item_RiskData>(true);

        foreach (var child in children)
        {
            //if (child == contents) continue;
            //child.parent = null;

            Destroy(child.gameObject);
        }

        list_risk.Clear();
    }
    public void UpdateView()
    {
        ClearListView();

        // 기본 차순 NO로 오름 차순 정렬한다.
        riskTable.DefaultView.Sort = SortTarget;


        listTable = null;

        listTable = riskTable.DefaultView.ToTable();

        int startIndex = (currentPage - 1) * itemsPerPage;
        int endIndex = Mathf.Min(startIndex + itemsPerPage, listTable.Rows.Count);

        Debug.Log($"<color=yellow>UpdateView startIndex {startIndex}, endIndex {endIndex}</color>");

        for (var rowIndex = startIndex; rowIndex < endIndex; rowIndex++)
        {
            // 행 가져오기
            var slot = listTable.Rows[rowIndex];

            Item_RiskData item = Instantiate(item_RiskData, contents);

            // 그룸 하면 안됨
            // item.toggle.group = toggleGroups;

            // item.no.text = slot["NO"].ToString();
            item.no.text = (rowIndex + 1).ToString();

            item.aptNum.text = slot["주택관리번호"].ToString();
            item.aptName.text = slot["주택명"].ToString();
            item.aptDong.text = slot["동"].ToString();
            item.aptHosu.text = slot["호수"].ToString();

            if ((slot["공급위치_시군구코드"].ToString().Trim().Length == 0) == false)
            {
                item.region.text = getMapName(slot["공급위치_시군구코드"].ToString());
            }

            item.userName.text = slot["성명"].ToString();

            item.aptdate.text = currentTime;

            item.relation.text = slot["세대주관계"].ToString();

            // 위험도 저장 
            //double unauthRF = double.Parse(slot["부정청약_rf_prob"].ToString());
            // double unauthXG = double.Parse(slot["부정청약_xgb_prob"].ToString());

            // double resultrisk = (unauthRF + unauthXG) / 2 * 100;

            //double resultrisk = double.Parse(slot["AverageProb"].ToString());
            double resultrisk = (double)slot["AverageProb"];

            // Debug.Log("AverageProb : " + resultrisk);

            item.userRisk.text = (Mathf.Floor(float.Parse(resultrisk.ToString()) * 10f) / 10f).ToString("N1");

            item.viewDangerous = this;

            list_risk.Add(item);
        }

        totalCount = riskTable.Rows.Count;

        if (list_risk.Count == 0)
        {
            item_Empty.SetActive(true);
        }
        else
        {
            item_Empty.SetActive(false);
        }

        getCount();

        ResetView();
    }

    // 재 계산이 필요할때
    public void UpdateListView()
    {
        ClearListView();

        for (var rowIndex = 0; rowIndex < listCount; rowIndex++)
        {
            // 행 가져오기
            var slot = riskTable.Rows[rowIndex];

            Item_RiskData item = Instantiate(item_RiskData, contents);

            // 그룸 하면 안됨
            // item.toggle.group = toggleGroups;           

            //item.no.text = slot["NO"].ToString();            
            item.no.text = (rowIndex + 1).ToString();

            item.aptNum.text = slot["주택관리번호"].ToString();
            item.aptName.text = slot["주택명"].ToString();
            item.aptDong.text = slot["동"].ToString();
            item.aptHosu.text = slot["호수"].ToString();

            if ((slot["공급위치_시군구코드"].ToString().Trim().Length == 0) == false)
            {
                item.region.text = getMapName(slot["공급위치_시군구코드"].ToString());
            }

            item.userName.text = slot["성명"].ToString();

            item.aptdate.text = currentTime;

            item.relation.text = slot["세대주관계"].ToString();

            // 위험도 저장 
            //double unauthRF = double.Parse(slot["부정청약_rf_prob"].ToString());
            // double unauthXG = double.Parse(slot["부정청약_xgb_prob"].ToString());

            // double resultrisk = (unauthRF + unauthXG) / 2 * 100;

            double resultrisk = double.Parse(slot["AverageProb"].ToString());
            // ebug.Log("AverageProb : " + resultrisk);

            item.userRisk.text = (Mathf.Floor(float.Parse(resultrisk.ToString()) * 10f) / 10f).ToString("N1");

            item.viewDangerous = this;


            list_risk.Add(item);
        }

        totalCount = riskTable.Rows.Count;

        if (list_risk.Count == 0)
        {
            item_Empty.SetActive(true);
        }
        else
        {
            item_Empty.SetActive(false);
        }

        getCount();
        ResetView();
    }

    public void SetListView()
    {
        ClearListView();

        // NO로 정렬 지정
        Clear_Sort();

        for (var rowIndex = 0; rowIndex < riskTable.Rows.Count; rowIndex++)
        {
            // 행 가져오기
            var slot = riskTable.Rows[rowIndex];

            slot["NO"] = rowIndex + 1;

            //Debug.Log("부정청약_rf_prob : " + slot["부정청약_rf_prob"].ToString());
            //Debug.Log("부정청약_xgb_prob : " + slot["부정청약_xgb_prob"].ToString());

            // 위험도 평균 산출
            double unauthRF = double.Parse(slot["부정청약_rf_prob"].ToString());
            double unauthXG = double.Parse(slot["부정청약_xgb_prob"].ToString());

            double resultrisk = (unauthRF + unauthXG) / 2 * 100;

            slot["AverageProb"] = resultrisk;
        }

        // 기본 차순 NO로 오름 차순 정렬한다.
        riskTable.DefaultView.Sort = SortTarget;

        listTable = riskTable.DefaultView.ToTable();

        int endIndex = Mathf.Min(listCount, listTable.Rows.Count);

        for (var rowIndex = 0; rowIndex < endIndex; rowIndex++)
        {
            // 행 가져오기
            var slot = riskTable.Rows[rowIndex];

            Item_RiskData item = Instantiate(item_RiskData, contents);

            // 그룸 하면 안됨
            // item.toggle.group = toggleGroups;

            //item.no.text = slot["NO"].ToString();
            item.no.text = (rowIndex + 1).ToString();

            item.aptNum.text = slot["주택관리번호"].ToString();
            item.aptName.text = slot["주택명"].ToString();
            item.aptDong.text = slot["동"].ToString();
            item.aptHosu.text = slot["호수"].ToString();

            if ((slot["공급위치_시군구코드"].ToString().Trim().Length == 0) == false)
            {
                item.region.text = getMapName(slot["공급위치_시군구코드"].ToString());
            }

            item.userName.text = slot["성명"].ToString();

            item.aptdate.text = currentTime;           

            item.relation.text = slot["세대주관계"].ToString();

            // 위험도 저장 
            //double unauthRF = double.Parse(slot["부정청약_rf_prob"].ToString());
            // double unauthXG = double.Parse(slot["부정청약_xgb_prob"].ToString());

            // double resultrisk = (unauthRF + unauthXG) / 2 * 100;

            //double resultrisk = double.Parse(slot["AverageProb"].ToString());
            double resultrisk = (double)slot["AverageProb"];

            // Debug.Log("AverageProb : " + resultrisk);

            item.userRisk.text = (Mathf.Floor(float.Parse(resultrisk.ToString()) * 10f) / 10f).ToString("N1");

            item.viewDangerous = this;

            list_risk.Add(item);
        }

        totalCount = riskTable.Rows.Count;

        if (list_risk.Count == 0)
        {
            item_Empty.SetActive(true);
        }
        else
        {
            item_Empty.SetActive(false);            
        }

        getCount();

        ResetView();

        // 처음으로 세팅

        GoToFirstPage();
    }

    public void Action_DeleteFileItem()
    {
        _path = "";

        Text_FileName.text = "";
        
        UI_Fileitem.SetActive(false);

        btn_ActionLauncher.interactable = false;
        btn_ActionLauncher.GetComponentInChildren<TextMeshProUGUI>().color = mainManager.OffTextColor;

        btn_SendRiskData.interactable = false;

        if (riskTable != null)
        {
            riskTable.Clear();
            riskTable.Dispose();
            riskTable = null;
        }

        if (contents.childCount > 0)
        {
            ClearListView();
        }

        totalCount = 0;

        getCount();

    }


    public string GetCurrentDate()
    {
        return DateTime.Now.ToString(("yyyy.MM.dd HH:mm:ss"));
    }

    public void ListCountDropdownValueChanged(TMP_Dropdown change)
    {
        Debug.Log("New Value : " + change.value);

        listCount = listCounts[change.value];

        itemsPerPage = listCount;

        // 파일 아이템이 있을 경우에만 새로 그린다

        if (riskTable != null)
        {
            UpdateListView();
        }
    }


    public string getMapName(string mapcode)
    {
        string mapname = "";

        //Debug.Log("serverManager.ds_map.Tables[0].TableName : " + serverManager.ds_map.Tables[0].TableName);

        //var str = serverManager.ds_map.Tables[0].Columns.Cast<DataColumn>()
        //                    .Select(x => x.ColumnName)
        //                    .ToArray();

        // foreach (string ss in str)
        // {
        //     Debug.Log("ss : "+ss + ", " +ss.GetType()); 
        // }

        serverManager.ds_map.Tables[0].DefaultView.RowFilter = "bjdong_code = '" + mapcode + "'";

        mapname = serverManager.ds_map.Tables[0].DefaultView[0]["bjdong_name"].ToString();

        // Debug.Log("mapname : " + mapname);

        return mapname;
    }

    private void CopyFile(string sourcePath, string destinationPath)
    {
        try
        {
            // 파일 복사
            File.Copy(sourcePath, destinationPath, true);
            //Debug.Log("파일이 성공적으로 복사되었습니다.");
        }
        catch (IOException e)
        {
            Debug.LogError("파일 복사 중 오류가 발생했습니다: " + e.Message);
        }
    }

}
