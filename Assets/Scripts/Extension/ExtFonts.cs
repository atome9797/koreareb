using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

/// <summary>
/// 일본어 글자(가나·한자)를 보이게 하는 대체 글꼴.
///
/// 기존 Pretendard SDF 에는 일본어 글자가 없다. 다음 순서로 대체 글꼴을 찾아 TMP 전체 대체 목록(TMP_Settings)에 붙인다.
///  1) Resources/Fonts/JapaneseFallback SDF (Unity 에디터에서 만든 TMP 글꼴. 가장 확실하다)
///     만드는 법: 일본어 글꼴 파일(예: Noto Sans JP — OFL)을 Assets/Resources/Fonts 에 넣고
///     Window > TextMeshPro > Font Asset Creator 에서 Atlas Population Mode = Dynamic 으로 만든 뒤
///     Assets/Resources/Fonts/JapaneseFallback SDF.asset 으로 저장한다.
///  2) 없으면 Windows 글꼴 파일(Yu Gothic, Meiryo, BIZ UDGothic, MS Gothic)로 동적 TMP 글꼴을 만들어 본다.
/// 둘 다 없으면 일본어 지역 이름은 영어 이름으로 보여 주고(ExtNames), 사람·물건 이름의 일본어는 네모로 보인다.
/// 에디터에서는 플레이를 끝낼 때 붙인 것을 떼어 TMP Settings 에셋이 바뀌지 않게 한다.
/// </summary>
public static class ExtFonts
{
    private const string ResourcePath = "Fonts/JapaneseFallback SDF";

    // Windows 기본 일본어 글꼴 파일 (C:\Windows\Fonts). 앞에서부터 쓴다.
    private static readonly string[] OsFontFiles =
    {
        "YuGothM.ttc", "YuGothR.ttc", "meiryo.ttc", "BIZ-UDGothicR.ttc", "msgothic.ttc",
    };

    private static bool tried;
    private static TMP_FontAsset japanese;

    public static bool HasJapanese
    {
        get { return japanese != null; }
    }

    public static void EnsureJapaneseFallback(TMP_FontAsset primary)
    {
        if (tried)
        {
            return;
        }
        tried = true;

        TMP_FontAsset found = null;
        try
        {
            found = Resources.Load<TMP_FontAsset>(ResourcePath);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[확장] 일본어 TMP 글꼴을 읽지 못했습니다: " + e.Message);
        }

        if (found == null)
        {
            found = FromOsFont(primary);
        }

        if (found == null)
        {
            Debug.LogWarning("[확장] 일본어 대체 글꼴이 없습니다. 일본어 이름이 네모로 보이면 " +
                             "Assets/Resources/Fonts/JapaneseFallback SDF 를 만드세요 (ExtFonts.cs 설명).");
            return;
        }

        try
        {
            List<TMP_FontAsset> fallbacks = TMP_Settings.fallbackFontAssets;
            if (fallbacks == null)
            {
                Debug.LogWarning("[확장] TMP Settings 가 없어 일본어 대체 글꼴을 붙이지 못했습니다.");
                return;
            }
            if (!fallbacks.Contains(found))
            {
                fallbacks.Add(found);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[확장] 일본어 대체 글꼴을 붙이지 못했습니다: " + e.Message);
            return;
        }

        japanese = found;
        Debug.Log("[확장] 일본어 대체 글꼴: " + found.name);

#if UNITY_EDITOR
        Application.quitting += Detach;
#endif
    }

#if UNITY_EDITOR
    private static void Detach()
    {
        Application.quitting -= Detach;
        try
        {
            if (japanese != null && TMP_Settings.fallbackFontAssets != null)
            {
                TMP_Settings.fallbackFontAssets.Remove(japanese);
            }
        }
        catch (Exception)
        {
            // 종료 중이라 무시
        }
        japanese = null;
        tried = false;
    }
#endif

    /// <summary>
    /// Windows 글꼴 폴더의 일본어 글꼴 파일로 동적 TMP 글꼴을 만든다.
    /// CreateDynamicFontFromOSFont 로 만든 Font 에는 글꼴 파일 자료가 없어 TMP 가 읽지 못하므로,
    /// Font.GetPathsToOSFonts() 의 파일 경로로 new Font(path) 를 만든다 (TMP 가 안내하는 방법).
    /// </summary>
    private static TMP_FontAsset FromOsFont(TMP_FontAsset primary)
    {
        string[] paths;
        try
        {
            paths = Font.GetPathsToOSFonts();
        }
        catch (Exception e)
        {
            Debug.LogWarning("[확장] Windows 글꼴 목록을 읽지 못했습니다: " + e.Message);
            return null;
        }
        if (paths == null)
        {
            return null;
        }

        foreach (string wanted in OsFontFiles)
        {
            foreach (string path in paths)
            {
                if (!string.Equals(Path.GetFileName(path), wanted, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                TMP_FontAsset asset = FromFile(primary, path);
                if (asset != null)
                {
                    return asset;
                }
            }
        }
        return null;
    }

    private static TMP_FontAsset FromFile(TMP_FontAsset primary, string path)
    {
        try
        {
            Font font = new Font(path);
            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font);
            if (asset == null)
            {
                return null;
            }
            // 빌드에 들어 있는 셰이더를 쓰도록 기존 글꼴의 셰이더로 맞춘다
            if (primary != null && primary.material != null && asset.material != null)
            {
                asset.material.shader = primary.material.shader;
            }
            // 실제로 일본어 글자를 만들 수 있는지 확인한다
            if (!asset.TryAddCharacters("日本語東京都区市町村あア"))
            {
                Debug.LogWarning("[확장] " + Path.GetFileName(path) + " 에서 일본어 글자를 만들지 못했습니다.");
                return null;
            }
            asset.name = "JapaneseFallback (" + Path.GetFileNameWithoutExtension(path) + ")";
            return asset;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[확장] " + Path.GetFileName(path) + " 을 쓰지 못했습니다: " + e.Message);
            return null;
        }
    }
}
