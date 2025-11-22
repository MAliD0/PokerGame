using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public struct DictEntry : INetworkSerializable
{
    public V2I Key;
    public List<V2I> Values; // HashSet → List

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        // 1) Ключ
        serializer.SerializeValue(ref Key);

        // 2) Список значений
        int count = Values?.Count ?? 0;
        serializer.SerializeValue(ref count);

        if (serializer.IsReader)
            Values = new List<V2I>(count);

        for (int i = 0; i < count; i++)
        {
            V2I v = default;

            if (serializer.IsWriter)
                v = Values[i];

            serializer.SerializeValue(ref v);

            if (serializer.IsReader)
                Values.Add(v);
        }
    }

    public static List<DictEntry> SerializeDictionary(Dictionary<Vector2Int, HashSet<Vector2Int>> dict)
    {
        var list = new List<DictEntry>(dict.Count);

        foreach (var kvp in dict)
        {
            var entry = new DictEntry
            {
                Key = kvp.Key,
                Values = new List<V2I>(kvp.Value.Count)
            };

            foreach (var v in kvp.Value)
                entry.Values.Add(v);   // Vector2Int -> V2I (implicit)

            list.Add(entry);
        }

        return list;
    }
    public static Dictionary<Vector2Int, HashSet<Vector2Int>> DictEntryToDictionary(List<DictEntry> list)
    {
        var result = new Dictionary<Vector2Int, HashSet<Vector2Int>>(list.Count);

        foreach (var entry in list)
        {
            var set = new HashSet<Vector2Int>();

            foreach (var v in entry.Values)
                set.Add(v); // implicit V2I → Vector2Int

            result[entry.Key] = set;
        }

        return result;
    }
}
