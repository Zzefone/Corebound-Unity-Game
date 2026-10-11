using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CoreRunner
{
    public sealed class MenuController : MonoBehaviour
    {
        public Button startButton;
        public Text loadingText;
        bool loading;
        void Awake() { Time.timeScale = 1; }
        public void StartGame()
        {
            if (loading) return;
            loading = true;
            startButton.interactable = false;
            loadingText.text = "CONNECTING...";
            SceneManager.LoadSceneAsync("Game");
        }
        public void Quit()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
