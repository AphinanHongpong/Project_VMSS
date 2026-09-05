using UnityEngine;
using System.IO.Ports;
using System;
using UnityEngine.UI;

public class KeyboardStep : MonoBehaviour
{
    [Header("Animator")]
    public Animator animator;

    [Header("Movement")]
    public float stepDistance = 5f; 
    public float stepSpeed = 10f;   

    [Header("UI System")]
    public Text stepText;           // เปลี่ยนชื่อจาก scoreText เป็น stepText
    public Text timerText;          
    
    private int stepCount = 0; 
    private float timeElapsed = 0f; 

    [Header("Arduino")]
    public string portName = "COM5";
    public int baudRate = 9600;

    [Header("Pitch Range")]
    public float leftPitchMin = 5f;
    public float leftPitchMax = 55f;
    public float rightPitchMin = 5f;
    public float rightPitchMax = 55f;

    [Header("Endless Mode")]
    public EndlessEnvironment[] environmentChunks; 

    private SerialPort serialPort;
    private bool isMoving = false;
    private float distanceMovedInCurrentStep = 0f;

    private bool leftReady = true;
    private bool rightReady = true;

    void Start()
    {
        timeElapsed = 0f; 
        stepCount = 0;    
        UpdateStepUI();
        UpdateTimerUI();

        try
        {
            serialPort = new SerialPort(portName, baudRate);
            serialPort.ReadTimeout = 20;
            serialPort.Open();
            Debug.Log("✅ Arduino Connected");
        }
        catch(Exception)
        {
            Debug.LogWarning("⚠️ ไม่พบ Arduino");
        }
    }

    void Update()
    {
        // --- 1. นับเวลาเดินหน้า ---
        timeElapsed += Time.deltaTime;
        UpdateTimerUI();

        // --- 2. ทดสอบด้วยคีย์บอร์ด ---
        if(!isMoving)
        {
            if(Input.GetKeyDown(KeyCode.A)) { animator.SetTrigger("doWalkL"); SetupNextStep(); }
            if(Input.GetKeyDown(KeyCode.D)) { animator.SetTrigger("doWalkR"); SetupNextStep(); }
        }

        // --- 3. อ่านค่าจาก Arduino ---
        if(serialPort != null && serialPort.IsOpen)
        {
            try
            {
                string line = serialPort.ReadLine().Trim();
                string[] value = line.Split(',');

                if(value.Length == 2)
                {
                    float leftPitch = Mathf.Abs(float.Parse(value[0]));
                    float rightPitch = Mathf.Abs(float.Parse(value[1]));

                    Debug.Log($"องศาซ้าย: {leftPitch:F1} | องศาขวา: {rightPitch:F1}");

                    if(!isMoving)
                    {
                        if(leftPitch >= leftPitchMin && leftPitch <= leftPitchMax)
                        {
                            if(leftReady) { animator.SetTrigger("doWalkL"); SetupNextStep(); leftReady = false; }
                        }
                        else { leftReady = true; }

                        if(rightPitch >= rightPitchMin && rightPitch <= rightPitchMax)
                        {
                            if(rightReady) { animator.SetTrigger("doWalkR"); SetupNextStep(); rightReady = false; }
                        }
                        else { rightReady = true; }
                    }
                }
            }
            catch (TimeoutException) { }
            catch (Exception) { }
        }

        // --- 4. เลื่อนฉาก ---
        if (isMoving)
        {
            float moveStep = stepSpeed * Time.deltaTime;
            distanceMovedInCurrentStep += moveStep;

            foreach (EndlessEnvironment chunk in environmentChunks)
            {
                if (chunk != null) chunk.MoveEnvironment(moveStep);
            }

            if (distanceMovedInCurrentStep >= stepDistance) isMoving = false;
        }
    }

    void SetupNextStep()
    {
        isMoving = true;
        distanceMovedInCurrentStep = 0f; 
        
        stepCount++; 
        UpdateStepUI();
    }

    void UpdateStepUI()
    {
        if(stepText != null) 
        {
            stepText.text = $"จำนวนก้าว: {stepCount} ก้าว";
        }
    }

    void UpdateTimerUI()
    {
        if(timerText != null) 
        {
            int minutes = Mathf.FloorToInt(timeElapsed / 60F);
            int seconds = Mathf.FloorToInt(timeElapsed - minutes * 60);
            timerText.text = string.Format("เวลา: {0:00}:{1:00}", minutes, seconds);
        }
    }

    void OnDestroy()
    {
        if(serialPort != null && serialPort.IsOpen) serialPort.Close();
    }
}