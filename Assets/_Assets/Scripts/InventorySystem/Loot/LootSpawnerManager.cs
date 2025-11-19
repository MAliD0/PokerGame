using System;
using System.Collections.Generic;
using System.Data;
using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))] // чтобы был доступен на сервере как сетевой сервис
public class LootSpawnerManager : NetworkBehaviour
{
    public static LootSpawnerManager Instance { get; private set; }
    public GameObject lootPrefab;

    [Header("Scatter")]
    [SerializeField, Tooltip("Максимальный радиус разлёта от центра клетки")]
    private float scatterRadius = 0.45f;

    [SerializeField, Tooltip("Случайный поворот лута при спавне")]
    private bool randomRotation = true;

    private void Awake() => Instance = this;

    /// <summary>
    /// Вызови это на СЕРВЕРЕ, когда блок/объект окончательно сломан.
    /// </summary>
    public void SpawnLootForBlock(MapBlockData data, Vector2 position, int? seed = null)
    {
        if (!IsServer) return;
        if (data == null || data.loot == null || data.loot.Count == 0) return;

        //var basePos = AnchorToWorld(anchor);
        var basePos = (Vector3)position;
        var rng = seed.HasValue ? new System.Random(seed.Value) : new System.Random(CombineHash(position, Time.frameCount));

        foreach (var rule in data.loot)
        {
            if (rng.NextDouble()*100 > rule.chance) continue;

            int count = Mathf.Clamp(UnityEngine.Random.Range(rule.min, rule.max + 1), 1, 100);
            // Равномерный угловой шаг + случайный старт → меньше «слипаний»
            float angle = (float)(rng.NextDouble() * Mathf.PI * 2.0);
            float golden = 2.39996323f; // золотой угол ~137.5° в радианах

            for (int i = 0; i < count; i++)
            {
                angle += golden + (float)rng.NextDouble() * 0.15f; // немного шума
                float r = Mathf.Sqrt((float)rng.NextDouble()) * scatterRadius;
                var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;

                var pos = basePos + new Vector3(offset.x, offset.y, 0f);
                GameObject lootObject= SpawnOne(lootPrefab, pos, randomRotation ? UnityEngine.Random.rotationUniform : Quaternion.identity);
                lootObject.GetComponent<LootObject>().InitServer(rule.item.GetItemID(), 1);
            }
        }
    }

    private GameObject SpawnOne(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (prefab.TryGetComponent<NetworkObject>(out _))
        {
            var go = Instantiate(prefab, pos, rot);
            var no = go.GetComponent<NetworkObject>();
            no.Spawn(true);
            return no.gameObject;
            // лёгкая «анимация разлёта» если нет на префабе
            /*            if (!go.TryGetComponent<LootFloater>(out _))
                            go.AddComponent<LootFloater>();*/
        }
        else
        {
            // Fallback: лут без NetworkObject (не рекомендуется).
            // Можно разослать как netless через WorldMapManager:
            // WorldMapManager.Instance.SpawnNetlessLootForAll(itemId, pos, ...);
            return null;
            Debug.LogWarning($"[LootSpawner] Prefab '{prefab.name}' не имеет NetworkObject. " +
                             $"Лут не будет виден/подобран синхронно без доп. RPC.");
        }
    }

    private static Vector3 AnchorToWorld(Vector2Int anchor) =>
        new Vector3(anchor.x + 0.5f, anchor.y + 0.5f, 0f);

    private static int CombineHash(Vector2 a, int b)
    {
        unchecked
        {
            int hx = a.x.GetHashCode();
            int hy = a.y.GetHashCode();
            return (hx * 73856093) ^ (hy * 19349663) ^ (b * 83492791);
        }
    }

}
