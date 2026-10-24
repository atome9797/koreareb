using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 확장 화면(시장 분석, 일본 점검)의 바탕. 화면은 처음 열 때 만든다 (ExtensionHost 가 붙인다).
/// 기존 화면과 같은 팝업(CommonPopup, CommonErrorPopup, CommonToastPopup)과 로딩 창(MainManager.viewLoading)을 쓴다.
/// </summary>
public abstract class ExtScreen : MonoBehaviour
{
    protected MainManager main;
    protected GnbViewControl gnb;
    protected ExtSettings settings;

    private bool built;
    private int busy;

    /// <summary>변경 이력(tb_changelog)에 남기는 메뉴 이름</summary>
    public abstract string MenuName { get; }

    public void Init(MainManager main, GnbViewControl gnb, ExtSettings settings)
    {
        this.main = main;
        this.gnb = gnb;
        this.settings = settings;
    }

    public bool IsVisible
    {
        get { return gameObject.activeSelf; }
    }

    public void Show()
    {
        gameObject.SetActive(true);
        if (!built)
        {
            try
            {
                Build();
                built = true;
            }
            catch (Exception e)
            {
                // 반쯤 만든 화면은 지우고, 다음에 열 때 처음부터 다시 만든다
                Debug.LogError("[확장] " + MenuName + " 화면을 만들지 못했습니다: " + e.Message);
                Debug.LogException(e);
                ExtUi.Clear(transform);
                Alert(MenuName + " 화면을 만들지 못했습니다.\n" + e.Message);
                return;
            }
        }
        try
        {
            OnShow();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    protected abstract void Build();

    protected abstract void OnShow();

    protected ServerManager Server
    {
        get { return main.serverManager; }
    }

    protected Transform Root
    {
        get { return transform; }
    }

    // ------------------------------------------------------------------ 진행 중 표시

    /// <summary>오래 걸리는 작업 중에는 다른 버튼을 눌러도 무시한다.</summary>
    protected bool Busy
    {
        get { return busy > 0; }
    }

    protected void BeginBusy(string message)
    {
        busy++;
        main.viewLoading.Open();
        main.loadMsg.text = message ?? "";
    }

    protected void SetBusyMessage(string message)
    {
        if (busy > 0)
        {
            main.loadMsg.text = message ?? "";
        }
    }

    protected void EndBusy()
    {
        busy = Math.Max(0, busy - 1);
        if (busy == 0 && main != null)
        {
            main.loadMsg.text = "";
            main.viewLoading.Close();
        }
    }

    // ------------------------------------------------------------------ 팝업

    protected void Confirm(string message, Action onYes)
    {
        CommonPopup popup = Instantiate(main.commonPopup, main.contents);
        popup.ShowPopup_TwoButton(message, "아니오", "네",
            () => popup.CloseDestroy(),
            () =>
            {
                popup.CloseDestroy();
                if (onYes != null)
                {
                    onYes();
                }
            });
    }

    protected void Alert(string message)
    {
        CommonErrorPopup popup = Instantiate(main.commonErrorPopup, main.contents);
        popup.ShowPopup_OneButton(message, "확인", () => popup.CloseDestroy());
    }

    protected void Toast(string message)
    {
        CommonToastPopup popup = Instantiate(main.commonToastPopup, main.contents);
        popup.msg = message;
        popup.Init();
    }

    // ------------------------------------------------------------------ 파이썬

    /// <summary>C:\korea_rebs\ (MainManager.filePath). 파이썬 스크립트가 있는 곳.</summary>
    protected string ScriptFolder
    {
        get { return main.filePath; }
    }

    /// <summary>
    /// korea_rebs 의 파이썬 스크립트를 앱 DB 로 실행한다. arguments 는 하위 명령부터 (예: "analyze").
    /// API 키는 환경 변수로만 넘긴다. 진행 줄('PROGRESS …')은 로딩 창에 보여 준다.
    /// </summary>
    protected async Task<PythonResult> RunScript(string script, string arguments, string title, List<string> log)
    {
        var environment = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(settings.dataGoKrKey))
        {
            environment["KOREAREB_DATA_GO_KR_KEY"] = settings.dataGoKrKey.Trim();
        }
        if (!string.IsNullOrEmpty(settings.reinfolibKey))
        {
            environment["KOREAREB_REINFOLIB_KEY"] = settings.reinfolibKey.Trim();
        }

        // --db 는 하위 명령 앞에 둔다 (argparse 의 공통 옵션)
        string fullArguments = script + " --db " + PythonRunner.Quote(Server.DatabasePath) + " " + arguments;
        string python = string.IsNullOrEmpty(settings.pythonCommand) ? "python" : settings.pythonCommand.Trim();

        if (log != null)
        {
            log.Add(DateTime.Now.ToString("HH:mm:ss") + "  " + title + " 시작");
        }
        SetBusyMessage(title + " 실행 중...");

        PythonResult result = await PythonRunner.RunAsync(python, ScriptFolder, fullArguments, environment, line =>
        {
            if (line.StartsWith("PROGRESS ", StringComparison.Ordinal))
            {
                SetBusyMessage(title + " 실행 중...\n" + line.Substring(9));
            }
            else if (log != null)
            {
                log.Add("    " + line);
            }
        });

        if (log != null)
        {
            log.Add(DateTime.Now.ToString("HH:mm:ss") + "  " + title + (result.Ok ? " 완료" : " 실패: " + Friendly(result)));
            if (log.Count > 200)
            {
                log.RemoveRange(0, log.Count - 200);
            }
        }
        if (!result.Ok)
        {
            Debug.LogWarning("[확장] " + script + " " + arguments.Split(' ')[0] + " 실패 (exit " + result.ExitCode + ")");
        }
        return result;
    }

    /// <summary>파이썬 오류를 사람이 읽을 문장으로</summary>
    protected static string Friendly(PythonResult result)
    {
        if (!result.Started)
        {
            return result.Error + "\n파이썬이 설치되어 있는지, 시장분석 > 데이터 수집의 '파이썬 명령'이 맞는지 확인하세요.";
        }
        string error = string.IsNullOrEmpty(result.Error) ? "알 수 없는 오류 (종료 코드 " + result.ExitCode + ")" : result.Error;
        if (error.Contains("KOREAREB_DATA_GO_KR_KEY"))
        {
            return "공공데이터포털 인증키가 없습니다. 시장분석 > 데이터 수집에서 키를 저장하세요.";
        }
        if (error.Contains("KOREAREB_REINFOLIB_KEY"))
        {
            return "일본 부동산정보 라이브러리 API 키가 없습니다. 시장분석 > 데이터 수집에서 키를 저장하세요.";
        }
        if (error.Contains("No module named"))
        {
            return error + "\n필요한 파이썬 패키지가 없습니다 (pandas). 'pip install pandas' 로 설치하세요.";
        }
        return error;
    }

    protected void OpenFolder(string path)
    {
        try
        {
            Application.OpenURL(new Uri(path).AbsoluteUri);
        }
        catch (Exception e)
        {
            Alert("폴더를 열지 못했습니다.\n" + path + "\n" + e.Message);
        }
    }

    protected string ScriptPath(params string[] parts)
    {
        string path = ScriptFolder;
        foreach (string part in parts)
        {
            path = Path.Combine(path, part);
        }
        return path;
    }
}
