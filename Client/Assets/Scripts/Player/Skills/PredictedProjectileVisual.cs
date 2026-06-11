using Unity.Netcode;
using UnityEngine;

public static class PredictedProjectileVisual
{
    private const float DefaultBridgeLifetime = 0.75f;

    public static void Spawn(
        GameObject prefab,
        Vector3 position,
        Quaternion rotation,
        Vector2 velocity,
        float requestedLifetime,
        Vector3? localScale = null)
    {
        if (prefab == null)
            return;

        GameObject visual = Object.Instantiate(prefab, position, rotation);
        visual.name = prefab.name + "_PredictedVisual";

        if (localScale.HasValue)
            visual.transform.localScale = localScale.Value;

        foreach (var networkObject in visual.GetComponentsInChildren<NetworkObject>(true))
            networkObject.enabled = false;

        foreach (var networkBehaviour in visual.GetComponentsInChildren<NetworkBehaviour>(true))
            networkBehaviour.enabled = false;

        foreach (var collider in visual.GetComponentsInChildren<Collider2D>(true))
            collider.enabled = false;

        Rigidbody2D rb = visual.GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = visual.AddComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.velocity = velocity;

        foreach (var animator in visual.GetComponentsInChildren<Animator>(true))
        {
            if (animator.runtimeAnimatorController == null) continue;
            animator.Rebind();
            animator.Update(0f);
        }

        float lifetime = requestedLifetime > 0f
            ? Mathf.Min(requestedLifetime, DefaultBridgeLifetime)
            : DefaultBridgeLifetime;

        Object.Destroy(visual, lifetime);
    }
}
