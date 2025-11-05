using AYellowpaper.SerializedCollections;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Единственный авторитет по сети: валидирует правила мира, кладёт/удаляет тайлы в слои,
/// спавнит/деспавнит сетевые GO, рассылает RPC для не-сетевых, держит серверный реестр.
/// </summary>
public class WorldMapManager : NetworkBehaviour
{
    [Header("Settings")]
    public MapBlockDataLibrary blockLibrary;

    [FoldoutGroup("Bounds")]
    [FoldoutGroup("Bounds")][SerializeField] private bool useBounds = false;      // false = бесконечная карта
    [FoldoutGroup("Bounds")][SerializeField] private int minX = -128, maxX = 127; // можно любые отриц/полож
    [FoldoutGroup("Bounds")][SerializeField] private int minY = -128, maxY = 127;

    [FoldoutGroup("Layer References")][Header("Back Layer")]
    [FoldoutGroup("Layer References")][SerializeField] private MapLayerGraphics baseLayerGraphics;
    [FoldoutGroup("Layer References")][SerializeField] private MapLayerLogic baseLayer;

    [FoldoutGroup("Layer References")][Header("Fore Layer")]
    [FoldoutGroup("Layer References")][SerializeField] private MapLayerGraphics foreLayerGraphics;
    [FoldoutGroup("Layer References")][SerializeField] private MapLayerLogic foreLayer;

    [Header("Wall Layer")]
    [FoldoutGroup("Layer References")][SerializeField] private MapLayerGraphics wallLayerGraphics;
    [FoldoutGroup("Layer References")][SerializeField] private MapLayerLogic wallLayer;

    [FoldoutGroup("Layer References")][Header("Water Layer")]
    [FoldoutGroup("Layer References")][SerializeField] private MapLayerGraphics waterLayerGraphics;
    [FoldoutGroup("Layer References")][SerializeField] private MapLayerLogic waterLayer;

    public static WorldMapManager Instance { get; private set; }

    public Action<GameObject, Vector2Int, string, string> onObjectInstantiated;

    // ---------- Серверные реестры ----------
    // Для сетевых GO: anchor -> netId
    [FoldoutGroup("Server References")]
    [SerializeField] public SerializedDictionary<MapLayerType, SerializedDictionary<Vector2Int, ulong>> _anchorToNetId = new SerializedDictionary<MapLayerType, SerializedDictionary<Vector2Int, ulong>>();

    // Для не-сетевых GO: anchor -> id, и полный id -> запись
    [FoldoutGroup("Server References")]
    [SerializeField]
    SerializedDictionary<MapLayerType, SerializedDictionary<Vector2Int, string>> _anchorToNetlessId = new SerializedDictionary<MapLayerType, SerializedDictionary<Vector2Int, string>>();

    [FoldoutGroup("Server References")]
    [SerializeField] SerializedDictionary<string, NetlessEntry> _netlessRegistry = new SerializedDictionary<string, NetlessEntry>();
    public Dictionary<string, NetlessEntry> GetNetlessRegistry()
    {
        return _netlessRegistry;
    }

    [Serializable]
    public struct NetlessEntry
    {
        public MapLayerType layer;
        public Vector2Int anchor;
        public Vector2Int[] cells;
        public string itemId;
        public Vector3 pos;
    }

    private string NewId() => Guid.NewGuid().ToString("N");

    private void Awake() => Instance = this;

