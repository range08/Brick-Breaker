using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Updates scene-authored HUD elements. UI objects are intentionally owned by
/// the scene so their layout and visual design remain editable in the Inspector.
/// </summary>
public class GameHud : MonoBehaviour
{
    [Header("Scene UI References")]
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private TMP_Text ballCountText;
    [SerializeField] private Button pauseButton;
    [SerializeField] private TMP_Text pauseButtonText;
    [SerializeField] private GameObject startScreen;
    [SerializeField] private Button startButton;

    public TMP_Text RoundText => roundText;
    public TMP_Text BallCountText => ballCountText;
    public Button PauseButton => pauseButton;
    public Button StartButton => startButton;

    public void Initialize()
    {
        // The hierarchy, components, and visual properties are authored in the
        // scene. Initialize only caches a serialized child label if one was not
        // explicitly assigned; it never creates or styles UI objects.
        if (pauseButtonText == null && pauseButton != null)
            pauseButtonText = pauseButton.GetComponentInChildren<TMP_Text>(true);

        if (startButton == null && startScreen != null)
            startButton = startScreen.GetComponentInChildren<Button>(true);
    }

    public void Refresh(int round, int permanentBallCount)
    {
        if (roundText != null)
            roundText.text = $"ROUND {round:00}";

        if (ballCountText != null)
            ballCountText.text = $"BALLS {permanentBallCount:00}";
    }

    public void SetPauseCallback(UnityAction callback)
    {
        if (pauseButton == null)
            return;

        pauseButton.onClick.RemoveAllListeners();

        if (callback != null)
            pauseButton.onClick.AddListener(callback);
    }

    public void SetPauseVisible(bool visible)
    {
        if (pauseButton != null)
            pauseButton.gameObject.SetActive(visible);
    }

    public void SetPaused(bool paused)
    {
        if (pauseButtonText != null)
            pauseButtonText.text = paused ? "RESUME" : "PAUSE";
    }

    public void SetStartCallback(UnityAction callback)
    {
        if (startButton == null)
            return;

        startButton.onClick.RemoveAllListeners();

        if (callback != null)
            startButton.onClick.AddListener(callback);
    }

    public void ShowStartScreen()
    {
        if (startScreen != null)
            startScreen.SetActive(true);

        SetPauseVisible(false);
    }

    public void HideStartScreen()
    {
        if (startScreen != null)
            startScreen.SetActive(false);
    }

}
