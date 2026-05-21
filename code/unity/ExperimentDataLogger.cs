// =============================================================
// ExperimentDataLogger.cs
//
// ACM THRI revision — "System Design and Validation of an Ego-Exocentric
// Mixed Reality Framework in Compensating Delay for ROV Teleoperation"
// (Manuscript ID THRI-2025-0268).
//
// Paper reference: §4 (per-trial logging)
//
// Writes one CSV row per simulation frame including ROV pose, body-frame velocities, collision events, and timestamps. Aggregated externally to produce `per_trial_metrics.csv` (in ../data/).
//
// Released under the MIT License — see ../../LICENSE.
// =============================================================
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Tobii.XR;
using ViveSR.anipal.Eye;

public class ExperimentDataLogger : MonoBehaviour
{
    public int participantID = 1;
    public int condition = 1;
    public GameObject rovCenter; // Assign in Inspector

    private string filePath;
    private int frameID = 0;
    private bool expStarted = false;
    private InputDevice rightHand;
    private StreamWriter writer;

    public ROVWallDetector wallDetector; // assign in Inspector

    public GameObject Lum;
    private List<string[]> csvData = new List<string[]>();

    public GameObject EyeSR;
    private VerboseData verbose_Data;
    Vector3 hitPoint = Vector3.zero;
    string objectName = "None";



