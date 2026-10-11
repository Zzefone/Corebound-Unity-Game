using System.Collections;
using UnityEngine;

namespace CoreRunner
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerController : MonoBehaviour
    {
        public float moveSpeed = 7.5f;
        public float acceleration = 34f;
        public float boostImpulse = 9.5f;
        public float boostLift = 4.3f;
        public float boostCooldown = 1.8f;
        public Rigidbody Body { get; private set; }
        public bool IsRespawning { get; private set; }
        public bool Grounded => Physics.SphereCast(transform.position, 0.43f, Vector3.down,
            out _, 0.25f, ~ (1 << 2), QueryTriggerInteraction.Ignore);
        public float BoostReady => Mathf.Clamp01((Time.time - lastBoost) / boostCooldown);
        public Vector3 Checkpoint { get; private set; }
        float lastBoost = -100f;
        float boostEnd;
        Vector3 input;
        Vector3 lastDirection = Vector3.forward;

        void Awake()
        {
            Body = GetComponent<Rigidbody>();
            Checkpoint = transform.position;
        }

        void Update()
        {
            var game = GameManager.Instance;
            if (game == null || !game.IsPlaying || IsRespawning) { input = Vector3.zero; return; }
            float x = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0)
                    - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
            float z = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0)
                    - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0);
            input = Vector3.ClampMagnitude(new Vector3(x, 0, z), 1);
            if (input.sqrMagnitude > 0.01f) lastDirection = input.normalized;
            if (Input.GetKeyDown(KeyCode.Space)) TryBoost(lastDirection);
            if (Input.GetKeyDown(KeyCode.R)) Respawn();
            if (transform.position.y < -7) Respawn();
        }

        void FixedUpdate()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying || IsRespawning) return;
            Vector3 horizontal = new Vector3(Body.linearVelocity.x, 0, Body.linearVelocity.z);
            // Acceleration keeps normal movement physical; a short grace period preserves the impulse.
            if (Time.time >= boostEnd)
            {
                Vector3 change = input * moveSpeed - horizontal;
                Body.AddForce(Vector3.ClampMagnitude(change / Time.fixedDeltaTime, acceleration), ForceMode.Acceleration);
            }
            else if (input.sqrMagnitude > 0)
                Body.AddForce(input * 8f, ForceMode.Acceleration);
            horizontal = new Vector3(Body.linearVelocity.x, 0, Body.linearVelocity.z);
            if (horizontal.magnitude > 18)
                Body.linearVelocity = horizontal.normalized * 18 + Vector3.up * Body.linearVelocity.y;
        }

        public bool TryBoost(Vector3 direction)
        {
            if (IsRespawning || GameManager.Instance == null || !GameManager.Instance.IsPlaying
                || BoostReady < 1 || !Grounded) return false;
            direction.y = 0;
            if (direction.sqrMagnitude < .01f) direction = Vector3.forward;
            Body.AddForce(direction.normalized * boostImpulse + Vector3.up * boostLift, ForceMode.Impulse);
            lastBoost = Time.time;
            boostEnd = Time.time + .55f;
            GameManager.Instance.Play(GameManager.Instance.boostSound);
            GameManager.Instance.Burst(transform.position, new Color(.1f, .85f, 1f));
            return true;
        }

        public void SetCheckpoint(Vector3 position) => Checkpoint = position;
        public void Respawn()
        {
            if (!IsRespawning && GameManager.Instance != null && GameManager.Instance.IsPlaying)
                StartCoroutine(RespawnRoutine());
        }
        IEnumerator RespawnRoutine()
        {
            IsRespawning = true;
            GameManager.Instance.RecordFall();
            GameManager.Instance.Play(GameManager.Instance.fallSound);
            Body.isKinematic = true;
            yield return new WaitForSeconds(.45f);
            Body.position = Checkpoint;
            Body.rotation = Quaternion.identity;
            Body.isKinematic = false;
            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            lastBoost = -100;
            boostEnd = 0;
            IsRespawning = false;
        }
        void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.GetComponent<MovingHazard>() != null) Respawn();
        }
    }
}