    private void Start()
    {
        // Инициализируем слои данных
        baseLayer = new MapLayerLogic(new MapBounds(minX,maxX,minY,maxY));
        foreLayer = new MapLayerLogic(new MapBounds(minX, maxX, minY, maxY));
        //wallLayer = new MapLayerLogic(new MapBounds(minX,maxX,minY,maxY));
        waterLayer = new MapLayerLogic(new MapBounds(minX, maxX, minY, maxY));

        // Инициализируем графику
        baseLayerGraphics.Init(baseLayer);
        foreLayerGraphics.Init(foreLayer);
        //wallLayerGraphics.Init(wallLayer);
        waterLayerGraphics.Init(waterLayer);

        _netlessRegistry = new SerializedDictionary<string, NetlessEntry>();
        _anchorToNetId = new SerializedDictionary<MapLayerType, SerializedDictionary<Vector2Int, ulong>>(); 


        ConnectionManager.instance.onServerActivate += (x) =>
        {
            // Подписки на события слоёв — ТОЛЬКО на сервере
            if (IsServer)
            {
                print("Enter");
                baseLayer.onMapTilePlaced += (cells, data) => OnServer_TilePlaced(MapLayerType.backGround, cells, data);
                foreLayer.onMapTilePlaced += (cells, data) => OnServer_TilePlaced(MapLayerType.foreGround, cells, data);
                //wallLayer.onMapTilePlaced += (cells, data) => OnServer_TilePlaced(MapLayerType.wallGround, cells, data);
                waterLayer.onMapTilePlaced += (cells, data) => OnServer_TilePlaced(MapLayerType.waterGround, cells, data);

                baseLayer.onMapTileRemoved += (cells, type) => OnServer_TileRemoved(MapLayerType.backGround, cells, type);
                foreLayer.onMapTileRemoved += (cells, type) => OnServer_TileRemoved(MapLayerType.foreGround, cells, type);
                //wallLayer.onMapTileRemoved += (cells, type) => OnServer_TileRemoved(MapLayerType.wallGround, cells, type);
                waterLayer.onMapTileRemoved += (cells, type) => OnServer_TileRemoved(MapLayerType.waterGround, cells, type);

                // Для снапшотов нетворк-лесс при позднем коннекте
                NetworkManager.OnClientConnectedCallback += OnClientConnectedServer;
            }
        };
    }

    // ============ Публичные команды (клиент → сервер) ============

