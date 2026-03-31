using UnityEngine;
using UnityEngine.InputSystem;


/// <summary>
/// Legacy/simple roll-a-ball controller kept for reference.
/// Reads a Vector2 from keyboard (WASD + arrows) and sets rb.linearVelocity in FixedUpdate.
/// Prefer using the main `PlayerController` in `Assets/Scripts/PlayerController.cs` for full features.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class LegacySimpleController : MonoBehaviour
{
   [SerializeField]
   Rigidbody rb;


   [Tooltip("Horizontal movement speed (multiplies the input Vector2)")]
   [SerializeField]
   float moveSpeed = 5f;


   // InputAction created in code (no Input Actions asset required)
   InputAction _moveAction;


   Vector2 _moveInput = Vector2.zero;


   void Awake()
   {
       // Try to find a Rigidbody on the same GameObject if none assigned in the inspector
       if (rb == null)
           rb = GetComponent<Rigidbody>();


       // Create action and add keyboard 2D composite bindings for WASD and arrow keys
       // Note: the constructor parameters are (name, type, binding, interactions, processors, expectedControlType)
       // Passing "Vector2" into the wrong slot registers it as a processor (which doesn't exist) and
       // causes the runtime error "No InputProcessor with name 'Vector2'...". Provide null for processors
       // and set expectedControlType to "Vector2" instead.
       _moveAction = new InputAction("Move", InputActionType.Value, null, null, null, "Vector2");


       // WASD
       _moveAction.AddCompositeBinding("2DVector")
           .With("up", "<Keyboard>/w")
           .With("down", "<Keyboard>/s")
           .With("left", "<Keyboard>/a")
           .With("right", "<Keyboard>/d");


       // Arrow keys
       _moveAction.AddCompositeBinding("2DVector")
           .With("up", "<Keyboard>/upArrow")
           .With("down", "<Keyboard>/downArrow")
           .With("left", "<Keyboard>/leftArrow")
           .With("right", "<Keyboard>/rightArrow");
   }


   void OnEnable()
   {
       if (_moveAction != null)
           _moveAction.Enable();
   }


   void OnDisable()
   {
       if (_moveAction != null)
           _moveAction.Disable();
   }

   void OnDestroy()
   {
       // Dispose created InputAction to free native resources
       if (_moveAction != null)
       {
           _moveAction.Disable();
           _moveAction.Dispose();
           _moveAction = null;
       }
   }


   void Update()
   {
       // Read input every frame (keeps input responsive); apply in FixedUpdate for physics
       if (_moveAction != null)
           _moveInput = _moveAction.ReadValue<Vector2>();
   }


   void FixedUpdate()
   {
       if (rb == null)
           return;


       // Convert 2D input (x = horizontal, y = vertical) to world XZ movement
       Vector3 desired = new Vector3(_moveInput.x, rb.linearVelocity.y, _moveInput.y) * moveSpeed;


       // Set Rigidbody linearVelocity directly (preserve current Y velocity)
       rb.linearVelocity = desired;
   }


   // Expose a small API to set speed at runtime if needed
   public void SetMoveSpeed(float speed)
   {
       moveSpeed = speed;
   }
}







