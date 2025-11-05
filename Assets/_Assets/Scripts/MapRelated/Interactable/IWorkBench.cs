using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IWorkBench : IInteractable
{
    public void OnInteract(GameObject interactor, ulong interacterId)
    {
        interactor.GetComponent<PlayerManager>();
    }

}
