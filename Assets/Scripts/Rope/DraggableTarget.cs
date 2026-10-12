using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CutFreePets.Rope
{
    /// <summary>
    /// Attach this to test dragging objects with New Input System and legacy Input support.
    /// </summary>
    public class DraggableTarget : MonoBehaviour
    {
        private Camera mainCamera;
        private Rigidbody rb3D;
        private Rigidbody2D rb2D;
        private bool isDragging;
        private Vector3 offset;
        private float distanceZ;

        private void Awake()
        {
            mainCamera = Camera.main;
            rb3D = GetComponent<Rigidbody>();
            rb2D = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            Vector2 screenPos = Vector2.zero;
            bool isDown = false;
            bool isHeld = false;
            bool isUp = false;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                screenPos = Mouse.current.position.ReadValue();
                isDown = Mouse.current.leftButton.wasPressedThisFrame;
                isHeld = Mouse.current.leftButton.isPressed;
                isUp = Mouse.current.leftButton.wasReleasedThisFrame;
            }
            else if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
            {
                var touch = Touchscreen.current.touches[0];
                screenPos = touch.position.ReadValue();
                isDown = touch.press.wasPressedThisFrame;
                isHeld = touch.press.isPressed;
                isUp = touch.press.wasReleasedThisFrame;
            }
#else
            screenPos = Input.mousePosition;
            isDown = Input.GetMouseButtonDown(0);
            isHeld = Input.GetMouseButton(0);
            isUp = Input.GetMouseButtonUp(0);
#endif

            if (isDown)
            {
                Ray ray = mainCamera.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0f));
                RaycastHit2D hit2D = Physics2D.Raycast(ray.origin, ray.direction);
                if (hit2D.collider != null && hit2D.transform == transform)
                {
                    StartDrag(screenPos);
                }
            }
            else if (isHeld && isDragging)
            {
                Vector3 world = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, distanceZ));
                Vector3 target = world + offset;
                target.z = 0f;
                transform.position = target;
            }
            else if (isUp && isDragging)
            {
                StopDrag();
            }
        }

        private void StartDrag(Vector2 screenPos)
        {
            isDragging = true;
            distanceZ = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
            Vector3 world = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, distanceZ));
            offset = transform.position - world;

            if (rb3D != null) rb3D.isKinematic = true;
            if (rb2D != null) rb2D.bodyType = RigidbodyType2D.Kinematic;
        }

        private void StopDrag()
        {
            isDragging = false;
            if (rb3D != null) rb3D.isKinematic = false;
            if (rb2D != null) rb2D.bodyType = RigidbodyType2D.Dynamic;
        }
    }
}
