using Photon.Pun;
using UnityEngine;

public class Item : MonoBehaviourPun, IPunInstantiateMagicCallback
{
    public ItemData item;
    [Header("Pickup Settings")]
    [SerializeField] private float pickupRange = 0.5f; // Khoảng cách tự động nhặt
    private bool isPickedUp = false;

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        if (info.photonView.InstantiationData == null || info.photonView.InstantiationData.Length == 0)
        {
            Debug.LogError("[Item] InstantiationData is null or empty!");
            return;
        }

        object[] data = info.photonView.InstantiationData;
        int id = (int)data[0];

        // convert ID → ItemData
        if (ItemDatabase.Instance == null)
        {
            Debug.LogError("[Item] ItemDatabase.Instance is null!");
            return;
        }

        item = ItemDatabase.Instance.GetItemByID(id);
        Debug.Log($"[Item] Đã khởi tạo Item với ID: {id}");

        if (item == null)
        {
            Debug.LogError($"[Item] Không tìm thấy ItemData với ID: {id}");
            return;
        }

        // Setup visuals
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && item.icon != null)
        {
            sr.sprite = item.icon;
        }
        else
        {
            Debug.LogWarning($"[Item] SpriteRenderer hoặc item.icon is null cho item ID: {id}");
        }
    }

    public void Pickup()
    {
        // Tránh nhặt nhiều lần
        if (isPickedUp) return;

        if (item == null)
        {
            Debug.LogError("[Item] Cannot pickup: item is null!");
            return;
        }

        isPickedUp = true;
        Debug.Log($"Nhặt được: {item.itemName} (ID: {item.itemID})");

        // Thêm vào inventory và lưu lên PlayFab
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(item, 1);
        }
        else
        {
            Debug.LogWarning("[Item] InventoryManager.Instance is null! Item not saved to database.");
        }

        // Destroy item trong game
        if (photonView != null && photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // Tự động nhặt item khi player đến gần (chỉ cho local player)
        if (isPickedUp) return;

        Character player = FindLocalPlayer();
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance <= pickupRange)
            {
                Pickup();
            }
        }
    }

    private Character FindLocalPlayer()
    {
        Character[] characters = FindObjectsByType<Character>(FindObjectsSortMode.None);
        foreach (Character character in characters)
        {
            PhotonView pv = character.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                return character;
            }
        }
        return null;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Cũng có thể nhặt bằng trigger nếu có collider
        if (isPickedUp) return;

        Character player = collision.GetComponent<Character>();
        if (player != null)
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                Pickup();
            }
        }
    }
}
