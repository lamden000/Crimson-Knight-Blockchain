using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static PlayerAnimationController;

public class PlayerAnimationController : MonoBehaviourPunCallbacks, IPunObservable
{
    [System.Serializable]
    public struct PartRendererPair
    {
        public CharacterPart part;
        public SpriteRenderer renderer;
    }

    [System.Serializable]
    public struct PartVariantPair
    {
        public CharacterPart part;
        public int variant;
    }

    [SerializeField] private float frameRate = 0.2f;

    [SerializeField]
    private List<PartRendererPair> spriteRenderersInspector;
    private Dictionary<CharacterPart, SpriteRenderer> spriteRenderers;

    [SerializeField]
    private List<PartVariantPair> variantsInspector;

    [SerializeField]
    private EyeState currentEyeState;
    [SerializeField]
    private SpriteRenderer weaponSpriteRenderer;
    [SerializeField]
    int weaponVariant = 0;

    private CharacterPart weaponType;
    public AttackAnimationController attackAnimation;

    private Dictionary<CharacterPart, int> partVariants;

    private float timer;
    private int currentFrame;
    private float blinkTimer = 0f;
    private float blinkDuration = 0.2f;
    private float blinkInterval = 2f;

    private Direction currentDir;
    private State currentState;

    private CharacterSpriteDatabase database;
    Character character;

    public Direction GetCurrentDirection()
    { return currentDir; }

    private void Awake()
    {
        spriteRenderers = new Dictionary<CharacterPart, SpriteRenderer>();
        partVariants = new Dictionary<CharacterPart, int>();

        // INIT toàn bộ enum để khỏi thiếu key
        foreach (CharacterPart part in Enum.GetValues(typeof(CharacterPart)))
        {
            if (!spriteRenderers.ContainsKey(part))
                spriteRenderers[part] = null;

            if (!partVariants.ContainsKey(part))
                partVariants[part] = 0;
        }

        // Ghi đè bằng inspector nếu có
        foreach (var pair in spriteRenderersInspector)
            spriteRenderers[pair.part] = pair.renderer;

        foreach (var pair in variantsInspector)
            partVariants[pair.part] = pair.variant;
    }



    private void Start()
    {
        database = CharacterSpriteDatabase.Instance;
        character = gameObject.GetComponent<Character>();
        currentDir=Direction.Down;
        currentState = State.Idle;

        weaponType = character.GetWeaponType();
        attackAnimation.SetWeaponType(weaponType);
        partVariants[weaponType]=weaponVariant;
        spriteRenderers[weaponType]=weaponSpriteRenderer;


        LoadSprites();
        ApplyAppearanceFromProperties();
        
        // Apply equipped items từ PlayFab (nếu có)
        StartCoroutine(ApplyEquippedItemsFromPlayFab());
    }

    private void LateUpdate()
    {
        PlayAnimation(currentDir, currentState,currentEyeState);
        Blink();
    }

    private void LoadSprites()
    {
        foreach (var kvp in partVariants.ToList()) 
        {
            LoadPart(kvp.Key, kvp.Value);
        }
    }
    public void SetAnimation(Direction dir, State state)
    {
        if (!photonView.IsMine) return;

        photonView.RPC("RPC_SetAnim", RpcTarget.All, (int)dir, (int)state);
    }

    [PunRPC]
    void RPC_SetAnim(int dir, int state)
    {

        if (dir != (int)currentDir || state != (int)currentState)
        {

            if (dir == (int)Direction.Up) SetDirectionUp(true);

            else SetDirectionUp(false);

            Vector3 scale = transform.localScale;
            bool isFlipped = (dir == (int)Direction.Right);
            scale.x = Mathf.Abs(scale.x) * (isFlipped ? -1 : 1);
            transform.localScale = scale;

            // Flip canvas children (name tag, interact prompt, etc.)
            FlipCanvasChildren(isFlipped);

            currentDir = (Direction)dir;

            if (state == (int)State.Attack)
                SetAttackAnimation(true);
            else if (currentState == State.Attack)
            {
                SetAttackAnimation(false);
            }
            currentState = (State)state;
            currentFrame = 0;
            timer = 0;
        }
    }


