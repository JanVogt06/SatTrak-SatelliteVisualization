using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;


public class HelpContentBuilder : MonoBehaviour
{
    /* ---------- Inspector ---------- */
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

    [Header("TOC")]
    [SerializeField] private GameObject tocContainerPrefab;
    [SerializeField] private GameObject tocButtonPrefab;

    [Header("Leerzeile")]
    [SerializeField] float spacerHeight      = 12f; // Abstand für "\"-Zeile
    [SerializeField] float bottomSpacerHeight = 80f;

    /* ---------- Regex ---------- */
    readonly Regex h1Rx     = new(@"^#\s+(.*)");
    readonly Regex h2Rx     = new(@"^##\s+(.*)");
    readonly Regex h3Rx     = new(@"^###\s+(.*)");
    readonly Regex imageRx  = new(@"!\[[^\]]*]\(([^)]+)\)");
    readonly Regex bulletRx = new(@"^(\s*)([-+*])\s+(.*)");
    readonly Regex boldRx   = new(@"\*\*(.+?)\*\*");
    readonly Regex italicRx = new(@"(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)|_(.+?)_");

    const string imageFolder = "HelpImages";

    /* ---------- Runtime ---------- */
    readonly List<(string title, RectTransform section)> tocEntries = new();

    void Start() => Build();

    void Build()
    {
        if (!markdownFile) { Debug.LogError($"{name}: Markdown fehlt"); return; }

        foreach (string raw in markdownFile.text.Split('\n'))
        {
            string line = raw.TrimEnd('\r');

            /* ----- Leerzeile "\" ----- */
            if (line.Trim() == "\\")
            {
                AddSpacer(spacerHeight);
                continue;
            }

            /* ----- H1 ----- */
            if (h1Rx.IsMatch(line))
            {
                string title = h1Rx.Match(line).Groups[1].Value.Trim();
                InstantiateTMP(h1Prefab, title);
                continue;
            }

            /* ----- H2 (für TOC) ----- */
            if (h2Rx.IsMatch(line))
            {
                string title = h2Rx.Match(line).Groups[1].Value.Trim();
                var go = InstantiateTMP(h2Prefab, title);
                tocEntries.Add((title, go.GetComponent<RectTransform>()));
                continue;
            }

            /* ----- H3 ----- */
            if (h3Rx.IsMatch(line))
            {
                string title = h3Rx.Match(line).Groups[1].Value.Trim();
                InstantiateTMP(h3Prefab, title);
                continue;
            }

            /* ----- Bild ----- */
            if (imageRx.IsMatch(line))
            {
                string shortName = Path.GetFileNameWithoutExtension(
                                    imageRx.Match(line).Groups[1].Value.Trim());
                TryInstantiateImage(shortName);
                continue;
            }

            /* ----- Bullet ----- */
            if (bulletRx.IsMatch(line))
            {
                var m      = bulletRx.Match(line);
                int indent = m.Groups[1].Value.Replace("\t", "    ").Length * 2;
                string txt = $"<indent={indent}>• {ParseInline(m.Groups[3].Value)}";
                InstantiateTMP(bodyPrefab, txt);
                continue;
            }

            /* ----- Fließtext ----- */
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

    /* ---------- TOC ---------- */
    void BuildTOC()
    {
        if (tocEntries.Count == 0 || tocContainerPrefab == null
            || tocButtonPrefab == null || scrollRect == null) return;

        /* Container erzeugen und direkt NACH der H1-Überschrift einfügen */
        var toc = Instantiate(tocContainerPrefab, contentRoot);
        toc.transform.SetSiblingIndex(1);

        /* ←── neuer Code: linken Margin setzen */
        if (toc.TryGetComponent(out HorizontalLayoutGroup hlg))
        {
            hlg.padding.left = 30;          // 20 px linker Rand
            hlg.SetLayoutHorizontal();      // sofort neu anwenden
        }

        foreach (var e in tocEntries)
        {
            var btn = Instantiate(tocButtonPrefab, toc.transform);
            btn.GetComponentInChildren<TMP_Text>().text = e.title;

            var tb = btn.GetComponent<TOCButton>() ?? btn.gameObject.AddComponent<TOCButton>();
            tb.Init(e.section, scrollRect);
        }
    }

    /* ---------- Helpers ---------- */
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