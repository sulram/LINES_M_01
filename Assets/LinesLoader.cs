using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;

public struct DataPoint
{
    public float sampleTime;
    public Vector3 euler;
    public Vector3 linAcc;

    public DataPoint(string[] v)
    {
        sampleTime = float.Parse(v[0]) * 0.001f;
        euler = new Vector3(float.Parse(v[3]), float.Parse(v[2]), float.Parse(v[1]));
        linAcc = new Vector3(float.Parse(v[4]), float.Parse(v[5]), float.Parse(v[6]));
    }
}

public class LinesLoader : MonoBehaviour
{
    public Transform target;
    protected TrailRenderer trail;

    public TextAsset datalog;
    public DataPoint[] points;
    public Vector3[] velocities;

    public int currentDataPoint;

    void Start()
    {

        trail = target.GetComponent<TrailRenderer>();

        // split datalog lines

        string[] lines = Regex.Split(datalog.text, "\r\n");

        int i = 0;
        int skiplines = 6;

        points = new DataPoint[lines.Length - skiplines];

        Vector3 average = new Vector3();

        foreach (string line in lines)
        {
            // skip header, 6 first lines

            if(i < skiplines)
            {
                Debug.Log("SKIP (" + i + "): " + line);
            }
            else
            {
                Debug.Log(line);

                string[] values = Regex.Split(line, "\t");
                if (values.Length == 8)
                {
                    points[i - skiplines] = new DataPoint(values);
                    average += points[i - skiplines].linAcc;
                    //Debug.Log(points[i-skiplines].euler);
                }
            }

            i++;

        }

        // calculate average

        average /= (lines.Length - skiplines);

        Debug.Log("AVERAGE: " + average);

        // Go through each datapoint and subtract the average value from each
        // point to try to normalize any drifting

        for (i = 0;  i < points.Length; ++i)
        {
            points[i].linAcc -= average;
        }

        // Now we do Riemann sum integrations on the sensor data 
        // first initialize arrays using trapezoidal method:
        // trapezoid = 0.5 * base * (height 1 + height 2)
        //           = 0.5 * time increment * (linear accel_1 + linear accel_2)

        velocities = new Vector3[points.Length];

        velocities[0] = new Vector3();
        Vector3 velSum = new Vector3();

        for (i = 1; i < velocities.Length; i++)
        {

            //the current velocity to be added
            Vector3 added = new Vector3();

            //sum of current linear accel plus next linear accel
            added += points[i].linAcc + points[i - 1].linAcc;

            //then multiply by the base of the trapezoid 
            velSum += added * 0.5f * points[i].sampleTime; // 0.5 * base * (above result)

            //store it
            velocities[i] = new Vector3(velSum.x, velSum.y, velSum.z);
        }

        // Prepare for DRAW
        
        currentDataPoint = 0;


    }

    // Update is called once per frame
    void Update()
    {
        target.SetPositionAndRotation(velocities[currentDataPoint], new Quaternion());
        currentDataPoint = (currentDataPoint + 1) % velocities.Length;
    }
}
