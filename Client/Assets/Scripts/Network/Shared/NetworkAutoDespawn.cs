using Unity.Netcode;
using UnityEngine;

// Despawns a NetworkObject after a short lifetime, or destroys a normal GameObject.
// Useful for transient boss/enemy effects spawned on the server.
public class NetworkAutoDespawn : MonoBehaviour
{
    [SerializeField] private float lifetime = 1f;

    public void Arm(float newLifetime)
    {
        lifetime = Mathf.Max(0.01f, newLifetime);
        CancelInvoke(nameof(DespawnOrDestroy));
        Invoke(nameof(DespawnOrDestroy), lifetime);
    }

    private void DespawnOrDestroy()
    {
        if (!gameObject)
            return;

        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn(true);
            return;
        }

        Destroy(gameObject);
    }
}
