using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;

/// <summary>
/// 확장 화면(시장 분석, 일본 점검)을 코드로 만드는 도우미.
///
/// 씬 파일(Main.unity, 약 6천7백만 바이트 YAML)을 손으로 고치면 깨지기 쉬워서, 화면은 실행할 때 만든다.
/// 좌표는 부모의 왼쪽 위를 (0, 0) 으로 하는 픽셀 값이다 (기존 화면과 같은 1920×1080 캔버스, 화면 영역 1820×1032).
/// 색·글꼴·둥근 상자(ProceduralImage)는 기존 계정 관리 화면과 맞췄다. 글꼴과 레이어는 ExtensionHost 가 채운다.
/// </summary>
public static class ExtUi
{
    public static TMP_FontAsset Font;          // Pretendard Regular
    public static TMP_FontAsset FontStrong;    // Pretendard SemiBold (제목)
    public static int Layer = 5;               // UI
    public static string Sqm = "㎡";           // 글꼴에 없으면 m² 또는 m2 (Init 에서 고른다)

    // 기존 화면의 색 (계정 관리 화면 배경·상자·목록 행, 선택된 메뉴 색)
    public static readonly Color Background = Rgb(0x15, 0x1B, 0x29);
    public static readonly Color CardColor = Rgb(0x1B, 0x21, 0x33);
    public static readonly Color Row = Rgb(0x21, 0x28, 0x3D);
    public static readonly Color RowAlt = Rgb(0x1D, 0x24, 0x37);
    public static readonly Color RowSelected = Rgb(0x27, 0x3B, 0x5F);
    public static readonly Color Line = Rgb(0x42, 0x4C, 0x6A);
    public static readonly Color InputBack = Rgb(0x10, 0x15, 0x22);
    public static readonly Color Text = Color.white;
    public static readonly Color TextSub = Rgb(0xC9, 0xD0, 0xE0);
    public static readonly Color TextMuted = Rgb(0x8A, 0x93, 0xA8);
    public static readonly Color Accent = Rgb(0x0B, 0x95, 0x96);
    public static readonly Color AccentText = Rgb(0x2D, 0xBB, 0xBC);
    public static readonly Color Danger = Rgb(0xEF, 0x86, 0x74);
    public static readonly Color Warning = Rgb(0xFA, 0xB2, 0x19);
    public static readonly Color Secondary = Rgb(0x2C, 0x35, 0x4D);
    public static readonly Color Bar = Rgb(0x39, 0x87, 0xE5);

    private static readonly CultureInfo Number = CultureInfo.InvariantCulture;

    private static Color Rgb(int r, int g, int b)
    {
        return new Color(r / 255f, g / 255f, b / 255f, 1f);
    }

    /// <summary>글꼴을 정한 뒤 한 번 부른다.</summary>
    public static void Init(TMP_FontAsset regular, TMP_FontAsset strong, int layer)
    {
        Font = regular;
        FontStrong = strong != null ? strong : regular;
        Layer = layer;
        Sqm = Glyphs("㎡", Glyphs("m²", "m2"));
    }

    /// <summary>preferred 의 글자가 모두 글꼴(대체 글꼴 포함)에 있으면 preferred, 아니면 fallback</summary>
    public static string Glyphs(string preferred, string fallback)
    {
        if (Font == null)
        {
            return fallback;
        }
        try
        {
            foreach (char c in preferred)
            {
                if (!Font.HasCharacter(c, true, true))
                {
                    return fallback;
                }
            }
            return preferred;
        }
        catch (Exception)
        {
            return fallback;
        }
    }

    // ------------------------------------------------------------------ 기본

    public static GameObject Create(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = Layer;     // 캔버스 카메라가 UI 레이어만 그린다
        go.transform.SetParent(parent, false);
        return go;
    }

