using UnityEngine;

namespace CutFreePets.Rope
{
    /// <summary>
    /// Attach point for ropes on pegs, hooks, obstacles, or pets.
    /// Orientates visual knots/rings towards rope pull direction.
    /// </summary>
    public class RopeAnchor : MonoBehaviour
    {
        [Tooltip("Optional transform of visual ring/knot to align with rope pull direction.")]
        [SerializeField] private Transform visualKnot;

        [Tooltip("Reference to the attached rope.")]
        [SerializeField] private VerletRope attachedRope;

        [Tooltip("True if this is the start anchor of the rope, false if end anchor.")]
        [SerializeField] private bool isStartAnchor = true;

        private void LateUpdate()
        {
            if (visualKnot == null || attachedRope == null || attachedRope.Particles.Count < 2) return;

            // Orient the knot towards the first/last free rope particle
            Vector3 ropeDir = isStartAnchor
                ? (attachedRope.Particles[1].Position - transform.position).normalized
                : (attachedRope.Particles[attachedRope.Particles.Count - 2].Position - transform.position).normalized;

            if (ropeDir.sqrMagnitude > 0.001f)
            {
                visualKnot.rotation = Quaternion.LookRotation(ropeDir, Vector3.up);
            }
        }

        public void BindRope(VerletRope rope, bool asStartAnchor)
        {
            attachedRope = rope;
            isStartAnchor = asStartAnchor;
        }
    }
}
