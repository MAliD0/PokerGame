using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] SpriteRenderer spriteRenderer;

    public void FlipSprite(bool tf)
    {
        //spriteRenderer.flipX = tf;
        spriteRenderer.transform.localScale = new Vector3(tf ? -1:1, 1,1);
    }
    public void PlayAnimation(string name)
    {
        animator.Play(name);
    }

    public void SetBool(string name, bool tf)
    {
        animator.SetBool(name, tf);
    }
}
