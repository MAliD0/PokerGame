namespace NoiseGeneration
{
    using Sirenix.OdinInspector;
    using UnityEngine;

    [CreateAssetMenu(fileName = "Noise settings")]
    public class NoiseSettings : ScriptableObject
    {
        [FoldoutGroup("Noise Settings")]
        [HorizontalGroup("Noise Settings/Size", Title = "Map size")]
        [SerializeField] public int mapWidth;
        [HorizontalGroup("Noise Settings/Size", Title = "Map size")]
        [SerializeField] public int mapHeight;
        [FoldoutGroup("Noise Settings")]
        [SerializeField] public float scale;
        [FoldoutGroup("Noise Settings")]
        [SerializeField] public float pow;
        [FoldoutGroup("Noise Settings")]
        [Range(0, 6)]
        [SerializeField] public int octaves;
        [FoldoutGroup("Noise Settings")]
        [Range(0f, 1f)]
        [SerializeField] public float persistance;
        [FoldoutGroup("Noise Settings")]
        [SerializeField] public float lacunarity;
        [FoldoutGroup("Noise Settings")]
        [SerializeField] public Vector2 offset;
    }

}
