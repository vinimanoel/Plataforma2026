using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Assign the Move action (Vector2) from your Input Actions asset.")]
    public InputActionReference moveAction;
    [Tooltip("Assign the Jump action (Button) from your Input Actions asset.")]
    public InputActionReference jumpAction;

    [Header("Movement")]
    [Tooltip("Strength of torque applied to roll the ball.")]
    public float torqueStrength = 5f;
    [Tooltip("Maximum angular velocity allowed on the rigidbody.")]
    public float maxAngularVelocity = 25f;
    [Tooltip("Max horizontal linear speed (m/s).")]
    public float maxLinearSpeed = 8f;

    [Header("Jump")]
    [Tooltip("Impulse force applied when jumping.")]
    public float jumpForce = 5f;
    [Tooltip("Layers that count as ground for the grounded check.")]
    public LayerMask groundLayers = ~0; // default: everything
    [Tooltip("Extra distance used for the ground raycast/test.")]
    public float groundTolerance = 0.05f;

    Rigidbody rb;
    SphereCollider sphereCollider;
    bool isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        sphereCollider = GetComponent<SphereCollider>();
        if (rb == null)
        {
            Debug.LogError("PlayerController requires a Rigidbody.", this);
            enabled = false;
            return;
        }

        // Keep Rigidbody settings reasonable for a rolling ball
        rb.maxAngularVelocity = maxAngularVelocity;
        rb.constraints = RigidbodyConstraints.None;
    }

    void OnEnable()
    {
        if (moveAction?.action != null)
            moveAction.action.Enable();

        if (jumpAction?.action != null)
        {
            jumpAction.action.Enable();
            jumpAction.action.performed += OnJumpPerformed;
        }
    }

    void OnDisable()
    {
        if (moveAction?.action != null)
            moveAction.action.Disable();

        if (jumpAction?.action != null)
        {
            jumpAction.action.performed -= OnJumpPerformed;
            jumpAction.action.Disable();
        }
    }

    void FixedUpdate()
    {
        UpdateGrounded();

        Vector2 input = Vector2.zero;
        if (moveAction?.action != null)
            input = moveAction.action.ReadValue<Vector2>();

        // Convert 2D input (x = right, y = forward) into torque applied to the ball.
        // The exact sign may be tuned depending on your scene orientation.
        Vector3 torque = new Vector3(-input.y, 0f, input.x) * torqueStrength;
        rb.AddTorque(torque, ForceMode.Force);

        // Limit horizontal linear speed
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (horizontalVelocity.magnitude > maxLinearSpeed)
        {
            Vector3 limited = horizontalVelocity.normalized * maxLinearSpeed;
            rb.linearVelocity = new Vector3(limited.x, rb.linearVelocity.y, limited.z);
        }

        // Keep rb.maxAngularVelocity synced with inspector if changed at runtime
        if (rb.maxAngularVelocity != maxAngularVelocity)
            rb.maxAngularVelocity = maxAngularVelocity;
    }

    void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        if (!isGrounded)
            return;

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    void UpdateGrounded()
    {
        // If the object has a SphereCollider, use its radius to test for ground.
        float radius = 0.5f;
        if (sphereCollider != null)
        {
            // Account for lossyScale
            float maxScale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
            radius = sphereCollider.radius * maxScale;
        }

        // Raycast down from center to check if within (radius + tolerance) of ground
        float checkDistance = radius + groundTolerance;
        isGrounded = Physics.Raycast(transform.position, Vector3.down, checkDistance, groundLayers.value);
    }

    void OnDrawGizmosSelected()
    {
        // Visualize ground check ray
        SphereCollider sc = sphereCollider;
        if (sc == null)
            sc = GetComponent<SphereCollider>();

        float radius = 0.5f;
        if (sc != null)
        {
            float maxScale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
            radius = sc.radius * maxScale;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * (radius + groundTolerance));
        Gizmos.DrawWireSphere(transform.position + Vector3.down * (radius + groundTolerance), 0.02f);
    }
}

