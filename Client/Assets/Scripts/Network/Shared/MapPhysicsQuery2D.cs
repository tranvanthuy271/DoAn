using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// Physics2D query helpers that always query the same physics scene as the
// provided context object.
public static class MapPhysicsQuery2D
{
    private const int InitialBufferSize = 16;
    private const int MaxBufferSize = 1024;

    public static Collider2D[] OverlapCircleAll(GameObject contextObject, Vector2 center, float radius)
    {
        return OverlapCircleAllInternal(contextObject, center, radius, false, Physics2D.DefaultRaycastLayers);
    }

    public static Collider2D[] OverlapCircleAll(GameObject contextObject, Vector2 center, float radius, int layerMask)
    {
        return OverlapCircleAllInternal(contextObject, center, radius, true, layerMask);
    }

    private static Collider2D[] OverlapCircleAllInternal(
        GameObject contextObject,
        Vector2 center,
        float radius,
        bool useLayerMask,
        int layerMask)
    {
        PhysicsScene2D physicsScene = ResolvePhysicsScene(contextObject);
        int bufferSize = InitialBufferSize;

        while (bufferSize <= MaxBufferSize)
        {
            var buffer = new Collider2D[bufferSize];
            int hitCount = useLayerMask
                ? physicsScene.OverlapCircle(center, radius, buffer, layerMask)
                : physicsScene.OverlapCircle(center, radius, buffer);

            if (hitCount == 0)
                return Array.Empty<Collider2D>();

            if (hitCount < bufferSize)
            {
                var results = new Collider2D[hitCount];
                Array.Copy(buffer, results, hitCount);
                return results;
            }

            bufferSize *= 2;
        }

        { /* Cảnh báo: OverlapCircleAll reached the max buffer size. Results may be truncated */ }

        var maxBuffer = new Collider2D[MaxBufferSize];
        int finalCount = useLayerMask
            ? physicsScene.OverlapCircle(center, radius, maxBuffer, layerMask)
            : physicsScene.OverlapCircle(center, radius, maxBuffer);

        if (finalCount == 0)
            return Array.Empty<Collider2D>();

        finalCount = Mathf.Min(finalCount, MaxBufferSize);
        var truncated = new Collider2D[finalCount];
        Array.Copy(maxBuffer, truncated, finalCount);
        return truncated;
    }

    private static PhysicsScene2D ResolvePhysicsScene(GameObject contextObject)
    {
        if (contextObject != null)
        {
            Scene scene = contextObject.scene;
            if (scene.IsValid())
            {
                PhysicsScene2D scenePhysics = scene.GetPhysicsScene2D();
                if (scenePhysics.IsValid())
                    return scenePhysics;
            }
        }

        return Physics2D.defaultPhysicsScene;
    }
}