    public bool CheckIfPlacementIsPossible(Vector2Int pos, string mapBlockDataId)
    {
        var data = blockLibrary.GetMapBlockData(mapBlockDataId);
        if (data == null) return false; 

        // Правила мира (межслойные проверки)
        var targetLayer = GetLayer(data.mapLayerType);
        if (targetLayer == null) { Debug.LogError("[SetTile] targetLayer null"); return false; }

        // Доп. правила (пример)
        if (data.mapLayerType == MapLayerType.foreGround)
        {
            if (!baseLayer.IsTilePresented(pos))
            {
                Debug.LogWarning($"[Rules] ForeGround без BackGround на {pos}");
                return false;
            }

            if (data.isMultiblock)
            {
                foreach (var off in data.tileOffsets)
                {
                    var cell = pos + off;
                    if (!baseLayer.IsTilePresented(cell))
                    {
                        Debug.LogWarning($"[Rules] ForeGround без BackGround на {cell}");
                        return false;
                    }
                }
            }
        }
        if (data.mapLayerType == MapLayerType.waterGround && baseLayer.IsTilePresented(pos))
        {
            Debug.LogWarning($"[Rules] Water на занятый BackGround {pos}");
            return false;
        }

        if (!targetLayer.CanBePlaced(pos, data)) return false;

        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void DamageTileRequestServerRpc(Vector2Int pos, int amount)
    {
        // 1) найдём слой/якорь/данные
        MapLayerType layerType = DetectLayerByCell(pos);
        MapLayerLogic layer = GetLayer(layerType);

        if (layer == null) return;

        var tile = layer.GetMapTile(pos);
        var data = tile?.BlockData;
        if (data == null || !data.breakable) return;

        var anchor = tile.Anchor;
        // 2) применим урон (серверная истина)
        bool broken = layer.Damage(anchor, data, Mathf.Max(1, amount));

        // 3) оповестим клиентов об HP (чтобы у них сработал графический хендлер)
        UpdateTileHealthClientRpc(layerType, anchor, layer.GetHealth(anchor), data.maxHealth);

        // 4) если сломали — дроп и удаление
        if (broken)
        {
            //DropLootServer(anchor, data);
            LootSpawnerManager.Instance.SpawnLootForBlock(layer.GetMapTile(pos).BlockData, pos);
            layer.RemoveTile(anchor); // вызовет onMapTileRemoved -> графика чистит
            DestroyTileForClientsClientRpc(anchor, layerType, ConnectionManager.instance.SendAllExceptHost());
        }
    }


    [ServerRpc(RequireOwnership = false)]
    public void SetTileRequestServerRpc(Vector2Int pos, string mapBlockDataId)
    {
        var data = blockLibrary.GetMapBlockData(mapBlockDataId);
        SetTileServer(pos, data);
    }

    [ServerRpc(RequireOwnership = false)]
    public void DestroyTileRequestServerRpc(Vector2Int pos)
    {
        DestroyTileServer(pos);
    }

    // ============ Серверная логика установки/удаления ============

    private void SetTileServer(Vector2Int pos, MapBlockData data)
    {
        if (data == null) return;

        // Правила мира (межслойные проверки)
        var targetLayer = GetLayer(data.mapLayerType);
        if (targetLayer == null) { Debug.LogError("[SetTile] targetLayer null"); return; }

        // Доп. правила (пример)
        if (data.mapLayerType == MapLayerType.foreGround)
        {
            if (!baseLayer.IsTilePresented(pos))
            {
                Debug.LogWarning($"[Rules] ForeGround без BackGround на {pos}");
                return;
            }

            if (data.isMultiblock)
                {
                    foreach (var off in data.tileOffsets)
                    {
                        var cell = pos + off;
                        if (!baseLayer.IsTilePresented(cell))
                        {
                            Debug.LogWarning($"[Rules] ForeGround без BackGround на {cell}");
                            return;
                        }
                    }
                }
        }
        if (data.mapLayerType == MapLayerType.waterGround && baseLayer.IsTilePresented(pos))
        {
            Debug.LogWarning($"[Rules] Water на занятый BackGround {pos}");
            return;
        }

        // Пишем на СЕРВЕРЕ в слой данных (вызывает OnServer_TilePlaced)
        if (!targetLayer.PlaceBlock(pos, data)) return;

        // Сообщаем КЛИЕНТАМ положить тайл/ячейки в свой слой данных
        SetTileForClientsClientRpc(data.mapLayerType, data.GetItemID(), pos, ConnectionManager.instance.SendAllExceptHost());
        return;
    }

    private void DestroyTileServer(Vector2Int pos)
    {
        // Находим слой по приоритету (как у тебя было)
        MapLayerLogic layer = null;
        MapLayerType layerType = MapLayerType.backGround;

        if (foreLayer.IsTilePresented(pos)) { layer = foreLayer; layerType = MapLayerType.foreGround; }
        else if (baseLayer.IsTilePresented(pos)) { layer = baseLayer; layerType = MapLayerType.backGround; }
        else if (wallLayer.IsTilePresented(pos)) { layer = wallLayer; layerType = MapLayerType.wallGround; }
        else if (waterLayer.IsTilePresented(pos)) { layer = waterLayer; layerType = MapLayerType.waterGround; }

        if (layer == null) return;

        LootSpawnerManager.Instance.SpawnLootForBlock(layer.GetMapTile(pos).BlockData, pos);

        // Удаляем на СЕРВЕРЕ (вызовет OnServer_TileRemoved)
        layer.RemoveTile(pos);

        // Просим КЛИЕНТОВ удалить то же самое у себя
        DestroyTileForClientsClientRpc(pos, layerType, ConnectionManager.instance.SendAllExceptHost());
    }

    // ============ Коллбеки сервера на изменения данных ============

    private void OnServer_TilePlaced(MapLayerType layer, List<Vector2Int> cells, MapBlockData data)
    {
        if (data.mapBlockType != MapBlockType.GameObject) return;

        var anchor = cells[0];
        var worldPos = new Vector3(anchor.x + 0.5f, anchor.y + 0.5f, 0f);
        var prefab = blockLibrary.GetMapBlockData(data.GetItemID())?.gameObject;

        if (!prefab)
        {
            Debug.LogError($"[TilePlaced] Prefab id={data.GetItemID()} не найден");
            return;
        }

        if (prefab.TryGetComponent<NetworkObject>(out _))
        {
            // СЕТЕВОЙ GO
            var go = Instantiate(prefab, worldPos, Quaternion.identity);
            var no = go.GetComponent<NetworkObject>();
            no.Spawn();

            // реестр
            if (!_anchorToNetId.TryGetValue(layer, out var dictNet)) _anchorToNetId[layer] = dictNet = new();
            dictNet[anchor] = no.NetworkObjectId;

            // Биндинг на клиентах по netId (чтобы графика знала cell->GO)
            BindObjectByNetIdClientRpc(cells.ToArray(), no.NetworkObjectId, layer);
        }
        else
        {
            // НЕ-СЕТЕВОЙ GO (гибрид)
            var id = NewId();

            if (!_anchorToNetlessId.TryGetValue(layer, out var dict)) _anchorToNetlessId[layer] = dict = new();
            dict[anchor] = id;

            _netlessRegistry[id] = new NetlessEntry
            {
                layer = layer,
                anchor = anchor,
                cells = cells.ToArray(),
                itemId = data.GetItemID(),
                pos = worldPos
            };

            // Рассылаем спавн всем
            SpawnNetlessClientRpc(layer, data.GetItemID(), anchor, cells.ToArray(), worldPos, id);
        }
    }

    private void OnServer_TileRemoved(MapLayerType layer, List<Vector2Int> cells, MapBlockType type)
    {
        if (type != MapBlockType.GameObject) return;

        var anchor = cells[0];

        // 1) Пытаемся удалить сетевой GO
        if (_anchorToNetId.TryGetValue(layer, out var dictNet) && dictNet.TryGetValue(anchor, out var netId))
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netId, out var no))
                no.Despawn(true);

