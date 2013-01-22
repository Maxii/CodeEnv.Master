// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: BillboardManager.cs
// Manages the Billboards that are a part of Cellestial Bodies. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Resources;
using CodeEnv.Master.Common.Unity;

/// <summary>
/// Manages the Billboards that are a part of Cellestial Bodies. Current functionality 
/// covers CameraFacing.
/// </summary>
public class BillboardManager : MonoBehaviourBase {

    private static System.Random rng = new System.Random();

    public Flare[] flares;
    public float flareIntensity = 1F;
    private Light flareLight;   // can be null if no flares are attached
    private float flareOriginalIntensity;

    private CameraFacing cameraFacing;
    private Transform billboardTransform;
    private Transform cameraTransform;

    void Awake() {
        billboardTransform = transform;
    }

    void Start() {
        // Keep at a minimum, an empty Start method so that instances receive the OnDestroy event
        cameraTransform = Camera.main.transform;
        cameraFacing = new CameraFacing(billboardTransform, cameraTransform);
        if (Utility.CheckForContent<Flare>(flares)) {
            // if flares doesn't contain any flares, it means I don't want to use any right now
            CreateFlare();
        }
    }

    private void CreateFlare() {
        Light[] lights = gameObject.GetComponentsInChildren<Light>();
        Arguments.ValidateNotNullOrEmpty<Light>(lights);

        int lightCount = lights.Length;
        if (lightCount == 1) {
            // there is only the primary light attached, so I need to create another for the flare
            GameObject flareLightPrefab = Resources.Load("Lights/FlareLight") as GameObject;
            GameObject flareLightGo = Instantiate(flareLightPrefab);
            flareLightGo.name = "FlareLight";   // renaming from FlareLight(Clone)
            flareLightGo.transform.parent = billboardTransform;
            flareLightGo.transform.localPosition = Vector3.forward * 2; // place behind the billboard relative to camera
            flareLight = flareLightGo.GetComponent<Light>();
        }
        else if (lightCount == 2) {
            flareLight = lights[1];
        }
        else {
            Debug.LogWarning("There are more lights attached than needed: {0}".Inject(lightCount));
            flareLight = lights[1];
        }
        flareLight.range = Constants.ZeroF;
        flareLight.flare = flares[rng.Next(flares.Length)];
        flareOriginalIntensity = flareLight.intensity;
    }

    void Update() {
        cameraFacing.UpdateFacing();
        if (flareLight) {
            float flareIntensityFactor = Mathf.Pow(Mathf.Clamp01(-Vector3.Dot(cameraTransform.forward, billboardTransform.forward)), Constants.OneHundredPercent / flareIntensity);
            flareLight.intensity = flareOriginalIntensity * flareIntensityFactor;
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

