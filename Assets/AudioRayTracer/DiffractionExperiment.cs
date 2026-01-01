using System.Linq;
using Unity.Android.Gradle.Manifest;
using Unity.VisualScripting;
using UnityEditor.PackageManager.UI;
using UnityEngine;

public class DiffractionExperiment : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private AudioRayTracer AudioRayTracerObject;
    [SerializeField] private int numRuns = 6;
    [SerializeField] private int numGatheringsPerSample = 50;
    [SerializeField] private int angleChange = 2;
    [SerializeField] private int maxAngle = 70;

    private int sample = 0;
    private float startAngle;
    private float[] measurements0;
    private float[] measurements1;
    private float[] measurements2;
    private float[] measurements3;

    private void Awake()
    {
        measurements0 = new float[maxAngle / angleChange + 1];
        measurements1 = new float[maxAngle / angleChange + 1];
        measurements2 = new float[maxAngle / angleChange + 1];
        measurements3 = new float[maxAngle / angleChange + 1];
    }
    void Start()
    {
        startAngle = gameObject.transform.eulerAngles.y;
    }

    private void Next()
    {
        if (sample >= numRuns * (maxAngle / angleChange))
        {
            return;
        }
        Debug.Log(AudioRayTracerObject.amplitudeSum0 / AudioRayTracerObject.amplitudeSumSamples);
        if (AudioRayTracerObject.amplitudeSum0 / AudioRayTracerObject.amplitudeSumSamples < 0.1f)
        {
            measurements0[sample % (maxAngle / angleChange)] += (AudioRayTracerObject.amplitudeSum0 / AudioRayTracerObject.amplitudeSumSamples) / numRuns;
            measurements1[sample % (maxAngle / angleChange)] += (AudioRayTracerObject.amplitudeSum1 / AudioRayTracerObject.amplitudeSumSamples) / numRuns;
            measurements2[sample % (maxAngle / angleChange)] += (AudioRayTracerObject.amplitudeSum2 / AudioRayTracerObject.amplitudeSumSamples) / numRuns;
            measurements3[sample % (maxAngle / angleChange)] += (AudioRayTracerObject.amplitudeSum3 / AudioRayTracerObject.amplitudeSumSamples) / numRuns;
        }
        Debug.Log("Sample: " + sample);
        sample++;
        if (sample >= numRuns * (maxAngle / angleChange))
        {
            Debug.Log(measurements0.ToCommaSeparatedString());
            Debug.Log(measurements1.ToCommaSeparatedString());
            Debug.Log(measurements2.ToCommaSeparatedString());
            Debug.Log(measurements3.ToCommaSeparatedString());
            return;
        }
        Debug.Log("New rotation y: " + (0 + (sample % (maxAngle / angleChange)) * angleChange));
        Quaternion newRotation = Quaternion.Euler(0, startAngle + (sample%(maxAngle/angleChange))*angleChange, 0);
        gameObject.transform.SetPositionAndRotation(gameObject.transform.position, newRotation);

        AudioRayTracerObject.amplitudeSum0 = 0;
        AudioRayTracerObject.amplitudeSum1 = 0;
        AudioRayTracerObject.amplitudeSum2 = 0;
        AudioRayTracerObject.amplitudeSum3 = 0;
        AudioRayTracerObject.amplitudeSumSamples = 0;

    }

    // Update is called once per frame
    void Update()
    {
        if (AudioRayTracerObject.amplitudeSumSamples > numGatheringsPerSample)
        {
            Next();
        }
    }
}
