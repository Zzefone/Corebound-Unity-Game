using UnityEngine;

namespace CoreRunner
{
    public sealed class CoreSpawner : MonoBehaviour
    {
        public GameObject corePrefab;
        public Vector3[] positions;
        void Start()
        {
            foreach (var p in positions) Instantiate(corePrefab, p, Quaternion.identity, transform);
        }
    }
}
