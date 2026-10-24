using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 한글처럼 IME 로 조합해서 치는 입력란에서 Enter(onSubmit)로 검색할 때 쓴다.
///
/// Unity 6 의 TMP 입력란은 조합 중이던 마지막 글자가 확정되기 전에 onSubmit 을 부른다
/// (Unity 이슈 UUM-100241, Won't Fix). 예) "역삼동" 을 치고 Enter → 그 순간 입력란 글자는 "역삼" 이다.
/// 그래서 조합이 끝날 때까지 몇 프레임 기다렸다가 실행한다. action 안에서 입력란의 text 를 읽으면 된다.
/// 2021.3(TMP 3.0)에서는 조합이 이미 끝나 있으므로 한 프레임 늦게 실행될 뿐 결과는 같다.
/// </summary>
public static class ImeSafeSubmit
{
    private const int MaxWaitFrames = 10;

    /// <summary>field.onSubmit.AddListener 대신 쓴다. host 는 코루틴을 돌릴 화면(보통 this).</summary>
    public static void AddListener(TMP_InputField field, MonoBehaviour host, Action action)
    {
        if (field == null || action == null)
        {
            return;
        }
        field.onSubmit.AddListener(_ =>
        {
            if (host != null && host.isActiveAndEnabled)
            {
                host.StartCoroutine(RunAfterComposition(action));
            }
            else
            {
                action();
            }
        });
    }

    private static IEnumerator RunAfterComposition(Action action)
    {
        int frames = 0;
        do
        {
            // 확정된 글자는 다음 프레임의 입력 처리에서 입력란에 들어간다
            yield return null;
            frames++;
        }
        while (IsComposing() && frames < MaxWaitFrames);
        action();
    }

    private static bool IsComposing()
    {
        try
        {
            return !string.IsNullOrEmpty(Input.compositionString);
        }
        catch (InvalidOperationException)
        {
            // 새 Input System 만 켠 프로젝트에서는 옛 Input 클래스를 쓸 수 없다
            return false;
        }
    }
}
