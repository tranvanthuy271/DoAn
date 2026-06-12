using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    // Network Variables - tự động sync
    public NetworkVariable<int> playerID = new NetworkVariable<int>();
    public NetworkVariable<string> playerName = new NetworkVariable<string>();
    public NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>();
    public NetworkVariable<int> hp = new NetworkVariable<int>(100);
    public NetworkVariable<int> maxHp = new NetworkVariable<int>(100);
    public NetworkVariable<int> level = new NetworkVariable<int>(1);

    private CharacterController characterController;
    private float moveSpeed = 5f;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Server set player ID dựa trên OwnerClientId
            playerID.Value = (int)OwnerClientId;
            
            // TODO: Load player name từ database dựa trên user_id
            // playerName.Value = LoadPlayerNameFromDB(userId);
        }

        if (IsOwner)
        {
            // Chỉ owner mới thấy camera này
            SetupLocalPlayer();
        }
        else
        {
            // Remote players: disable camera và input
            DisableRemotePlayer();
        }
    }

    private void SetupLocalPlayer()
    {
        // Khi co NetworkPlayerController, movement + camera da duoc no xu ly.
        // PlayerNetwork khong can thiet lap CharacterController hay camera.
        if (GetComponent<NetworkPlayerController>() != null)
            return;

        // Setup camera, input, UI cho local player (fallback khi khong co NetworkPlayerController)
        if (Camera.main != null)
        {
            Camera.main.transform.SetParent(transform);
            Camera.main.transform.localPosition = new Vector3(0, 1.6f, 0);
        }
        
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
        }
    }

    private void DisableRemotePlayer()
    {
        // Disable camera và input cho remote players
        if (Camera.main != null && Camera.main.transform.parent == transform)
        {
            Camera.main.transform.SetParent(null);
        }
    }

    void Update()
    {
        if (IsOwner)
        {
            // Khi co NetworkPlayerController, no da xu ly movement trong FixedUpdate.
            // PlayerNetwork khong can HandleMovement() de tranh double-update position.
            if (GetComponent<NetworkPlayerController>() == null)
                HandleMovement();
        }
        else
        {
            // Nếu NetworkPlayerController đang active trên object này,
            // nó đã xử lý interpolation mượt mà hơn → bỏ qua ở đây để tránh 2 script fight nhau.
            if (GetComponent<NetworkPlayerController>() != null && GetComponent<NetworkPlayerController>().enabled)
                return;

            // Remote players: sync position từ network (fallback khi không có NetworkPlayerController)
            if (Vector3.Distance(transform.position, networkPosition.Value) > 0.1f)
            {
                transform.position = Vector3.Lerp(transform.position, networkPosition.Value, Time.deltaTime * 10f);
            }
        }
    }

    private void HandleMovement()
    {
        if (characterController == null) return;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 move = transform.right * horizontal + transform.forward * vertical;
        move *= moveSpeed;

        characterController.Move(move * Time.deltaTime);

        // Gửi position lên server
        if (IsClient)
        {
            UpdatePositionServerRpc(transform.position);
        }
    }

    [ServerRpc]
    private void UpdatePositionServerRpc(Vector3 newPosition)
    {
        if (!IsServer) return;

        // Validate movement (check speed hack)
        float distance = Vector3.Distance(networkPosition.Value, newPosition);
        float timeDelta = Time.deltaTime;
        float speed = distance / timeDelta;

        float maxSpeed = moveSpeed * 1.5f; // Cho phép 50% tolerance
        if (speed > maxSpeed)
        {
            // Debug.LogWarning($"Player {OwnerClientId} moving too fast! Speed: {speed}");
            return;
        }

        networkPosition.Value = newPosition;
    }

    public override void OnNetworkDespawn()
    {
        // Cleanup khi player disconnect
        if (IsOwner && Camera.main != null)
        {
            Camera.main.transform.SetParent(null);
        }
    }
}
