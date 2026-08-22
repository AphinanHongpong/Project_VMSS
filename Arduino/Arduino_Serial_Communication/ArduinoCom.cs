using UnityEngine;
using System;
using System.IO.Ports;
using System.Globalization;

public class ArduinoDualCom : MonoBehaviour
{
    [Header("Serial")]
    public string portName = "COM3";
    public int baudRate = 9600;

    private SerialPort serialPort;

    [Header("Legs")]
    public Transform leftLeg;
    public Transform rightLeg;

    [Header("Player")]
    public Transform xrOrigin;

    public float moveDistance = 0.20f;

    [Header("Step Detection")]
    public float stepUpAngle = 25f;
    public float stepDownAngle = 10f;

    private bool leftRaised = false;
    private bool rightRaised = false;

    // เริ่มรอให้ขาซ้ายก้าวก่อน
    private bool expectLeft = true;

    void Start()
    {
        serialPort = new SerialPort(portName, baudRate);
        serialPort.ReadTimeout = 20;

        try
        {
            serialPort.Open();
            Debug.Log("Serial Connected");
        }
        catch (Exception e)
        {
            Debug.LogError("Cannot open COM Port : " + e.Message);
        }
    }

    void Update()
    {
        if (serialPort == null || !serialPort.IsOpen)
            return;

        if (serialPort.BytesToRead <= 0)
            return;

        try
        {
            string data = serialPort.ReadLine();

            string[] values = data.Split(',');

            if (values.Length != 6)
                return;

            float lPitch = float.Parse(values[0], CultureInfo.InvariantCulture);
            float lRoll  = float.Parse(values[1], CultureInfo.InvariantCulture);
            float lYaw   = float.Parse(values[2], CultureInfo.InvariantCulture);

            float rPitch = float.Parse(values[3], CultureInfo.InvariantCulture);
            float rRoll  = float.Parse(values[4], CultureInfo.InvariantCulture);
            float rYaw   = float.Parse(values[5], CultureInfo.InvariantCulture);

            //-----------------------------------
            // หมุนขา
            //-----------------------------------

            leftLeg.localRotation =
                Quaternion.Euler(lPitch, -lRoll, lYaw);

            rightLeg.localRotation =
                Quaternion.Euler(rPitch, -rRoll, rYaw);

            //-----------------------------------
            // ตรวจจับยกขาซ้าย
            //-----------------------------------

            if (expectLeft)
            {
                if (!leftRaised && lPitch > stepUpAngle)
                {
                    leftRaised = true;
                }

                if (leftRaised && lPitch < stepDownAngle)
                {
                    xrOrigin.position += xrOrigin.forward * moveDistance;

                    leftRaised = false;
                    expectLeft = false; // ต่อไปต้องรอขาขวา
                }
            }

            //-----------------------------------
            // ตรวจจับยกขาขวา
            //-----------------------------------

            if (!expectLeft)
            {
                if (!rightRaised && rPitch > stepUpAngle)
                {
                    rightRaised = true;
                }

                if (rightRaised && rPitch < stepDownAngle)
                {
                    xrOrigin.position += xrOrigin.forward * moveDistance;

                    rightRaised = false;
                    expectLeft = true; // กลับไปรอขาซ้าย
                }
            }
        }
        catch (TimeoutException)
        {
            // ไม่มีข้อมูลในเฟรมนี้
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);
        }
    }

    void OnApplicationQuit()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
        }
    }
}