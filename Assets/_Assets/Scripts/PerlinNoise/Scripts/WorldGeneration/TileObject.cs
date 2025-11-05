using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileObject : MonoBehaviour
{
    public Tile tile;

    public void SetTile(Tile tile)
    {
        this.tile = tile;
        Debug.Log("Connect");
    }
}
