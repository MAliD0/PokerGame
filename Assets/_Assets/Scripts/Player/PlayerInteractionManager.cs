using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerInteractionManager : MonoBehaviour
{
    [SerializeField] Transform originPoint;
    [SerializeField] float radius;

    public IInteractable[] CastForInteractables(Vector2 position, float radius)
    {
        //RaycastHit2D[] hits = Physics2D.CircleCastAll(position, radius, Vector2.down);
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, radius);


        List<IInteractable> interactables = new List<IInteractable>();

        foreach (Collider2D hit in hits)
        {
            if(hit.gameObject.TryGetComponent<IInteractable>(out IInteractable interactable))
            {
                interactables.Add(interactable);    
            }
        }

        //interactables.Reverse();

        return interactables.ToArray();
    }
}
