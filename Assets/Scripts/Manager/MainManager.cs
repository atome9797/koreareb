using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using System.Runtime.InteropServices;
using System;
using System.Data;
using TMPro;
using UnityEngine.SceneManagement;
using PygmyMonkey.ColorPalette;

public class MainManager : MonoBehaviour
{
    public ServerManager serverManager;

    public string filePath = @"C:\korea_rebs\";
    public string filePrepare = @"Preparation.py";

    public GnbViewControl gnbViewControl;

    public ViewMain viewMain;

    // 그래프 별 사진 저장
    public CanvasInImagePosition canvasInImage;

    public Loading viewLoading;
    public TMP_Text loadMsg;


    public bool isPython = false;
  
    public CommonPopup commonPopup;
    public CommonToastPopup commonToastPopup;
    public CommonErrorPopup commonErrorPopup;

    public Transform contents;

    public MemberData myData;
    public enum Thememode
    {
        dark,
        light
    }

    public Thememode thememode = Thememode.dark;
    public GameObject DarkTheme;
    public GameObject LightTheme;

    public GameObject MapDarkTheme;
    public GameObject MapLightTheme;

    public Color OnTextColor;
    public Color OffTextColor;

    // 페이지 버튼 컬러
    public Color PageOnColor;
    public Color PageOffColor;

    // 페이지 버튼 텍스트 컬러
    public Color PageTextOnColor;
    public Color PageTextOffColor;


    [DllImport("user32.dll")]
    private static extern bool AnimateWindow(IntPtr hwnd, int time, int flags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_MINIMIZE = 2;
    
    public void MinimizeWithTransition()
    {
        // 창을 숨기고 최소화합니다.
        // ShowWindow(windowHandle, SW_HIDE);
        ShowWindow(GetActiveWindow(), SW_MINIMIZE);
    }

    public void Awake()
    {
        Screen.SetResolution(1920, 1080, true);

        //double value = 999999999999999999;

        //UnityEngine.Debug.Log("N0 : " + value.ToString("N0"));

        ColorPaletteData.Singleton.setCurrentPalette(2);

    }

    public void ChangeTheme()
    {
        if (thememode == MainManager.Thememode.dark)
        {
            thememode = Thememode.light;
            DarkTheme.SetActive(false);
            LightTheme.SetActive(true);

            MapDarkTheme.SetActive(false);
            MapLightTheme.SetActive(true);
        }
        else
        {
            thememode = Thememode.dark;
            DarkTheme.SetActive(true);
            LightTheme.SetActive(false);

            MapDarkTheme.SetActive(true);
            MapLightTheme.SetActive(false);
        }
    }

    async void TaskOnPrepare()
    {
        viewLoading.Open();

        isPython = true;

        UnityEngine.Debug.Log("Processing...");

        var result = await RunProcessAsync("python", "Preparation.py");

        UnityEngine.Debug.Log("Result: " + result.ToString());

        viewLoading.Close();

        isPython = false;
    }

    async void TaskOnLauncher()
    {
        viewLoading.Open();

        isPython = true;

        UnityEngine.Debug.Log("Processing...");

        var result = await RunProcessAsync("python", "Launcher.py");

        UnityEngine.Debug.Log("Result: " + result.ToString());

        viewLoading.Close();

        isPython = false;
    }

    async void TaskOnSendInsert()
    {
        viewLoading.Open();

        isPython = true;

        UnityEngine.Debug.Log("Processing...");

        var result = await RunProcessAsync("python", "SendInsert.py");

        UnityEngine.Debug.Log("Result: " + result.ToString());

        viewLoading.Close();

        isPython = false;
    }

    async void TaskOnSendUpdate()
    {
        viewLoading.Open();

        isPython = true;

        UnityEngine.Debug.Log("Processing...");

        var result = await RunProcessAsync("python", "SendUpdate.py");

        UnityEngine.Debug.Log("Result: " + result.ToString());

        viewLoading.Close();

        isPython = false;
    }

    public async Task<int> RunProcessAsync(string fileName, string arguments)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                WorkingDirectory = @"C:\korea_rebs\",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        var output = new StringBuilder();
        var error = new StringBuilder();

        process.OutputDataReceived += (sender, args) => output.AppendLine(args.Data);
        process.ErrorDataReceived += (sender, args) => error.AppendLine(args.Data);
        //process.ErrorDataReceived += AppendErrorToLoadMsg;


        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var processExited = new TaskCompletionSource<bool>();

        process.Exited += (sender, args) =>
        {
            processExited.SetResult(true);
        };

        await processExited.Task;

        int exitCode = process.ExitCode;

        return exitCode;
    }

