using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Mathematics;
using static UnityEngine.EventSystems.EventTrigger;
using System.Collections;

[Serializable]
public class LocationEntry
{
    public string name;
    public double lat;
    public double lon;

    // Laufzeit-Cache für schnelleren Vergleich:
    [NonSerialized] public string nameLower;
}

[Serializable]
public class LocationDatabase
{
    public List<LocationEntry> entries;
}

public class GeoNamesSearchFromJSON : MonoBehaviour
{
    [Header("GeoNames JSON")]
    public TextAsset geoJson;

    [Header("UI")]
    public TMP_InputField inputField;
    public RectTransform suggestionPanel;
    public GameObject suggestionButtonPrefab;
    public int maxResults = 5;

    [Header("Controllers")]
    public CesiumZoomController zoomController;

    // Laufzeit-DB
    private List<LocationEntry> _db;
    private Dictionary<char, List<LocationEntry>> _group1;
    private Dictionary<string, List<LocationEntry>> _group2;

    // Pool für Buttons
    private Stack<GameObject> _buttonPool = new Stack<GameObject>();

    void Awake()
    {
        // 1) JSON parsen
        var dbWrap = JsonUtility.FromJson<LocationDatabase>(geoJson.text);
        _db = dbWrap.entries;

        // 2) Namen lower-cachen
        foreach (var e in _db)
            e.nameLower = e.name.ToLowerInvariant();

        // 3) Ein- und Zwei-Buchstaben Gruppen bauen
        _group1 = new Dictionary<char, List<LocationEntry>>();
        _group2 = new Dictionary<string, List<LocationEntry>>();
        foreach (var e in _db)
        {
            if (string.IsNullOrEmpty(e.nameLower)) continue;

            // Gruppe 1 Buchstabe
            char c1 = e.nameLower[0];
            if (!_group1.TryGetValue(c1, out var list1))
            {
                list1 = new List<LocationEntry>();
                _group1[c1] = list1;
            }
            list1.Add(e);

            // Gruppe 2 Buchstaben
            if (e.nameLower.Length >= 2)
            {
                string c2 = e.nameLower.Substring(0, 2);
                if (!_group2.TryGetValue(c2, out var list2))
                {
                    list2 = new List<LocationEntry>();
                    _group2[c2] = list2;
                }
                list2.Add(e);
            }
        }
    }

    void Start()
    {
        inputField.onValueChanged.AddListener(OnTextChanged);
    }

    void OnTextChanged(string text)
    {
        // 1) Alte Buttons zurück in Pool
        foreach (Transform child in suggestionPanel)
        {
            var go = child.gameObject;
            go.SetActive(false);
            _buttonPool.Push(go);
        }
        suggestionPanel.DetachChildren();

        if (string.IsNullOrWhiteSpace(text))
            return;

        string lower = text.ToLowerInvariant();
        List<LocationEntry> candidates;

        // 2) Suche nur in der passenden Gruppe
        if (lower.Length >= 2 && _group2.TryGetValue(lower.Substring(0, 2), out candidates))
        {
            // zwei-Buchstaben-Gruppe
        }
        else if (_group1.TryGetValue(lower[0], out candidates))
        {
            // ein-Buchstaben-Gruppe
        }
        else
        {
            return; // keine Gruppe → kein Match
        }

        int count = 0;
        foreach (var entry in candidates)
        {
            if (entry.nameLower.StartsWith(lower))
            {
                var btn = GetPooledButton();
                btn.transform.SetParent(suggestionPanel, false);
                btn.GetComponentInChildren<TMP_Text>().text = entry.name;

                var button = btn.GetComponent<Button>();
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => {
                    SelectLocation(entry);
                });

                count++;
                if (count >= maxResults)
                    break;
            }
        }
    }

    GameObject GetPooledButton()
    {
        if (_buttonPool.Count > 0)
        {
            var go = _buttonPool.Pop();
            go.SetActive(true);
            return go;
        }
        else
        {
            return Instantiate(suggestionButtonPrefab);
        }
    }

    void SelectLocation(LocationEntry entry)
    {
        zoomController.ZoomToEarth(new double3(entry.lon, entry.lat, 400));

        inputField.text = entry.name;
        OnTextChanged(entry.name);

        inputField.text = "";
    }

    void OnSelect(LocationEntry entry)
    {
        // Alte Coroutines abbrechen, wenn noch eine läuft
        StopAllCoroutines();

        // Start der neuen Sequenz
        StartCoroutine(SwitchSpaceThenZoom(entry));
    }

    IEnumerator SwitchSpaceThenZoom(LocationEntry entry)
    {
        // 1) in Space schalten
        zoomController.ZoomToSpace();

        // 2) warten bis Space-Zoom + FOV fertig ist
        float waitTime = zoomController.zoomDuration + zoomController.fovTransitionDuration;
        yield return new WaitForSeconds(waitTime);

        // 3) reinzoomen auf die neue Stadt
        zoomController.ZoomToEarth(new double3(entry.lon, entry.lat, 400));

        // 4) UI aufräumen (Input-Feld & Vorschläge)
        inputField.text = entry.name;
        ClearSuggestions();
    }

    void ClearSuggestions()
    {
        foreach (Transform child in suggestionPanel)
        {
            child.gameObject.SetActive(false);
            _buttonPool.Push(child.gameObject);
        }
        suggestionPanel.DetachChildren();
    }
}