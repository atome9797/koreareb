using System;
using System.IO;
using UnityEngine;

/// <summary>
/// 확장 기능 설정 (이 PC 의 사용자 폴더에만 저장: Application.persistentDataPath/market_settings.json).
/// API 키는 저장소·DB·로그에 남기지 않고, 파이썬을 실행할 때 환경 변수로만 넘긴다.
/// </summary>
[Serializable]
public class ExtSettings
{
    public string dataGoKrKey = "";
    public string reinfolibKey = "";
    public string pythonCommand = "python";

    // 데이터 수집 탭에 마지막으로 넣은 값
    public string krTradeFrom = "";
    public string krTradeTo = "";
    public string krPrefs = "11,41,28";
    public string krCompFrom = "";
    public string krCompTo = "";
    public string jpYears = "";
    public string jpQuarters = "1-4";
    public string jpPrefs = "13,14,27";
    public string fx = "";                 // 웹 대시보드의 엔화 환산 (원/엔). 비우면 환산하지 않는다

    private static string FilePath
    {
        get { return Path.Combine(Application.persistentDataPath, "market_settings.json"); }
    }

    public static ExtSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                ExtSettings loaded = JsonUtility.FromJson<ExtSettings>(File.ReadAllText(FilePath));
                if (loaded != null)
                {
                    return loaded;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[확장] 설정을 읽지 못했습니다: " + e.Message);
        }
        return new ExtSettings();
    }

    public bool Save()
    {
        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(this, true));
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[확장] 설정을 저장하지 못했습니다: " + e.Message);
            return false;
        }
    }
}
