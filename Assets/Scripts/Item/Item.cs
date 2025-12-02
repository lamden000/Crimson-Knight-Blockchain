using Photon.Pun;
using UnityEngine;

public class Item : MonoBehaviour
{
    public ItemData itemData;
    public SpriteRenderer iconRenderer;
    public ItemData item;
    void Start()
    {
        iconRenderer.sprite = itemData.icon;
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        object[] data = info.photonView.InstantiationData;
        int id = (int)data[0];

        // convert ID → ItemData
        item = ItemDatabase.Instance.GetItemByID(id);

        // Setup visuals
        GetComponent<SpriteRenderer>().sprite = item.icon;
    }

    public void Pickup()
    {
        Debug.Log($"Nhặt được: {itemData.itemName}");
        //InventorySystem.Instance.Add(itemData);
        Destroy(gameObject);
    }
}
