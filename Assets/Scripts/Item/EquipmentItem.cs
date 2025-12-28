using UnityEngine;

public enum EquipmentSlot
{
    Head, Body, Legs,Wing, Weapon,Hair, Feet
}

[CreateAssetMenu(menuName = "Game/Items/Equipment")]
public class EquipmentItem : ItemData
{
    public EquipmentSlot slot;
    public int defense;
    public int stylePoints;
    public int variantId;

    public override void Use()
    {
        // Tìm local player's PlayerAnimationController
        PlayerAnimationController animController = GetLocalPlayerAnimationController();
        if (animController == null)
        {
            Debug.LogError("[EquipmentItem] Không tìm thấy PlayerAnimationController của local player!");
            return;
        }

        // Map EquipmentSlot sang CharacterPart
        CharacterPart characterPart = MapEquipmentSlotToCharacterPart(slot, animController.GetComponent<Character>());
        if (characterPart == CharacterPart.Eyes) // Eyes không phải equipment slot hợp lệ
        {
            Debug.LogWarning($"[EquipmentItem] EquipmentSlot '{slot}' không được hỗ trợ!");
            return;
        }

        // Set part variant để đổi sprite
        Debug.Log($"[EquipmentItem] Equipping {itemName} (Slot: {slot}, Variant: {variantId})");
        animController.SetPart(characterPart, variantId);
    }

    /// <summary>
    /// Tìm PlayerAnimationController của local player (player có photonView.IsMine = true)
    /// </summary>
    private PlayerAnimationController GetLocalPlayerAnimationController()
    {
        PlayerAnimationController[] controllers = FindObjectsByType<PlayerAnimationController>(FindObjectsSortMode.None);
        foreach (var controller in controllers)
        {
            var photonView = controller.GetComponent<Photon.Pun.PhotonView>();
            if (photonView != null && photonView.IsMine)
            {
                return controller;
            }
        }
        return null;
    }

    /// <summary>
    /// Map EquipmentSlot sang CharacterPart
    /// </summary>
    private CharacterPart MapEquipmentSlotToCharacterPart(EquipmentSlot slot, Character character)
    {
        switch (slot)
        {
            case EquipmentSlot.Head:
                return CharacterPart.Head;
            case EquipmentSlot.Body:
                return CharacterPart.Body;
            case EquipmentSlot.Legs:
                return CharacterPart.Legs;
            case EquipmentSlot.Wing:
                return CharacterPart.Wings;
            case EquipmentSlot.Weapon:
                // Weapon type phụ thuộc vào class của character
                if (character != null)
                {
                    return character.GetWeaponType();
                }
                Debug.LogWarning("[EquipmentItem] Character is null, không thể xác định weapon type!");
                return CharacterPart.Sword; // Fallback
            case EquipmentSlot.Hair:
                return CharacterPart.Hair;
            case EquipmentSlot.Feet:
                // Feet không hiển thị trên nhân vật, chỉ cung cấp chỉ số thôi
                // Không map vào CharacterPart nào
                Debug.LogWarning("[EquipmentItem] Feet slot không hiển thị trên nhân vật, chỉ cung cấp chỉ số!");
                return CharacterPart.Eyes; // Invalid, sẽ được check ở Use()
            default:
                Debug.LogWarning($"[EquipmentItem] EquipmentSlot '{slot}' không được map!");
                return CharacterPart.Eyes; // Invalid, sẽ được check ở Use()
        }
    }
}


