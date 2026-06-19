using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;

public class CalibrationManager : MonoBehaviour
{
    public TextMeshProUGUI instructionText;
    public GameObject captureButton;

    [Header("Calibration Settings")]
    public float calibrationDuration = 30f;

    private float timer;
    private bool calibrating;

    //public float minX, maxX;
    public float minY, maxY;

    public void StartCalibration()
    {
        //minX = float.MaxValue;
        //maxX = float.MinValue;
        minY = float.MaxValue;
        maxY = float.MinValue;

        timer = calibrationDuration;
        calibrating = true;

        Debug.Log("🟢 Kalibrierung gestartet – Hände bewegen");
    }

    void Update()
    {
        if (!calibrating) return;

        timer -= Time.deltaTime;

        CaptureHand(Handedness.Left);
        CaptureHand(Handedness.Right);

        if (timer <= 0f)
        {
            calibrating = false;
            SaveCalibration();
            Debug.Log("✅ Kalibrierung beendet");
        }
    }

    public Vector3 center = new Vector3(0, 0, 0.5f);
    public Vector3 areaSize = new Vector3(1.2f, 1.0f, 1.0f);

    public void Reset()
    {
        //float centerY = Camera.main.transform.position.y;


        minY = center.y - areaSize.y / 2f;
        maxY = center.y + areaSize.y / 2f;

        PlayerPrefs.SetFloat("Calib_MinY", minY);
        PlayerPrefs.SetFloat("Calib_MaxY", maxY);
        PlayerPrefs.DeleteKey("Level_MinY");
        PlayerPrefs.DeleteKey("Level_MaxY");
        PlayerPrefs.Save();

        instructionText.text = "Kalibrierung zurückgesetzt";
        /*minY = 0f;
        maxY = 1f;
        Debug.Log("Reset Calibration");
        SaveCalibration();
        //instructionText.gameObject.SetActive(false);
        instructionText.text = "Kalibrierung zurückgesetzt";*/
    }

    void CaptureHand(Handedness hand)
    {
        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.Palm, hand, out MixedRealityPose pose))
        {
            Vector3 p = pose.Position;

            //minX = Mathf.Min(minX, p.x);
            //maxX = Mathf.Max(maxX, p.x);
            minY = Mathf.Min(minY, p.y);
            maxY = Mathf.Max(maxY, p.y);
        }
        instructionText.text = "Kalibrierung läuft ... MinY: " + minY + ", MaxY: " + maxY;
    }

    void SaveCalibration()
    {
        //PlayerPrefs.SetFloat("Calib_MinX", minX);
        //PlayerPrefs.SetFloat("Calib_MaxX", maxX);
        PlayerPrefs.SetFloat("Calib_MinY", minY);
        PlayerPrefs.SetFloat("Calib_MaxY", maxY);
        PlayerPrefs.Save();
        Debug.Log("MinY: " + minY + ", MaxY: " + maxY);
        instructionText.text = "Kalibrierung beendet";
    }

    /*public float minHeadY;
    public float maxHeadY;
    public float neutralHeadY;

    private int step = 0; // 0=neutral, 1=min, 2=max, 3=done

    public bool IsCalibrated { get; private set; } = false;

    void Start()
    {
        step = 0;
        instructionText.text = "Bitte halte den Kopf gerade in neutraler Position und drücke 'Messen'";
        //captureButton.onClick.AddListener(CaptureStep);
    }

    public void CaptureStep()
    {
        float headY = Camera.main.transform.position.y;

        switch (step)
        {
            case 0:
                neutralHeadY = headY;
                instructionText.text = "Bitte bücke dich so tief wie möglich und drücke 'Messen'";
                step++;
                Debug.Log("neutralHeadY: " + neutralHeadY);
                break;

            case 1:
                minHeadY = headY;
                instructionText.text = "Bitte strecke dich so weit wie möglich nach oben und drücke 'Messen'";
                step++;
                Debug.Log("minHeadY: " + minHeadY);
                break;

            case 2:
                maxHeadY = headY;
                instructionText.text = "Kalibrierung abgeschlossen!";
                IsCalibrated = true;
                captureButton.gameObject.SetActive(false);
                Debug.Log("maxHeadY: " + maxHeadY);
                // Optional: Event feuern oder SceneManager.LoadScene für nächsten Schritt
                break;
        }
    }

    // Beispiel-Getter für den Bewegungsbereich
    public float GetMinHeight() => minHeadY;
    public float GetMaxHeight() => maxHeadY;
    public float GetNeutralHeight() => neutralHeadY;*/
}
