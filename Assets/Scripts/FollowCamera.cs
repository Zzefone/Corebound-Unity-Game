using UnityEngine;

namespace CoreRunner
{
    public sealed class FollowCamera : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0, 12.5f, -11.5f);
        Vector3 velocity;
        void Start() { if (target) transform.position = target.position + offset; }
        void LateUpdate()
        {
            if (!target || (GameManager.Instance != null && GameManager.Instance.Paused)) return;
            Vector3 focus = new Vector3(target.position.x * .38f, Mathf.Max(target.position.y, .6f), target.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, focus + offset, ref velocity, .16f);
            transform.rotation = Quaternion.Euler(48, 0, 0);
        }
    }
}
