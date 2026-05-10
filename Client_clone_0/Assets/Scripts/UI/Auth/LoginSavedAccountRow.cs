using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginSavedAccountRow : MonoBehaviour
{
    [SerializeField] private TMP_Text usernameText;
    [SerializeField] private Button selectButton;
    [SerializeField] private Button deleteButton;

    private LoginSavedAccountStore.AccountRecord account;
    private Action<LoginSavedAccountStore.AccountRecord> onSelect;
    private Action<LoginSavedAccountStore.AccountRecord> onDelete;

    public void Bind(
        LoginSavedAccountStore.AccountRecord accountRecord,
        Action<LoginSavedAccountStore.AccountRecord> selectCallback,
        Action<LoginSavedAccountStore.AccountRecord> deleteCallback)
    {
        account = accountRecord;
        onSelect = selectCallback;
        onDelete = deleteCallback;

        ResolveReferences();

        if (usernameText != null)
        {
            usernameText.text = account != null ? account.username : string.Empty;
        }

        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(HandleSelectClicked);
            selectButton.onClick.AddListener(HandleSelectClicked);
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveListener(HandleDeleteClicked);
            deleteButton.onClick.AddListener(HandleDeleteClicked);
        }
    }

    private void OnDestroy()
    {
        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(HandleSelectClicked);
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveListener(HandleDeleteClicked);
        }
    }

    private void ResolveReferences()
    {
        if (usernameText == null)
        {
            Transform username = transform.Find("UsernameText");
            usernameText = username != null ? username.GetComponent<TMP_Text>() : GetComponentInChildren<TMP_Text>(true);
        }

        if (selectButton == null)
        {
            selectButton = GetComponent<Button>();
        }

        if (deleteButton == null)
        {
            Transform delete = transform.Find("DeleteButton");
            deleteButton = delete != null ? delete.GetComponent<Button>() : null;
        }
    }

    private void HandleSelectClicked()
    {
        onSelect?.Invoke(account);
    }

    private void HandleDeleteClicked()
    {
        onDelete?.Invoke(account);
    }
}