    /// <summary>부모 왼쪽 위 기준 (x, y) 에 w × h 크기로 둔다.</summary>
    public static RectTransform Place(GameObject go, float x, float y, float w, float h)
    {
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    public static RectTransform Place(Component c, float x, float y, float w, float h)
    {
        return Place(c.gameObject, x, y, w, h);
    }

    /// <summary>부모를 채우되 네 변에서 여백만큼 뗀다.</summary>
    public static RectTransform Stretch(GameObject go, float left, float top, float right, float bottom)
    {
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
        return rt;
    }

    public static void Clear(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject child = parent.GetChild(i).gameObject;
            child.SetActive(false);
            UnityEngine.Object.Destroy(child);
        }
    }

    /// <summary>모서리 없는 색 상자 (목록 행, 구분선)</summary>
    public static Image Panel(Transform parent, string name, Color color, bool raycast = false)
    {
        GameObject go = Create(name, parent);
        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycast;
        return image;
    }

    /// <summary>둥근 상자 (기존 화면과 같은 ProceduralImage)</summary>
    public static Image Rounded(Transform parent, string name, Color color, float radius, bool raycast = false)
    {
        GameObject go = Create(name, parent);
        ProceduralImage image = go.AddComponent<ProceduralImage>();
        UniformModifier modifier = go.AddComponent<UniformModifier>();
        modifier.Radius = radius;
        image.color = color;
        image.raycastTarget = raycast;
        return image;
    }

    /// <summary>내용을 묶는 상자 (계정 관리 화면의 BG_Box)</summary>
    public static RectTransform Card(Transform parent, float x, float y, float w, float h)
    {
        Image card = Rounded(parent, "Card", CardColor, 10f);
        return Place(card, x, y, w, h);
    }

    public static TextMeshProUGUI Label(Transform parent, string text, float size, Color color,
        TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft, bool strong = false)
    {
        GameObject go = Create("Text", parent);
        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = strong ? FontStrong : Font;
        if (font != null)
        {
            label.font = font;
        }
        label.fontSize = size;
        label.color = color;
        label.alignment = align;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.raycastTarget = false;
        label.richText = false;              // 지역명·이름 같은 데이터 글자를 태그로 읽지 않는다
        label.text = text ?? "";
        return label;
    }

    /// <summary>여러 줄 글 (상세 패널, 실행 기록)</summary>
    public static TextMeshProUGUI Paragraph(Transform parent, string text, float size, Color color)
    {
        TextMeshProUGUI label = Label(parent, text, size, color, TextAlignmentOptions.TopLeft);
        label.enableWordWrapping = true;
        label.overflowMode = TextOverflowModes.Truncate;
        label.lineSpacing = 6f;
        return label;
    }

    /// <summary>화면 제목 (계정 관리 화면과 같은 위치: 청록 막대 + 22px)</summary>
    public static TextMeshProUGUI Title(Transform parent, string text)
    {
        Image bar = Rounded(parent, "Bar", Accent, 2f);
        Place(bar, 40f, 24f, 4f, 22f);
        TextMeshProUGUI label = Label(parent, text, 22f, TextSub, TextAlignmentOptions.MidlineLeft, true);
        Place(label, 63f, 22f, 900f, 26f);
        return label;
    }

    /// <summary>상자 안 소제목 (청록 막대 + 18px)</summary>
    public static TextMeshProUGUI Heading(Transform parent, float x, float y, string text, float width = 600f)
    {
        Image bar = Rounded(parent, "Bar", Accent, 2f);
        Place(bar, x, y + 2f, 4f, 17f);
        TextMeshProUGUI label = Label(parent, text, 18f, Text, TextAlignmentOptions.MidlineLeft, true);
        Place(label, x + 18f, y, width, 21f);
        return label;
    }

    /// <summary>버튼 너비를 정할 때 쓰는 어림값 (한글·한자·가나는 글자 크기만큼, 영문·숫자는 그 절반쯤)</summary>
    public static float TextWidth(string text, float size)
    {
        float width = 0f;
        foreach (char c in text ?? "")
        {
            if (c >= 0x1100)
            {
                width += size;
            }
            else if (char.IsUpper(c) || char.IsDigit(c))
            {
                width += size * 0.62f;
            }
            else
            {
                width += size * 0.5f;
            }
        }
        return width;
    }

