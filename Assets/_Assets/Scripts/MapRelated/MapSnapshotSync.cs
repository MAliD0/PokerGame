using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Late-join синхронизация: тайлы чанками, нетворк-лесс GO, биндинг сетевых GO.
/// Без вращений/доп. фич — только базовая догрузка нового клиента.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class MapSnapshotSync : NetworkBehaviour
{
    [Header("Refs (автопоиск при спавне)")]
    [SerializeField] private WorldMapManager world;
    [SerializeField] private MapBlockDataLibrary blockLibrary;

    [Header("Chunk sizes")]
    [SerializeField] private int tileChunkSize = 800;
    [SerializeField] private int netlessChunkSize = 300;

    [Header("Options")]
    [Tooltip("Присылать биндинг сетевых GO (если нет биндера на префабах)")]
    [SerializeField] private bool sendNetworkedBindSnapshot = true;

    // -------- DTO --------
    [Serializable]
    public struct TileSnapshotEntry : INetworkSerializable
    {
        public MapLayerType layer;
        public string itemId; // MapBlockData.GetItemID()
        public V2I anchor;    // якорь мульти-группы (для single = сама клетка)
        public int hp;

        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
        {
            s.SerializeValue(ref layer);
            s.SerializeValue(ref itemId);
            s.SerializeValue(ref anchor);
            s.SerializeValue(ref hp);
        }
    }

    [Serializable]
    public struct NetlessSnapshotEntry : INetworkSerializable
    {
        public MapLayerType layer;
        public string itemId;
        public V2I anchor;
        public Vector3 pos;
        public string id; // стабильный id сервера

        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
        {
            s.SerializeValue(ref layer);
            s.SerializeValue(ref itemId);
            s.SerializeValue(ref anchor);
            s.SerializeValue(ref pos);
            s.SerializeValue(ref id);
        }
    }

    // ----- lifecycle -----
    public override void OnNetworkSpawn()
    {
        if (!world) world = WorldMapManager.Instance;
        if (!blockLibrary) blockLibrary = world ? world.blockLibrary : null;

        if (IsServer)
            NetworkManager.OnClientConnectedCallback += OnClientConnectedServer;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
            NetworkManager.OnClientConnectedCallback -= OnClientConnectedServer;
    }

    // ----- server entry -----
    private void OnClientConnectedServer(ulong clientId)
    {
        StartCoroutine(SendFullSnapshotRoutine(clientId));
    }

    private IEnumerator SendFullSnapshotRoutine(ulong clientId)
    {
        if (world == null || blockLibrary == null)
        {
            Debug.LogWarning("[MapSnapshotSync] Missing refs");
            yield break;
        }

        var target = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
        };

        // 1) Тайлы чанками
        var tiles = BuildTileSnapshot();
        for (int i = 0; i < tiles.Count; i += tileChunkSize)
        {
            var slice = tiles.GetRange(i, Mathf.Min(tileChunkSize, tiles.Count - i)).ToArray();
            var isLast = (i + tileChunkSize) >= tiles.Count;
            ApplyTileSnapshotChunkClientRpc(slice, isLast, target);
            yield return null;
        }

        // 2) Нетворк-лесс GO чанками
        var netless = BuildNetlessSnapshot();
        for (int i = 0; i < netless.Count; i += netlessChunkSize)
        {
            var slice = netless.GetRange(i, Mathf.Min(netlessChunkSize, netless.Count - i)).ToArray();
            var isLast = (i + netlessChunkSize) >= netless.Count;
            SpawnNetlessSnapshotChunkClientRpc(slice, isLast, target);
            yield return null;
        }

        // 3) Сетевые GO — биндинг (если не используете Binder на префабах)
        if (sendNetworkedBindSnapshot)
        {
            int sent = 0;
            foreach (var layerPair in world._anchorToNetId) // MapLayerType -> (anchor -> netId)
            {
                var layer = layerPair.Key;
                var logic = world.GetLayer(layer);
                if (logic == null) continue;

                foreach (var kv in layerPair.Value)
                {
                    var anchor = kv.Key;
                    var netId = kv.Value;

                    // восстановим список клеток группы по anchor + tileOffsets
                    var tile = logic.GetMapTile(anchor);
                    if (tile?.BlockData == null) continue;

                    Vector2Int[] cells;
                    if (tile.BlockData.isMultiblock)
                    {
                        var offs = tile.BlockData.tileOffsets;
                        cells = new Vector2Int[offs.Count-1];
                        for (int i = 0; i < offs.Count - 1; i++) cells[i] = anchor + offs[i];
                    }
                    else cells = new[] { anchor };

                    BindObjectByNetIdClientRpc(ToV2IArray(cells), netId, layer, target);

                    if (++sent % 200 == 0) yield return null; // не душим транспорт
                }
            }
        }
    }

    // ----- snapshot builders (server) -----
    private List<TileSnapshotEntry> BuildTileSnapshot()
    {
        var result = new List<TileSnapshotEntry>();

        void AddLayer(MapLayerType t, MapLayerLogic logic)
        {
            if (logic == null) return;
            var seen = new HashSet<Vector2Int>();
            foreach (var kv in logic.LayerTiles)
            {
                var tile = kv.Value;
                var anchor = tile.Anchor;
                
                if (!seen.Add(anchor)) continue; // один раз на мульти-группу

                int hp = tile.BlockData.breakable ? logic.GetHealth(anchor) : 0; 
                result.Add(new TileSnapshotEntry { layer = t, itemId = tile.BlockData.GetItemID(), anchor = anchor, hp= hp});
            }
        }

        AddLayer(MapLayerType.backGround, world.GetLayer(MapLayerType.backGround));
        AddLayer(MapLayerType.foreGround, world.GetLayer(MapLayerType.foreGround));
        AddLayer(MapLayerType.wallGround, world.GetLayer(MapLayerType.wallGround));
        AddLayer(MapLayerType.waterGround, world.GetLayer(MapLayerType.waterGround));
        return result;
    }

    private List<NetlessSnapshotEntry> BuildNetlessSnapshot()
    {
        var list = new List<NetlessSnapshotEntry>();
        foreach (var kv in world.GetNetlessRegistry()) // string id -> NetlessEntry
        {
            var id = kv.Key;
            var e = kv.Value;
            list.Add(new NetlessSnapshotEntry
            {
                id = id,             // ВАЖНО: id = key
                layer = e.layer,
                itemId = e.itemId,       // itemId из записи
                anchor = e.anchor,
                pos = e.pos
            });
        }
        return list;
    }

    private static V2I[] ToV2IArray(Vector2Int[] arr)
    {
        var res = new V2I[arr.Length];
        for (int i = 0; i < arr.Length; i++) res[i] = arr[i];
        return res;
    }

    // ----- client RPCs -----
    [ClientRpc]
    private void ApplyTileSnapshotChunkClientRpc(TileSnapshotEntry[] chunk, bool isLast, ClientRpcParams p = default)
    {
        if (world == null || blockLibrary == null) return;
        print("Enter ApplyTileSnapshotChunkClientRpc");
        foreach (var e in chunk)
        {
            var layer = world.GetLayer(e.layer);
            var data = blockLibrary.GetMapBlockData(e.itemId);
            if (layer == null || data == null) continue;

            // идемпотентность
            if (!layer.IsTilePresented(e.anchor))
                layer.PlaceBlock(e.anchor, data);

            if (data.breakable)
                layer.SetHealth(e.anchor, data, e.hp, true);
        }
    }

    [ClientRpc]
    private void SpawnNetlessSnapshotChunkClientRpc(NetlessSnapshotEntry[] chunk, bool isLast, ClientRpcParams p = default)
    {
        if (world == null || blockLibrary == null) return;

        foreach (var e in chunk)
        {
            var data = blockLibrary.GetMapBlockData(e.itemId);
            if (data == null || data.gameObject == null) continue;

            var go = Instantiate(data.gameObject, e.pos, Quaternion.identity);

            // восстановим клетки группы (anchor + offsets)
            var cells = new List<Vector2Int>();
            if (data.isMultiblock) foreach (var off in data.tileOffsets) cells.Add(e.anchor + off);
            else cells.Add(e.anchor);

            world.GetGraphics(e.layer).BindObject(cells, go, e.id);
        }
    }

    [ClientRpc]
    private void BindObjectByNetIdClientRpc(V2I[] cellsV2I, ulong netId, MapLayerType layer, ClientRpcParams p = default)
    {
        var sm = NetworkManager.Singleton.SpawnManager;
        if (!sm.SpawnedObjects.TryGetValue(netId, out var no))
        {
            StartCoroutine(RetryBind(cellsV2I, netId, layer));
            return;
        }

        var cells = new List<Vector2Int>(cellsV2I.Length);
        foreach (var c in cellsV2I) cells.Add(c);
        world.GetGraphics(layer).BindObject(cells, no.gameObject, null);
    }

    private IEnumerator RetryBind(V2I[] cellsV2I, ulong netId, MapLayerType layer)
    {
        var sm = NetworkManager.Singleton.SpawnManager;
        for (int i = 0; i < 60; i++) // до ~1 сек при 60 FPS
        {
            yield return null;
            if (sm.SpawnedObjects.TryGetValue(netId, out var no))
            {
                var cells = new List<Vector2Int>(cellsV2I.Length);
                foreach (var c in cellsV2I) cells.Add(c);
                world.GetGraphics(layer).BindObject(cells, no.gameObject, null);
                yield break;
            }
        }
        Debug.LogWarning($"[MapSnapshotSync] RetryBind timeout for netId={netId}");
    }
}
