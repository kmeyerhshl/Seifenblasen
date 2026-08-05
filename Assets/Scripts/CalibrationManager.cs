using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;

/// <summary>
/// Verwaltet die Kalibrierung des vertikalen Bewegungsbereichs des Spielers.
/// Während der Kalibrierung werden die minimalen und maximalen Y-Positionen
/// der Hände erfasst und gespeichert. Diese Werte dienen anschließend
/// als individuelle Grenzen für die Platzierung und Bewegung der Blasen.
/// </summary>
public class CalibrationManager : MonoBehaviour
{
    // UI-Text zur Anzeige von Anweisungen und Statusmeldungen
    public TextMeshProUGUI instructionText;

    // Referenz auf den Kalibrierungs-Button
    public GameObject captureButton;

    // ------------------- KALIBRIERUNGSEINSTELLUNGEN -------------------

    [Header("Calibration Settings")]

    // Dauer der Kalibrierung in Sekunden
    public float calibrationDuration = 30f;

    // Verbleibende Zeit während der Kalibrierung
    private float timer;

    // Gibt an, ob aktuell kalibriert wird
    private bool calibrating;

    // Erfasste minimale und maximale Handhöhe
    public float minY, maxY;

    /// <summary>
    /// Startet den Kalibrierungsvorgang.
    /// Die bisherigen Werte werden zurückgesetzt und die
    /// Handpositionen werden für die festgelegte Dauer aufgezeichnet.
    /// </summary>
    public void StartCalibration()
    {
        // Startwerte setzen, damit die ersten Messwerte
        // korrekt übernommen werden
        minY = float.MaxValue;
        maxY = float.MinValue;

        timer = calibrationDuration;
        calibrating = true;

        Debug.Log("🟢 Kalibrierung gestartet – Hände bewegen");
    }

    /// <summary>
    /// Wird in jedem Frame aufgerufen.
    /// Während der Kalibrierung werden kontinuierlich die
    /// Positionen beider Hände erfasst.
    /// </summary>
    void Update()
    {
        if (!calibrating)
            return;

        // Restzeit reduzieren
        timer -= Time.deltaTime;

        // Linke und rechte Hand erfassen
        CaptureHand(Handedness.Left);
        CaptureHand(Handedness.Right);

        // Kalibrierung beenden, sobald die Zeit abgelaufen ist
        if (timer <= 0f)
        {
            calibrating = false;

            SaveCalibration();

            Debug.Log("✅ Kalibrierung beendet");
        }
    }

    // ------------------- STANDARD SPIELBEREICH -------------------

    // Mittelpunkt des Standardbereichs
    public Vector3 center = new Vector3(0, 0, 0.5f);

    // Größe des Standardbereichs
    public Vector3 areaSize = new Vector3(1.2f, 1.0f, 1.0f);

    /// <summary>
    /// Setzt die Kalibrierung auf die Standardwerte zurück.
    /// Individuelle Kalibrierungs- und Levelgrenzen werden gelöscht.
    /// </summary>
    public void Reset()
    {
        // Standard-Höhenbereich berechnen
        minY = center.y - areaSize.y / 2f;
        maxY = center.y + areaSize.y / 2f;

        // Werte speichern
        PlayerPrefs.SetFloat("Calib_MinY", minY);
        PlayerPrefs.SetFloat("Calib_MaxY", maxY);

        // Eventuell vorhandene Level-spezifische Grenzen entfernen
        PlayerPrefs.DeleteKey("Level_MinY");
        PlayerPrefs.DeleteKey("Level_MaxY");

        PlayerPrefs.Save();

        instructionText.text = "Kalibrierung zurückgesetzt";
    }

    /// <summary>
    /// Liest die aktuelle Position einer Hand aus
    /// und aktualisiert die minimalen bzw. maximalen Höhenwerte.
    /// </summary>
    void CaptureHand(Handedness hand)
    {
        // Position des Handgelenks bzw. der Handfläche abrufen
        if (HandJointUtils.TryGetJointPose(
            TrackedHandJoint.Palm,
            hand,
            out MixedRealityPose pose))
        {
            Vector3 p = pose.Position;

            // Minimalen und maximalen Y-Wert aktualisieren
            minY = Mathf.Min(minY, p.y);
            maxY = Mathf.Max(maxY, p.y);
        }

        // Aktuelle Werte während der Kalibrierung anzeigen
        instructionText.text =
            "Kalibrierung läuft ... MinY: " +
            minY +
            ", MaxY: " +
            maxY;
    }

    /// <summary>
    /// Speichert die ermittelten Kalibrierungswerte dauerhaft
    /// in den Unity PlayerPrefs.
    /// </summary>
    void SaveCalibration()
    {
        PlayerPrefs.SetFloat("Calib_MinY", minY);
        PlayerPrefs.SetFloat("Calib_MaxY", maxY);

        PlayerPrefs.Save();

        Debug.Log(
            "MinY: " + minY +
            ", MaxY: " + maxY);

        instructionText.text = "Kalibrierung beendet";
    }
}