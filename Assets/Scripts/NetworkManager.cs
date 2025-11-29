using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using System.Collections;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public bool debugMode = false;
    [SerializeField] private GridmapLoader gridmapLoader;
    public bool playerSpawned = false;
    public static NetworkManager Instance;
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    void Start()
    {
        if(debugMode)
            Debug.Log("Đang kết nối tới Photon...");
        PhotonNetwork.ConnectUsingSettings();
        string newMapId = gridmapLoader.jsonFileName;
        gridmapLoader.LoadMapByName(newMapId, "Default");
    }

    public override void OnConnectedToMaster()
    {
        if (debugMode)
            Debug.Log("Connected to master");
        PhotonNetwork.JoinOrCreateRoom(
        "Map_" + gridmapLoader.jsonFileName,
        new RoomOptions()
        {
            MaxPlayers = 20,  
            IsOpen = true,
            IsVisible = true
        },
        TypedLobby.Default
        );
    }

    public override void OnJoinedLobby()
    {
        if (debugMode)
            Debug.Log("Đã vào Lobby. Giờ thử join hoặc tạo Room.");
    }

    public override void OnLeftRoom()
    {
        if (debugMode)
            Debug.Log("Leaved...");
        playerSpawned = false;
    }
    public override void OnMasterClientSwitched(Player newMaster)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            gridmapLoader.TakeOverMonsters();
        }
    }

    public override void OnJoinedRoom()
    {
        if(PhotonNetwork.IsMasterClient)
            gridmapLoader.SpawnAllMonsters();
        Transform spawn = gridmapLoader.FindSpawnPoint(gridmapLoader.currentOrigin);
        GameObject player = PhotonNetwork.Instantiate(
            "Prefabs/Character",
            spawn.position,
            spawn.rotation
        );

        // snap camera ngay lập tức vào player local
        var camFollow = Camera.main.GetComponent<CameraFollow>();
        camFollow.SetTarget(player.transform);
        if (camFollow != null)
            camFollow.SnapToTargetImmediate();
        AssignPlayerToUI(player);
        gridmapLoader.FindAllMonster();
        playerSpawned = true;
        if (debugMode)
            Debug.Log("Joined..." + PhotonNetwork.CurrentRoom);
    }
    private void AssignPlayerToUI(GameObject obj)
    {
        var view = obj.GetComponent<PhotonView>();
        if (view != null && view.IsMine)
        {
            var anim = obj.GetComponent<PlayerAnimationController>();
            var ui = FindAnyObjectByType<CharacterEquipmentController>();
            if (ui != null)
            {
                ui.AssignLocalPlayer(anim);
                if (debugMode)
                    Debug.Log("Assigned local player to UI via OnJoinedRoom.");
            }
        }
    }

}
