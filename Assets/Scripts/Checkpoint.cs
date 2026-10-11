using UnityEngine;

namespace CoreRunner
{
    public sealed class Checkpoint : MonoBehaviour
    {
        public Vector3 spawnPosition;
        public Renderer beacon;
        public Material activatedMaterial;
        bool activated;
        void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponent<PlayerController>();
            if (!player || activated || GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
            activated = true;
            player.SetCheckpoint(spawnPosition);
            if (beacon && activatedMaterial) beacon.sharedMaterial = activatedMaterial;
            GameManager.Instance.ShowMessage("Checkpoint linked. Collected cores are kept.", 3);
            GameManager.Instance.Play(GameManager.Instance.checkpointSound);
        }
    }
}
