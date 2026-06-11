using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// BXH voi 4 tab chinh + 5 sub-tab.
public class LeaderboardPanelUI : MonoBehaviour
{
    private static readonly int[] SubCatId = { 1, 5, 2, 3, 4 };
    private static readonly string[] ValueColHdr = { "Cap", "Vang", "N.Vu", "Ngay", "Wave" };

    private const string BlockKey = "LeaderboardPanelUI";
    private const string LogTag = "[BXH]";

    [Header("Main Tab Buttons [4]: Dua Top | Su Kien | Tuan&Thang | Thuong")]
    [SerializeField] private Button[] mainTabs = new Button[4];

    [Header("Sub Tab Buttons [5]: Cao Thu | Nap Vang | Hoa Chi | Chuyen Can | Pho Ban")]
    [SerializeField] private Button[] subTabs = new Button[5];

    [Header("Groups")]
    [SerializeField] private GameObject contentGroup;
    [SerializeField] private GameObject emptyStateGroup;

    [Header("Empty State Text")]
    [SerializeField] private TMP_Text emptyStateText;

    [Header("Table Header Cells [4]: Hang | Ten | (dynamic) | Thong tin")]
    [SerializeField] private TMP_Text[] headerCells = new TMP_Text[4];

    [Header("Row Scroll")]
    [SerializeField] private Transform rowContent;
    [SerializeField] private GameObject rowPrefab;
    [SerializeField] private string rowPrefabPath = "Prefabs/UI/Leaderboard/LeaderboardRowEntry";

    [Header("Misc")]
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private Button closeButton;

    [Header("Tab Colors")]
    [SerializeField] private Color mainActiveColor = new Color(1.00f, 0.85f, 0.10f);
    [SerializeField] private Color mainInactiveColor = new Color(0.70f, 0.45f, 0.20f);
    [SerializeField] private Color subActiveColor = new Color(1.00f, 0.85f, 0.10f);
    [SerializeField] private Color subInactiveColor = new Color(0.85f, 0.72f, 0.55f);

    private int _mainIdx;
    private int _subIdx;
    private readonly List<GameObject> _rows = new();
    private LeaderboardService _svc;
    private TMP_Text[] _mainLabels;
    private TMP_Text[] _subLabels;
    private GameObject _rowPrefabCache;

    private void Awake()
    {
        _mainLabels = new TMP_Text[mainTabs.Length];
        _subLabels = new TMP_Text[subTabs.Length];

        for (int i = 0; i < mainTabs.Length; i++)
        {
            if (!mainTabs[i]) continue;
            _mainLabels[i] = mainTabs[i].GetComponentInChildren<TMP_Text>();
            int k = i;
            mainTabs[i].onClick.AddListener(() => OnMainTab(k));
        }

        for (int i = 0; i < subTabs.Length; i++)
        {
            if (!subTabs[i]) continue;
            _subLabels[i] = subTabs[i].GetComponentInChildren<TMP_Text>();
            int k = i;
            subTabs[i].onClick.AddListener(() => OnSubTab(k));
        }

        closeButton?.onClick.AddListener(Close);
        UIPanelManager.Register(gameObject, Close);
    }

    private void OnEnable()
    {
        InputManager.Instance?.SetGameplayInputBlocked(BlockKey, true);
        EnsureSvc();
        ShowMain(_mainIdx, true);
    }

    private void OnDisable()
    {
        InputManager.Instance?.SetGameplayInputBlocked(BlockKey, false);
    }

    public void Open()
    {
        UIPanelManager.CloseOthers(gameObject);
        gameObject.SetActive(true);
        UIPanelManager.NotifyOpened(gameObject);
    }

    public void Close()
    {
        gameObject.SetActive(false);
        UIPanelManager.NotifyClosed(gameObject);
    }

    public void SelectTab(int idx, bool forceReload = false)
    {
        _mainIdx = idx;
        ShowMain(idx, forceReload);
    }

    private void OnMainTab(int idx) => ShowMain(idx);

    private void OnSubTab(int idx)
    {
        _subIdx = idx;
        RefreshSubColors();
        ApplyHeader();
        Fetch();
    }

    private void ShowMain(int idx, bool forceLoad = false)
    {
        _mainIdx = idx;
        RefreshMainColors();

        bool isContent = idx == 0;
        contentGroup?.SetActive(isContent);
        emptyStateGroup?.SetActive(!isContent);

        if (!isContent)
        {
            if (emptyStateText) emptyStateText.text = "Sự kiện đua top này chưa mở";
            return;
        }

        RefreshSubColors();
        ApplyHeader();
        if (forceLoad || _rows.Count == 0) Fetch();
    }

    private void Fetch()
    {
        int catId = SubCatId[_subIdx];
        { /* {LogTag} Fetch subIdx={_subIdx} → catId={catId}, _svc={((_svc != null) ? _svc.name */ }
        SetLoading(true);
        _svc.FetchCategory(catId, OnData, OnError);
    }