    // ------------------------------------------------------------------ 버튼·입력

    public static Button Button(Transform parent, string text, UnityAction onClick, bool primary = false, float fontSize = 15f)
    {
        Image back = Rounded(parent, "Button", primary ? Accent : Secondary, 6f, true);
        Button button = back.gameObject.AddComponent<Button>();
        button.targetGraphic = back;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.highlightedColor = Color.white;
        colors.selectedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        button.colors = colors;

        TextMeshProUGUI label = Label(back.transform, text, fontSize, Text, TextAlignmentOptions.Center);
        Stretch(label.gameObject, 8f, 0f, 8f, 0f);
        if (onClick != null)
        {
            button.onClick.AddListener(onClick);
        }
        return button;
    }

    /// <summary>글자 길이에 맞춘 너비로 버튼을 만들어 (x, y) 에 둔다. 반환: 오른쪽 끝 + 간격</summary>
    public static float ButtonAt(Transform parent, float x, float y, float height, string text, UnityAction onClick,
        bool primary = false, float minWidth = 96f, List<Button> collect = null)
    {
        Button button = Button(parent, text, onClick, primary, 15f);
        float w = Mathf.Max(minWidth, TextWidth(text, 15f) + 36f);
        Place(button, x, y, w, height);
        if (collect != null)
        {
            collect.Add(button);
        }
        return x + w + 8f;
    }

