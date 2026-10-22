using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 화면 왼쪽에 가사 목록을 띄우고, 지금 박의 가사를 강조한다
// 누르기 판정은 InputManager가 TryGetLyricBeat로 묻는다. 씬에 EventSystem이 없으므로 UI 이벤트를 쓰지 않는다
public class UIManager : MonoBehaviour
{
    [SerializeField] private PromptManager promptManager;
    // CanvasScaler 기준 해상도(1920x1080)에서의 크기
    [SerializeField] private float panelWidth = 380f;
    [SerializeField] private float lineHeight = 52f;
    [SerializeField] private float padding = 24f;
    [SerializeField] private int fontSize = 30;
    [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.45f);
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.55f);
    [SerializeField] private Color currentColor = new Color(1f, 0.85f, 0.3f, 1f);
    // 스크롤이 목표 위치로 따라가는 속도
    [SerializeField] private float scrollSmoothing = 10f;

    private class LyricLine
    {
        public int beat;
        public RectTransform rect;
        public Text text;
    }

    // 가사가 있는 줄만. [ ]가 빈 줄은 넣지 않는다
    private readonly List<LyricLine> lines = new List<LyricLine>();
    private RectTransform panel;
    private RectTransform content;
    private int currentIndex = -1;
    // content를 위로 올린 거리
    private float scroll;
    private float targetScroll;

    private float MaxScroll => Mathf.Max(0f, content.sizeDelta.y - panel.rect.height);

    // PromptManager는 Awake에서 텍스트를 읽으므로 Start에서 만든다
    private void Start()
    {
        BuildPanel();
        BuildLines();
    }

    private void Update()
    {
        if (content == null) return;

        targetScroll = Mathf.Clamp(targetScroll, 0f, MaxScroll);
        scroll = Mathf.Lerp(scroll, targetScroll, 1f - Mathf.Exp(-scrollSmoothing * Time.unscaledDeltaTime));
        content.anchoredPosition = new Vector2(0f, scroll);
    }

    // beat 이전에 시작한 마지막 가사를 강조하고, 그 줄이 보이도록 스크롤한다
    public void SetCurrentBeat(int beat)
    {
        int index = -1;
        for (int i = 0; i < lines.Count && lines[i].beat <= beat; i++)
            index = i;
        if (index == currentIndex) return;

        if (currentIndex >= 0)
            SetHighlight(lines[currentIndex], false);
        currentIndex = index;
        if (currentIndex < 0) return;

        SetHighlight(lines[currentIndex], true);
        // 지금 가사를 패널 위에서 1/3 지점에 둔다
        targetScroll = padding + currentIndex * lineHeight - panel.rect.height / 3f;
    }

    public bool IsOverLyrics(Vector2 screenPosition) =>
        panel != null && RectTransformUtility.RectangleContainsScreenPoint(panel, screenPosition, null);

    // screenPosition에 있는 가사의 박. 가사 위가 아니면 false
    public bool TryGetLyricBeat(Vector2 screenPosition, out int beat)
    {
        beat = -1;
        // 패널 밖으로 스크롤된 줄은 가려져 있으므로 누를 수 없다
        if (!IsOverLyrics(screenPosition)) return false;

        foreach (LyricLine line in lines)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(line.rect, screenPosition, null))
            {
                beat = line.beat;
                return true;
            }
        }
        return false;
    }

    // amount는 줄 수. 양수면 아래 가사로 내려간다
    public void Scroll(float amount) => targetScroll += amount * lineHeight;

    private void BuildPanel()
    {
        GameObject canvasObject = new GameObject("LyricCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;

        // 왼쪽 가장자리에 붙은 세로 띠. 넘치는 가사는 RectMask2D로 가린다
        panel = new GameObject("LyricPanel", typeof(RectTransform), typeof(Image), typeof(RectMask2D)).GetComponent<RectTransform>();
        panel.SetParent(canvasObject.transform, false);
        panel.anchorMin = new Vector2(0f, 0f);
        panel.anchorMax = new Vector2(0f, 1f);
        panel.pivot = new Vector2(0f, 0.5f);
        panel.sizeDelta = new Vector2(panelWidth, 0f);
        panel.anchoredPosition = Vector2.zero;
        Image background = panel.GetComponent<Image>();
        background.color = panelColor;
        background.raycastTarget = false;

        content = new GameObject("Lyrics", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(panel, false);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
    }

    private void BuildLines()
    {
        // LegacyRuntime.ttf는 OS 폰트로 대체해 그리므로 한글도 보인다
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        foreach (Prompt prompt in promptManager.Prompts)
        {
            if (prompt == null || string.IsNullOrWhiteSpace(prompt.lyric)) continue;

            RectTransform rect = new GameObject($"Lyric_{prompt.beat}", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
            rect.SetParent(content, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(padding, 0f);
            rect.offsetMax = new Vector2(-padding, 0f);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, lineHeight);
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -padding - lines.Count * lineHeight);

            Text text = rect.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.raycastTarget = false;
            text.text = prompt.lyric.Trim();

            LyricLine line = new LyricLine { beat = prompt.beat, rect = rect, text = text };
            SetHighlight(line, false);
            lines.Add(line);
        }

        content.sizeDelta = new Vector2(0f, lines.Count * lineHeight + padding * 2f);
    }

    private void SetHighlight(LyricLine line, bool current)
    {
        line.text.color = current ? currentColor : normalColor;
        line.text.fontStyle = current ? FontStyle.Bold : FontStyle.Normal;
    }
}
