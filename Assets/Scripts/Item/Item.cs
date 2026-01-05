using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class Item : MonoBehaviourPun, IPunInstantiateMagicCallback
{
    public ItemData item;
    [Header("Pickup Settings")]
    [SerializeField] private float pickupRange = 0.5f; // Khoảng cách tự động nhặt
    private bool isPickedUp = false;
    
    // Track player đang trong range
    private Character playerInRange = null;
    private GameObject collectPromptUI = null;

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
        if (isPickedUp) return;

        Character player = FindLocalPlayer();
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            bool isInRange = distance <= pickupRange;

            // Nếu player vào range và chưa có player nào trong range
            if (isInRange && playerInRange == null)
            {
                playerInRange = player;
                ShowCollectPrompt(player);
            }
            // Nếu player ra khỏi range
            else if (!isInRange && playerInRange == player)
            {
                HideCollectPrompt();
                playerInRange = null;
            }

            // Nếu player trong range và nhấn F để nhặt
            if (isInRange && playerInRange == player)
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
                {
                    Pickup();
                    HideCollectPrompt();
                    playerInRange = null;
                }
            }
        }
        else
        {
            // Nếu không có player, ẩn UI
            if (playerInRange != null)
            {
                HideCollectPrompt();
                playerInRange = null;
            }
        }
    }

    private void ShowCollectPrompt(Character player)
    {
        if (player == null) return;

        // Tìm UI collect prompt trong children của player
        if (collectPromptUI == null)
        {
            // Tìm trong tất cả children của player
            Transform[] children = player.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                // Tìm UI có thể là collect prompt (có thể có tên chứa "Collect", "Pickup", "Prompt", "Interact", v.v.)
                if (child.name.Contains("Interact"))
                {
                    collectPromptUI = child.gameObject;
                    break;
                }
            }

            // Nếu không tìm thấy, thử tìm bằng TextMeshProUGUI component
            if (collectPromptUI == null)
            {
                TextMeshProUGUI[] texts = player.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (TextMeshProUGUI text in texts)
                {
                    if (text.text.Contains("F") || text.text.Contains("collect") || 
                        text.text.Contains("Collect") || text.text.Contains("Press"))
                    {
                        collectPromptUI = text.gameObject;
                        break;
                    }
                }
            }
        }

        // Hiển thị UI
        if (collectPromptUI != null)
        {
            collectPromptUI.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"[Item] Không tìm thấy collect prompt UI trong player: {player.name}. Hãy đảm bảo UI là child của player prefab.");
        }
    }

    private void HideCollectPrompt()
    {
        if (collectPromptUI != null)
        {
            collectPromptUI.SetActive(false);
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
        // Khi player vào trigger, hiển thị prompt (không tự động nhặt nữa)
        if (isPickedUp) return;

        Character player = collision.GetComponent<Character>();
        if (player != null)
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                playerInRange = player;
                ShowCollectPrompt(player);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // Khi player ra khỏi trigger, ẩn prompt
        if (isPickedUp) return;

        Character player = collision.GetComponent<Character>();
        if (player != null)
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine && playerInRange == player)
            {
                HideCollectPrompt();
                playerInRange = null;
            }
        }
    }
}
