using UnityEngine;

namespace CoreRunner
{
    public sealed class FinishGate : MonoBehaviour
    {
        void OnTriggerEnter(Collider other)
        { if (other.GetComponent<PlayerController>() && GameManager.Instance != null) GameManager.Instance.TryFinish(); }
    }
}