            dictNet.Remove(anchor);

            // отвязываем cell->GO на клиентах (без уничтожения — объект уже деспавнен)
            UnbindByCellsClientRpc(cells.ToArray(), layer, destroyNonNetworked: false, ConnectionManager.instance.SendAllExceptHost());
            return;
        }

        // 2) Иначе — не-сетевой
        if (_anchorToNetlessId.TryGetValue(layer, out var dict) && dict.TryGetValue(anchor, out var id))
        {
            dict.Remove(anchor);
            _netlessRegistry.Remove(id);

            // Клиенты сами найдут GO по id и уничтожат его
            RemoveNetlessClientRpc(id, layer);
            return;
        }

        // Fallback: просто отвязать на клиентах (если где-то несостыковка)
        UnbindByCellsClientRpc(cells.ToArray(), layer, destroyNonNetworked: true);
    }

    // ============ Клиентские RPC (все клиенты или таргет) ============
    
    [ClientRpc]
    private void UpdateTileHealthClientRpc(MapLayerType layerType, Vector2Int anchor, int hp, int maxHp, ClientRpcParams p = default)
    {
        var layer = GetLayer(layerType);
        var tile = layer?.GetMapTile(anchor);
        var data = tile?.BlockData;
        if (layer == null || data == null) return;

        // установим HP и сгенерим эвент для графики
        layer.SetHealth(anchor, data, hp, fireEvent: true);
    }

    [ClientRpc]
    private void SetTileForClientsClientRpc(MapLayerType tileType, string mapBlockDataID, Vector2Int position, ClientRpcParams rpcParams = default)
    {
        var layer = GetLayer(tileType);
        var data = blockLibrary.GetMapBlockData(mapBlockDataID);
        if (layer == null || data == null) return;

        layer.PlaceBlock(position, data);
    }

    [ClientRpc]
    private void DestroyTileForClientsClientRpc(Vector2Int pos, MapLayerType tileType, ClientRpcParams rpcParams = default)
    {
        var layer = GetLayer(tileType);
        if (layer == null) return;

        layer.RemoveTile(pos);
    }

    [ClientRpc]
    private void BindObjectByNetIdClientRpc(Vector2Int[] cells, ulong netId, MapLayerType layer, ClientRpcParams rpcParams = default)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netId, out var no))
        {
            // На крайне редких лагах — попробуем пару кадров подождать
            StartCoroutine(RetryBind(cells, netId, layer));
            return;
        }
        GetGraphics(layer).BindObject(new List<Vector2Int>(cells), no.gameObject, null);
    }

    private System.Collections.IEnumerator RetryBind(Vector2Int[] cells, ulong netId, MapLayerType layer)
    {
        for (int i = 0; i < 10; i++)
        {
            yield return null;
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netId, out var no))
            {
                GetGraphics(layer).BindObject(new List<Vector2Int>(cells), no.gameObject, null);

                yield break;
            }
        }
        Debug.LogWarning($"[RetryBind] net object {netId} не найден");
    }

    [ClientRpc]
    private void SpawnNetlessClientRpc(MapLayerType layer, string itemId, Vector2Int anchor, Vector2Int[] cells, Vector3 pos, string id, ClientRpcParams rpcParams = default)
    {
        var prefab = blockLibrary.GetMapBlockData(itemId)?.gameObject;
        if (!prefab) { Debug.LogError($"[SpawnNetless] нет префаба {itemId}"); return; }

        var go = Instantiate(prefab, pos, Quaternion.identity);

        //Testing:
        onObjectInstantiated?.Invoke(go, anchor, itemId, id);

        GetGraphics(layer).BindObject(new List<Vector2Int>(cells), go, id);
    }

    [ClientRpc]
    private void RemoveNetlessClientRpc(string id, MapLayerType layer, ClientRpcParams rpcParams = default)
    {
        GetGraphics(layer).UnbindById(id);
    }

    [ClientRpc]
    private void UnbindByCellsClientRpc(Vector2Int[] cells, MapLayerType layer, bool destroyNonNetworked, ClientRpcParams rpcParams = default)
    {
        GetGraphics(layer).UnbindByCells(new List<Vector2Int>(cells), destroyNonNetworked);
    }

    // ============ Снапшот нетворк-лесс для поздних клиентов ============

    private void OnClientConnectedServer(ulong clientId)
    {
        // Шлём только нетворк-лесс (сетевые придут автоматически от NGO)
        var target = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
        };
        
        foreach (var kv in _netlessRegistry)
        {
            var e = kv.Value;
            SpawnNetlessClientRpc(e.layer, e.itemId, e.anchor, e.cells, e.pos, kv.Key, target);
        }

        // Актуализируем тайлы: пробежать все слои и переслать, если требуется.
        // Если клиенты уже в курсе (через поздний join), можно опустить.
        // Здесь оставлено минималистично.
    }


    // ============ Вспомогалки ============

    public MapLayerLogic GetLayer(MapLayerType t) => t switch
    {
        MapLayerType.backGround => baseLayer,
        MapLayerType.foreGround => foreLayer,
        MapLayerType.wallGround => wallLayer,
        MapLayerType.waterGround => waterLayer,
        _ => null
    };

    public MapLayerGraphics GetGraphics(MapLayerType t) => t switch
    {
        MapLayerType.backGround => baseLayerGraphics,
        MapLayerType.foreGround => foreLayerGraphics,
        MapLayerType.wallGround => wallLayerGraphics,
        MapLayerType.waterGround => waterLayerGraphics,
        _ => baseLayerGraphics
    };

    private MapLayerType DetectLayerByCell(Vector2Int pos)
    {
        MapLayerLogic layer = null;
        MapLayerType layerType = MapLayerType.backGround;

        if (foreLayer.IsTilePresented(pos)) {  layerType = MapLayerType.foreGround; }
        else if (baseLayer.IsTilePresented(pos)) {  layerType = MapLayerType.backGround; }
        else if (wallLayer.IsTilePresented(pos)) { layerType = MapLayerType.wallGround; }
        else if (waterLayer.IsTilePresented(pos)) { layerType = MapLayerType.waterGround; }
        return layerType;
    }

    // ============ Сервис ============

    [Button]
    public void ClearTilemaps()
    {
        foreach(var item in _anchorToNetId)
        {
            foreach (var id in item.Value)
            {
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id.Value, out var no))
                    no.Despawn(true);
            }
        }

        ClearTilemapsClientRpc();
    }

    [ClientRpc]
    private void ClearTilemapsClientRpc()
    {
        waterLayerGraphics.ClearTilemap();
        baseLayerGraphics.ClearTilemap();
        foreLayerGraphics.ClearTilemap();
        //wallLayerGraphics.ClearTilemap();

        waterLayer.LayerTiles.Clear();
        baseLayer.LayerTiles.Clear();
        foreLayer.LayerTiles.Clear();
        //wallLayer.LayerTiles.Clear();
    }
}