    public static void SetText(Button button, string text)
    {
        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            label.text = text;
        }
    }

    /// <summary>버튼을 켜고 끈다. 꺼지면 글자도 흐리게.</summary>
    public static void SetEnabled(Button button, bool enabled)
    {
        button.interactable = enabled;
        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            label.color = enabled ? Text : TextMuted;
        }
    }

    public static TMP_InputField Input(Transform parent, string placeholder, string value, bool password = false, float fontSize = 15f)
    {
        GameObject root = Create("Input", parent);
        root.SetActive(false);               // 필요한 참조를 모두 넣은 뒤 켠다 (OnEnable 에서 참조를 쓴다)
        ProceduralImage back = root.AddComponent<ProceduralImage>();
        UniformModifier modifier = root.AddComponent<UniformModifier>();
        modifier.Radius = 6f;
        back.color = InputBack;

        GameObject area = Create("Text Area", root.transform);
        Stretch(area, 10f, 4f, 10f, 4f);
        area.AddComponent<RectMask2D>();

        TextMeshProUGUI hint = Label(area.transform, placeholder, fontSize, TextMuted);
        Stretch(hint.gameObject, 0f, 0f, 0f, 0f);
        TextMeshProUGUI text = Label(area.transform, "", fontSize, Text);
        text.overflowMode = TextOverflowModes.Overflow;
        Stretch(text.gameObject, 0f, 0f, 0f, 0f);

        TMP_InputField input = root.AddComponent<TMP_InputField>();
        input.targetGraphic = back;
        input.textViewport = (RectTransform)area.transform;
        input.textComponent = text;
        input.placeholder = hint;
        if (Font != null)
        {
            input.fontAsset = Font;
        }
        input.pointSize = fontSize;
        input.customCaretColor = true;
        input.caretColor = Text;
        input.selectionColor = new Color(0.16f, 0.47f, 0.84f, 0.45f);
        input.contentType = password ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.richText = false;
        root.SetActive(true);
        input.text = value ?? "";
        return input;
    }

    public static Toggle Check(Transform parent, bool isOn, UnityAction<bool> onChange)
    {
        Image box = Rounded(parent, "Check", Secondary, 4f, true);
        Image mark = Rounded(box.transform, "Mark", AccentText, 2f);
        Stretch(mark.gameObject, 4f, 4f, 4f, 4f);
        Toggle toggle = box.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = box;
        toggle.graphic = mark;
        toggle.toggleTransition = Toggle.ToggleTransition.None;
        toggle.isOn = isOn;
        if (onChange != null)
        {
            toggle.onValueChanged.AddListener(onChange);
        }
        return toggle;
    }

    /// <summary>하나만 고르는 버튼 줄. 고른 버튼은 청록색.</summary>
    public sealed class Segmented
    {
        private readonly Transform parent;
        private readonly float x;
        private readonly float y;
        private readonly float height;
        private readonly Action<int> onChange;
        private readonly List<Button> buttons = new List<Button>();

        public int Selected { get; private set; }
        public float Width { get; private set; }

        public Segmented(Transform parent, float x, float y, float height, Action<int> onChange)
        {
            this.parent = parent;
            this.x = x;
            this.y = y;
            this.height = height;
            this.onChange = onChange;
        }

        public int Count
        {
            get { return buttons.Count; }
        }

        public void SetLabels(IList<string> labels, int selected)
        {
            foreach (Button b in buttons)
            {
                if (b != null)
                {
                    b.gameObject.SetActive(false);
                    UnityEngine.Object.Destroy(b.gameObject);
                }
            }
            buttons.Clear();

            float left = x;
            for (int i = 0; i < labels.Count; i++)
            {
                int index = i;
                Button b = ExtUi.Button(parent, labels[i], () => Select(index, true), false, 14f);
                float w = Mathf.Max(64f, TextWidth(labels[i], 14f) + 28f);
                Place(b.gameObject, left, y, w, height);
                left += w + 4f;
                buttons.Add(b);
            }
            Width = labels.Count > 0 ? left - 4f - x : 0f;
            Select(Mathf.Clamp(selected, 0, Mathf.Max(0, labels.Count - 1)), false);
        }

        public void Select(int index, bool notify)
        {
            Selected = index;
            for (int i = 0; i < buttons.Count; i++)
            {
                Image back = (Image)buttons[i].targetGraphic;
                back.color = i == index ? Accent : Secondary;
            }
            if (notify && onChange != null)
            {
                onChange(index);
            }
        }

        public void SetActive(bool active)
        {
            foreach (Button b in buttons)
            {
                if (b != null)
                {
                    b.gameObject.SetActive(active);
                }
            }
        }
    }

    /// <summary>목록이 길 때 쓰는 하나 고르기: [<] 값 [>] (기간, 회차)</summary>
    public sealed class Stepper
    {
        private readonly Action<int> onChange;
        private readonly Button prev;
        private readonly Button next;
        private readonly TextMeshProUGUI valueText;
        private readonly GameObject[] parts;
        private IList<string> items = new List<string>();

        public int Selected { get; private set; }

        public Stepper(Transform parent, float x, float y, float width, float height, Action<int> onChange)
        {
            this.onChange = onChange;
            prev = ExtUi.Button(parent, "<", () => Step(-1), false, 16f);
            Place(prev, x, y, 36f, height);
            Image back = Rounded(parent, "Value", InputBack, 6f);
            Place(back, x + 40f, y, width - 80f, height);
            valueText = Label(back.transform, "", 14f, Text, TextAlignmentOptions.Center);
            Stretch(valueText.gameObject, 6f, 0f, 6f, 0f);
            next = ExtUi.Button(parent, ">", () => Step(1), false, 16f);
            Place(next, x + width - 36f, y, 36f, height);
            parts = new[] { prev.gameObject, back.gameObject, next.gameObject };
        }

        public int Count
        {
            get { return items.Count; }
        }

        public void SetItems(IList<string> labels, int selected)
        {
            items = labels ?? new List<string>();
            Select(Mathf.Clamp(selected, 0, Mathf.Max(0, items.Count - 1)), false);
        }

        public void Select(int index, bool notify)
        {
            Selected = index;
            valueText.text = items.Count > 0 ? items[index] : "-";
            SetEnabled(prev, index > 0);
            SetEnabled(next, index < items.Count - 1);
            if (notify && onChange != null)
            {
                onChange(index);
            }
        }

        private void Step(int delta)
        {
            int target = Selected + delta;
            if (target >= 0 && target < items.Count)
            {
                Select(target, true);
            }
        }

        public void SetActive(bool active)
        {
            foreach (GameObject part in parts)
            {
                part.SetActive(active);
            }
        }
    }

    // ------------------------------------------------------------------ 표

    /// <summary>표 한 줄에 글자 칸들을 둔다. widths 의 합은 행 너비보다 작게.</summary>
    public static TextMeshProUGUI[] Cells(Transform row, float height, float[] widths, string[] texts, Color color, float size,
        bool[] rightAlign = null, float left = 12f)
    {
        var cells = new TextMeshProUGUI[widths.Length];
        for (int i = 0; i < widths.Length; i++)
        {
            bool right = rightAlign != null && i < rightAlign.Length && rightAlign[i];
            string text = texts != null && i < texts.Length ? texts[i] : "";
            TextMeshProUGUI cell = Label(row, text, size, color,
                right ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft);
            Place(cell.gameObject, left, 0f, widths[i] - 12f, height);
            left += widths[i];
            cells[i] = cell;
        }
        return cells;
    }

    /// <summary>
    /// 쪽 단위 표. 행 칸을 미리 만들어 두고 글자만 바꾼다 (쪽을 넘길 때 객체를 새로 만들지 않는다).
    /// 체크 칸을 켜면 행마다 선택 상자와 머리글의 '이 쪽 모두' 상자가 생긴다.
    /// </summary>
    public sealed class PagedTable
    {
        private const float HeaderHeight = 36f;
        private const float Gap = 2f;
        private const float CheckWidth = 40f;

        private readonly List<Image> rows = new List<Image>();
        private readonly List<TextMeshProUGUI[]> cells = new List<TextMeshProUGUI[]>();
        private readonly List<Toggle> checks = new List<Toggle>();
        private readonly Toggle checkAll;
        private readonly Button prev;
        private readonly Button next;
        private readonly TextMeshProUGUI pageLabel;
        private readonly TextMeshProUGUI empty;
        private bool updating;

        public readonly int PageSize;
        public int Page { get; private set; }
        public int PageCount { get; private set; }
        public int SelectedSlot { get; private set; }
        public float Height { get; private set; }

        public Action<int> RowClicked;
        public Action<int, bool> RowChecked;
        public Action<bool> AllChecked;
        public Action<int> PageChanged;

        public PagedTable(Transform parent, float x, float y, float width, string[] headers, float[] widths, bool[] rightAlign,
            int pageSize, float rowHeight, bool withCheck)
        {
            PageSize = pageSize;
            SelectedSlot = -1;
            float left = withCheck ? CheckWidth : 12f;

            Image head = Rounded(parent, "Header", Secondary, 6f);
            Place(head, x, y, width, HeaderHeight);
            Cells(head.transform, HeaderHeight, widths, headers, TextSub, 14f, rightAlign, left);
            if (withCheck)
            {
                checkAll = Check(head.transform, false, on =>
                {
                    if (!updating && AllChecked != null)
                    {
                        AllChecked(on);
                    }
                });
                Place(checkAll, 12f, (HeaderHeight - 18f) / 2f, 18f, 18f);
            }

            for (int i = 0; i < pageSize; i++)
            {
                int slot = i;
                float top = y + HeaderHeight + Gap + i * (rowHeight + Gap);
                Image back = Panel(parent, "Row", i % 2 == 0 ? Row : RowAlt, true);
                Place(back, x, top, width, rowHeight);
                Button click = back.gameObject.AddComponent<Button>();
                click.transition = Selectable.Transition.None;
                click.onClick.AddListener(() =>
                {
                    if (RowClicked != null)
                    {
                        RowClicked(slot);
                    }
                });
                if (withCheck)
                {
                    Toggle check = Check(back.transform, false, on =>
                    {
                        if (!updating && RowChecked != null)
                        {
                            RowChecked(slot, on);
                        }
                    });
                    Place(check, 12f, (rowHeight - 18f) / 2f, 18f, 18f);
                    checks.Add(check);
                }
                rows.Add(back);
                cells.Add(Cells(back.transform, rowHeight, widths, null, Text, 14f, rightAlign, left));
                back.gameObject.SetActive(false);
            }

            float body = HeaderHeight + Gap + pageSize * (rowHeight + Gap);
            empty = Label(parent, "", 15f, TextMuted, TextAlignmentOptions.Center);
            Place(empty, x, y + HeaderHeight + 40f, width, 30f);

            float footer = y + body + 10f;
            float middle = x + width / 2f;
            prev = Button(parent, "< 이전", () => Turn(-1), false, 14f);
            Place(prev, middle - 230f, footer, 110f, 34f);
            pageLabel = Label(parent, "", 14f, TextSub, TextAlignmentOptions.Center);
            Place(pageLabel, middle - 115f, footer, 230f, 34f);
            next = Button(parent, "다음 >", () => Turn(1), false, 14f);
            Place(next, middle + 120f, footer, 110f, 34f);
            Height = body + 10f + 34f;
            SetPage(0, 0);
        }

        /// <summary>쪽 번호와 전체 행 수. 쪽이 범위를 넘으면 마지막 쪽으로 맞추고 맞춘 쪽을 돌려준다.</summary>
        public int SetPage(int page, int totalRows)
        {
            PageCount = Math.Max(1, (totalRows + PageSize - 1) / PageSize);
            Page = Mathf.Clamp(page, 0, PageCount - 1);
            pageLabel.text = (Page + 1).ToString(Number) + " / " + PageCount.ToString(Number) + "쪽  (전체 " +
                             totalRows.ToString("N0", Number) + ")";
            SetEnabled(prev, Page > 0);
            SetEnabled(next, Page < PageCount - 1);
            return Page;
        }

        public void SetEmptyText(string text)
        {
            empty.text = text ?? "";
        }

        /// <summary>slot 행에 글자를 넣는다. colors 가 있으면 칸마다 글자색.</summary>
        public void SetRow(int slot, string[] texts, Color[] colors = null, bool isChecked = false)
        {
            updating = true;
            rows[slot].gameObject.SetActive(true);
            TextMeshProUGUI[] row = cells[slot];
            for (int i = 0; i < row.Length; i++)
            {
                row[i].text = texts != null && i < texts.Length ? texts[i] ?? "" : "";
                row[i].color = colors != null && i < colors.Length ? colors[i] : Text;
            }
            if (slot < checks.Count)
            {
                checks[slot].SetIsOnWithoutNotify(isChecked);
            }
            updating = false;
            Paint(slot);
        }

        public void HideRowsFrom(int slot)
        {
            for (int i = slot; i < rows.Count; i++)
            {
                rows[i].gameObject.SetActive(false);
            }
            empty.gameObject.SetActive(slot == 0);
        }

        public void SetAllChecked(bool on)
        {
            if (checkAll != null)
            {
                checkAll.SetIsOnWithoutNotify(on);
            }
        }

        public void Select(int slot)
        {
            int old = SelectedSlot;
            SelectedSlot = slot;
            if (old >= 0 && old < rows.Count)
            {
                Paint(old);
            }
            if (slot >= 0 && slot < rows.Count)
            {
                Paint(slot);
            }
        }

        private void Paint(int slot)
        {
            rows[slot].color = slot == SelectedSlot ? RowSelected : (slot % 2 == 0 ? Row : RowAlt);
        }

        private void Turn(int delta)
        {
            int target = Page + delta;
            if (target >= 0 && target < PageCount && PageChanged != null)
            {
                PageChanged(target);
            }
        }
    }

    // ------------------------------------------------------------------ 가로 막대 그래프

    public struct BarItem
    {
        public string Label;
        public double Value;
        public string Text;
    }

    /// <summary>
    /// parent 의 (x, y) 부터 가로 막대를 그린다. 한 계열이라 범례 없이 제목이 무엇인지 말한다.
    /// 막대는 기준선에 붙이고 끝만 둥글게, 값 글자는 막대 색이 아닌 글자 색으로.
    /// </summary>
    public static void BarChart(Transform parent, float x, float y, float width, IList<BarItem> items, float rowHeight = 30f,
        float labelWidth = 210f)
    {
        if (items.Count == 0)
        {
            return;
        }
        float valueWidth = 140f;
        float plotWidth = Mathf.Max(60f, width - labelWidth - valueWidth - 12f);
        double max = 0;
        foreach (BarItem item in items)
        {
            if (!double.IsNaN(item.Value))
            {
                max = Math.Max(max, item.Value);
            }
        }
        if (max <= 0)
        {
            max = 1;
        }

        Image axis = Panel(parent, "Axis", Line);
        Place(axis.gameObject, x + labelWidth, y, 1f, items.Count * rowHeight);

        for (int i = 0; i < items.Count; i++)
        {
            float top = y + i * rowHeight;
            TextMeshProUGUI label = Label(parent, items[i].Label, 14f, TextSub, TextAlignmentOptions.MidlineRight);
            Place(label.gameObject, x, top, labelWidth - 10f, rowHeight);

            double v = double.IsNaN(items[i].Value) ? 0 : items[i].Value;
            float barWidth = Mathf.Max(3f, (float)(v / max) * plotWidth);
            GameObject barObject = Create("Bar", parent);
            ProceduralImage bar = barObject.AddComponent<ProceduralImage>();
            OnlyOneEdgeModifier edge = barObject.AddComponent<OnlyOneEdgeModifier>();
            edge.Side = OnlyOneEdgeModifier.ProceduralImageEdge.Right;
            edge.Radius = 4f;
            bar.color = Bar;
            bar.raycastTarget = false;
            float barHeight = Mathf.Min(16f, rowHeight - 10f);
            Place(barObject, x + labelWidth + 1f, top + (rowHeight - barHeight) / 2f, barWidth, barHeight);

            TextMeshProUGUI value = Label(parent, items[i].Text, 13f, Text);
            Place(value.gameObject, x + labelWidth + barWidth + 8f, top, valueWidth, rowHeight);
        }
    }

    // ------------------------------------------------------------------ 숫자·데이터

    public static string Str(System.Data.DataRow row, string column)
    {
        if (row == null || !row.Table.Columns.Contains(column) || row[column] == DBNull.Value)
        {
            return "";
        }
        return Convert.ToString(row[column], Number);
    }

    public static double Num(System.Data.DataRow row, string column)
    {
        if (row == null || !row.Table.Columns.Contains(column) || row[column] == DBNull.Value)
        {
            return double.NaN;
        }
        double value;
        return double.TryParse(Convert.ToString(row[column], Number), NumberStyles.Float, Number, out value) ? value : double.NaN;
    }

    public static string Int(double value)
    {
        return double.IsNaN(value) ? "-" : Math.Round(value).ToString("N0", Number);
    }

    /// <summary>㎡당 가격: 한국 '935만 원', 일본 '99.4만 엔'</summary>
    public static string Unit(string country, double value)
    {
        if (double.IsNaN(value))
        {
            return "-";
        }
        if (country == "KR")
        {
            return Math.Round(value / 10000.0).ToString("N0", Number) + "만 원";
        }
        return (value / 10000.0).ToString("#,0.#", Number) + "만 엔";
    }

    /// <summary>거래가: 한국 '7.6억 원', 일본 '5,292만 엔'</summary>
    public static string Price(string country, double value)
    {
        if (double.IsNaN(value))
        {
            return "-";
        }
        if (country == "KR")
        {
            return value >= 1e8 ? (value / 1e8).ToString("#,0.##", Number) + "억 원"
                                : Math.Round(value / 1e4).ToString("N0", Number) + "만 원";
        }
        return Math.Round(value / 1e4).ToString("N0", Number) + "만 엔";
    }

    public static string Rate(double value)
    {
        return double.IsNaN(value) ? "-" : value.ToString("#,0.#", Number) + " 대 1";
    }

    public static string Index(double value)
    {
        return double.IsNaN(value) ? "-" : value.ToString("0.0", Number);
    }

    public static string Area(double value)
    {
        return double.IsNaN(value) ? "-" : value.ToString("0.#", Number) + Sqm;
    }
}
