using UnityEngine;

public class StartButton : MonoBehaviour
{
    public GameTimer gameTimer;
    public GameManager gameManager;
    public EyeTracking eyeTracker;
    public float holdTime = 1.0f;

    private float timer = 0f;
    private bool isTouching = false;
    private bool triggered = false;

    public void OnTouchStarted()
    {
        isTouching = true;
        timer = 0f;
        triggered = false;
        Debug.Log("OnTouchStarted");
    }

    public void OnTouchCompleted()
    {
        isTouching = false;
        timer = 0f;
        Debug.Log("OnTouchCompleted");
    }

    void Update()
    {
        if (!isTouching || triggered) return;

        timer += Time.deltaTime;

        if (timer >= holdTime)
        {
            triggered = true;
            isTouching = false;

            gameTimer.StartCountdown(); // 👈 DEIN bestehender Flow bleibt!
            eyeTracker.ClearData();
            gameManager.gazeLogs.Clear();
            gameManager.bubbleLogs.Clear();
        }
    }
}
