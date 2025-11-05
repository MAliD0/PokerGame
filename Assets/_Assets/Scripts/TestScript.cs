using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TestScript : MonoBehaviour
{
    [SerializeField] WorldMapManager mapManager;
    [Space]
    [InlineButton("SetTile","SetTile")] public Vector2Int tilePos; public MapBlockData mapBlockData; 

    public void SetTile()
    {
        mapManager.SetTileRequestServerRpc(tilePos, mapBlockData.GetItemID());
    }
}
