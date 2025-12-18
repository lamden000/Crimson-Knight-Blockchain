using ExitGames.Client.Photon;
using NUnit.Framework;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public bool debugMode = false;
    [SerializeField] private GridmapLoader gridmapLoader;
    public bool playerSpawned = false;
    public static NetworkManager Instance;
    public bool useFakeProfileForTesting = false;
    public string fakeUsername = "TestPlayer";
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

        if (useFakeProfileForTesting)
            CreateFakeProfileForTesting(fakeUsername);
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
    public override void OnJoinedRoom()
    {
        // Lấy username
        string myUsername;

        if (!useFakeProfileForTesting)
        {
            // Dùng username thật từ PlayerData
            var realName = PlayerDataManager.Instance?.Data?.username;

            if (!string.IsNullOrEmpty(realName))
            {
                myUsername = realName;
                Debug.Log($"[OnJoinedRoom] Using REAL username: {myUsername}");
            }
            else
            {
                // Fallback nếu Data bị null (đỡ crash)
                myUsername = "Player_" + PhotonNetwork.LocalPlayer.ActorNumber;
                Debug.LogWarning("[OnJoinedRoom] REAL username missing → using fallback: " + myUsername);
            }
        }
        else
        {
            // Fake cho testing
            myUsername = "Player_" + PhotonNetwork.LocalPlayer.ActorNumber + "_" + Random.Range(1000, 9999);
            Debug.Log($"[OnJoinedRoom] Using FAKE username for testing: {myUsername}");
        }


        // Spawn local player trước
        Transform spawn = gridmapLoader.FindSpawnPoint(gridmapLoader.currentOrigin);
        if (PhotonNetwork.IsMasterClient)
            gridmapLoader.SpawnAllMonsters();

        gridmapLoader.FindAllMonster();

        GameObject player = PhotonNetwork.Instantiate(
            "Prefabs/Character",
            spawn.position,
            spawn.rotation
        );

        Debug.Log($"[OnJoinedRoom] Instantiated Player Object for Actor {PhotonNetwork.LocalPlayer.ActorNumber}");


        // Set photon nickname + custom props
        PhotonNetwork.LocalPlayer.NickName = myUsername;
        PhotonNetwork.LocalPlayer.SetCustomProperties(
            new ExitGames.Client.Photon.Hashtable { { "username", myUsername } }
        );

        Debug.Log($"[OnJoinedRoom] Set 'username' property for Actor {PhotonNetwork.LocalPlayer.ActorNumber}: {myUsername}");


        // Apply tên cho những player đã vào trước
        foreach (var other in PhotonNetwork.PlayerList)
        {
            if (other == PhotonNetwork.LocalPlayer) continue;

            if (other.CustomProperties.ContainsKey("username"))
            {
                Debug.Log($"[OnJoinedRoom] Found existing player {other.ActorNumber}, applying name = {other.CustomProperties["username"]}");
                StartCoroutine(ApplyNameTagWhenReady(other));
            }
            else
            {
                Debug.Log($"[OnJoinedRoom] Existing player {other.ActorNumber} HAS NO username yet");
            }
        }

        // Local setup
        SetupPlayerObject(player);
        playerSpawned = true;
    }


    //--------------------------------------------------------------------------------------

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {

        if (changedProps.ContainsKey("username"))
        {
            Debug.Log($"[OnPlayerPropertiesUpdate] username changed: {changedProps["username"]}");

            // Đợi player object spawn rồi hẵng set name
            StartCoroutine(ApplyNameTagWhenReady(targetPlayer));
        }
        else
        {
            Debug.Log("[OnPlayerPropertiesUpdate] username NOT found in changes.");
        }
    }


    //--------------------------------------------------------------------------------------

    private IEnumerator ApplyNameTagWhenReady(Player p)
    {
        string username = p.CustomProperties["username"] as string;

        while (true)
        {
            foreach (var character in FindObjectsByType<Character>(FindObjectsSortMode.None))
            {
                PhotonView view = character.GetComponent<PhotonView>();
                if (view.Owner == p)
                {
                    NameTag tag = view.GetComponentInChildren<NameTag>(true);

                    if (tag != null)
                    {
                        tag.SetName(username);

                        if (view.IsMine)
                            tag.nameText.color = Color.yellow;

                        yield break;
                    }
                }
            }

            yield return null; // đợi frame sau
        }
    }


    //--------------------------------------------------------------------------------------

    private void SetupPlayerObject(GameObject player)
    {

        PhotonView view = player.GetComponent<PhotonView>();
        var tag = player.GetComponentInChildren<NameTag>();

        // Luôn apply bằng coroutine
        StartCoroutine(ApplyNameTagWhenReady(view.Owner));

        if (view.IsMine && tag != null)
            tag.nameText.color = Color.yellow;

        if (view.IsMine)
        {
            var camFollow = Camera.main.GetComponent<CameraFollow>();
            camFollow.SetTarget(player.transform);
            camFollow.SnapToTargetImmediate();
        }

        AssignPlayerToUI(player);
    }


    // Helper test profile (khỏi login)
    private void CreateFakeProfileForTesting(string username)
    {
        var pd = new PlayerData();
        pd.userId = "FAKE_" + Random.Range(1000, 9999);
        pd.username = string.IsNullOrEmpty(username) ? "TestUser" : username;
        pd.level = 1;
        pd.exp = 0;
        pd.gold = 0;

        // ensure PlayerDataManager exists
        var existing = FindAnyObjectByType<PlayerDataManager>();
        if (existing == null)
        {
            var go = new GameObject("PlayerDataManager");
            go.AddComponent<PlayerDataManager>();
        }

        // initialize instance (if Awake hasn't run yet, this will run in OnJoinedRoom start check)
        if (PlayerDataManager.Instance == null)
        {
            // Delay initialize until Awake runs on newly created manager
            StartCoroutine(InitializeWhenReady(pd));
        }
        else
        {
            PlayerDataManager.Instance.Initialize(pd);
        }

        // also set Photon NickName so other players can see quick fallback
        PhotonNetwork.LocalPlayer.NickName = pd.username;
    }

    private IEnumerator InitializeWhenReady(PlayerData pd)
    {
        while (PlayerDataManager.Instance == null)
        {
            yield return null;
        }
        PlayerDataManager.Instance.Initialize(pd);
    }
}
