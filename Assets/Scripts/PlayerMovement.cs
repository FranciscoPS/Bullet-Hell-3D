using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private float moveSpeed = 10f;
    private Rigidbody rb;
    private PlayerInput playerInput;
    private Vector2 input;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();
    }

    private void Update()
    {
        input = playerInput.actions["Move"].ReadValue<Vector2>();
        Debug.Log(input);
    }

    private void FixedUpdate()
    {
        rb.AddForce(new Vector3(input.x, 0f, input.y) * moveSpeed);
    }

}
