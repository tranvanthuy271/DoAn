using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class UIRuntimeAssetHelper
{
    private const string NotoSansResourcePath = "Fonts & Materials/NotoSans-Regular SDF";

    private static TMP_FontAsset cachedNotoSans;

    public static TMP_FontAsset GetNotoSans()
    {
        if (cachedNotoSans != null)
        {
            return cachedNotoSans;
        }

        cachedNotoSans = Resources.Load<TMP_FontAsset>(NotoSansResourcePath);
        if (cachedNotoSans == null)
        {
            Debug.LogWarning($"[UIRuntimeAssetHelper] Khong tim thay TMP font asset tai Resources/{NotoSansResourcePath}");
        }

        return cachedNotoSans;
    }

    public static void ApplyNotoSans(params TMP_Text[] texts)
    {
        ApplyNotoSans((IEnumerable<TMP_Text>)texts);
    }

    public static void ApplyNotoSans(IEnumerable<TMP_Text> texts)
    {
        if (texts == null)
        {
            return;
        }

        TMP_FontAsset fontAsset = GetNotoSans();
        if (fontAsset == null)
        {
            return;
        }

        Material sharedMaterial = fontAsset.material;
        foreach (TMP_Text text in texts)
        {
            if (text == null)
            {
                continue;
            }

            text.font = fontAsset;
            if (sharedMaterial != null)
            {
                text.fontSharedMaterial = sharedMaterial;
            }

            text.UpdateMeshPadding();
        }
    }

    public static void SetSpriteWithNativeFit(Image image, Sprite sprite, Vector2 maxSize, bool hideWhenMissing = true, bool allowUpscale = false)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = sprite;
        image.preserveAspect = true;

        if (sprite == null)
        {
            if (hideWhenMissing)
            {
                image.enabled = false;
            }

            return;
        }

        image.enabled = true;

        Vector2 targetSize = GetSpriteNativeSize(image, sprite);
        if (targetSize.x <= 0f || targetSize.y <= 0f)
        {
            targetSize = maxSize;
        }
        else if (maxSize.x > 0f && maxSize.y > 0f)
        {
            float widthScale = maxSize.x / targetSize.x;
            float heightScale = maxSize.y / targetSize.y;
            float scale = Mathf.Min(widthScale, heightScale);

            if (!allowUpscale)
            {
                scale = Mathf.Min(scale, 1f);
            }

            targetSize *= Mathf.Max(scale, 0f);
        }

        RectTransform rectTransform = image.rectTransform;
        rectTransform.sizeDelta = targetSize;

        LayoutElement layoutElement = image.GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            layoutElement.preferredWidth = targetSize.x;
            layoutElement.preferredHeight = targetSize.y;
        }
    }

    private static Vector2 GetSpriteNativeSize(Image image, Sprite sprite)
    {
        float referencePixelsPerUnit = 100f;
        Canvas canvas = image.canvas;
        if (canvas != null && canvas.referencePixelsPerUnit > 0f)
        {
            referencePixelsPerUnit = canvas.referencePixelsPerUnit;
        }

        float spritePixelsPerUnit = sprite.pixelsPerUnit > 0f ? sprite.pixelsPerUnit : referencePixelsPerUnit;
        float scaleFactor = referencePixelsPerUnit / spritePixelsPerUnit;
        return sprite.rect.size * scaleFactor;
    }
}