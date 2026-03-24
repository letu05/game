using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f; // Player movement speed
    private Rigidbody2D rb;
    private Vector2 movement;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        float x = getInput("Horizontal");
        movement = new Vector2(x, 0).normalized;
    }

    void FixedUpdate()
    {
        // Move the player
        rb.linearVelocity = new Vector2(movement.x * speed, rb.linearVelocity.y);
    }

    float getInput(string inputName)
    {
        return Input.GetAxis(inputName);
    }
}
