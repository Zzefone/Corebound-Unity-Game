using UnityEngine;
using UnityEngine.UI;

namespace CoreRunner
{
    public sealed class GameHUD : MonoBehaviour
    {
        public Text cores, timer, falls, boostLabel, message, stage, victoryStats;
        public Image boostFill;
        public GameObject pausePanel, victoryPanel;
        Canvas[] floorLabels;
        void Awake()
        {
            floorLabels = System.Array.FindAll(FindObjectsByType<Canvas>(FindObjectsSortMode.None), c => c.renderMode == RenderMode.WorldSpace);
        }
        void Update()
        {
            var g = GameManager.Instance;
            if (g == null) return;
            foreach (var label in floorLabels) label.enabled = !g.Paused && !g.Won;
            cores.text = "CORES  " + g.Collected + " / " + g.requiredCores;
            timer.text = FormatTime(g.Elapsed);
            falls.text = "RESETS  " + g.Falls;
            float ready = g.player.BoostReady;
            boostFill.rectTransform.anchorMax = new Vector2(.06f + .88f * ready, .25f);
            boostLabel.text = ready >= 1 ? "SPACE  /  BOOST READY" : "BOOST RECHARGING";
            message.text = g.Paused || g.Won ? "" : g.Message;
            float z = g.player.transform.position.z;
            stage.text = z < 22 ? "01 / ARRIVAL" : z < 47 ? "02 / THE GAUNTLET" : "03 / REACTOR APPROACH";
        }
        public static string FormatTime(float seconds) => ((int)seconds / 60).ToString("00") + ":" + ((int)seconds % 60).ToString("00");
        public void ShowPause(bool show) => pausePanel.SetActive(show);
        public void ShowVictory()
        {
            var g = GameManager.Instance;
            victoryStats.text = "8 CORES RECOVERED    /    " + FormatTime(g.Elapsed) + "    /    " + g.Falls + " RESETS";
            victoryPanel.SetActive(true);
        }
    }
}