    void Start()
    {
        string folderPath = @"C:\Users\essie-adm-xufang\Desktop\Unity_works\Experiment3_HRC\Assets\Delay_exp\CSV_output";

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string fileName = $"Rovdelay_P{participantID:00}_C{condition}.csv";
        filePath = Path.Combine(folderPath, fileName);

        WriteCSVHeader();

        //writer = new StreamWriter(filePath, false, Encoding.UTF8);
        //writer.AutoFlush = true;

        //writer.WriteLine("FrameID,TimeSinceStart,RealTime,H,M,S,MS,ExpStarted," +
        //    "ROV_Pos_X,ROV_Pos_Y,ROV_Pos_Z,ROV_Rot_X,ROV_Rot_Y,ROV_Rot_Z," +
        //    "TriggerValue,Controller_Pos_X,Controller_Pos_Y,Controller_Pos_Z," +
        //    "Controller_Rot_X,Controller_Rot_Y,Controller_Rot_Z,IsCollidingWithWall," +
        //    "GazeOriginX,GazeOriginY,GazeOriginZ,GazeDirectionX,GazeDirectionY,GazeDirectionZ," +
        //    "LeftPupilDiameter,RightPupilDiameter,HitPosX,HitPosY,HitPosZ,HitObjectName,Luminance");

        var rightHandDevices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);
        if (rightHandDevices.Count > 0)
            rightHand = rightHandDevices[0];
    }

    void Update()
    {
        if (!expStarted && Input.GetKeyDown(KeyCode.S))
        {
            expStarted = true;
            Debug.Log("Experiment Started!");
        }

        if (expStarted && Input.GetKeyDown(KeyCode.E))
        {
            expStarted = false;
            Debug.Log("Experiment Ended!");
        }

        frameID++;
        float timeSinceStart = Time.time;
        DateTime now = DateTime.Now;

        Vector3 rovPos = rovCenter != null ? rovCenter.transform.position : Vector3.zero;
        Vector3 rovRot = rovCenter != null ? rovCenter.transform.eulerAngles : Vector3.zero;

        float triggerValue = 0f;
        Vector3 controllerPos = Vector3.zero;
        Vector3 controllerRot = Vector3.zero;

        int isColliding = wallDetector != null && wallDetector.isCollidingWithWall ? 1 : 0;


        if (rightHand.isValid)
        {
            rightHand.TryGetFeatureValue(CommonUsages.trigger, out triggerValue);
            rightHand.TryGetFeatureValue(CommonUsages.devicePosition, out controllerPos);
            rightHand.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion controllerQuat);
            controllerRot = controllerQuat.eulerAngles;
        }

        verbose_Data = EyeSR.GetComponent<TestGetData>().verbose_Data;
        var eyeData = TobiiXR.EyeTrackingData;
        Vector3 gazeOrigin = eyeData.GazeRay.Origin;
        Vector3 gazeDirection = eyeData.GazeRay.Direction;
        float leftPupil = verbose_Data.left.pupil_diameter_mm;
        float rightPupil = verbose_Data.right.pupil_diameter_mm;

        Vector3 hitPoint = Vector3.zero;
        string objectName = "None";
        Ray gazeRay = new Ray(gazeOrigin, gazeDirection);
        if (Physics.Raycast(gazeRay, out RaycastHit hit, 200f))
        {
            hitPoint = hit.point + (-0.05f * gazeDirection);
            objectName = hit.collider.name;
        }

        double lum = Lum.GetComponent<Luminosity>().lum;

        string[] row = new string[]
        {
            frameID.ToString(),
            timeSinceStart.ToString("F4"),
            now.ToString("HH:mm:ss.fff"),
            now.Hour.ToString(), now.Minute.ToString(), now.Second.ToString(), now.Millisecond.ToString(),
            expStarted ? "1" : "0",
            rovPos.x.ToString("F4"), rovPos.y.ToString("F4"), rovPos.z.ToString("F4"),
            rovRot.x.ToString("F2"), rovRot.y.ToString("F2"), rovRot.z.ToString("F2"),
            triggerValue.ToString("F3"),
            controllerPos.x.ToString("F4"), controllerPos.y.ToString("F4"), controllerPos.z.ToString("F4"),
            controllerRot.x.ToString("F2"), controllerRot.y.ToString("F2"), controllerRot.z.ToString("F2"),
            isColliding.ToString(),
            gazeOrigin.x.ToString("F4"), gazeOrigin.y.ToString("F4"), gazeOrigin.z.ToString("F4"),
            gazeDirection.x.ToString("F4"), gazeDirection.y.ToString("F4"), gazeDirection.z.ToString("F4"),
            leftPupil.ToString("F4"), rightPupil.ToString("F4"),
            hitPoint.x.ToString("F4"), hitPoint.y.ToString("F4"), hitPoint.z.ToString("F4"),
            objectName,
            lum.ToString("F4")
        };

        csvData.Add(row);

    }

    void WriteCSVHeader()
    {
        string[] header = new string[]
        {
            "FrameID", "TimeSinceStart", "RealTime", "H", "M", "S", "MS", "ExpStarted",
            "ROV_Pos_X", "ROV_Pos_Y", "ROV_Pos_Z", "ROV_Rot_X", "ROV_Rot_Y", "ROV_Rot_Z",
            "TriggerValue", "Controller_Pos_X", "Controller_Pos_Y", "Controller_Pos_Z",
            "Controller_Rot_X", "Controller_Rot_Y", "Controller_Rot_Z", "IsCollidingWithWall",
            "GazeOriginX", "GazeOriginY", "GazeOriginZ", "GazeDirectionX", "GazeDirectionY", "GazeDirectionZ",
            "LeftPupilDiameter", "RightPupilDiameter", "HitPosX", "HitPosY", "HitPosZ", "HitObjectName", "Luminance"
        };
        csvData.Add(header);
    }

    private void OnApplicationQuit()
    {
        ExportCSVFile();
    }

    public void StopLogging()
    {
        
        expStarted = false;
        Debug.Log("Experiment Ended!");
        
    }

    void ExportCSVFile()
    {
        writer = new StreamWriter(filePath, false, Encoding.UTF8);
        writer.AutoFlush = true;

        foreach (var row in csvData)
        {
            writer.WriteLine(string.Join(",", row));
        }

        writer.Close();
        Debug.Log("CSV exported to: " + filePath);
    }
}
