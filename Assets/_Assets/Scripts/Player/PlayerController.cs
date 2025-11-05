using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5f;           // Maximum movement speed
    public float acceleration = 10f;   // How quickly the player accelerates
    public float deceleration = 10f;   // How quickly the player slows down

    public Rigidbody2D rb;
    private Vector2 currentVelocity = Vector2.zero;
    private Vector2 inputDirection = Vector2.zero;

    public void Move(float inputX, float inputY)
    {
        // Normalize to ensure the same movement speed in every direction.
        inputDirection = new Vector2(inputX, inputY).normalized;

        // Calculate the target velocity based on input.
        Vector2 targetVelocity = inputDirection * speed;

        // Smoothly accelerate (or decelerate) towards the target velocity.
        if (inputDirection != Vector2.zero)
        {
            currentVelocity = Vector2.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.deltaTime);
        }
        else
        {
            currentVelocity = Vector2.MoveTowards(currentVelocity, Vector2.zero, deceleration * Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        rb.velocity = currentVelocity;
    }
}