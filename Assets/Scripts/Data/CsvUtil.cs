using System.Text;

/// <summary>
/// CSV 한 줄 만들기 (RFC 4180).
/// 이전 코드는 쉼표가 있는 값만 따옴표로 감쌌고, 값 안의 따옴표/줄바꿈은 처리하지 않아 엑셀에서 열이 밀렸다.
/// </summary>
public static class CsvUtil
{
    public static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value ?? "";
        }

        bool needsQuotes = value.IndexOf(',') >= 0 || value.IndexOf('"') >= 0 || value.IndexOf('\n') >= 0 || value.IndexOf('\r') >= 0;

        return needsQuotes ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
    }

    public static string JoinLine(string[] values)
    {
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < values.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }

            sb.Append(Escape(values[i]));
        }

        return sb.ToString();
    }
}
