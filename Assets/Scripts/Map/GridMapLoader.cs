using NavMeshPlus.Components;
using NavMeshPlus.Extensions;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(Pathfinder))]
public class GridmapLoader : MonoBehaviour
{
    public string jsonFileName = "map1.json";
    public Tilemap tilemap;
    public float tileScale = 1f;
    public GameObject npcPrefab;
    public GameObject departPointPrefab;
    public int subGridDivisions=2;
    // UI overlay used for map loading transition (set in inspector)
    public Image loadingOverlay;
    public float overlayFadeDuration = 0.25f;

    private Dictionary<int, Tile> gidToTile = new Dictionary<int, Tile>();
    public bool loadInEditMode = false;
    private Pathfinder pathfinder;
    private TiledMap map;
    [HideInInspector]
    public string currentOrigin = "Default";
    public bool drawGizmo=false;
    public List<GameObject> spawnPoints = new List<GameObject>();
    private List<TiledObject> pendingMonsters = new List<TiledObject>();
    public List<MonsterMovementController> monsters = new List<MonsterMovementController>();
    private bool firstLoadDone = false;
    private GameObject monsterParent;
    private void Start()
    {
        pathfinder=Pathfinder.Instance;
        transform.position = Vector3.zero;
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && loadInEditMode)
        {
            loadInEditMode = false;
            // Use LoadMapByName which now wraps the loader with the overlay coroutine.
            LoadMapByName(jsonFileName, currentOrigin);
        }
#endif
    }

    IEnumerator LoadJsonFile(string jsonFileName)
    {
        string jsonPath = Path.Combine(Application.streamingAssetsPath, jsonFileName);
        string jsonPathUri = new System.Uri(jsonPath).AbsoluteUri;

        using (UnityWebRequest www = UnityWebRequest.Get(jsonPathUri))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"❌ Lỗi khi tải file từ streamingAssets: {www.error} tại {jsonPathUri}");
                yield break;
            }

            string jsonText = www.downloadHandler.text;
            map = JsonUtility.FromJson<TiledMap>(jsonText);

            if (map == null)
            {
                Debug.LogError($"❌ Không parse được JSON: {jsonFileName}");
                yield break;
            }

            // 🔹 B1: Quét GID được sử dụng trong cả tilelayer & objectlayer
            HashSet<int> usedGids = new HashSet<int>();
            foreach (var layer in map.layers)
            {
                if (layer.type == "tilelayer" && layer.data != null)
                {
                    foreach (int raw in layer.data)
                    {
                        int gid = (int)(raw & 0x1FFFFFFF);
                        if (gid > 0) usedGids.Add(gid);
                    }
                }
                else if (layer.type == "objectgroup" && layer.objects != null)
                {
                    foreach (var obj in layer.objects)
                    {
                        if (obj.gid > 0) usedGids.Add(obj.gid);
                    }
                }
            }

            foreach (var ts in map.tilesets)
                LoadTileset(ts, usedGids);

            LoadTileLayers();

            LoadObjectLayers();
        }
    }

    void LoadTileset(TiledTileset ts, HashSet<int> usedGids)
    {
        int firstGid = ts.firstgid;
        string folder = Path.GetFileNameWithoutExtension(ts.source);

        if (!usedGids.Any(gid => gid >= firstGid))
            return;

        int loadedCount = 0;

        foreach (int gid in usedGids)
        {
            if (gid < firstGid) continue;

            int localId = gid - firstGid;
            string resourcePath = $"Tiles/{folder}/{localId + 1}";

            // 🔸 Nếu là folder "Objects" thì load sprite sheet multiple
            Sprite[] sprites;
            Sprite s = Resources.Load<Sprite>(resourcePath);
            if (s == null)
            {
                continue;
            }
            sprites = new Sprite[] { s };

            if (!gidToTile.ContainsKey(gid))
            {
                Tile tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprites[0];
                gidToTile[gid] = tile;
                loadedCount++;
            }
        }

    }


    void LoadTileLayers()
    {
        int width = map.width;
        int height = map.height;

        TileNode[,] gridNodes = new TileNode[height, width];

        foreach (var layer in map.layers)
        {
            if (layer.type != "tilelayer" || layer.data == null)
                continue;

            for (int i = 0; i < layer.data.Length; i++)
            {
                uint rawGid = (uint)layer.data[i];
                if (rawGid == 0) continue;

                int gid = (int)(rawGid & 0x1FFFFFFF);
                if (!gidToTile.ContainsKey(gid)) continue;

                int x = i % width;
                int logicY = i / width;
                int drawY = height - 1 - logicY;

                Vector3Int pos = new Vector3Int(x, drawY, 0);
                Tile tile = gidToTile[gid];

                tilemap.SetTile(pos, tile);

                if (gridNodes[logicY, x] == null)
                {
                    Vector3 worldPos = tilemap.CellToWorld(pos);
                    gridNodes[logicY, x] = new TileNode(x, logicY, worldPos, true);
                }
            }
        }

        // --- B2: tạo subgrid chi tiết từ collider thực tế ---
        TileNode[,] subGrid = CreateSubGridFromColliders(gridNodes, subGridDivisions);

        // --- B3: gửi subgrid cho Pathfinder ---
        Pathfinder.Instance.Init(subGrid);

        AdjustBoundaryCollider();
    }

    TileNode[,] CreateSubGridFromColliders(TileNode[,] baseGrid, int subDivisions)
    {
        int baseHeight = baseGrid.GetLength(0);
        int baseWidth = baseGrid.GetLength(1);
        float tileW = map.tilewidth;
        float tileH = map.tileheight;

        int newWidth = baseWidth * subDivisions;
        int newHeight = baseHeight * subDivisions;
        float subW = tileW / subDivisions;
        float subH = tileH / subDivisions;

        TileNode[,] subGrid = new TileNode[newHeight, newWidth];

        // ✅ Offset để dịch collider box về đúng tile (lệch sang phải + lên trên 1 tile)
        // Nếu nó đang lệch *sang phải và lên trên*, ta trừ bớt:
        float colliderOffsetX = 0;
        float colliderOffsetY = 0;

        for (int y = 0; y < newHeight; y++)
        {
            for (int x = 0; x < newWidth; x++)
            {
                int baseX = x / subDivisions;
                int baseY = y / subDivisions;

                TileNode parent = baseGrid[baseY, baseX];
                if (parent == null) continue;

                float worldX = x * subW + subW / 2f;
                float worldY = y * subH + subH / 2f;
                Vector3 worldPos = new Vector3(worldX, worldY, 0f);

                bool walkable = true;

                foreach (var layer in map.layers)
                {
                    if (layer.type != "objectgroup" || layer.objects == null)
                        continue;

                    foreach (var obj in layer.objects)
                    {
                        if (obj.type != "Collider" || obj.width <= 0 || obj.height <= 0)
                            continue;

                        // --- Convert Tiled → Unity ---
                        float worldYBottom = (map.height * tileH) - (obj.y + obj.height);
                        float worldYTop = (map.height * tileH) - obj.y;

                        // --- Áp dụng offset cho collider ---
                        float objMinX = obj.x + colliderOffsetX;
                        float objMaxX = objMinX + obj.width;
                        float objMinY = worldYBottom + colliderOffsetY;
                        float objMaxY = worldYTop + colliderOffsetY;

                        // --- AABB của subcell ---
                        float subMinX = x * subW;
                        float subMaxX = subMinX + subW;
                        float subMinY = y * subH;
                        float subMaxY = subMinY + subH;

                        // --- Overlap ---
                        float overlapX = Mathf.Max(0, Mathf.Min(subMaxX, objMaxX) - Mathf.Max(subMinX, objMinX));
                        float overlapY = Mathf.Max(0, Mathf.Min(subMaxY, objMaxY) - Mathf.Max(subMinY, objMinY));

                        float overlapArea = overlapX * overlapY;
                        float subArea = subW * subH;
                        float overlapRatio = overlapArea / subArea;

                        if (overlapRatio >= 0.2f)
                        {
                            walkable = false;
                            goto SkipRemaining;
                        }
                    }
                }

            SkipRemaining:
                subGrid[y, x] = new TileNode(x, y, worldPos, walkable);
            }
        }
        return subGrid;
    }



    private void AdjustBoundaryCollider()
    {
        if (map == null) return;

        float mapWorldWidth = map.width * map.tilewidth;
        float mapWorldHeight = map.height * map.tileheight;
        BoxCollider2D boundaryCollider;
        var go = new GameObject("MapBoundary");
        go.transform.SetParent(transform, false);
        go.tag = "Map Boundary";
        go.layer = LayerMask.NameToLayer("Ignore Raycast");
        boundaryCollider = go.AddComponent<BoxCollider2D>();
        boundaryCollider.isTrigger = true;
        boundaryCollider.offset = Vector2.zero;
        boundaryCollider.size = new Vector2(mapWorldWidth, mapWorldHeight);

        boundaryCollider.transform.localPosition = new Vector3(mapWorldWidth / 2f, mapWorldHeight / 2f, 0f);
        Camera.main.GetComponent<CameraFollow>().InitializeBounds();
    }
    
    // Unload currently loaded map: clear tilemap, destroy spawned parents and clear caches.
    public void UnloadCurrentMap()
    {
        if (tilemap != null)
            tilemap.ClearAllTiles();

        // destroy known parent containers created during LoadObjectLayers
        string[] parentNames = new string[] { "Colliders", "NPCs", "Monsters", "Objects", "SpawnPoints", "DepartPoints", "MapBoundary" };
        foreach (var name in parentNames)
        {
            var child = transform.Find(name);
            if (child != null)
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        // clear loaded tile cache
        gidToTile.Clear();

        // clear spawn points list
        spawnPoints.Clear();

        // drop loaded map reference
        map = null;
        // Note: Pathfinder grid will be replaced when a new map is loaded.
        Debug.Log("GridmapLoader: Unloaded current map.");
    }
    public void LoadMapByName(String newJsonFileName, string origin)
    {
        currentOrigin = origin;
        this.jsonFileName = newJsonFileName;
        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();
        if (!newJsonFileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            newJsonFileName = newJsonFileName + ".json";

        // NOTE: UnloadCurrentMap is deferred until the overlay has fully faded in
        // to prevent showing unload/load artifacts to the player.
        // Start a coroutine that fades the overlay in, unloads+loads the map, then fades out.
        if (Application.isPlaying || loadInEditMode)
            StartCoroutine(LoadMapWithOverlay(newJsonFileName));
    }

    IEnumerator LoadMapWithOverlay(string newJsonFileName)
    {
        // Show overlay and fade to 1
        if (loadingOverlay != null)
        {
            loadingOverlay.gameObject.SetActive(true);
            Color c = loadingOverlay.color;
            c.a = 0f;
            loadingOverlay.color = c;
            yield return StartCoroutine(FadeImageAlpha(0f, 1f, overlayFadeDuration));
        }

        // Only after overlay reached alpha=1 do we unload the current map so the player
        // never sees intermediate unload artifacts.
        UnloadCurrentMap();
        // Now load the map JSON (this will recreate tiles, objects, etc.)
        yield return StartCoroutine(LoadJsonFile(newJsonFileName));

        yield return new WaitUntil(() => NetworkManager.Instance.playerSpawned);
        // Fade overlay out and hide
        if (loadingOverlay != null)
        {
            yield return StartCoroutine(FadeImageAlpha(1f, 0f, overlayFadeDuration));
            loadingOverlay.gameObject.SetActive(false);
        }
    }

    IEnumerator FadeImageAlpha(float from, float to, float duration)
    {
        if (loadingOverlay == null)
            yield break;

        float elapsed = 0f;
        Color c = loadingOverlay.color;
        while (elapsed < duration)
        {
            float t = duration <= 0f ? 1f : (elapsed / duration);
            c.a = Mathf.Lerp(from, to, t);
            loadingOverlay.color = c;
            elapsed += Time.deltaTime;
            yield return null;
        }
        c.a = to;
        loadingOverlay.color = c;
    }

    void LoadObjectLayers()
    {
        GameObject colliderParent = new GameObject("Colliders");
        GameObject npcParent = new GameObject("NPCs");
        monsterParent = new GameObject("Monsters");
        GameObject objectParent = new GameObject("Objects");
		GameObject spawnParent = new GameObject("SpawnPoints");
		GameObject departParent = new GameObject("DepartPoints");

        colliderParent.transform.SetParent(transform);
        npcParent.transform.SetParent(transform);
        monsterParent.transform.SetParent(transform);
        objectParent.transform.SetParent(transform);
        spawnParent.transform.SetParent(transform);
		departParent.transform.SetParent(transform);
        foreach (var layer in map.layers)
        {
            if (layer.type != "objectgroup" || layer.objects == null)
                continue;

            foreach (var obj in layer.objects)
            {
                switch (obj.type)
                {
                    case "Collider":
                        CreateColliderBox(obj, false, colliderParent.transform);
                        break;

                    case "Water":
                        CreateColliderBox(obj, true,colliderParent.transform);
                        break;

                    case "NPC":
                        SpawnNPC(obj,npcParent.transform);
                        break;

                    case "Monster":
                        pendingMonsters.Add(obj);
                        break;
                    case "Spawn Point":
                        CreateSpawnPoint(obj, spawnParent.transform);
                        break;
                    case "Depart Point":
                        CreateDepartPoint(obj, departParent.transform);
                        break;

                    default:
                        if (obj.gid > 0 && gidToTile.TryGetValue(obj.gid, out Tile baseTile))
                            CreateStaticObject(obj, baseTile,objectParent.transform);
                        break;
                }
            }
        }
    }
    public void SpawnAllMonsters()
    {
        foreach (var obj in pendingMonsters)
        {
            SpawnMonster(obj, monsterParent.transform);
        }
        pendingMonsters.Clear();
    }


    private void SpawnMonster(TiledObject obj, Transform monsterParent)
    {
        Vector3 pos = GetWorldPosition(obj);
        if (System.Enum.TryParse(obj.name, out MonsterName npcEnum))
        {
            GameObject monster = PhotonNetwork.InstantiateRoomObject(
            "Prefabs/Enemy",
            pos,
            Quaternion.identity,
            0,
            new object[] { (int)npcEnum }
            );
            monster.name = $"Monster_{obj.name}_{obj.id}";
            monster.transform.SetParent(monsterParent);
            var monsterCtrl = monster.GetComponent<Monster>();
            monsterCtrl.monsterName = npcEnum;
        }
    }


    private void SpawnNPC(TiledObject obj,Transform npcParent)
    {
        if (npcPrefab == null)
        {
            Debug.LogError("NPC prefab not assigned!");
            return;
        }

        Vector3 pos = GetWorldPosition(obj);
        GameObject npc = Instantiate(npcPrefab, pos, Quaternion.identity);
        npc.name = $"NPC_{obj.name}_{obj.id}";
        npc.transform.SetParent(npcParent);

        // Auto-assign NPCName enum if it matches
        var npcCtrl = npc.GetComponent<NPC>();
        if (npcCtrl != null && System.Enum.TryParse(obj.name, out NPCName npcEnum))
        {
            npcCtrl.Init(npcEnum);
        }
    }

    private void CreateStaticObject(TiledObject obj, Tile baseTile, Transform objectParent)
    {
        GameObject go = new GameObject($"Object_{obj.id}");
        go.transform.SetParent(objectParent);
        float mapHeightInWorldUnits = map.height * map.tileheight;
        float centerX = obj.x + obj.width / 2f;
        float centerY = (mapHeightInWorldUnits - obj.y) - obj.height / 2f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = baseTile.sprite;
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 0;
        sr.spriteSortPoint = SpriteSortPoint.Pivot;

        float spriteHeight = sr.bounds.size.y;

        Vector2 spriteSize = sr.sprite.rect.size;
        Vector2 pivotPixel = sr.sprite.pivot;

        float pixelsPerUnit = sr.sprite.pixelsPerUnit;
        Vector2 pivotDelta = (pivotPixel - spriteSize / 2f) / pixelsPerUnit;

        go.transform.position = new Vector3(centerX + pivotDelta.x, centerY + spriteHeight + pivotDelta.y, 0);
    }

    private Vector3 GetWorldPosition(TiledObject obj)
    {
        float mapHeight = map.height * map.tileheight;
        float x = obj.x + obj.width / 2f;
        float y = (mapHeight - obj.y) - obj.height / 2f;
        return new Vector3(x, y, 0);
    }
    private void CreateSpawnPoint(TiledObject obj, Transform spawnParent)
    {
        Vector3 pos = GetWorldPosition(obj);
        // Use the object's name as the spawn point GameObject name so callers can find by place name
        GameObject sp = new GameObject(obj.name);
        sp.transform.position = pos;
        sp.transform.SetParent(spawnParent);
        // record for later use
        spawnPoints.Add(sp);
    }
    private void CreateDepartPoint(TiledObject obj, Transform departParent)
    {
        // Instantiate the depart point prefab if provided; otherwise create a fallback GameObject.
        Vector3 pos = GetWorldPosition(obj);
        GameObject dp;
        if (departPointPrefab == null)
        {
           Debug.LogWarning("Depart Point prefab is not set") ;
           return;
        }
        dp = Instantiate(departPointPrefab, pos, Quaternion.identity, departParent);
        // Parse object name like "city_left" => baseName="city", direction="left"
        string rawName = obj.name ?? string.Empty;
        string baseName = rawName;
        string direction = "right";
        int idx = rawName.LastIndexOf('_');
        if (idx > 0 && idx < rawName.Length - 1)
        {
            baseName = rawName.Substring(0, idx);
            direction = rawName.Substring(idx + 1);
        }

        // Rename depart point GameObject to base name
        dp.name = baseName;

        // Ensure BoxCollider2D exists and matches object size
        var col = dp.GetComponent<BoxCollider2D>();
        if (col == null) col = dp.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(obj.width, obj.height);
        col.offset = Vector2.zero;

        // Ensure DepartPoint component exists and set metadata
        var departComp = dp.GetComponent<DepartPoint>();
        if (departComp == null) departComp = dp.AddComponent<DepartPoint>();
        departComp.destinationMapName = baseName;
        departComp.direction = direction.ToLowerInvariant();
    }
    public void TakeOverMonsters()
    {
        foreach (var m in monsters)
        {
            m.BecomeMasterControlled();
        }
    }

    public Transform FindSpawnPoint(string origin)
    {
        string target = string.IsNullOrEmpty(origin) ? "Default" : origin;

        GameObject found = spawnPoints.Find(s => s != null && s.name == target);

        if (found != null)
            return found.transform;

        Debug.LogWarning($"Spawn origin '{target}' not found. Using fallback.");
        return spawnPoints[0].transform; // fallback
    }
    void CreateColliderBox(TiledObject obj, bool isWater,Transform colliderParent)
    {
        string name = isWater? "WaterBox":"ColliderBox";
        GameObject go = new GameObject(name);
        var col = go.AddComponent<BoxCollider2D>();

        col.isTrigger = isWater;

        float centerX = obj.x + obj.width / 2f;
        float mapHeightInWorldUnits = map.height * map.tileheight;
        float centerY = (mapHeightInWorldUnits - obj.y) - obj.height / 2f;

        go.transform.position = new Vector3(centerX, centerY, 0);

        col.size = new Vector2(obj.width, obj.height);

        col.offset = Vector2.zero;

        go.transform.SetParent(colliderParent);
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmo||!Application.isPlaying) return;
        TileNode[,] gridNodes = pathfinder.grid;
        if (gridNodes == null)
            return;

        int gridHeight = gridNodes.GetLength(0);
        int gridWidth = gridNodes.GetLength(1);


        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                TileNode node = gridNodes[y, x];
                if (node == null) continue;

                // 1. Xác định màu
                if (node.walkable == false)
                {
                    // Màu đỏ cho các ô bị chặn/Collider chặn
                    Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
                }
                else
                {
                    // Màu xanh lá nhạt cho các ô có thể đi
                    Gizmos.color = new Color(0f, 1f, 0f, 0.1f);
                }

                float worldTileWidth = map.tilewidth;
                float worldTileHeight = map.tileheight;

                // Tính vị trí tâm của ô: Góc + 1/2 kích thước
                Vector3 center = node.worldPos;
                center.x += worldTileWidth / 2f;
                center.y += worldTileHeight / 2f;

                // 3. Vẽ hình hộp 2D (Cube) tại vị trí tâm
                Gizmos.DrawCube(center, new Vector3(worldTileWidth, worldTileHeight, 0.1f));
            }
        }
    }

    public void FindAllMonster()
    {
        StartCoroutine(WaitUntilAllMonstersAppear());
    }

    IEnumerator WaitUntilAllMonstersAppear()
    {
        WaitForSeconds wait = new WaitForSeconds(0.1f);

        while (true)
        {
            var found = FindObjectsByType<MonsterMovementController>(FindObjectsSortMode.None);

            if (found.Length >= pendingMonsters.Count)
            {
                monsters = found.ToList();
                yield break;
            }

            yield return wait; 
        }
    }


}
