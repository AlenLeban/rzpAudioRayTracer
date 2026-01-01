using System;
using UnityEngine;

public class RTBox : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private MeshRenderer _mr;
    [SerializeField] private ESurface surfacePreset;
    [SerializeField] private float absorption0 = 0.5f;
    [SerializeField] private float absorption1 = 0.12f;
    [SerializeField] private float absorption2 = 0.12f;
    [SerializeField] private float absorption3 = 0.12f;
    [SerializeField] private float roughness = 0.5f;
    //[SerializeField] private float speedOfSound = 3500f;
    [SerializeField] private bool isDiffractionVolume = false;

    public enum ESurface
    {
        Glass,
        Concrete,
        Wood,
        Rubber,
        Custom
    }


    public FBox box;
    private void Awake()
    {
        _mr = gameObject.GetComponent<MeshRenderer>();
        if (_mr == null)
        {
            Debug.LogError(gameObject + "does not have mesh renderer");
        }
        box.minPoint = new Vector3(-0.5f, -0.5f, -0.5f);
        box.maxPoint = new Vector3(0.5f, 0.5f, 0.5f);
        FSurfaceProperties properties;
        //properties.nonAbsorption0 = 1 - 0.34f;
        //properties.nonAbsorption1 = 1 - 0.56f;
        properties.roughness = roughness;
        properties.nonAbsorption0 = 1 - absorption0;
        properties.nonAbsorption1 = 1 - absorption1;
        properties.nonAbsorption2 = 1 - absorption2;
        properties.nonAbsorption3 = 1 - absorption3;
        properties.speedOfSound = 343; // wood
        properties.isDiffractionVolume = (uint)(isDiffractionVolume ? 1 : 0);
        box.properties = properties;
        box.localToWorldMatrix = transform.localToWorldMatrix;
        box.worldToLocalMatrix = transform.localToWorldMatrix.inverse;
    }
    void Start()
    {
        
    }

    private void OnValidate()
    {
        UpdateSurfaceParameters();
    }

    private void UpdateSurfaceParameters()
    {
        switch (surfacePreset)
        {
            case ESurface.Glass:
                absorption0 = 0.30f;
                absorption1 = 0.10f;
                absorption2 = 0.07f;
                absorption3 = 0.02f;
                roughness = 0.02f;
                break;
            case ESurface.Concrete:
                absorption0 = 0.02f;
                absorption1 = 0.03f;
                absorption2 = 0.03f;
                absorption3 = 0.07f;
                roughness = 0.50f;
                break;
            case ESurface.Wood:
                absorption0 = 0.19f;
                absorption1 = 0.25f;
                absorption2 = 0.30f;
                absorption3 = 0.42f;
                roughness = 0.40f;
                break;
            case ESurface.Rubber:
                absorption0 = 0.00f;
                absorption1 = 0.05f;
                absorption2 = 0.10f;
                absorption3 = 0.00f;
                roughness = 0.20f;
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        UpdateSurfaceParameters();
        box.properties.nonAbsorption0 = 1 - absorption0;
        box.properties.nonAbsorption1 = 1 - absorption1;
        box.properties.nonAbsorption2 = 1 - absorption2;
        box.properties.nonAbsorption3 = 1 - absorption3;
        box.properties.isDiffractionVolume = (uint)(isDiffractionVolume ? 1 : 0);
        box.properties.roughness = roughness;
        box.properties.speedOfSound = 343;
        box.localToWorldMatrix = transform.localToWorldMatrix;
        box.worldToLocalMatrix = transform.worldToLocalMatrix;
    }
}