    private void OnData(LeaderboardEntryDto[] list)
    {
        SetLoading(false);
        ClearRows();

        int count = list?.Length ?? 0;
        { /* {LogTag} OnData: {count} entries, rowContent={(rowContent != null ? rowContent.name */ }

        if (list == null || list.Length == 0)
        {
            ShowMsg("Chua co du lieu.");
            return;
        }

        if (rowContent == null)
        {
            { /* Lỗi: {LogTag} rowContent chua duoc wire trong Inspector! Hang se khong hien thi */ }
            return;
        }

        GameObject prefab = GetRowPrefab();
        { /* {LogTag} rowPrefab={(prefab != null ? prefab.name */ }
        for (int i = 0; i < list.Length; i++)
        {
            var e = list[i];
            { /* {LogTag} Row {i}: Rank={e.Rank} Name={e.CharacterName} Value={e.Value} Extra={e.Extra} */ }
            GameObject go = prefab ? Instantiate(prefab, rowContent) : BuildCodeRow(rowContent);
            go.GetComponent<LeaderboardRowEntryUI>()?.Setup(list[i], i % 2 == 1);
            _rows.Add(go);
        }
    }

    private void OnError(string msg)
    {
        SetLoading(false);
        ShowMsg(msg);
        { /* Cảnh báo: {LogTag} {msg} */ }
    }

    private void SetLoading(bool on)
    {
        if (!loadingText) return;
        loadingText.gameObject.SetActive(on);
        if (on) loadingText.text = "Dang tai...";
    }

    private void ShowMsg(string msg)
    {
        if (!loadingText) return;
        loadingText.gameObject.SetActive(true);
        loadingText.text = msg;
    }

    private void ClearRows()
    {
        foreach (GameObject row in _rows)
            if (row) Destroy(row);

        _rows.Clear();

        if (loadingText) loadingText.gameObject.SetActive(false);
    }

    private void RefreshMainColors()
    {
        for (int i = 0; i < _mainLabels.Length; i++)
            if (_mainLabels[i])
                _mainLabels[i].color = i == _mainIdx ? mainActiveColor : mainInactiveColor;
    }

    private void RefreshSubColors()
    {
        for (int i = 0; i < _subLabels.Length; i++)
            if (_subLabels[i])
                _subLabels[i].color = i == _subIdx ? subActiveColor : subInactiveColor;
    }

    private void ApplyHeader()
    {
        if (headerCells == null || headerCells.Length < 4) return;
        if (headerCells[0]) headerCells[0].text = "Hang";
        if (headerCells[1]) headerCells[1].text = "Ten";
        if (headerCells[2]) headerCells[2].text = ValueColHdr[_subIdx];
        if (headerCells[3]) headerCells[3].text = "Thong tin";
    }

    private void EnsureSvc()
    {
        if (_svc && _svc.gameObject.activeInHierarchy)
        {
            { /* {LogTag} EnsureSvc: đã có _svc={_svc.name} */ }
            return;
        }
        _svc = FindObjectOfType<LeaderboardService>();
        if (_svc)
        {
            { /* {LogTag} EnsureSvc: tìm thấy trong scene → {_svc.name} */ }
        }
        else
        {
            _svc = new GameObject("LeaderboardSvc[auto]").AddComponent<LeaderboardService>();
            { /* {LogTag} EnsureSvc: tạo mới LeaderboardSvc[auto] */ }
        }
    }

    private GameObject GetRowPrefab()
    {
        if (_rowPrefabCache) return _rowPrefabCache;
        if (rowPrefab)
        {
            _rowPrefabCache = rowPrefab;
            return _rowPrefabCache;
        }

        GameObject loaded = Resources.Load<GameObject>(rowPrefabPath);
        if (loaded) _rowPrefabCache = loaded;
        return _rowPrefabCache;
    }

    private static GameObject BuildCodeRow(Transform parent)
    {
        var go = new GameObject("LBRow");
        go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 44);
        go.AddComponent<Image>().color = Color.clear;

        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.padding = new RectOffset(8, 8, 0, 0);
        hlg.spacing = 4;

        (string cname, float w, TextAlignmentOptions a)[] cols =
        {
            ("RankText", 55f, TextAlignmentOptions.Center),
            ("NameText", 180f, TextAlignmentOptions.Left),
            ("ValueText", 80f, TextAlignmentOptions.Center),
            ("ExtraText", 145f, TextAlignmentOptions.Left),
        };

        foreach ((string cname, float w, TextAlignmentOptions a) in cols)
        {
            var cell = new GameObject(cname);
            cell.transform.SetParent(go.transform, false);
            var le = cell.AddComponent<LayoutElement>();
            le.preferredWidth = w;
            le.minWidth = w;
            le.flexibleWidth = 0;

            var tmp = cell.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 17;
            tmp.color = Color.white;
            tmp.alignment = a;
        }

        var rowUI = go.AddComponent<LeaderboardRowEntryUI>();
        rowUI.SetRefs(
            go.transform.Find("RankText")?.GetComponent<TextMeshProUGUI>(),
            go.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>(),
            go.transform.Find("ValueText")?.GetComponent<TextMeshProUGUI>(),
            go.transform.Find("ExtraText")?.GetComponent<TextMeshProUGUI>()
        );

        go.transform.SetParent(parent, false);
        return go;
    }
}
