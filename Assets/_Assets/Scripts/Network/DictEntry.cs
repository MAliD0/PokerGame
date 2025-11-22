using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public struct DictEntry : INetworkSerializable
{
    public Vector2Int Key;
    public List<Vector2Int> Values; // HashSet → List

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        int count = Values?.Count ?? 0;
        serializer.SerializeValue(ref count);

        if (serializer.IsReader)
            Values = new List<Vector2Int>(count);

        for (int i = 0; i < count; i++)
        {
            Vector2Int v = default;

            if (serializer.IsWriter)
                v = Values[i];

            serializer.SerializeValue(ref v);

            if (serializer.IsReader)
                Values.Add(v);
        }
    }

    public List<DictEntry> SerializeDictionary(Dictionary<Vector2Int, HashSet<Vector2Int>> dict)
    {
        var list = new List<DictEntry>(dict.Count);

        foreach (var kvp in dict)
        {
            list.Add(new DictEntry
            {
                Key = kvp.Key,
                Values = new List<Vector2Int>(kvp.Value)
            });
        }

        return list;
    }
    public Dictionary<Vector2Int, HashSet<Vector2Int>> DictEntryToDictionary(List<DictEntry> list)
    {
        Dictionary<Vector2Int, HashSet<Vector2Int>> result =
            new Dictionary<Vector2Int, HashSet<Vector2Int>>(list.Count);

        foreach (var entry in list)
        {
            result[entry.Key] = new HashSet<Vector2Int>(entry.Values);
        }

        return result;
    }
}
