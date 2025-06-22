using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Mathematics;
using System.Collections;
using CesiumForUnity;

[Serializable]
public class LocationEntry
{
    public string name;
    public double lat;
    public double lon;

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

    private List<LocationEntry> _db;
    private Dictionary<char, List<LocationEntry>> _group1;
    private Dictionary<string, List<LocationEntry>> _group2;

    private Stack<GameObject> _buttonPool = new Stack<GameObject>();

    public CesiumGeoreference georeference;

    void Awake()
    {
        var dbWrap = JsonUtility.FromJson<LocationDatabase>(geoJson.text);
        _db = dbWrap.entries;

        foreach (var e in _db)
            e.nameLower = e.name.ToLowerInvariant();

        _group1 = new Dictionary<char, List<LocationEntry>>();
        _group2 = new Dictionary<string, List<LocationEntry>>();
        foreach (var e in _db)
        {
            if (string.IsNullOrEmpty(e.nameLower)) continue;

            char c1 = e.nameLower[0];
            if (!_group1.TryGetValue(c1, out var list1))
            {
                list1 = new List<LocationEntry>();
                _group1[c1] = list1;
            }
            list1.Add(e);

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

    private void OnTextChanged(string text)
    {
        ClearSuggestions();

        if (string.IsNullOrWhiteSpace(text))
            return;

        string lower = text.ToLowerInvariant();

        List<LocationEntry> candidates = null;
        if (lower.Length >= 2 && _group2.TryGetValue(lower.Substring(0, 2), out var g2))
            candidates = g2;
        else if (_group1.TryGetValue(lower[0], out var g1))
            candidates = g1;
        else
            return;

        int count = 0;
        foreach (var entry in candidates)
        {
            if (!entry.nameLower.StartsWith(lower))
                continue;

            var btn = GetPooledButton();
            btn.transform.SetParent(suggestionPanel, false);
            btn.GetComponentInChildren<TMP_Text>().text = entry.name;

            var uiBtn = btn.GetComponent<Button>();
            uiBtn.onClick.RemoveAllListeners();
            uiBtn.onClick.AddListener(() => OnSelect(entry));

            if (++count >= maxResults)
                break;
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

    private void OnSelect(LocationEntry entry)
    {
        ClearSuggestions();
        inputField.text = "";

        StopAllCoroutines();
        StartCoroutine(SwitchSpaceThenZoom(entry));
    }

    private IEnumerator SwitchSpaceThenZoom(LocationEntry entry)
    {
        if (Mathf.Abs(zoomController.targetCamera.fieldOfView - zoomController.spaceFov) < 0.1f)
        {
            georeference.latitude = entry.lat; 
            georeference.longitude = entry.lon;
            georeference.height = 400;
            zoomController.ZoomToEarth(new double3(entry.lon, entry.lat, 1000));
            yield break;
        }

        zoomController.ZoomToSpace();
        
        yield return new WaitForSeconds(2f);
        georeference.latitude = entry.lat;
        georeference.longitude = entry.lon;
        georeference.height = 400;
        zoomController.ZoomToEarth(new double3(entry.lon, entry.lat, 1000));
    }

    private void ClearSuggestions()
    {
        foreach (Transform child in suggestionPanel)
        {
            var go = child.gameObject;
            go.SetActive(false);
            _buttonPool.Push(go);
        }
    }
}