using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UtillityMath {
    public static Vector2Int VectorToVectorInt(Vector2 vector2)
    {
        Vector2Int vector2Int = new Vector2Int();

        vector2Int.x = vector2.x < 0 ? -Mathf.CeilToInt(Mathf.Abs(vector2.x)) : (int)vector2.x;
        vector2Int.y = vector2.y < 0 ? -Mathf.CeilToInt(Mathf.Abs(vector2.y)) : (int)vector2.y;

        return vector2Int;
    }

}
