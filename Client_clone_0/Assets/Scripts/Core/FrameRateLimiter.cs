using System;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class FrameRateLimiter : MonoBehaviour
{
    private const int TargetFrameRate = 60;
    private const double TargetFrameInterval = 1.0 / TargetFrameRate;

    private static FrameRateLimiter _instance;
    private double _nextFrameTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        ApplyLimit();

        if (_instance != null)
            return;

        var go = new GameObject(nameof(FrameRateLimiter));
        _instance = go.AddComponent<FrameRateLimiter>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        _nextFrameTime = Time.realtimeSinceStartupAsDouble;
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyLimit();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _instance = null;
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        ApplyLimit();
        _nextFrameTime = Time.realtimeSinceStartupAsDouble;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyLimit();
        _nextFrameTime = Time.realtimeSinceStartupAsDouble;
    }

    private void Update()
    {
        ApplyLimit();
    }

    private void LateUpdate()
    {
        double now = Time.realtimeSinceStartupAsDouble;
        if (_nextFrameTime <= now)
        {
            _nextFrameTime = now + TargetFrameInterval;
            return;
        }

        double remaining = _nextFrameTime - now;
        if (remaining > 0.002d)
            Thread.Sleep(Mathf.Max(0, (int)((remaining - 0.001d) * 1000d)));

        while (Time.realtimeSinceStartupAsDouble < _nextFrameTime)
        {
            Thread.Yield();
        }

        _nextFrameTime = Math.Max(_nextFrameTime + TargetFrameInterval, Time.realtimeSinceStartupAsDouble);
    }

    private static void ApplyLimit()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = TargetFrameRate;
    }
}
