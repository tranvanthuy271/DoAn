using UnityEngine;

[DisallowMultipleComponent]
public class BuffOutlineVisual : MonoBehaviour
{
    private SpriteRenderer sourceRenderer;
    private SpriteRenderer outlineRenderer;
    private float scaleMultiplier = 1.12f;
    private int sortingOrderOffset = -1;

    private void Awake()
    {
        outlineRenderer = GetComponent<SpriteRenderer>();
        ResolveSourceRenderer();
    }

    public void Configure(Color tintColor, float outlineScaleMultiplier, int orderOffset)
    {
        if (outlineRenderer == null)
            outlineRenderer = GetComponent<SpriteRenderer>();

        scaleMultiplier = outlineScaleMultiplier;
        sortingOrderOffset = orderOffset;

        if (outlineRenderer != null)
        {
            outlineRenderer.color = tintColor;
            outlineRenderer.maskInteraction = SpriteMaskInteraction.None;
        }

        SyncVisual();
    }

    private void LateUpdate()
    {
        SyncVisual();
    }

    private void ResolveSourceRenderer()
    {
        if (transform.parent == null)
            return;

        sourceRenderer = transform.parent.GetComponent<SpriteRenderer>();
        if (sourceRenderer != null && sourceRenderer.sprite != null)
            return;

        sourceRenderer = null;

        foreach (SpriteRenderer candidate in transform.parent.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (candidate.gameObject == gameObject)
                continue;

            if (candidate.sprite == null)
                continue;

            if (candidate.transform.name == "SkillEffect")
                continue;

            sourceRenderer = candidate;
            break;
        }
    }

    private void SyncVisual()
    {
        if (outlineRenderer == null)
            return;

        if (sourceRenderer == null)
            ResolveSourceRenderer();

        if (sourceRenderer == null)
        {
            outlineRenderer.enabled = false;
            return;
        }

        outlineRenderer.enabled = sourceRenderer.enabled && sourceRenderer.sprite != null;
        outlineRenderer.sprite = sourceRenderer.sprite;
        outlineRenderer.color = new Color(outlineRenderer.color.r, outlineRenderer.color.g, outlineRenderer.color.b, 1f);
        outlineRenderer.flipX = sourceRenderer.flipX;
        outlineRenderer.flipY = sourceRenderer.flipY;
        outlineRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        outlineRenderer.sortingOrder = sourceRenderer.sortingOrder + sortingOrderOffset;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = new Vector3(scaleMultiplier, scaleMultiplier, 1f);
    }
}