    private void PlayAnimation(Direction dir, State state, EyeState eyeState)
    {
        if (database == null) return; 

        timer += Time.deltaTime;
        if (timer >= frameRate)
        {
            timer = 0f;
            currentFrame++;
        }

        foreach (var part in spriteRenderers.Keys)
        {
            List<Sprite> frames = null;
            if(dir == Direction.Right)
                dir = Direction.Left;

            if (part != CharacterPart.Eyes)
            {
                frames = database.GetSprites(dir, state, partVariants[part], part);

                if ((frames == null || frames.Count == 0))
                {
                    frames = database.GetSprites(dir, State.Idle, partVariants[part], part);
                }
            }
            else
            {
                frames = database.GetEyeSprites(partVariants[part],dir, eyeState);
            }

            if (frames == null || frames.Count == 0) continue;

            int frameIndex = currentFrame % frames.Count;
            spriteRenderers[part].sprite = frames[frameIndex];
        }
    }

    private void Blink()
    {
        if(currentEyeState!=EyeState.Idle)
            return;
        blinkTimer += Time.deltaTime;
        if (blinkTimer >= blinkInterval)
        {
            blinkTimer = 0f;
        }

        float t = blinkTimer / blinkDuration;
        if (t <= 1f)
        {
            float scaleY = 1f - Mathf.Abs(Mathf.Lerp(-1f, 1f, t));
            var eyesGO = spriteRenderers[CharacterPart.Eyes].gameObject;
            Vector3 s = eyesGO.transform.localScale;
            s.y = Mathf.Max(0f, scaleY); // tránh âm
            eyesGO.transform.localScale = s;
        }
        else
        {
            var eyesGO = spriteRenderers[CharacterPart.Eyes].gameObject;
            Vector3 s = eyesGO.transform.localScale;
            s.y = 1f;
            eyesGO.transform.localScale = s;
        }
    }

    public void SetDirectionUp(bool isUp)
    {
        if (isUp)
        {
            spriteRenderers[CharacterPart.Eyes].gameObject.SetActive(false);
            int hairOrder = spriteRenderers[CharacterPart.Hair].sortingOrder;
            spriteRenderers[weaponType].sortingOrder = hairOrder+2;
            spriteRenderers[CharacterPart.Wings].sortingOrder = hairOrder + 1;
        }
        else
        {
            spriteRenderers[CharacterPart.Eyes].gameObject.SetActive(true);
            int legOrder = spriteRenderers[CharacterPart.Legs].sortingOrder;
            spriteRenderers[weaponType].sortingOrder = legOrder - 1;
            spriteRenderers[CharacterPart.Wings].sortingOrder = legOrder - 2;
        }

    }

    private void SetAttackAnimation(bool isAttacking)
    {
        if (isAttacking)
        {
            spriteRenderers[weaponType].gameObject.SetActive(false);
            currentEyeState = EyeState.Attack;

            attackAnimation.gameObject.SetActive(true);
            attackAnimation.PlayAttackAnimation(currentDir);

            StartCoroutine(ResetAttackAnimation(0.5f));
        }
        else
        {
            attackAnimation.gameObject.SetActive(false);
            spriteRenderers[weaponType].gameObject.SetActive(true);
            currentEyeState = EyeState.Idle;
        }
    }


    public void SetGetHitAnimation(bool getHit)
    {
        if (getHit)
        {
            currentEyeState = EyeState.GetHit;
        }
        else
        {
            currentEyeState = EyeState.Idle;
        }

    }

    private IEnumerator ResetAttackAnimation(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetAttackAnimation(false);
    }

