using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterEquipmentController : MonoBehaviour
{
    [Header("References")]
    public TMP_InputField outfitInput;
    public TMP_InputField hairInput;
    public TMP_InputField headInput;
    public TMP_InputField weaponInput;
    public TMP_InputField wingInput;
    public TMP_InputField hatInput;
    public TMP_InputField eyesInput;

    private Character character;

    private PlayerAnimationController characterLoader; 

    void Update()
    {
        // Nhấn Enter để apply
        if (Input.GetKeyDown(KeyCode.Return))
        {
            ApplyVariants();
        }
    }

    public void AssignLocalPlayer(PlayerAnimationController anim)
    {
        characterLoader = anim;
        character = anim.GetComponent<Character>();

        Debug.Log("UI now linked to local player.");
    }

    void ApplyVariants()
    {
        // Body
        if (!string.IsNullOrEmpty(outfitInput.text))
        {
            if (int.TryParse(outfitInput.text, out int bodyVariant))
            {
                characterLoader.SetPart(CharacterPart.Body, bodyVariant);
                characterLoader.SetPart(CharacterPart.Legs, bodyVariant);
            }
        }

        // Legs
        if (!string.IsNullOrEmpty(hairInput.text))
        {
            if (int.TryParse(hairInput.text, out int hairVariant))
            {
                characterLoader.SetPart(CharacterPart.Hair, hairVariant);
            }
        }

        if (!string.IsNullOrEmpty(headInput.text))
        {
            if (int.TryParse(headInput.text, out int headVariant))
            {
                characterLoader.SetPart(CharacterPart.Head, headVariant);
            }
        }

        if (!string.IsNullOrEmpty(wingInput.text))
        {
            if (int.TryParse(wingInput.text, out int wingVariant))
            {
                characterLoader.SetPart(CharacterPart.Wings, wingVariant);
            }
        }

        if (!string.IsNullOrEmpty(weaponInput.text))
        {
            if (int.TryParse(weaponInput.text, out int weaponVariant))
            {
                characterLoader.SetPart(character.getWeaponType(), weaponVariant);
            }
        }

        if (!string.IsNullOrEmpty(hatInput.text))
        {
            if (int.TryParse(hatInput.text, out int hatVariant))
            {
                characterLoader.SetPart(CharacterPart.Hat, hatVariant);
            }
        }

        if (!string.IsNullOrEmpty(eyesInput.text))
        {
            if (int.TryParse(eyesInput.text, out int eyesVariant))
            {
                characterLoader.SetPart(CharacterPart.Eyes, eyesVariant);
            }
        }
    }
}
