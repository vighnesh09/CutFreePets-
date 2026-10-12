using UnityEngine;

namespace CutFreePets.Rope
{
    /// <summary>
    /// Interactive test controller for verifying rope physics, stretching, tension feedback,
    /// 3D obstacle collision, and swipe-cutting.
    /// </summary>
    public class RopeDemoController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private VerletRope testRope;
        [SerializeField] private Transform movableTarget;
        [SerializeField] private Camera mainCamera;

        [Header("Interaction Settings")]
        [SerializeField] private LayerMask draggableLayer;
        [SerializeField] private float cutRadius = 0.25f;

        private Transform currentDraggedTransform;
        private Vector3 dragOffset;
        private float dragDistanceZ;
        private Vector3 previousMousePos;

        private void Start()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (testRope == null) testRope = FindFirstObjectByType<VerletRope>();
        }

        private void Update()
        {
            HandleDragging();
            HandleSwipeCutting();

            // Press 'R' to reload/reset the test rope
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetRope();
            }
        }

        private void HandleDragging()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 100f, draggableLayer))
                {
                    currentDraggedTransform = hit.transform;
                    dragDistanceZ = hit.distance;
                    dragOffset = hit.transform.position - hit.point;
                }
            }
            else if (Input.GetMouseButton(0) && currentDraggedTransform != null)
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                Vector3 targetPoint = ray.GetPoint(dragDistanceZ) + dragOffset;
                currentDraggedTransform.position = targetPoint;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                currentDraggedTransform = null;
            }
        }

        private void HandleSwipeCutting()
        {
            // Right-click or drag without hitting a draggable object to cut
            if (Input.GetMouseButton(1) || (Input.GetMouseButton(0) && currentDraggedTransform == null))
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                // Cast onto the rope plane (Z = 0 or testRope plane)
                Plane ropePlane = new Plane(Vector3.forward, testRope != null ? testRope.StartPosition : Vector3.zero);
                if (ropePlane.Raycast(ray, out float enter))
                {
                    Vector3 cutPoint = ray.GetPoint(enter);
                    if (testRope != null && !testRope.IsCut)
                    {
                        testRope.CutAtPoint(cutPoint, cutRadius);
                    }
                }
            }
        }

        public void ResetRope()
        {
            if (testRope != null)
            {
                // Destroy any severed pieces
                foreach (var piece in FindObjectsByType<VerletRope>(FindObjectsSortMode.None))
                {
                    if (piece != testRope)
                    {
                        Destroy(piece.gameObject);
                    }
                }

                testRope.InitializeRope();
            }
        }
    }
}
