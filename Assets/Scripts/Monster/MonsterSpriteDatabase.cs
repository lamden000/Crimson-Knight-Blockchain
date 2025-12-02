using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MonsterSpriteDatabase : MonoBehaviour
{

    enum EnemySprite { Idle_1 = 0, Walk_1 = 1, Walk_2 = 2, Attack = 3, GetHit = 4, Idle_2 = 5 }

    private Dictionary<MonsterName,Dictionary<MonsterState,List<Sprite>>> database
      = new Dictionary<MonsterName, Dictionary<MonsterState, List<Sprite>>>();

    public string folderPath = "Monsters/Sprites";
    public static MonsterSpriteDatabase Instance { get; private set; }

    private List<MonsterName> loadedEnemy = new List<MonsterName>();
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


    public void LoadSprites(MonsterName enemyName)
    {
        if (loadedEnemy.Contains(enemyName))
            return;

        AddSprite(enemyName, EnemySprite.Walk_1, MonsterState.Walk);
        AddSprite(enemyName, EnemySprite.Walk_2, MonsterState.Walk);

        AddSprite(enemyName, EnemySprite.Attack, MonsterState.Attack);
        AddSprite(enemyName, EnemySprite.GetHit, MonsterState.GetHit);

        AddSprite(enemyName, EnemySprite.Idle_1, MonsterState.Idle);
      //  AddSprite(enemyName, EnemySprite.Idle_2, EnemyState.Idle);

        loadedEnemy.Add(enemyName);
    }

    private void AddSprite(MonsterName enemyName,EnemySprite stateCode, MonsterState state)
    {
        if (!database.ContainsKey(enemyName))
            database[enemyName] = new Dictionary<MonsterState, List<Sprite>>();

        if (!database[enemyName].ContainsKey(state))
            database[enemyName][state] = new List<Sprite>();

        int enemyIndex = (int)enemyName;
        string sheetName = $"{enemyIndex}";
        string spriteName = $"{enemyIndex}_{(int)stateCode}";

        Sprite[] all = Resources.LoadAll<Sprite>(folderPath + "/" + sheetName);
        Sprite sprite = System.Array.Find(all, s => s.name == spriteName);

        if (sprite != null)
            database[enemyName][state].Add(sprite);
        else if((int)stateCode!=5)
            Debug.LogWarning($"Sprite not found: {spriteName}");
    }

    public List<Sprite> GetSprites(MonsterName name,MonsterState state)
    {
        if (database.TryGetValue(name, out var enemyDict))
        {
            if (enemyDict.TryGetValue(state, out var spriteList))
            {
                return spriteList;
            }
        }
        return null;
    }

}
