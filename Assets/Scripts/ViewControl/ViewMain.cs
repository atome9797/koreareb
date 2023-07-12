using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ViewMain : MonoBehaviour
{
    public MainManager mainManager;

    // GNB
    public GnbViewControl gnbViewControl;

    // 대쉬보드
    public ViewDashBoard viewDashBoard;

    // 데이터 전처리
    public ViewPrepare viewPrepare;

    // 단지 선정 
    public ViewDanji viewDanji;

    // 실사 등록
    public ViewExecute viewExecute;

    // 내정보보기
    public ViewMyInfo viewMyInfo;

    // 내정보수정
    public ViewMyInfoEdit viewMyInfoEdit;



    // 창 정리 
    public Button btn_min;
    public Button btn_exit;

    public void minmize()
    {
        GetComponent<MainManager>().MinimizeWithTransition();
    }

    // Init
    public void Init()
    {


    }

    // Start is called before the first frame update
    void Start()
    {
        btn_min.onClick.AddListener(minmize);

        btn_exit.onClick.AddListener(() =>
        {
            CommonPopup commonPopup = Instantiate(mainManager.commonPopup, mainManager.contents);

            commonPopup.ShowPopup_TwoButton(
            "프로그램을 종료하시겠습니까?",
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

                Application.Quit();
            });

        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
