using Photon.Pun;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum MonsterState { Idle, Walk, Attack, GetHit }
public enum MonsterName { Slime = 1000, Snail = 1001, Scorpion = 1103,Hell_Bat=1030,Bone_Hunter=1054,Death=1096, Bunny = 1173, Frog = 1215 }

public class Monster : MonoBehaviourPun, IPunInstantiateMagicCallback
{
    private MonsterMovementController movementController;
    private MonsterAnimationController animationController;
    private HealthBar healthBar;
    public MonsterData monsterData { get; private set; }

    public List<DropEntry> dropTable; 

    public GameObject itemPickupPrefab; 

    public MonsterName monsterName = MonsterName.Slime;

    public MonsterState currentState;
    private bool isDead = false;
    public int currentHealth { private set; get; }

    private string dataPath = $"Monsters/Data";

    public bool IsDead => isDead;

    void Awake()
    {
        animationController = GetComponent<MonsterAnimationController>();
        movementController = GetComponent<MonsterMovementController>();
        healthBar = GetComponentInChildren<HealthBar>();
    }

    void Start()
    {
        var nametag = GetComponentInChildren<NameTag>();
        string rawName = monsterName.ToString();
        string uiName = rawName.Replace("_", " ");
        nametag.SetName(uiName);
    }    

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        object[] data = info.photonView.InstantiationData;
        monsterName = (MonsterName)(int)data[0];
        string path = $"{dataPath}/{(int)monsterName}";
        monsterData = Resources.Load<MonsterData>(path);
        animationController.InitializeMonster();
        if (data == null)
        {
            Debug.LogError($"[Monster] Không tìm được MonsterData tại path: {path}. Bạn gõ sai tên rồi đó bạn ơi.");
            return;
        }
        currentHealth= monsterData.maxHP;
        movementController.moveSpeed = monsterData.moveSpeed;
    }

    public void SetState(MonsterState s, GameObject attacker = null)
    {
        if(attacker != null) 
            Debug.Log($"Monster {monsterName} got hit ");
        if (!PhotonNetwork.IsMasterClient) return;
        if (isDead) return;

        currentState = s;

        int attackerID = -1;
        if (attacker != null)
        {
            PhotonView pv = attacker.GetComponent<PhotonView>();
            if (pv != null)
                attackerID = pv.ViewID;
        }

        photonView.RPC(nameof(RPC_SetState), RpcTarget.AllBuffered, (int)s, attackerID);
    }

    [PunRPC]
    void RPC_SetState(int s, int attackerID)
    {
        currentState = (MonsterState)s;

        if (!PhotonNetwork.IsMasterClient)
            return;

        Transform attackerTf = null;

        if (attackerID != -1)
        {
            PhotonView pv = PhotonView.Find(attackerID);
            if (pv != null)
                attackerTf = pv.transform;
        }

        switch (currentState)
        {
            case MonsterState.GetHit:
                movementController.HandleGetHit(attackerTf);
                break;
        }
    }

    [PunRPC]
    void RPC_SyncHealth(int health)
    {
        currentHealth = health;

        if(healthBar != null)
            healthBar.SetHealth(currentHealth,monsterData.maxHP);
        else 
            Debug.LogWarning("HealthBar component not found on Monster.");

        if (currentHealth <= 0)
        {
            currentState = MonsterState.Idle;
          //  animationController.HandleDeath();
        }
    }


    public void RequestDamage(int dmg, GameObject attacker)
    {
        int attackerID = attacker.GetComponent<PhotonView>().ViewID;
        photonView.RPC(nameof(RPC_ApplyDamageMaster), RpcTarget.MasterClient, dmg, attackerID);
    }

    [PunRPC]
    void RPC_ApplyDamageMaster(int dmg, int attackerID)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        PhotonView pv = PhotonView.Find(attackerID);
        GameObject attackerObj = pv ? pv.gameObject : null;

        TakeDamage(dmg, attackerObj); // CHỈ MASTER xử lý AI
    }

    private void TakeDamage(int dmg, GameObject attacker)
    {
        if (isDead) return;

        // Master trừ máu
        currentHealth -= dmg;
        currentHealth = Mathf.Clamp(currentHealth, 0,monsterData.maxHP);

        Debug.Log($"[Monster] {monsterName} took {dmg}, HP = {currentHealth}/{monsterData.maxHP}");

        // Sync máu với tất cả client (buffered để người join sau vẫn thấy máu đúng)
        photonView.RPC(nameof(RPC_SyncHealth), RpcTarget.AllBuffered, currentHealth);

        // Trigger animation/state
        SetState(MonsterState.GetHit, attacker);

        if (currentHealth <= 0)
            Die(attacker);
    }
    private void Die(GameObject attacker)
    {
        if (isDead) return;

        if (PhotonNetwork.IsMasterClient)
        {
            ItemData drop = GetRandomDrop();

            if (drop != null)
            {
                int id = drop.itemID; 
                PhotonNetwork.Instantiate("Prefabs/Item", transform.position, Quaternion.identity, 0, new object[] { id });
            }
        }

        isDead = true;
        currentState = MonsterState.Idle;

      //  animationController.HandleDeath(); // hoặc hàm bạn có

        // tắt AI + movement
        movementController.enabled = false;
        PhotonNetwork.Destroy(gameObject);
    }
    private ItemData GetRandomDrop()
    {
        float roll = Random.value;
        float cumulative = 0f;

        foreach (var entry in monsterData.dropTable)
        {
            cumulative += entry.dropRate;
            if (roll <= cumulative)
            {
                return entry.item;
            }
        }
        return null;
    }
}