    void AppendOutputToLoadMsg(object sender, DataReceivedEventArgs args)
    {
        print("OUTPUT EVENT : "  +args.Data);

        if (!string.IsNullOrEmpty(args.Data))
        {
            loadMsg.text += args.Data + Environment.NewLine;
        }
    }
    void AppendErrorToLoadMsg(object sender, DataReceivedEventArgs args)
    {
        if (!string.IsNullOrEmpty(args.Data))
        {
            loadMsg.text += args.Data + Environment.NewLine;
        }
    }



    public IEnumerator Run_Risk()
    {
        viewLoading.Open();

        isPython = true;

        yield return new WaitForSeconds(0.2f);

        try
        {
            Process process = new Process();
            process.StartInfo.FileName = "python";
            process.StartInfo.Arguments = "Launcher.py";
            //process.StartInfo.WorkingDirectory = @"C:\korea_rebs\";
            process.StartInfo.WorkingDirectory = filePath;

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;

            process.Start();

            while (!process.StandardOutput.EndOfStream)
            {
                string output = process.StandardOutput.ReadLine();

                if (output != null)
                {
                    UnityEngine.Debug.Log(output);
                    UnityEngine.Debug.Log(process.StandardOutput.EndOfStream);
                    // 파이썬 프로그램의 표준 출력을 읽어서 처리하는 코드

                    if (output.Contains("Completed"))
                    {
                        UnityEngine.Debug.Log("Break");
                        // 위험도 산출 완료                        

                        break;
                    }

                }

            }
                       

            process.WaitForExit();
            process.Close();
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.Log("Unable to launch app: " + e.Message);
        }

        viewLoading.Close();
        isPython = false;
    }

    public IEnumerator Run_Prepare()
    {
        viewLoading.Open();

        isPython = true;

        yield return new WaitForSeconds(0.2f);

        try
        {
            Process process = new Process();
            process.StartInfo.FileName = "python";
            process.StartInfo.Arguments = "Preparation.py";
            //process.StartInfo.WorkingDirectory = @"C:\korea_rebs\";
            process.StartInfo.WorkingDirectory = filePath;

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;

            process.Start();

            while (!process.StandardOutput.EndOfStream)
            {
                string output = process.StandardOutput.ReadLine();

                if (output != null)
                {
                    UnityEngine.Debug.Log(output);
                    UnityEngine.Debug.Log(process.StandardOutput.EndOfStream);
                    // 파이썬 프로그램의 표준 출력을 읽어서 처리하는 코드

                    if (output.Contains("Completed"))
                    {
                        UnityEngine.Debug.Log("Break");
                        // 전처리 완료
                        gnbViewControl.SelectMenu_01(1);

                        break;
                    }

                }

            }


            process.WaitForExit();
            process.Close();
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.Log("Unable to launch app: " + e.Message);
        }

        viewLoading.Close();
        isPython = false;

    }

    public string FormatNumberWithSymbol(double number)
    {
        // UnityEngine.Debug.Log("<color=megenta>" + number + "</color>");

        if (number < 1000)
        {
            return number.ToString("0");
        }
        else if (number < 1000000)
        {
            return (number / 1000).ToString("0.0") + "K";
        }
        else
        {
            return (number / 1000000).ToString("0.0") + "M";
        }
    }


    // 엑셀 다운로드용 
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