    public void SetPart(CharacterPart part, int variant)
    {
        // Local change
        LoadPart(part, variant);

        // Sync to others
        ExitGames.Client.Photon.Hashtable hash = new ExitGames.Client.Photon.Hashtable();
        hash[part.ToString()] = variant;

        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
    }



    private void LoadPart(CharacterPart part, int variant)
    {
        partVariants[part] = variant;
        switch (part)
        {
            case CharacterPart.Body:
                spriteRenderers[CharacterPart.Body].sprite = null;
                database.LoadBody(variant);
                break;
            case CharacterPart.Legs:
                spriteRenderers[CharacterPart.Legs].sprite = null;
                database.LoadLegs(variant);
                break;
            case CharacterPart.Head:
                spriteRenderers[CharacterPart.Head].sprite = null;
                database.LoadHead(variant);
                break;
            case CharacterPart.Hair:
                spriteRenderers[CharacterPart.Hair].sprite = null;
                database.LoadHair(variant);
                break;
            case CharacterPart.Hat:
                spriteRenderers[CharacterPart.Hat].sprite = null;
                database.LoadHat(variant);
                break;
            case CharacterPart.Sword:
                spriteRenderers[weaponType].sprite = null;
                database.LoadWeapon(variant,character.GetWeaponType());
                break;
            case CharacterPart.Gun:
                spriteRenderers[weaponType].sprite = null;
                database.LoadWeapon(variant, character.GetWeaponType());
                break;
            case CharacterPart.Knive:
                spriteRenderers[weaponType].sprite = null;
                database.LoadWeapon(variant, character.GetWeaponType());
                break;
            case CharacterPart.Staff:
                spriteRenderers[weaponType].sprite = null;
                database.LoadWeapon(variant, character.GetWeaponType());
                break;
            case CharacterPart.Wings:
                spriteRenderers[CharacterPart.Wings].sprite = null;
                database.LoadWings(variant);
                break;
            case CharacterPart.Eyes:
                spriteRenderers[CharacterPart.Eyes].sprite = null;
                database.LoadEyes(variant);
                break;
        }
    }
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // SEND animation state
            stream.SendNext((int)currentDir);
            stream.SendNext((int)currentState);
            stream.SendNext((int)currentEyeState);

