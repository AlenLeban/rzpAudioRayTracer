using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Video;



public struct FRay
{
    public uint debugId;
    public Vector3 origin;
    public Vector3 direction;
    public float pressure0;
    public float pressure1;
    public float pressure2;
    public float pressure3;
    public float distance;
    public float totalDistance;
}

public struct FHitInfo
{
    public uint wasHit;
    public Vector3 hitLocation;
    public Vector3 hitNormal;
    public uint hitObjectId;
    public uint hitObjectType;
}
public struct FSurfaceProperties
{
    public float nonAbsorption0;
    public float nonAbsorption1;
    public float nonAbsorption2;
    public float nonAbsorption3;
    public float roughness;
    public float speedOfSound;
    public uint isDiffractionVolume;
}

public struct FBox
{
    public Vector3 minPoint;
    public Vector3 maxPoint;
    public FSurfaceProperties properties;
    public Matrix4x4 localToWorldMatrix;
    public Matrix4x4 worldToLocalMatrix;
}

public struct FCylinder
{
    public Vector3 a;
    public Vector3 b;
    public float radius;
}


public struct FAudioSource
{
    public int id;
    public Vector3 location;
}

public class AudioRayTracer : MonoBehaviour
{
    [SerializeField] public int IRLength = 44100;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField] private UI uiObject;
    [SerializeField] private ComputeShader rayTracingShader;
    [SerializeField] private int numberOfRays = 50;
    [SerializeField] private List<AudioSource> audioSourceObjects;
    [SerializeField] private uint maxBounces = 2;
    [SerializeField] private GameObject listenerObject;
    [SerializeField] private CSCoreTest cscoreComponent;
    [SerializeField] private GameObject environment;
    [SerializeField] private bool showRays = true;
    [SerializeField] private bool hideEmittedRays = true;
    [SerializeField] private bool ignoreIR = false;
    [SerializeField] private bool updateIRTexture = false;
    [SerializeField] private float IRUpdateInterval = 0.1f;
    [SerializeField] private float textureHeightFactor = 1.0f;
    [SerializeField] private float volumeMultiplier = 1.0f;
    private const int frequencyBands = 4;
    [SerializeField] private int textureIRIndex = 0;
    [SerializeField] private bool updateRayTime = false;
    [SerializeField] private float IRAttenuation = 5;
    [SerializeField] private bool useDiffraction = true;
    [SerializeField] private bool drawAllRays = false;

    [SerializeField] private float angle = 0.0f;

    public float amplitudeSum0 = 0;
    public float amplitudeSum1 = 0;
    public float amplitudeSum2 = 0;
    public float amplitudeSum3 = 0;
    public int amplitudeSumSamples = 0;

    public Texture2D texture;

    private FRay[] _testRays;
    private FAudioSource[] _audioSources;
    private FHitInfo[] _finalHits;
    private FBox[] _boxes;
    private FCylinder[] _cylinders;

    private ComputeBuffer _raysBuffer;
    private ComputeBuffer _audioSourceBuffer;
    private ComputeBuffer _finalHitsBuffer;
    private ComputeBuffer _boxesBuffer;
    private ComputeBuffer _cylindersBuffer;

    private int prevNumRays = 40;
    private bool prevDrawAllRays = false;
    
    private Color[] debugColors = { Color.green, Color.yellow };

    private const int surfacePropertiesSizeof = 
          sizeof(float)
        + sizeof(float)
        + sizeof(float)
        + sizeof(float)
        + sizeof(float)
        + sizeof(float)
        + sizeof(uint);

    private const int raySizeof =
          sizeof(uint)
        + 3 * sizeof(float)
        + 3 * sizeof(float)
        + frequencyBands * sizeof(float)
        + sizeof(float)
        + sizeof(float);

    private const int hitInfoSizeof = 2 * sizeof(int) + 2 * 3 * sizeof(float) + sizeof(int);
    private const int fboxSizeof = 2 * 3 * sizeof(float) + surfacePropertiesSizeof + 4 * 4 * sizeof(float) * 2;
    private const int fcylinderSizeof = 2 * 3 * sizeof(float) + sizeof(float);

    private float[][] IRHistogramLeft = new float[frequencyBands][];
    private float[][] IRHistogramRight = new float[frequencyBands][];

    private void Awake()
    {
        prevDrawAllRays = drawAllRays;
        prevNumRays = numberOfRays;
        for (int i = 0; i < frequencyBands; i++)
        {
            IRHistogramLeft[i] = new float[IRLength];
            IRHistogramRight[i] = new float[IRLength];
        }
    }
    private void GatherEnvironment()
    {
        RTBox[] boxes = environment.gameObject.GetComponentsInChildren<RTBox>();
        _boxes = new FBox[boxes.Length];
        for (int i = 0; i < boxes.Length; i++)
        {
            _boxes[i] = boxes[i].box;
        }

        RTCylinder[] cylinders = environment.gameObject.GetComponentsInChildren<RTCylinder>();
        _cylinders = new FCylinder[cylinders.Length];
        for (int i = 0; i < cylinders.Length; i++)
        {
            _cylinders[i] = cylinders[i].cylinder;
        }
    }
    void Start()
    {
        texture = new Texture2D(720, 128);
        uiObject.rawImage.texture = texture;
        _audioSources = new FAudioSource[audioSourceObjects.Count];

        for (int i = 0; i < _audioSources.Length; i++)
        {
            _audioSources[0].id = i;
            _audioSources[0].location = audioSourceObjects[i].gameObject.transform.position;
        }

        GatherEnvironment();

        int kernelId = rayTracingShader.FindKernel("CSMain");
        _testRays = new FRay[numberOfRays * (maxBounces + 1) * (1 + (drawAllRays ? 1 : 0))];
        _raysBuffer = new ComputeBuffer(_testRays.Length, raySizeof);
        _raysBuffer.SetData(_testRays);
        rayTracingShader.SetBuffer(kernelId, "GeneratedRays", _raysBuffer);
        rayTracingShader.SetInt("NumRays", numberOfRays);

        _audioSourceBuffer = new ComputeBuffer(_audioSources.Length, sizeof(int) + 3 * sizeof(float));
        _audioSourceBuffer.SetData(_audioSources);
        rayTracingShader.SetBuffer(kernelId, "AudioSources", _audioSourceBuffer);
        
        _finalHits = new FHitInfo[numberOfRays * (maxBounces + 1) * (1 + (drawAllRays ? 1 : 0))];
        _finalHitsBuffer = new ComputeBuffer(_finalHits.Length, hitInfoSizeof);
        _finalHitsBuffer.SetData(_finalHits);
        rayTracingShader.SetBuffer(kernelId, "FinalHits", _finalHitsBuffer);

        _boxesBuffer = new ComputeBuffer(_boxes.Length, fboxSizeof);
        _boxesBuffer.SetData(_boxes);
        rayTracingShader.SetBuffer(kernelId, "BoxCollisions", _boxesBuffer);

        _cylindersBuffer = new ComputeBuffer(_cylinders.Length, fcylinderSizeof);
        _cylindersBuffer.SetData(_cylinders);
        rayTracingShader.SetBuffer(kernelId, "CylinderCollisions", _cylindersBuffer);

        rayTracingShader.SetInt("DrawAllRays", drawAllRays ? 1 : 0);
        rayTracingShader.SetFloat("MaxBounces", maxBounces);
        rayTracingShader.SetVector("ListenerPosition", new Vector4(
            listenerObject.transform.position.x,
            listenerObject.transform.position.y,
            listenerObject.transform.position.z,
            0.0f)
        );

        InvokeRepeating("UpdateTexture", IRUpdateInterval, IRUpdateInterval);
        InvokeRepeating("GatherEnvironment", IRUpdateInterval, IRUpdateInterval);
    }

    private void ClearTexture()
    {
        Color[] allBlack = new Color[texture.width * texture.height];
        for (int i = 0; i < allBlack.Length; i++)
        {
            allBlack[i] = Color.black;
        }
        texture.SetPixels(allBlack);
    }

    public void UpdateTexture()
    {
        _boxesBuffer.SetData(_boxes);
        _cylindersBuffer.SetData(_cylinders);
        for (int i = 0; i < _audioSources.Length; i++)
        {
            _audioSources[0].id = i;
            _audioSources[0].location = audioSourceObjects[i].gameObject.transform.position;
        }
        int kernelId = rayTracingShader.FindKernel("CSMain");
        rayTracingShader.SetBuffer(kernelId, "BoxCollisions", _boxesBuffer);
        rayTracingShader.SetBuffer(kernelId, "CylinderCollisions", _cylindersBuffer);
        _audioSourceBuffer.SetData(_audioSources);
        rayTracingShader.SetBuffer(kernelId, "AudioSources", _audioSourceBuffer);
        /*rayTracingShader.SetVector("ListenerPosition", new Vector4(
            listenerObject.transform.position.x,
            listenerObject.transform.position.y,
            listenerObject.transform.position.z,
            0.0f)
        );*/
        cscoreComponent.SetIRFromHistogram(IRHistogramLeft, IRHistogramRight);
        if (updateIRTexture)
        {
            Debug.Log("UpdateTexture");
            ClearTexture();
        }
        float colSum = 0;
        float histColsPerPixel = IRHistogramLeft[textureIRIndex].Length / (float)texture.width;
        Debug.Log(histColsPerPixel);
        int pixelCol = 0;
        
       
        
        /*Debug.Log("AS0: " + amplitudeSum0);
        Debug.Log("AS1: " + amplitudeSum1);
        Debug.Log("AS2: " + amplitudeSum2);
        Debug.Log("AS3: " + amplitudeSum3);*/

        for (int i = 0; i < IRHistogramLeft[textureIRIndex].Length; i++)
        {
            //Debug.Log((i - histColsPerPixel * pixelCol) / (histColsPerPixel));
            if ((i - histColsPerPixel * pixelCol) / (histColsPerPixel) <= 1)
            {
                colSum += (IRHistogramLeft[textureIRIndex][i] + IRHistogramRight[textureIRIndex][i]) / 2.0f;
            }
            else
            {
                if (updateIRTexture)
                {
                    /*if (colSum > 0)
                        Debug.LogError(colSum * textureHeightFactor);*/
                    int colHeight = (int)(colSum * textureHeightFactor);
                    for (int j = 0; j < colHeight; j++)
                    {
                        texture.SetPixel(pixelCol, j, Color.white);
                    }
                }
                pixelCol++;
                colSum = 0;
            }
            
            IRHistogramLeft[0][i] /= IRAttenuation;
            IRHistogramLeft[1][i] /= IRAttenuation;
            IRHistogramLeft[2][i] /= IRAttenuation;
            IRHistogramLeft[3][i] /= IRAttenuation;
            IRHistogramRight[0][i] /= IRAttenuation;
            IRHistogramRight[1][i] /= IRAttenuation;
            IRHistogramRight[2][i] /= IRAttenuation;
            IRHistogramRight[3][i] /= IRAttenuation;

            /*            IRHistogramLeft[0][i] = 0;
                        IRHistogramLeft[1][i] = 0;
                        IRHistogramLeft[2][i] = 0;
                        IRHistogramLeft[3][i] = 0;
                        IRHistogramRight[0][i] = 0;
                        IRHistogramRight[1][i] = 0;
                        IRHistogramRight[2][i] = 0;
                        IRHistogramRight[3][i] = 0;*/
        }
        //IRHistogram[IRHistogram.Length-1] /= 2;
        if (updateIRTexture)
            texture.Apply();
    }

    // Update is called once per frame
    void Update()
    {
        int kernelId = rayTracingShader.FindKernel("CSMain");
        if (numberOfRays != prevNumRays || prevDrawAllRays != drawAllRays)
        {
            prevNumRays = numberOfRays;
            prevDrawAllRays = drawAllRays;
            _testRays = new FRay[numberOfRays * (maxBounces + 1) * (1 + (drawAllRays ? 1 : 0))];
            _raysBuffer = new ComputeBuffer(_testRays.Length, raySizeof);
            _raysBuffer.SetData(_testRays);
            rayTracingShader.SetBuffer(kernelId, "GeneratedRays", _raysBuffer);
            rayTracingShader.SetInt("NumRays", numberOfRays);

            _finalHits = new FHitInfo[numberOfRays * (maxBounces + 1) * (1 + (drawAllRays ? 1 : 0))];
            _finalHitsBuffer = new ComputeBuffer(_finalHits.Length, hitInfoSizeof);
            _finalHitsBuffer.SetData(_finalHits);
            rayTracingShader.SetBuffer(kernelId, "FinalHits", _finalHitsBuffer);

        }
        CSCoreTest.isIREnabled = !ignoreIR;
        rayTracingShader.Dispatch(kernelId, (int)(Math.Ceiling(numberOfRays / 8.0)), 1, 1);
        if (updateRayTime)
        {
            rayTracingShader.SetInt("Time", (int)(Time.time*500));
        }
        rayTracingShader.SetFloat("Angle", angle);
        rayTracingShader.SetInt("DrawAllRays", drawAllRays ? 1 : 0);
        rayTracingShader.SetInt("useDiffraction", (int)(useDiffraction ? 1 : 0));
        _raysBuffer.GetData(_testRays);
        _finalHitsBuffer.GetData(_finalHits);
        rayTracingShader.SetVector("ListenerPosition", new Vector4(
            listenerObject.transform.position.x,
            listenerObject.transform.position.y,
            listenerObject.transform.position.z,
            0.0f)
        );

        float normalizeFactor = 1.0f/128 * IRAttenuation;
        for (int i = 0; i < _testRays.Length; i++)
        {
            FRay ray = _testRays[i];
            FHitInfo finalHit = _finalHits[i];
            if (finalHit.wasHit == 1)
            {
                float distance = (ray.origin - finalHit.hitLocation).magnitude;
                if (showRays)
                {
                    if (!(hideEmittedRays && ray.debugId == 0))
                    {
                        Debug.DrawLine(ray.origin, ray.origin + ray.direction * distance, debugColors[ray.debugId] * ((float)Math.Sqrt(ray.pressure0)));
                        Debug.DrawLine(finalHit.hitLocation, finalHit.hitLocation + finalHit.hitNormal * 0.3f, Color.blue);
                    }
                    //Debug.DrawLine(ray.origin, ray.origin + ray.direction * distance, debugColors[ray.debugId]);
                }
                if (ray.debugId == 1)
                {
                    float t = (ray.totalDistance / 343.0f * IRLength);
                    int col = Math.Clamp((int)t, 0, IRHistogramLeft[0].Length - 2);
                    if (col == IRHistogramLeft[0].Length - 2)
                    {
                        continue;
                    }
                    //double phase = 2 * Math.PI * 1000 * ray.totalDistance / 343.0f * 0.5f;
                    float frac = t - col;
                    float panningLeftFactor = (Vector3.Dot(ray.direction, listenerObject.transform.right) + 1) / 2;
                    float panningRightFactor = 1 - panningLeftFactor;
                    float hrtf_high_right = Math.Max(Vector3.Dot(-ray.direction, (listenerObject.transform.forward + listenerObject.transform.right).normalized), 0);
                    float hrtf_high_left = Math.Max(Vector3.Dot(-ray.direction, (listenerObject.transform.forward - listenerObject.transform.right).normalized), 0);
                    float hrtf_from_back_volume = Math.Clamp((Vector3.Dot(-ray.direction, listenerObject.transform.forward) + 1), 0, 1) * 0.3f + 0.7f;

                    IRHistogramLeft[0][col] += (float)(ray.pressure0) / ray.totalDistance * volumeMultiplier * normalizeFactor * frac * panningLeftFactor;
                    IRHistogramLeft[0][col + 1] += (float)(ray.pressure0) / ray.totalDistance * volumeMultiplier * normalizeFactor * (1 - frac) * panningLeftFactor;
                    
                    IRHistogramLeft[1][col] += (float)(ray.pressure1) / ray.totalDistance * volumeMultiplier * normalizeFactor * frac * panningLeftFactor;
                    IRHistogramLeft[1][col + 1] += (float)(ray.pressure1) / ray.totalDistance * volumeMultiplier * normalizeFactor * (1 - frac) * panningLeftFactor;

                    IRHistogramLeft[2][col] += (float)(ray.pressure2) / ray.totalDistance * volumeMultiplier * normalizeFactor * frac * panningLeftFactor;
                    IRHistogramLeft[2][col + 1] += (float)(ray.pressure2) / ray.totalDistance * volumeMultiplier * normalizeFactor * (1 - frac) * panningLeftFactor;

                    IRHistogramLeft[3][col] += (float)(ray.pressure3) / ray.totalDistance * volumeMultiplier * normalizeFactor * frac * panningLeftFactor;
                    IRHistogramLeft[3][col + 1] += (float)(ray.pressure3) / ray.totalDistance * volumeMultiplier * normalizeFactor * (1 - frac) * panningLeftFactor;




                    IRHistogramRight[0][col] += (float)(ray.pressure0) / ray.totalDistance * volumeMultiplier * normalizeFactor * frac * panningRightFactor;
                    IRHistogramRight[0][col + 1] += (float)(ray.pressure0) / ray.totalDistance * volumeMultiplier * normalizeFactor * (1 - frac) * panningRightFactor;
                    
                    IRHistogramRight[1][col] += (float)(ray.pressure1) / ray.totalDistance * volumeMultiplier * normalizeFactor * frac * panningRightFactor;
                    IRHistogramRight[1][col + 1] += (float)(ray.pressure1) / ray.totalDistance * volumeMultiplier * normalizeFactor * (1 - frac) * panningRightFactor;

                    IRHistogramRight[2][col] += (float)(ray.pressure2) / ray.totalDistance * volumeMultiplier * normalizeFactor * frac * panningRightFactor;
                    IRHistogramRight[2][col + 1] += (float)(ray.pressure2) / ray.totalDistance * volumeMultiplier * normalizeFactor * (1 - frac) * panningRightFactor;

                    IRHistogramRight[3][col] += (float)(ray.pressure3) / ray.totalDistance * volumeMultiplier * normalizeFactor * frac * panningRightFactor;
                    IRHistogramRight[3][col + 1] += (float)(ray.pressure3) / ray.totalDistance * volumeMultiplier * normalizeFactor * (1 - frac) * panningRightFactor;

                    //amplitudeSum0++;
                    amplitudeSum0 += (float)(ray.pressure0) / ray.totalDistance * volumeMultiplier * normalizeFactor * frac * panningLeftFactor;
                    amplitudeSum1 += (float)(ray.pressure1) / ray.totalDistance * volumeMultiplier * normalizeFactor * frac * panningLeftFactor;
                    amplitudeSum2 += (float)(ray.pressure2) / ray.totalDistance * volumeMultiplier * normalizeFactor * frac * panningLeftFactor;
                    amplitudeSum3 += (float)(ray.pressure3) / ray.totalDistance * volumeMultiplier * normalizeFactor * frac * panningLeftFactor;
                }
            }
            else if(ray.debugId == 0)
            {
                if (showRays)
                {
                    Debug.DrawLine(ray.origin, ray.origin + ray.direction, Color.red);
                }
            }


        }

        amplitudeSumSamples++;
    }
}
