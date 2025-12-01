using Photon.Pun;
using UnityEngine;

public enum MonsterState { Idle, Walk, Attack, GetHit }
public enum MonsterName { Slime = 1000, Snail = 1001, Scorpion = 1103, Bunny = 1173, Frog = 1215 }

public class Monster : MonoBehaviourPun, IPunInstantiateMagicCallback
{
    private MonsterMovementController movementController;
    private MonsterAnimationController animationController;

    public MonsterName monsterName = MonsterName.Slime;

    public MonsterState currentState;
    private bool isDead = false;

    public float damage { private set; get; } = 10f;

    public bool IsDead => isDead;

    void Awake()
    {
        animationController = GetComponent<MonsterAnimationController>();
        movementController = GetComponent<MonsterMovementController>();
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        object[] data = info.photonView.InstantiationData;
        monsterName = (MonsterName)(int)data[0];

        animationController.InitializeMonster();

        // Late-joiner sẽ phát lại tất cả RPC buffered, bao gồm state hiện tại
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
        SetState(MonsterState.GetHit,attacker);
    }
}
