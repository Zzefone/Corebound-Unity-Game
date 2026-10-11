using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoreRunner
{
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public PlayerController player;
        public GameHUD hud;
        public ParticleSystem burstPrefab;
        public AudioClip collectSound, boostSound, fallSound, checkpointSound, winSound;
        public int requiredCores = 8;
        public int Collected { get; private set; }
        public int Falls { get; private set; }
        public float Elapsed { get; private set; }
        public bool Won { get; private set; }
        public bool Paused { get; private set; }
        public bool IsPlaying => !Won && !Paused;
        public string Message { get; private set; }
        float messageUntil;
        AudioSource audioSource;

        void Awake()
        {
            Instance = this;
            Time.timeScale = 1;
            audioSource = GetComponent<AudioSource>();
            ShowMessage("Restore the station. Collect all 8 cores.", 5);
        }
        void Update()
        {
            if (IsPlaying) Elapsed += Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.Escape) && !Won) TogglePause();
            if (Time.unscaledTime > messageUntil) Message = "";
        }
        void OnDestroy() { if (Instance == this) Instance = null; Time.timeScale = 1; }
        public void Collect()
        {
            if (!IsPlaying) return;
            Collected++;
            Play(collectSound);
            if (Collected == requiredCores) ShowMessage("All cores recovered. Reach the orange reactor!", 6);
        }
        public bool TryFinish()
        {
            if (Won) return true;
            if (!IsPlaying) return false;
            if (Collected < requiredCores)
            {
                ShowMessage("Reactor locked: recover " + (requiredCores - Collected) + " more cores.", 3);
                return false;
            }
            Won = true;
            Message = "";
            player.Body.linearVelocity = Vector3.zero;
            player.Body.angularVelocity = Vector3.zero;
            player.Body.isKinematic = true;
            Play(winSound);
            Burst(player.transform.position, new Color(1, .55f, .15f));
            hud.ShowVictory();
            return true;
        }
        public void RecordFall() { Falls++; ShowMessage("Signal lost. Returning to checkpoint...", 2); }
        public void ShowMessage(string text, float seconds = 3)
        { Message = text; messageUntil = Time.unscaledTime + seconds; }
        public void Play(AudioClip clip) { if (clip && audioSource) audioSource.PlayOneShot(clip); }
        public void Burst(Vector3 position, Color color)
        {
            if (!burstPrefab) return;
            var p = Instantiate(burstPrefab, position, Quaternion.identity);
            var main = p.main; main.startColor = color;
            p.Play(); Destroy(p.gameObject, 3);
        }
        public void TogglePause()
        {
            if (Won) return;
            Paused = !Paused;
            Time.timeScale = Paused ? 0 : 1;
            hud.ShowPause(Paused);
        }
        public void Restart() { Time.timeScale = 1; SceneManager.LoadScene("Game"); }
        public void Menu() { Time.timeScale = 1; SceneManager.LoadScene("MainMenu"); }
        public void Quit()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