            // 데이터 쓰기
            foreach (DataRow row in dataTable.Rows)
            {
                List<string> values = new List<string>();

                for (int columnIndex = 0; columnIndex < columname.Length; columnIndex++)
                {
                    DataColumn column = dataTable.Columns[columnIndex];

                    if (row[column].ToString().Contains(","))
                    {
                        //Debug.Log("쉼표가 있어" + row[column].ToString());
                        row[column] = $"\"{row[column]}\"";
                    }

                    values.Add(row[column].ToString());
                }

                //foreach (DataColumn column in dataTable.Columns)
                //{
                //    if (row[column].ToString().Contains(","))
                //    {
                //        //Debug.Log("쉼표가 있어" + row[column].ToString());

                //        row[column] = $"\"{row[column]}\"";
                //    }

                //    values.Add(row[column].ToString());
                //}

                streamWriter.WriteLine(string.Join(",", values));
            }
        }
    }

    //접속 로그 추가
    public void InsertConnectionLog()
    {
        serverManager.InsertConnectionLog(myData.mem_user_id, myData.mem_name, myData.mem_depart, myData.mem_title);
    }

    /// <summary>
    /// 변경 로그 추가
    /// </summary>
    /// <param name="ChangeLogMenu">메뉴명</param>
    /// <param name="ChangeMenuTitle">이력</param>
    public void ChangeLogInsert(string ChangeLogTitle , string ChangeLogContent)
    {
        serverManager.InsertChangeLog(myData.mem_name, ChangeLogTitle, ChangeLogContent);
    }

    /// <summary>
    /// 다운로드 로그 추가
    /// </summary>
    /// <param name="DownloadLogTitle">메뉴명</param>
    /// <param name="DownloadLogContent">이력</param>
    /// 
    public void DownloadLodInsert(string DownloadLogTitle, string DownloadLogContent)
    {
        serverManager.InsertDownloadLog(myData.mem_name, DownloadLogTitle, DownloadLogContent);
    }

    /// <summary>
    /// 날짜 포맷
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public string DateFormat(string date)
    {
        DateTime tmpDate = DateTime.ParseExact(date, "yyyyMMdd", null);
        return string.Format("{0:yyyy.MM.dd}", tmpDate);
    }

    /// <summary>
    /// 패턴 사용 여부
    /// </summary>
    /// <param name="password"></param>
    /// <returns></returns>
    public bool CheckingString(string password)
    {
        bool check = true;

        string strAcChar = "-!@#$%^&*()_";

        bool[] checkList = { false, false, false, false };

        foreach (char ch in password)
        {
            //소문자
            if (0x61 <= ch && ch <= 0x7A)
            {
                checkList[0] = true;
            }
            else if (0x41 <= ch && ch <= 0x5A)//대문자
            {
                checkList[1] = true;
            }
            else if (0x30 <= ch && ch <= 0x39) //숫자
            {
                checkList[2] = true;
            }
            else if (strAcChar.Contains(ch)) //특수 문자 
            {
                checkList[3] = true;
            }
            else //그 이외의 것을 입력하면 error 로 처리
            {
                check = false;
            }
        }

        if (check)
        {
            int count = 0;
            for (int i = 0; i < 4; i++)
            {
                if (checkList[i])
                {
                    count++;
                }
            }

            if (count >= 3)
            {
                check = true;
            }
            else
            {
                check = false;
            }
        }


        return check;
    }


    public string Encode(string input)
    {
        try
        {
            byte[] buffer = Encoding.UTF8.GetBytes(input);
            return Convert.ToBase64String(buffer);

        }
        catch (Exception e)
        {
            throw new Exception("Encoding Error: " + e.Message);
        }
    }

    public string Decode(string input)
    {
        try
        {
            byte[] buffer = Convert.FromBase64String(input);
            return Encoding.UTF8.GetString(buffer);
        }
        catch (Exception e)
        {
            throw new Exception("Decoding Error: " + e.Message);
        }
    }

}
