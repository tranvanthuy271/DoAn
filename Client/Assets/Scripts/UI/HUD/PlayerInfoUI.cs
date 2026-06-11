using UnityEngine;
using TMPro;
using Unity.Netcode;
using Unity.Collections;

// UI để hiển thị thông tin người chơi (name, level, stats)
// Gắn vào player prefab hoặc UI canvas
public class PlayerInfoUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text hiển thị tên nhân vật")]
    public TMP_Text playerNameText;
    
    [Tooltip("Text hiển thị level")]
    public TMP_Text levelText;
    
    [Tooltip("Text hiển thị HP")]
    public TMP_Text hpText;
    
    [Tooltip("Text hiển thị MP")]
    public TMP_Text mpText;
    
    [Tooltip("Text hiển thị element type")]
    public TMP_Text elementText;

    [Header("Settings")]
    [Tooltip("Tự động tìm NetworkPlayerDataSync trong parent")]
    public bool autoFindDataSync = true;

    private NetworkPlayerDataSync dataSync;

    void Start()
    {
        // Tìm NetworkPlayerDataSync
        if (autoFindDataSync)
        {
            dataSync = GetComponentInParent<NetworkPlayerDataSync>();
            if (dataSync == null)
            {
                dataSync = GetComponent<NetworkPlayerDataSync>();
            }
            if (dataSync == null)
            {
                dataSync = FindObjectOfType<NetworkPlayerDataSync>();
            }
        }

        if (dataSync != null)
        {
            // Subscribe callbacks
            dataSync.networkCharacterName.OnValueChanged += OnNameChanged;
            dataSync.networkLevel.OnValueChanged += OnLevelChanged;
            dataSync.networkHp.OnValueChanged += OnHpChanged;
            dataSync.networkMaxHp.OnValueChanged += OnMaxHpChanged;
            dataSync.networkMp.OnValueChanged += OnMpChanged;
            dataSync.networkMaxMp.OnValueChanged += OnMaxMpChanged;
            dataSync.networkElementType.OnValueChanged += OnElementChanged;

            // Update ngay lập tức
            UpdateUI();
            
            // Debug.Log("[PlayerInfoUI] Subscribed to NetworkPlayerDataSync callbacks");
        }
        else
        {
            // Debug.LogWarning("[PlayerInfoUI] NetworkPlayerDataSync not found! UI will not update.");
        }
    }

    void OnDestroy()
    {
        if (dataSync != null)
        {
            dataSync.networkCharacterName.OnValueChanged -= OnNameChanged;
            dataSync.networkLevel.OnValueChanged -= OnLevelChanged;
            dataSync.networkHp.OnValueChanged -= OnHpChanged;
            dataSync.networkMaxHp.OnValueChanged -= OnMaxHpChanged;
            dataSync.networkMp.OnValueChanged -= OnMpChanged;
            dataSync.networkMaxMp.OnValueChanged -= OnMaxMpChanged;
            dataSync.networkElementType.OnValueChanged -= OnElementChanged;
        }
    }

    // Set NetworkPlayerDataSync manually (nếu không dùng auto-find)
    public void SetDataSync(NetworkPlayerDataSync sync)
    {
        // Unsubscribe old
        if (dataSync != null)
        {
            dataSync.networkCharacterName.OnValueChanged -= OnNameChanged;
            dataSync.networkLevel.OnValueChanged -= OnLevelChanged;
            dataSync.networkHp.OnValueChanged -= OnHpChanged;
            dataSync.networkMaxHp.OnValueChanged -= OnMaxHpChanged;
            dataSync.networkMp.OnValueChanged -= OnMpChanged;
            dataSync.networkMaxMp.OnValueChanged -= OnMaxMpChanged;
            dataSync.networkElementType.OnValueChanged -= OnElementChanged;
        }

        // Subscribe new
        dataSync = sync;
        if (dataSync != null)
        {
            dataSync.networkCharacterName.OnValueChanged += OnNameChanged;
            dataSync.networkLevel.OnValueChanged += OnLevelChanged;
            dataSync.networkHp.OnValueChanged += OnHpChanged;
            dataSync.networkMaxHp.OnValueChanged += OnMaxHpChanged;
            dataSync.networkMp.OnValueChanged += OnMpChanged;
            dataSync.networkMaxMp.OnValueChanged += OnMaxMpChanged;
            dataSync.networkElementType.OnValueChanged += OnElementChanged;
            
            UpdateUI();
        }
    }

    private void OnNameChanged(FixedString64Bytes oldValue, FixedString64Bytes newValue)
    {
        if (playerNameText != null)
        {
            playerNameText.text = newValue.ToString();
        }
    }

    private void OnLevelChanged(int oldValue, int newValue)
    {
        if (levelText != null)
        {
            levelText.text = $"Lv.{newValue}";
        }
    }

    private void OnHpChanged(int oldValue, int newValue)
    {
        UpdateHpText();
    }

    private void OnMaxHpChanged(int oldValue, int newValue)
    {
        UpdateHpText();
    }

    private void OnMpChanged(int oldValue, int newValue)
    {
        UpdateMpText();
    }

    private void OnMaxMpChanged(int oldValue, int newValue)
    {
        UpdateMpText();
    }

    private void OnElementChanged(FixedString32Bytes oldValue, FixedString32Bytes newValue)
    {
        if (elementText != null)
        {
            // Tự động convert English key (từ server) → Tên Tiếng Việt
            elementText.text = ElementHelper.ToVietnamese(newValue.ToString());
        }
    }

    private void UpdateHpText()
    {
        if (hpText != null && dataSync != null)
        {
            hpText.text = $"{dataSync.networkHp.Value}/{dataSync.networkMaxHp.Value} HP";
        }
    }

    private void UpdateMpText()
    {
        if (mpText != null && dataSync != null)
        {
            mpText.text = $"{dataSync.networkMp.Value}/{dataSync.networkMaxMp.Value} MP";
        }
    }

    private void UpdateUI()
    {
        if (dataSync == null) return;

        OnNameChanged(default(FixedString64Bytes), dataSync.networkCharacterName.Value);
        OnLevelChanged(0, dataSync.networkLevel.Value);
        UpdateHpText();
        UpdateMpText();
        OnElementChanged(default(FixedString32Bytes), dataSync.networkElementType.Value);
    }
}
