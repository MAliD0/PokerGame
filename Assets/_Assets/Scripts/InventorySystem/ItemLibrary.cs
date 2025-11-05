using AYellowpaper.SerializedCollections;
using Sirenix.OdinInspector;
using UnityEngine;
[CreateAssetMenu(fileName = "ItemLibrary", menuName = "Map/ItemLibrary")]
    
public class ItemLibrary : ScriptableObject
{
    public SerializedDictionary<string, ItemData> dataLibrary;

    [Button("Generate Library")]
    public void GenerateLibrary()
    {
        ItemData[] assets = Resources.LoadAll<ItemData>("Items");
        foreach (ItemData asset in assets)
        {
            if (!dataLibrary.ContainsKey(asset.GetItemID()))
            {
                dataLibrary.Add(asset.GetItemID(), asset);
            }
            else
            {
                Debug.LogWarning($"Item {asset.GetItemID()} already has instance");
            }
        }
    }

    public ItemData GetMapBlockData(string id)
    {
        dataLibrary.TryGetValue(id, out ItemData itemData);
        return itemData;
    }
}
