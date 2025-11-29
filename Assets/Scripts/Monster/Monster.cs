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

    // MASTER → gọi cái này
    public void SetState(MonsterState s)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (isDead) return;

        currentState = s;

        // gửi buffered state và frame để người vào sau vẫn nhận được
        photonView.RPC(nameof(RPC_SetState), RpcTarget.AllBuffered, (int)s);
    }

    // nhận từ master
    [PunRPC]
    void RPC_SetState(int s)
    {
        currentState = (MonsterState)s;
        // AnimationController tự đọc currentState trong Update
    }

    public void TakeDamage(int dmg, GameObject attacker)
    {
        if (isDead) return;

        SetState(MonsterState.GetHit);

        if (movementController != null && attacker != null)
            movementController.HandleGetHit(attacker.transform);
    }
}
