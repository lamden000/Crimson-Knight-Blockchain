using System.Collections.Generic;
using UnityEngine;

public class CharacterSpriteDatabase : MonoBehaviour
{
    public static CharacterSpriteDatabase Instance { get; private set; }

    private bool loadedEyes = false;
    public string folderPath = "Character Sprites";
    private Dictionary<Direction, Dictionary<State, Dictionary<int, Dictionary<CharacterPart, List<Sprite>>>>> database
        = new Dictionary<Direction, Dictionary<State, Dictionary<int, Dictionary<CharacterPart, List<Sprite>>>>>();

    private Dictionary<Direction, Dictionary<EyeState, List<Sprite>>> sharedEyeDatabase
    = new Dictionary<Direction, Dictionary<EyeState, List<Sprite>>>();

    private Dictionary<int, Dictionary<Direction, List<Sprite>>> idleEyeDatabase
    = new Dictionary<int, Dictionary<Direction, List<Sprite>>>();
    
    private HashSet<(CharacterPart, int)> loadedVariants = new HashSet<(CharacterPart, int)>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }


    public List<Sprite> GetSprites(Direction dir, State state,int variant, CharacterPart part)
    {
        if (database.TryGetValue(dir, out var stateDict))
        {
            if (stateDict.TryGetValue(state, out var variantDict))
            {
                if (variantDict.TryGetValue(variant, out var partDict))
                {
                    if (partDict.TryGetValue(part, out var spriteList))
                    {
                        return spriteList;
                    }
                }
            }
        }
        return null;
    }

    public List<Sprite> GetEyeSprites(int eyeVariant, Direction dir, EyeState state)
    {
        if (state == EyeState.Idle)
        {
            if (idleEyeDatabase.TryGetValue(eyeVariant, out var dirDict))
            {
                if (dirDict.TryGetValue(dir, out var spriteList))
                    return spriteList;
            }
        }
        else
        {
            if (sharedEyeDatabase.TryGetValue(dir, out var stateDict))
            {
                if (stateDict.TryGetValue(state, out var spriteList))
                    return spriteList;
            }
        }

        return null;
    }

    private void AddSprite(CharacterPart part, int variant, int stateCode, Direction dir, State state)
    {
        EnsureDatabase(dir, state,variant, part);

        string sheetName = $"{(int)part}_{variant}";
        string spriteName = $"{(int)part}_{variant}_{stateCode}";

        Sprite[] all = Resources.LoadAll<Sprite>(folderPath + "/" + sheetName);
        Sprite sprite = System.Array.Find(all, s => s.name == spriteName);

        if (sprite != null)
            database[dir][state][variant][part].Add(sprite);
        else
            Debug.LogWarning($"Sprite not found: {spriteName}");
    }

    private void AddIdleEyeSprite(int variant, int stateCode, Direction dir)
    {
        string sheetName = $"{(int)CharacterPart.Eyes}_{variant}";
        string spriteName = $"{(int)CharacterPart.Eyes}_{variant}_{stateCode}";

        Sprite[] all = Resources.LoadAll<Sprite>(folderPath + "/" + sheetName);
        Sprite sprite = System.Array.Find(all, s => s.name == spriteName);

        if (!idleEyeDatabase[variant].ContainsKey(dir))
            idleEyeDatabase[variant][dir] = new List<Sprite>();

        if (sprite != null)
            idleEyeDatabase[variant][dir].Add(sprite);
        else
            Debug.LogWarning($"Idle Eye sprite not found: {spriteName}");
    }

    private void AddSharedEyeSprite(int variant, int stateCode, Direction dir, EyeState state)
    {
        EnsureEyeDatabase(dir,state); 

        string sheetName = $"{(int)CharacterPart.Eyes}_{variant}";
        string spriteName = $"{(int)CharacterPart.Eyes}_{variant}_{stateCode}";

        Sprite[] all = Resources.LoadAll<Sprite>(folderPath + "/" + sheetName);
        Sprite sprite = System.Array.Find(all, s => s.name == spriteName);

        if (sprite != null)
            sharedEyeDatabase[dir][state].Add(sprite);
        else
            Debug.LogWarning($"Shared Eye sprite not found: {spriteName}");
    }

    private void EnsureDatabase(Direction dir, State state, int variant, CharacterPart part)
    {
        if (!database.ContainsKey(dir))
            database[dir] = new Dictionary<State, Dictionary<int, Dictionary<CharacterPart, List<Sprite>>>>();

        if (!database[dir].ContainsKey(state))
            database[dir][state] = new Dictionary<int, Dictionary<CharacterPart, List<Sprite>>>();

        if (!database[dir][state].ContainsKey(variant))
            database[dir][state][variant] = new Dictionary<CharacterPart, List<Sprite>>();

        if (!database[dir][state][variant].ContainsKey(part))
            database[dir][state][variant][part] = new List<Sprite>();
    }

    private void EnsureEyeDatabase(Direction dir, EyeState state)
    {
        if (!sharedEyeDatabase.ContainsKey(dir))
            sharedEyeDatabase[dir] = new Dictionary<EyeState, List<Sprite>>();

        if (!sharedEyeDatabase[dir].ContainsKey(state))
            sharedEyeDatabase[dir][state] = new List<Sprite>();

    }

    public void LoadBody(int outfitVariant)
    {
        if (outfitVariant == -1 || loadedVariants.Contains((CharacterPart.Body, outfitVariant)))
            return;
        AddSprite(CharacterPart.Body, outfitVariant, (int)BodyState.IdleDown, Direction.Down, State.Idle);
        AddSprite(CharacterPart.Body, outfitVariant, (int)BodyState.IdleUp, Direction.Up, State.Idle);
        AddSprite(CharacterPart.Body, outfitVariant, (int)BodyState.IdleLeft, Direction.Left, State.Idle);

        AddSprite(CharacterPart.Body, outfitVariant, (int)BodyState.WalkDown_1, Direction.Down, State.Walk);
        AddSprite(CharacterPart.Body, outfitVariant, (int)BodyState.WalkDown_2, Direction.Down, State.Walk);
        AddSprite(CharacterPart.Body, outfitVariant, (int)BodyState.WalkUp_1, Direction.Up, State.Walk);
        AddSprite(CharacterPart.Body, outfitVariant, (int)BodyState.WalkUp_2, Direction.Up, State.Walk);
        AddSprite(CharacterPart.Body, outfitVariant, (int)BodyState.WalkLeft_1, Direction.Left, State.Walk);
        AddSprite(CharacterPart.Body, outfitVariant, (int)BodyState.WalkLeft_2, Direction.Left, State.Walk);


        AddSprite(CharacterPart.Body, outfitVariant, (int)BodyState.AttackDown_1, Direction.Down, State.Attack);
        AddSprite(CharacterPart.Body, outfitVariant, (int)BodyState.AttackUp_1, Direction.Up, State.Attack);
        AddSprite(CharacterPart.Body, outfitVariant, (int)BodyState.AttackLeft_1, Direction.Left, State.Attack);

        AddSprite(CharacterPart.Body, outfitVariant, (int)BodyState.AttackDown_2, Direction.Down, State.Attack);
        AddSprite(CharacterPart.Body, outfitVariant, (int)BodyState.AttackUp_2, Direction.Up, State.Attack);
        AddSprite(CharacterPart.Body, outfitVariant, (int)BodyState.AttackLeft_2, Direction.Left, State.Attack);

        loadedVariants.Add((CharacterPart.Body, outfitVariant));
    }



    public void LoadLegs(int outfitVariant)
    {
        if (outfitVariant == -1 || loadedVariants.Contains((CharacterPart.Legs, outfitVariant)))
            return;
        AddSprite(CharacterPart.Legs, outfitVariant, (int)BodyState.IdleDown, Direction.Down, State.Idle);
        AddSprite(CharacterPart.Legs, outfitVariant, (int)BodyState.IdleUp, Direction.Up, State.Idle);
        AddSprite(CharacterPart.Legs, outfitVariant, (int)BodyState.IdleLeft, Direction.Left, State.Idle);

        AddSprite(CharacterPart.Legs, outfitVariant, (int)BodyState.WalkDown_1, Direction.Down, State.Walk);
        AddSprite(CharacterPart.Legs, outfitVariant, (int)BodyState.WalkDown_2, Direction.Down, State.Walk);
        AddSprite(CharacterPart.Legs, outfitVariant, (int)BodyState.WalkUp_1, Direction.Up, State.Walk);
        AddSprite(CharacterPart.Legs, outfitVariant, (int)BodyState.WalkUp_2, Direction.Up, State.Walk);
        AddSprite(CharacterPart.Legs, outfitVariant, (int)BodyState.WalkLeft_1, Direction.Left, State.Walk);
        AddSprite(CharacterPart.Legs, outfitVariant, (int)BodyState.WalkLeft_2, Direction.Left, State.Walk);

        AddSprite(CharacterPart.Legs, outfitVariant, (int)BodyState.AttackDown_1, Direction.Down, State.Attack);
        AddSprite(CharacterPart.Legs, outfitVariant, (int)BodyState.AttackUp_1, Direction.Up, State.Attack);
        AddSprite(CharacterPart.Legs, outfitVariant, (int)BodyState.AttackLeft_1, Direction.Left, State.Attack);

        AddSprite(CharacterPart.Legs, outfitVariant, (int)BodyState.AttackDown_2, Direction.Down, State.Attack);
        AddSprite(CharacterPart.Legs, outfitVariant, (int)BodyState.AttackUp_2, Direction.Up, State.Attack);
        AddSprite(CharacterPart.Legs, outfitVariant, (int)BodyState.AttackLeft_2, Direction.Left, State.Attack);

        loadedVariants.Add((CharacterPart.Legs, outfitVariant));
    }
    public void LoadHead(int headVariant)
    {
        if (headVariant == -1 || loadedVariants.Contains((CharacterPart.Head, headVariant)))
            return;
        AddSprite(CharacterPart.Head, headVariant, (int)HeadState.Down, Direction.Down, State.Idle);
        AddSprite(CharacterPart.Head, headVariant, (int)HeadState.Up, Direction.Up, State.Idle);
        AddSprite(CharacterPart.Head, headVariant, (int)HeadState.Left, Direction.Left, State.Idle);

        loadedVariants.Add((CharacterPart.Head, headVariant));
    }

    public void LoadHat(int hatVariant)
    {
        if (hatVariant == -1 || loadedVariants.Contains((CharacterPart.Hat, hatVariant)))
            return;
        AddSprite(CharacterPart.Hat, hatVariant, (int)HeadState.Down, Direction.Down, State.Idle);
        AddSprite(CharacterPart.Hat, hatVariant, (int)HeadState.Up, Direction.Up, State.Idle);
        AddSprite(CharacterPart.Hat, hatVariant, (int)HeadState.Left, Direction.Left, State.Idle);

        loadedVariants.Add((CharacterPart.Hat, hatVariant));
    }

    public void LoadHair (int hairVariant)
    {
        if (hairVariant == -1 || loadedVariants.Contains((CharacterPart.Hair, hairVariant)))
            return;
        AddSprite(CharacterPart.Hair, hairVariant, (int)HeadState.Down, Direction.Down, State.Idle);
        AddSprite(CharacterPart.Hair, hairVariant, (int)HeadState.Up, Direction.Up, State.Idle);
        AddSprite(CharacterPart.Hair, hairVariant, (int)HeadState.Left, Direction.Left, State.Idle);

        loadedVariants.Add((CharacterPart.Hair, hairVariant));
    }

    public void LoadEyes(int eyeVariant)
    {
        if (eyeVariant == -1 || loadedVariants.Contains((CharacterPart.Eyes, eyeVariant)))
            return;

        // Load Idle (theo variant màu mắt)
        if (!idleEyeDatabase.ContainsKey(eyeVariant))
        {
            idleEyeDatabase[eyeVariant] = new Dictionary<Direction, List<Sprite>>();

            AddIdleEyeSprite(eyeVariant, (int)HeadState.Down, Direction.Down);
            AddIdleEyeSprite(eyeVariant, (int)HeadState.Up, Direction.Up);
            AddIdleEyeSprite(eyeVariant, (int)HeadState.Left, Direction.Left);
        }

        // Nếu đã load sprite chung rồi thì không cần load lại
        if (loadedEyes)
            return;

        AddSharedEyeSprite((int)EyeVariant.Attack, (int)HeadState.Down, Direction.Down, EyeState.Attack);
        AddSharedEyeSprite((int)EyeVariant.Attack, (int)HeadState.Up, Direction.Up, EyeState.Attack);
        AddSharedEyeSprite((int)EyeVariant.Attack, (int)HeadState.Left, Direction.Left, EyeState.Attack);

        AddSharedEyeSprite((int)EyeVariant.GetHit, (int)HeadState.Down, Direction.Down, EyeState.GetHit);
        AddSharedEyeSprite((int)EyeVariant.GetHit, (int)HeadState.Up, Direction.Up, EyeState.GetHit);
        AddSharedEyeSprite((int)EyeVariant.GetHit, (int)HeadState.Left, Direction.Left, EyeState.GetHit);

        AddSharedEyeSprite((int)EyeVariant.Beaten, (int)HeadState.Down, Direction.Down, EyeState.Beaten);
        AddSharedEyeSprite((int)EyeVariant.Beaten, (int)HeadState.Up, Direction.Up, EyeState.Beaten);
        AddSharedEyeSprite((int)EyeVariant.Beaten, (int)HeadState.Left, Direction.Left, EyeState.Beaten);

        AddSharedEyeSprite((int)EyeVariant.Laugh, (int)HeadState.Down, Direction.Down, EyeState.Laugh);
        AddSharedEyeSprite((int)EyeVariant.Laugh, (int)HeadState.Up, Direction.Up, EyeState.Laugh);
        AddSharedEyeSprite((int)EyeVariant.Laugh, (int)HeadState.Left, Direction.Left, EyeState.Laugh);

        AddSharedEyeSprite((int)EyeVariant.Smile, (int)HeadState.Down, Direction.Down, EyeState.Smile);
        AddSharedEyeSprite((int)EyeVariant.Smile, (int)HeadState.Up, Direction.Up, EyeState.Smile);
        AddSharedEyeSprite((int)EyeVariant.Smile, (int)HeadState.Left, Direction.Left, EyeState.Smile);

        loadedEyes = true;
    }

    public void LoadWings(int wingVariant)
    {
        if (wingVariant == -1 || loadedVariants.Contains((CharacterPart.Wings, wingVariant)))
            return;
        AddSprite(CharacterPart.Wings, wingVariant, (int)WingState.Down_1, Direction.Down, State.Idle);
        AddSprite(CharacterPart.Wings, wingVariant, (int)WingState.Down_2, Direction.Down, State.Idle);

        AddSprite(CharacterPart.Wings, wingVariant, (int)WingState.Down_1, Direction.Up, State.Idle);
        AddSprite(CharacterPart.Wings, wingVariant, (int)WingState.Down_2, Direction.Up, State.Idle);


        AddSprite(CharacterPart.Wings, wingVariant, (int)WingState.Left_1, Direction.Left, State.Idle);
        AddSprite(CharacterPart.Wings, wingVariant, (int)WingState.Left_2, Direction.Left, State.Idle);

        loadedVariants.Add((CharacterPart.Wings, wingVariant));
    }

    public void LoadWeapon(int weaponVariant, CharacterPart weapon)
    {     
        if (weaponVariant == -1|| loadedVariants.Contains((weapon, weaponVariant)))
            return;
        AddSprite(weapon, weaponVariant, (int)WeaponState.Down, Direction.Down, State.Idle);
        AddSprite(weapon, weaponVariant, (int)WeaponState.Up, Direction.Up, State.Idle);
        AddSprite(weapon, weaponVariant, (int)WeaponState.Left, Direction.Left, State.Idle);

        loadedVariants.Add((weapon, weaponVariant));
    }

}
