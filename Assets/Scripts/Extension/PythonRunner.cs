using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>파이썬 스크립트 실행 결과</summary>
public sealed class PythonResult
{
    public int ExitCode;
    public string Error = "";        // 표준 오류의 'ERROR …' 줄 (없으면 마지막 줄)
    public string LastOutput = "";   // 표준 출력의 마지막 줄 (요약)
    public bool Started;

    public bool Ok
    {
        get { return Started && ExitCode == 0; }
    }
}

/// <summary>
/// 파이썬 실행 (MainManager.RunProcessAsync 와 같은 방식 + 환경 변수, UTF-8, 진행 줄 전달).
/// API 키는 명령줄이 아니라 환경 변수로 넘긴다 (명령줄은 다른 프로그램이 볼 수 있다).
/// 진행 줄(onLine)은 호출한 스레드(메인 스레드)로 돌려서 부른다.
/// </summary>
public static class PythonRunner
{
    public static Task<PythonResult> RunAsync(string command, string workingDirectory, string arguments,
        IDictionary<string, string> environment, Action<string> onLine)
    {
        SynchronizationContext context = SynchronizationContext.Current;

        return Task.Run(() =>
        {
            var result = new PythonResult();
            var errors = new List<string>();
            var info = new ProcessStartInfo
            {
                FileName = string.IsNullOrEmpty(command) ? "python" : command,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };
            info.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
            info.EnvironmentVariables["PYTHONUTF8"] = "1";
            if (environment != null)
            {
                foreach (KeyValuePair<string, string> pair in environment)
                {
                    info.EnvironmentVariables[pair.Key] = pair.Value ?? "";
                }
            }

            using (var process = new Process { StartInfo = info })
            {
                process.OutputDataReceived += (sender, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data))
                    {
                        return;
                    }
                    result.LastOutput = e.Data;
                    if (onLine != null)
                    {
                        string line = e.Data;
                        if (context != null)
                        {
                            context.Post(_ => onLine(line), null);
                        }
                        else
                        {
                            onLine(line);
                        }
                    }
                };
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        lock (errors)
                        {
                            errors.Add(e.Data);
                        }
                    }
                };

                try
                {
                    process.Start();
                }
                catch (Exception e)
                {
                    result.Error = "파이썬을 실행하지 못했습니다 (" + info.FileName + "): " + e.Message;
                    result.ExitCode = -1;
                    return result;
                }

                result.Started = true;
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();          // 인자 없는 WaitForExit 은 출력 스트림을 끝까지 읽은 뒤 돌아온다
                result.ExitCode = process.ExitCode;
            }

            lock (errors)
            {
                string error = errors.FindLast(l => l.StartsWith("ERROR ", StringComparison.Ordinal));
                if (error != null)
                {
                    result.Error = error.Substring(6);
                }
                else if (errors.Count > 0)
                {
                    result.Error = errors[errors.Count - 1];
                }
            }
            return result;
        });
    }

    /// <summary>명령줄 인자로 넣을 경로 (Windows 경로에는 " 가 들어갈 수 없다)</summary>
    public static string Quote(string path)
    {
        return "\"" + (path ?? "").Replace('\\', '/').Replace("\"", "") + "\"";
    }

    /// <summary>양수 소수 (환율 등). 읽을 수 없으면 빈 문자열.</summary>
    public static string PositiveNumber(string text)
    {
        double value;
        if (double.TryParse(Ascii(text), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out value) && value > 0 && value < 1e6)
        {
            return value.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture);
        }
        return "";
    }

    /// <summary>전각 숫자·쉼표·하이픈(일본어·한국어 입력기)을 반각으로 바꾼다.</summary>
    public static string Ascii(string text)
    {
        var sb = new StringBuilder();
        foreach (char c in (text ?? "").Trim())
        {
            if (c >= '０' && c <= '９')
            {
                sb.Append((char)('0' + (c - '０')));
            }
            else if (c == '，' || c == '、')
            {
                sb.Append(',');
            }
            else if (c == '－' || c == '‐' || c == '‑' || c == '‒' || c == '–' || c == '−')
            {
                sb.Append('-');
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    /// <summary>코드·연도 같은 입력값: 숫자 0-9, 쉼표, 하이픈만 남긴다 (명령줄에 다른 글자가 들어가지 않게)</summary>
    public static string Codes(string text)
    {
        var sb = new StringBuilder();
        foreach (char c in Ascii(text))
        {
            if ((c >= '0' && c <= '9') || c == ',' || c == '-')
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}