            // SEND equipment variants
            // Gửi count rồi gửi từng part
            stream.SendNext(partVariants.Count);
            foreach (var kvp in partVariants)
            {
                stream.SendNext((int)kvp.Key);
                stream.SendNext(kvp.Value);
            }
        }
        else
        {
            // RECEIVE animation state
            object raw = stream.ReceiveNext();
            if (TryGetIntFromObject(raw, out int val))
            {
                currentDir = (Direction)val;
            }
            currentState = (State)(int)stream.ReceiveNext();
            currentEyeState = (EyeState)(int)stream.ReceiveNext();

            // RECEIVE equipment variants
            int count = (int)stream.ReceiveNext();

            for (int i = 0; i < count; i++)
            {
                CharacterPart part = (CharacterPart)(int)stream.ReceiveNext();
                int variant = (int)stream.ReceiveNext();

                if (partVariants[part] != variant)
                {
                    partVariants[part] = variant;
                    LoadPart(part, variant);
                }
            }
        }
    }

    private void ApplyAppearanceFromProperties()
    {
        var props = photonView.Owner.CustomProperties;

        foreach (DictionaryEntry entry in props)
        {
            if (Enum.TryParse<CharacterPart>(entry.Key.ToString(), out CharacterPart part))
            {
                int variant = (int)entry.Value;
                partVariants[part] = variant;
                LoadPart(part, variant);
            }
        }
    }


    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (targetPlayer != photonView.Owner)
            return;

        foreach (DictionaryEntry entry in changedProps)
        {
            string keyStr = entry.Key.ToString();

            if (!Enum.TryParse<CharacterPart>(keyStr, out CharacterPart part))
                continue;

            object rawVal = entry.Value;
            int variant;
            if (!TryGetIntFromObject(rawVal, out variant))
            {
                continue;
            }

            // nếu chưa có key thì thêm, hoặc nếu khác thì cập nhật
            if (!partVariants.ContainsKey(part) || partVariants[part] != variant)
            {
                partVariants[part] = variant;
                LoadPart(part, variant);
            }
        }
    }

    /// <summary>
    /// Apply equipped items từ PlayFab (sau khi inventory đã load)
    /// </summary>
    private System.Collections.IEnumerator ApplyEquippedItemsFromPlayFab()
    {
        // Đợi InventoryManager load xong
        while (InventoryManager.Instance == null)
        {
            yield return null;
        }

        // Đợi inventory được load từ PlayFab
        yield return new WaitForSeconds(0.5f); // Tạm thời đợi 0.5s, có thể cải thiện bằng event

        // Lấy tất cả equipped items
        var equippedItems = InventoryManager.Instance.GetAllEquippedItems();
        
        if (equippedItems == null || equippedItems.Count == 0)
        {
            Debug.Log("[PlayerAnimationController] No equipped items to apply");
            yield break;
        }

        Debug.Log($"[PlayerAnimationController] Applying {equippedItems.Count} equipped items from PlayFab");

        // Apply từng equipped item
        foreach (var kvp in equippedItems)
        {
            EquipmentSlot slot = kvp.Key;
            int variantId = kvp.Value;

            // Map EquipmentSlot sang CharacterPart
            CharacterPart characterPart = MapEquipmentSlotToCharacterPart(slot);
            
            if (characterPart != CharacterPart.Eyes) // Eyes là invalid marker
            {
                Debug.Log($"[PlayerAnimationController] Applying equipment: Slot={slot}, Variant={variantId}, Part={characterPart}");
                SetPart(characterPart, variantId);
            }
            else
            {
                Debug.LogWarning($"[PlayerAnimationController] Cannot apply equipment slot {slot}: not supported for visual display");
            }
        }
    }

    /// <summary>
    /// Map EquipmentSlot sang CharacterPart (tương tự như trong EquipmentItem)
    /// </summary>
    private CharacterPart MapEquipmentSlotToCharacterPart(EquipmentSlot slot)
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
                return CharacterPart.Sword; // Fallback
            case EquipmentSlot.Hair:
                return CharacterPart.Hair;
            case EquipmentSlot.Feet:
                // Feet không hiển thị trên nhân vật
                return CharacterPart.Eyes; // Invalid marker
            default:
                return CharacterPart.Eyes; // Invalid marker
        }
    }

    // helper an toàn để chuyển object -> int
    private bool TryGetIntFromObject(object o, out int result)
    {
        result = 0;
        if (o == null) return false;

        switch (o)
        {
            case int i:
                result = i;
                return true;
            case long l:
                result = (int)l;
                return true;
            case short s:
                result = s;
                return true;
            case byte b:
                result = b;
                return true;
            case float f:
                result = Mathf.RoundToInt(f);
                return true;
            case double d:
                result = (int)d;
                return true;
            case string str when int.TryParse(str, out var parsed):
                result = parsed;
                return true;
            default:
                try
                {
                    // fallback: try Convert (thường xử được boxed numeric types)
                    result = System.Convert.ToInt32(o);
                    return true;
                }
                catch
                {
                    return false;
                }
        }
    }

    /// <summary>
    /// Flip tất cả Canvas children (name tag, interact prompt, etc.) khi character flip
    /// </summary>
    private void FlipCanvasChildren(bool isFlipped)
    {
        Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
        foreach (Canvas canvas in canvases)
        {
            if (canvas != null)
            {
                Vector3 canvasScale = canvas.transform.localScale;
                canvasScale.x = isFlipped ? -Mathf.Abs(canvasScale.x) : Mathf.Abs(canvasScale.x);
                canvas.transform.localScale = canvasScale;
            }
        }
    }

}
