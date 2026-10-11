using UnityEngine;

namespace CoreRunner
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class MovingHazard : MonoBehaviour
    {
        public float distance = 4;
        public float speed = 1.2f;
        public float phase;
        Vector3 origin;
        Rigidbody body;
        void Awake() { origin = transform.position; body = GetComponent<Rigidbody>(); }
        void FixedUpdate() => body.MovePosition(origin + Vector3.right * Mathf.Sin(Time.time * speed + phase) * distance);
    }
}
