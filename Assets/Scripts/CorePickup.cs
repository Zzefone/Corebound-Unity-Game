using UnityEngine;

namespace CoreRunner
{
    public sealed class CorePickup : MonoBehaviour
    {
        public Transform visual;
        Vector3 initial;
        bool collected;
        void Start() { if (visual) initial = visual.localPosition; }
        void Update()
        {
            if (!visual) return;
            visual.Rotate(0, 75 * Time.deltaTime, 0, Space.World);
            visual.localPosition = initial + Vector3.up * Mathf.Sin(Time.time * 2.5f + transform.position.z) * .12f;
        }
        void OnTriggerEnter(Collider other)
        {
            if (collected || !other.GetComponent<PlayerController>() || GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
            collected = true;
            GameManager.Instance.Collect();
            GameManager.Instance.Burst(transform.position, new Color(.1f, 1, .8f));
            Destroy(gameObject);
        }
    }
}
