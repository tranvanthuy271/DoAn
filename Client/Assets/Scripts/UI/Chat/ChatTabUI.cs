using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý thanh tab phía dưới Chat Panel.
/// Tự tạo các tab button từ danh sách kênh cấu hình.
/// </summary>
public class ChatTabUI : MonoBehaviour
{
    [Header("Tab Config")]
    [SerializeField] private List<ChatChannel> tabs = new List<ChatChannel>
    {
        ChatChannel.World, ChatChannel.Private, ChatChannel.Clan, ChatChannel.Group, ChatChannel.Class
    };

    [Header("Prefab / Style")]
    [SerializeField] private GameObject  tabButtonPrefab;    // Button + TextMeshProUGUI
    [SerializeField] private Color       activeColor   = new Color(0.9f, 0.7f, 0.2f);
    [SerializeField] private Color       inactiveColor = new Color(0.4f, 0.3f, 0.1f);

    // ── State ─────────────────────────────────────────────────────────────────

    private readonly List<Button>          _buttons  = new List<Button>();
    private readonly List<TextMeshProUGUI> _labels   = new List<TextMeshProUGUI>();
    private Action<ChatChannel>            _onSelect;
    private ChatChannel                    _active;

    // ── Setup ─────────────────────────────────────────────────────────────────

    public void SetupTabs(Action<ChatChannel> onSelect)
    {
        _onSelect = onSelect;

        int existingCount = transform.childCount;

        if (existingCount == tabs.Count)
        {
            // Tái sử dụng tab button đã config trong prefab — chỉ wire events, không rebuild
            _buttons.Clear();
            _labels.Clear();
            for (int i = 0; i < tabs.Count; i++)
            {
                var child = transform.GetChild(i);
                var btn   = child.GetComponent<Button>();
                var lbl   = child.GetComponentInChildren<TextMeshProUGUI>();
                _buttons.Add(btn);
                _labels.Add(lbl);

                int idx = i;
                btn?.onClick.RemoveAllListeners();
                btn?.onClick.AddListener(() => OnTabClicked(tabs[idx]));
            }
        }
        else
        {
            // Số tab không khớp → xóa và tạo lại
            foreach (Transform child in transform)
                Destroy(child.gameObject);
            _buttons.Clear();
            _labels.Clear();

            for (int i = 0; i < tabs.Count; i++)
            {
                var ch = tabs[i];
                GameObject go = tabButtonPrefab != null
                    ? Instantiate(tabButtonPrefab, transform)
                    : BuildDefaultTabButton();
                if (tabButtonPrefab == null)
                    go.transform.SetParent(transform, false);

                var btn = go.GetComponent<Button>();
                var lbl = go.GetComponentInChildren<TextMeshProUGUI>();
                if (lbl != null) lbl.text = ch.DisplayName();

                int idx = i;
                btn?.onClick.AddListener(() => OnTabClicked(tabs[idx]));
                _buttons.Add(btn);
                _labels.Add(lbl);
            }
        }

        SelectTab(_active);
    }

    public void SelectTab(ChatChannel ch)
    {
        _active = ch;
        for (int i = 0; i < _buttons.Count; i++)
        {
            if (_buttons[i] == null) continue;
            var img = _buttons[i].GetComponent<Image>();
            if (img != null) img.color = (tabs[i] == ch) ? activeColor : inactiveColor;
        }
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private void OnTabClicked(ChatChannel ch)
    {
        SelectTab(ch);
        _onSelect?.Invoke(ch);
    }

    private static GameObject BuildDefaultTabButton()
    {
        var go  = new GameObject("Tab", typeof(RectTransform), typeof(Image), typeof(Button));
        var rt  = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(80, 30);

        var txtGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGo.transform.SetParent(go.transform, false);
        var trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        var tmp = txtGo.GetComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize  = 14;
        tmp.color     = Color.white;

        return go;
    }
}
