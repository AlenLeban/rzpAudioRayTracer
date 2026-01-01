using System;
using UnityEngine;

public class RTCylinder : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public FCylinder cylinder;
    private void Awake()
    {
        cylinder.a = gameObject.transform.position + gameObject.transform.up * gameObject.transform.localScale.y;
        cylinder.b = gameObject.transform.position - gameObject.transform.up * gameObject.transform.localScale.y;
        cylinder.radius = 0.5f * gameObject.transform.localScale.x;
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        cylinder.a = gameObject.transform.position + gameObject.transform.up * gameObject.transform.localScale.y;
        cylinder.b = gameObject.transform.position - gameObject.transform.up * gameObject.transform.localScale.y;
        cylinder.radius = 0.5f * gameObject.transform.localScale.x;
    }
}
