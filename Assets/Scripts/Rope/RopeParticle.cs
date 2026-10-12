using System;
using UnityEngine;

namespace CutFreePets.Rope
{
    /// <summary>
    /// Represents a single simulation particle along the rope.
    /// </summary>
    [Serializable]
    public class RopeParticle
    {
        public Vector3 Position;
        public Vector3 PreviousPosition;
        public Vector3 Acceleration;
        public bool IsPinned;
        public float Radius;

        public RopeParticle(Vector3 position, float radius = 0.05f)
        {
            Position = position;
            PreviousPosition = position;
            Acceleration = Vector3.zero;
            IsPinned = false;
            Radius = radius;
        }

        /// <summary>
        /// Updates particle position using Verlet integration with velocity damping.
        /// </summary>
        public void UpdateVerlet(float deltaTime, float damping)
        {
            if (IsPinned)
            {
                Acceleration = Vector3.zero;
                return;
            }

            Vector3 velocity = (Position - PreviousPosition) * damping;
            PreviousPosition = Position;
            Position += velocity + Acceleration * (deltaTime * deltaTime);
            Acceleration = Vector3.zero;
        }
    }
}
