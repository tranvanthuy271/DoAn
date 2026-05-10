using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class LoadingSpinnerAnimator : MonoBehaviour
{
    [SerializeField] private string resourcesFolder = "Loading";
    [SerializeField] private float frameRate = 12f;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private float fallbackRotationSpeed = -180f;

    private Image _image;
    private RectTransform _rectTransform;
    private Sprite[] _frames;
    private float _frameTimer;
    private int _frameIndex;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _rectTransform = GetComponent<RectTransform>();
        LoadFramesIfNeeded();
    }

    private void OnEnable()
    {
        _frameTimer = 0f;
        _frameIndex = 0;
        LoadFramesIfNeeded();
        ApplyCurrentFrame();
    }

    private void Update()
    {
        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (deltaTime <= 0f)
        {
            return;
        }

        if (_frames != null && _frames.Length > 1)
        {
            float frameDuration = 1f / Mathf.Max(1f, frameRate);
            _frameTimer += deltaTime;

            while (_frameTimer >= frameDuration)
            {
                _frameTimer -= frameDuration;
                _frameIndex = (_frameIndex + 1) % _frames.Length;
                ApplyCurrentFrame();
            }

            return;
        }

        if (_rectTransform != null && Math.Abs(fallbackRotationSpeed) > 0.01f)
        {
            _rectTransform.Rotate(0f, 0f, fallbackRotationSpeed * deltaTime);
        }
    }

    public void ReloadFrames()
    {
        _frames = null;
        LoadFramesIfNeeded();
        _frameIndex = 0;
        ApplyCurrentFrame();
    }

    private void LoadFramesIfNeeded()
    {
        if (_frames != null && _frames.Length > 0)
        {
            return;
        }

        _frames = Resources.LoadAll<Sprite>(resourcesFolder);
        if (_frames == null || _frames.Length == 0)
        {
            return;
        }

        Array.Sort(_frames, CompareSpritesByName);
    }

    private void ApplyCurrentFrame()
    {
        if (_image == null || _frames == null || _frames.Length == 0)
        {
            return;
        }

        _image.sprite = _frames[Mathf.Clamp(_frameIndex, 0, _frames.Length - 1)];
        _image.SetAllDirty();
    }

    private static int CompareSpritesByName(Sprite a, Sprite b)
    {
        string aName = a != null ? a.name : string.Empty;
        string bName = b != null ? b.name : string.Empty;

        bool aIsNumber = int.TryParse(aName, out int aValue);
        bool bIsNumber = int.TryParse(bName, out int bValue);

        if (aIsNumber && bIsNumber)
        {
            return aValue.CompareTo(bValue);
        }

        return string.CompareOrdinal(aName, bName);
    }
}
