using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DateText : MonoBehaviour
{
    [SerializeField] DateRangePicker m_DatePicker;
    [SerializeField] Text m_DateText;
    
    public TextMeshProUGUI Canlendar;
    [SerializeField] Single_DateRangePicker picker;
    public int FromDate = 20200000;
    public int ToDate = 20240000;
    public string text;
    int[] priodMonth = { 6, 12, 24, 36 }; // 20023.06.11 기간 30일, 60일, 90일, 1년에서 => 6개월, 1년, 2년, 3년으로 변경

    private void Awake()
    {
        picker = gameObject.GetComponent<Single_DateRangePicker>();
        m_DatePicker.CalendersUpdated += CalenderUpdated;
    }

    private void Start()
    {
        // SetToday();
    }

    /// <summary>
    /// defualt 날짜 지정 1년 단위
    /// </summary>
    private void SetToday()
    {
        DateTime selectedEndDate = DateTime.Now;
        DateTime selectedStartDate = selectedEndDate.AddYears(-1);
        FormateDate(selectedStartDate, selectedEndDate);
    }

    /// <summary>
    /// 날짜 업데이트
    /// </summary>
    /// <param name="selectedStartDate"></param>
    /// <param name="selectedEndDate"></param>
    public void CalenderUpdated(DateTime? selectedStartDate, DateTime? selectedEndDate)
    {
        if (selectedEndDate != null && selectedStartDate != null)
        {
            FormateDate(selectedStartDate, selectedEndDate);
            picker.OnClick_ToggleCalenders();
        }
    }

    /// <summary>
    /// 날짜 업데이트 실시등록용
    /// </summary>
    /// <param name="selectedStartDate"></param>
    /// <param name="selectedEndDate"></param>
    public void SetCalenderUpdate(DateTime? selectedStartDate, DateTime? selectedEndDate)
    {
        if (selectedEndDate != null && selectedStartDate != null)
        {
            FormateDate(selectedStartDate, selectedEndDate);
            //picker.OnClick_ToggleCalenders();
        }
    }
    /// <summary>
    /// 토글 선택 일수에 따른 기간 지정
    /// </summary>
    /// <param name="priod"></param>
    public void ChangeCalendarPriod(int priod)
    {
        int prevMonth = priodMonth[priod];
        DateTime selectedEndDate = DateTime.Now;
        DateTime selectedStartDate = selectedEndDate.AddMonths(-prevMonth);
        FormateDate(selectedStartDate, selectedEndDate);
    }

    private void FormateDate(DateTime? selectedStartDate, DateTime? selectedEndDate)
    {
        text = "";
        text += string.Format("{0:yyyy.MM.dd}", selectedStartDate);
        text += " - " + string.Format("{0:yyyy.MM.dd}", selectedEndDate);
        FromDate = int.Parse(Regex.Replace(selectedStartDate.Value.ToShortDateString(), @"\D", ""));
        ToDate = int.Parse(Regex.Replace(selectedEndDate.Value.ToShortDateString(), @"\D", ""));
        // m_DateText.text = text;
        Canlendar.text = text;
    }
}
