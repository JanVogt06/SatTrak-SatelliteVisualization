using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;

public class HelpContentBuilder : MonoBehaviour
{
    /* ───────── Inspector ───────── */
    [Header("Scroll View & Container")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform contentRoot;

    [Header("Markdown Asset")]
    [SerializeField] private TextAsset markdownFile;

    [Header("Prefabs")]
    [SerializeField] private GameObject h1Prefab;
    [SerializeField] private GameObject h2Prefab;
    [SerializeField] private GameObject h3Prefab;
    [SerializeField] private GameObject bodyPrefab;
    [SerializeField] private GameObject imagePrefab;
    [SerializeField] private GameObject thumbImagePrefab;
    [SerializeField] private GameObject thumbRowPrefab;

    [Header("TOC")]
    [SerializeField] private GameObject tocContainerPrefab;
    [SerializeField] private GameObject tocButtonPrefab;

    [Header("Spacers")]
    [SerializeField] private float spacerHeight = 12f;
    [SerializeField] private float bottomSpacerHeight = 80f;

    /* ───────── Regex ───────── */
    readonly Regex h1Rx     = new(@"^#\s+(.*)");
    readonly Regex h2Rx     = new(@"^##\s+(.*)");
    readonly Regex h3Rx     = new(@"^###\s+(.*)");
    readonly Regex imageRx  = new(@"!\[[^\]]*]\(([^)]+)\)");
    readonly Regex bulletRx = new(@"^(\s*)([-+*])\s+(.*)");
    readonly Regex boldRx   = new(@"\*\*(.+?)\*\*");
    readonly Regex italicRx = new(@"(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)|_(.+?)_");
    readonly Regex thumbRx  = new(@"!\[\s*thumb\s*]\(([^)]+)\)", RegexOptions.IgnoreCase);

    const string imageFolder = "HelpImages";

    /* ───────── Runtime ───────── */
    readonly List<(string title, RectTransform section)> tocEntries = new();
    RectTransform currentThumbRow = null;

    /* ───────── Entry ───────── */
    void Start() => Build();

    /* ───────── Build ───────── */
    void Build()
    {
        if (!markdownFile) { Debug.LogError($"{name}: Markdown fehlt"); return; }

        foreach (string raw in markdownFile.text.Split('\n'))
        {
            string line = raw.TrimEnd('\r');

            /* Leerzeile "\" */
            if (line.Trim() == "\\")
            {
                AddSpacer(spacerHeight);
                continue;
            }

            /* H1 */
            if (h1Rx.IsMatch(line))
            {
                InstantiateTMP(h1Prefab, h1Rx.Match(line).Groups[1].Value.Trim());
                continue;
            }

            /* H2 (TOC) */
            if (h2Rx.IsMatch(line))
            {
                string title = h2Rx.Match(line).Groups[1].Value.Trim();
                var rect = InstantiateTMP(h2Prefab, title);
                tocEntries.Add((title, rect));
                continue;
            }

            /* H3 */
            if (h3Rx.IsMatch(line))
            {
                InstantiateTMP(h3Prefab, h3Rx.Match(line).Groups[1].Value.Trim());
                continue;
            }

            /* Thumbnails (all thumbs in same markdown line share a row) */
            if (thumbRx.IsMatch(line))
            {
                int thumbCountInLine = thumbRx.Matches(line).Count;

                if (currentThumbRow == null)
                {
                    currentThumbRow = Instantiate(thumbRowPrefab, contentRoot)
                                    .GetComponent<RectTransform>();

                    if (currentThumbRow.TryGetComponent(out HorizontalLayoutGroup rowHLG))
                    {
                        rowHLG.padding.left = 30;          // linker Rand
                        rowHLG.SetLayoutHorizontal();
                    }
                }

                foreach (Match m in thumbRx.Matches(line))
                {
                    string shortName = Path.GetFileNameWithoutExtension(m.Groups[1].Value.Trim());
                    bool   isSingle  = thumbCountInLine == 1;
                    TryInstantiateThumb(shortName, currentThumbRow, isSingle);
                }

                currentThumbRow = null;
                continue;
            }

            /* Full-size Image */
            if (imageRx.IsMatch(line))
            {
                string shortName = Path.GetFileNameWithoutExtension(
                                    imageRx.Match(line).Groups[1].Value.Trim());
                TryInstantiateImage(shortName);
                continue;
            }

            /* Bullet */
            if (bulletRx.IsMatch(line))
            {
                var m = bulletRx.Match(line);
                int indent = m.Groups[1].Value.Replace("\t", "    ").Length * 2;
                string txt = $"<indent={indent}>• {ParseInline(m.Groups[3].Value)}";
                InstantiateTMP(bodyPrefab, txt);
                continue;
            }

            /* Plain text */
            if (!string.IsNullOrWhiteSpace(line))
            {
                InstantiateTMP(bodyPrefab, ParseInline(line));
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot as RectTransform);
        BuildTOC();
        AddSpacer(bottomSpacerHeight);
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot as RectTransform);
    }

    /* ───────── TOC ───────── */
    void BuildTOC()
    {
        if (tocEntries.Count == 0 || tocContainerPrefab == null
            || tocButtonPrefab == null || scrollRect == null) return;

        var toc = Instantiate(tocContainerPrefab, contentRoot);
        toc.transform.SetSiblingIndex(1);                        // direkt unter H1

        if (toc.TryGetComponent(out HorizontalLayoutGroup hlg))
        {
            hlg.padding.left = 30;                               // linker Rand
            hlg.SetLayoutHorizontal();
        }

        foreach (var e in tocEntries)
        {
            var btn = Instantiate(tocButtonPrefab, toc.transform);
            btn.GetComponentInChildren<TMP_Text>().text = e.title;

            var tb = btn.GetComponent<TOCButton>() ?? btn.AddComponent<TOCButton>();
            tb.Init(e.section, scrollRect);
        }
    }

    /* ───────── Helpers ───────── */
    RectTransform InstantiateTMP(GameObject prefab, string text)
    {
        var go = Instantiate(prefab, contentRoot);
        go.GetComponent<TMP_Text>().text = text;
        return go.GetComponent<RectTransform>();
    }

    void TryInstantiateImage(string shortName)
    {
        Sprite spr = Resources.Load<Sprite>($"{imageFolder}/{shortName}");
        if (spr)
        {
            var go = Instantiate(imagePrefab, contentRoot);
            go.GetComponent<Image>().sprite = spr;
        }
        else
        {
            InstantiateTMP(bodyPrefab,
                $"<i>[Image <b>{shortName}.png</b> not found]</i>");
        }
    }

    void TryInstantiateThumb(string shortName, RectTransform row, bool isSingle)
    {
        Sprite spr = Resources.Load<Sprite>($"{imageFolder}/{shortName}");
        if (!spr)
        {
            InstantiateTMP(bodyPrefab,
                $"<i>[Thumb <b>{shortName}.png</b> not found]</i>");
            return;
        }

        var go  = Instantiate(thumbImagePrefab, row);
        var img = go.GetComponent<Image>();
        img.sprite = spr;
        img.preserveAspect = true;

        /* Breite: 600 px wenn allein, sonst max 120 px */
        float w = isSingle ? 600f : Mathf.Min(120f, spr.rect.width);
        float h = w * spr.rect.height / spr.rect.width;

        var le = go.GetComponent<LayoutElement>();
        le.preferredWidth  = w;
        le.preferredHeight = h;
        le.minWidth        = w;
        le.minHeight       = h;
        le.flexibleWidth   = 0;
        le.flexibleHeight  = 0;
    }

    void AddSpacer(float height)
    {
        var space = new GameObject("Spacer", typeof(LayoutElement));
        space.transform.SetParent(contentRoot, false);
        var le = space.GetComponent<LayoutElement>();
        le.minHeight = le.preferredHeight = height;
    }

    string ParseInline(string s)
    {
        s = boldRx.Replace(s, "<b>$1</b>");
        return italicRx.Replace(s, m =>
            $"<i>{(m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value)}</i>");
    }
}
