using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
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
    [Tooltip("If true, movement input is relative to the camera's orientation. If false, uses world axes.")]
    public bool useCameraRelative = true;
    [Tooltip("Optional camera transform to use for camera-relative movement. If null, Camera.main is used.")]
    public Transform cameraTransform;

    [Header("Jump")]
    [Tooltip("Impulse force applied when jumping.")]
    public float jumpForce = 5f;
    [Tooltip("Layers that count as ground for the grounded check.")]
    public LayerMask groundLayers = ~0; // default: everything
    [Tooltip("Extra distance used for the ground raycast/test.")]
    public float groundTolerance = 0.05f;

    Rigidbody _rb;
    SphereCollider _sphereCollider;
    bool _isGrounded;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _sphereCollider = GetComponent<SphereCollider>();
        if (_rb == null)
        {
            Debug.LogError("PlayerController requires a Rigidbody.", this);
            enabled = false;
            return;
        }

        // Keep Rigidbody settings reasonable for a rolling ball
        _rb.maxAngularVelocity = maxAngularVelocity;
        _rb.constraints = RigidbodyConstraints.None;
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
        // If camera-relative movement is enabled, map input into world-space using the camera's forward/right projected onto XZ plane.
        Vector3 desiredDir;
        if (useCameraRelative)
        {
            Transform cam = cameraTransform != null ? cameraTransform : Camera.main != null ? Camera.main.transform : null;
            if (cam != null)
            {
                Vector3 camForward = cam.forward;
                camForward.y = 0f;
                camForward.Normalize();
                Vector3 camRight = cam.right;
                camRight.y = 0f;
                camRight.Normalize();
                desiredDir = camForward * input.y + camRight * input.x;
            }
            else
            {
                // fallback to world-relative mapping
                desiredDir = new Vector3(input.x, 0f, input.y);
            }
        }
        else
        {
            desiredDir = new Vector3(input.x, 0f, input.y);
        }

        // Convert desired direction into torque that rolls the ball in that direction.
        Vector3 torque = Vector3.zero;
        if (desiredDir.sqrMagnitude > 1e-6f)
        {
            torque = Vector3.Cross(desiredDir.normalized, Vector3.up) * torqueStrength;
        }
        _rb.AddTorque(torque, ForceMode.Force);

        // Limit horizontal linear speed
        // Limit horizontal linear speed
        Vector3 horizontalVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        if (horizontalVelocity.magnitude > maxLinearSpeed)
        {
            Vector3 limited = horizontalVelocity.normalized * maxLinearSpeed;
            _rb.linearVelocity = new Vector3(limited.x, _rb.linearVelocity.y, limited.z);
        }

        // Keep rb.maxAngularVelocity synced with inspector if changed at runtime
        if (!Mathf.Approximately(_rb.maxAngularVelocity, maxAngularVelocity))
            _rb.maxAngularVelocity = maxAngularVelocity;
    }

    void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        if (!_isGrounded)
            return;

        _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    void UpdateGrounded()
    {
        // If the object has a SphereCollider, use its radius to test for ground.
        float radius = 0.5f;
        if (_sphereCollider != null)
        {
            // Account for lossyScale
            float maxScale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
            radius = _sphereCollider.radius * maxScale;
        }

        // Raycast down from center to check if within (radius + tolerance) of ground
        float checkDistance = radius + groundTolerance;
        _isGrounded = Physics.Raycast(transform.position, Vector3.down, checkDistance, groundLayers.value);
    }

    void OnDrawGizmosSelected()
    {
        // Visualize ground check ray
        SphereCollider sc = _sphereCollider;
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

