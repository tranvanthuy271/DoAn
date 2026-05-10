using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class LoginSavedAccountStore
{
    private const string PrefKey = "LOGIN_SAVED_ACCOUNTS_V1";
    private const int MaxAccounts = 10;
    private const string ObfuscationKey = "DoAn.Login.SavedAccounts.V1";

    [Serializable]
    public class AccountRecord
    {
        public string username;
        public string password;
        public long lastLoginUtcTicks;

        public string GetPassword()
        {
            return Decode(password);
        }
    }

    [Serializable]
    private class AccountRecordList
    {
        public List<AccountRecord> accounts = new List<AccountRecord>();
    }

    public static List<AccountRecord> GetAccounts()
    {
        AccountRecordList list = Load();
        list.accounts.RemoveAll(record => record == null || string.IsNullOrWhiteSpace(record.username));
        list.accounts.Sort((a, b) => b.lastLoginUtcTicks.CompareTo(a.lastLoginUtcTicks));
        return list.accounts;
    }

    public static void Upsert(string username, string password)
    {
        username = (username ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            return;
        }

        AccountRecordList list = Load();
        AccountRecord existing = list.accounts.Find(record =>
            record != null &&
            string.Equals(record.username, username, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
        {
            existing = new AccountRecord();
            list.accounts.Add(existing);
        }

        existing.username = username;
        existing.password = Encode(password);
        existing.lastLoginUtcTicks = DateTime.UtcNow.Ticks;

        Trim(list);
        Save(list);
    }

    public static void Remove(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return;
        }

        AccountRecordList list = Load();
        list.accounts.RemoveAll(record =>
            record == null ||
            string.Equals(record.username, username, StringComparison.OrdinalIgnoreCase));
        Save(list);
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(PrefKey);
        PlayerPrefs.Save();
    }

    private static AccountRecordList Load()
    {
        string json = PlayerPrefs.GetString(PrefKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new AccountRecordList();
        }

        try
        {
            AccountRecordList list = JsonUtility.FromJson<AccountRecordList>(json);
            return list ?? new AccountRecordList();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LoginSavedAccountStore] Cannot read saved accounts: {ex.Message}");
            return new AccountRecordList();
        }
    }

    private static void Save(AccountRecordList list)
    {
        Trim(list);
        PlayerPrefs.SetString(PrefKey, JsonUtility.ToJson(list));
        PlayerPrefs.Save();
    }

    private static void Trim(AccountRecordList list)
    {
        if (list == null)
        {
            return;
        }

        list.accounts.RemoveAll(record => record == null || string.IsNullOrWhiteSpace(record.username));
        list.accounts.Sort((a, b) => b.lastLoginUtcTicks.CompareTo(a.lastLoginUtcTicks));

        while (list.accounts.Count > MaxAccounts)
        {
            list.accounts.RemoveAt(list.accounts.Count - 1);
        }
    }

    private static string Encode(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        byte[] bytes = Encoding.UTF8.GetBytes(value);
        byte[] key = Encoding.UTF8.GetBytes(ObfuscationKey);
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)(bytes[i] ^ key[i % key.Length]);
        }

        return Convert.ToBase64String(bytes);
    }

    private static string Decode(string encoded)
    {
        if (string.IsNullOrEmpty(encoded))
        {
            return string.Empty;
        }

        try
        {
            byte[] bytes = Convert.FromBase64String(encoded);
            byte[] key = Encoding.UTF8.GetBytes(ObfuscationKey);
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(bytes[i] ^ key[i % key.Length]);
            }

            return Encoding.UTF8.GetString(bytes);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LoginSavedAccountStore] Cannot decode saved password: {ex.Message}");
            return string.Empty;
        }
    }
}
