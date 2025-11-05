using ProceduralGeneration;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
public class WorldGenerationManager : MonoBehaviour
{
    [Header("Settings:")]
    [ShowInInspector]
    public MapBlockDataLibrary tilesLibrary;
    public Grid grid;
    public Tilemap tilemap;

    public bool affectCorners;
    [ShowInInspector]
    public static int gridSize = 30;
    public float cellSize;
    public GameObject tilePrefab;

    public static Tile[,] tiles;
    public List<Tile> entropyList;

    public static WorldGenerationManager instance;
    private void Awake()
    {
        instance = this;
    }
    private void Start()
    {
        InitGrid();
    }
    private void Remake()
    {
        tiles = null;
        entropyList = null;

        foreach (Transform child in this.transform)
        {
            Destroy(child.gameObject);
        }
        
        InitGrid();
        int value = gridSize * gridSize;

        while (value > 0)
        {
            EntropyNext();
            value--;
        }
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            Remake();
        }
    }
    public void InitGrid()
    {
        tiles = new Tile[gridSize, gridSize];
        Tile addedTile = null;
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                tiles[x, y] = new Tile(new int[]{x,y});
                addedTile = tiles[x, y];
                if (affectCorners)
                {
                    List<int[]> neighbors = new List<int[]>() {(x == 0 ? null : new int[] { x - 1, y }),
                        (x == gridSize - 1 ? null : new int[] { x + 1, y }),
                        (y == gridSize - 1 ? null : new int[] { x, y + 1 }),
                        (y == 0 ? null : new int[] { x, y - 1 }),

                        (x == 0 || y ==0? null : new int[] { x - 1, y - 1 })/*left-down*/,
                        (x == 0 || y == gridSize - 1 ? null : new int[] { x - 1, y + 1 })/*left-up*/,
                        (x == gridSize - 1 || y == 0? null : new int[] { x + 1, y - 1 })/*right-down*/,
                        (x == gridSize - 1 && y == gridSize - 1 ? null : new int[] { x + 1, y + 1 })/*right-up*/
                    };

                    addedTile.SetNeighbors(neighbors);
                }
                else
                {
                    addedTile.SetNeighbors
                    ((x == 0 ? null : new int[] { x - 1, y }),
                    (x == gridSize - 1 ? null : new int[] { x + 1, y }),
                    (y == gridSize - 1 ? null : new int[] { x, y + 1 }),
                    (y == 0 ? null : new int[] { x, y - 1 }));
                }
            }
        }
    }

    private void CreateEntropyList()
    {
        entropyList = tiles.Cast<Tile>().ToList();
        entropyList.RemoveAll(x => x.collapsed == true);
        entropyList = entropyList.OrderBy(tile => tile.EntropyNumber()).ToList();
    }

    public void EntropyNext()
    {
        CreateEntropyList();

        if (entropyList == null || entropyList.Count == 0) return;
        
        Tile tile = entropyList[0];
        tile.Collapse();

        tilemap.SetTile(new Vector3Int(tile.gridPosition[0], tile.gridPosition[1], 0), tile.data.ruleTile);
        tilemap.size = new Vector3Int(100, 100);
    }

    public static Tile GetTile(int[] coords)
    {
        if ((coords[0] < gridSize && coords[0] >= 0) && (coords[1] < gridSize && coords[1] >= 0))
        {
            return tiles[coords[0], coords[1]];
        }
        return null;
    }

}
[Serializable]
public class Tile
{
    public Tile(int[] gridPosition)
    {
        collapsed = false;
        this.gridPosition = gridPosition;

        // Инициализируем все варианты тайла равным весом 1f
        entropyIntoOptions = Enum.GetValues(typeof(TileOptions))
                                 .Cast<TileOptions>()
                                 .ToDictionary(opt => opt, opt => 1f);
    }

    public ProceduralGeneration.TileData data { get; private set; } // выбранные данные после коллапса
    public bool collapsed;

    public Dictionary<TileOptions, float> entropyIntoOptions; // <вариант, вес>
    public int[] gridPosition;
    public List<int[]> neighbors = new List<int[]>();

    // --- Сжатие энтропии у соседей с учётом текущего тайла ---
    public void EntropyNearest()
    {
        if (data == null || data.options == null) return;

        foreach (var npos in neighbors)
        {
            if (npos == null) continue;

            Tile neighbor = WorldGenerationManager.GetTile(npos);
            if (neighbor == null || neighbor.collapsed) continue;

            var newOptions = new Dictionary<TileOptions, float>();

            foreach (var kvp in neighbor.entropyIntoOptions)
            {
                // Оставляем только разрешённые текущим тайлом варианты
                if (data.options.TryGetValue(kvp.Key, out float compatWeight))
                {
                    // Пересчитываем вес: старый вес * коэффициент совместимости
                    newOptions[kvp.Key] = kvp.Value * compatWeight;
                }
            }

            // Если всё вырезали, можно (по желанию) fallback-нуть что-то базовое, чтобы не повесить генерацию
            neighbor.entropyIntoOptions = newOptions.Count > 0
                ? newOptions
                : new Dictionary<TileOptions, float> { { TileOptions.Plains, 1f } };
        }
    }

    // --- Коллапс текущей клетки ---
    public void Collapse()
    {
        // Защитный кейс
        if (entropyIntoOptions == null || entropyIntoOptions.Count == 0)
        {
            //data = WorldGenerationManager.instance.tilesLibrary.tileDatas[TileOptions.Plains];
            collapsed = true;
            EntropyNearest();
            return;
        }

        // Случайный выбор с весами
        TileOptions chosen = GetRandomElement();
        //data = WorldGenerationManager.instance.tilesLibrary.GetMapBlockData[chosen];

        collapsed = true;

        // После выбора — сжимаем энтропию у соседей
        EntropyNearest();
    }

    // --- Взвешенный рандом по словарю энтропии ---
    public TileOptions GetRandomElement()
    {
        float totalWeight = entropyIntoOptions.Values.Sum();
        if (totalWeight <= 0f)
            return TileOptions.Plains;

        float r = (float)(UnityEngine.Random.value * totalWeight);
        float cum = 0f;

        foreach (var kvp in entropyIntoOptions)
        {
            cum += kvp.Value;
            if (r <= cum)
                return kvp.Key;
        }

        // На всякий – последний элемент
        return entropyIntoOptions.Keys.Last();
    }

    // Перегрузка: выбор по списку данных с полем chance (если нужно)
    public ProceduralGeneration.TileData GetRandomElement(List<ProceduralGeneration.TileData> tileDatas)
    {
        float total = tileDatas.Sum(e => e.chance);
        if (total <= 0f) return null;

        float r = UnityEngine.Random.value * total;
        float cum = 0f;

        foreach (var e in tileDatas)
        {
            cum += e.chance;
            if (r <= cum)
                return e;
        }
        return tileDatas.LastOrDefault();
    }

    public void SetNeighbors(int[] left, int[] right, int[] up, int[] down)
    {
        neighbors = new List<int[]> { left, right, up, down };
    }

    public void SetNeighbors(List<int[]> neighbors) => this.neighbors = neighbors;

    public int EntropyNumber() => entropyIntoOptions?.Count ?? 0;

    public void DeleteConnectionOption(TileOptions option)
    {
        entropyIntoOptions?.Remove(option);
    }
}


