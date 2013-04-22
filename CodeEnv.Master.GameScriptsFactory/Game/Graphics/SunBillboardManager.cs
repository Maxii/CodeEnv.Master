// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SunBillboardManager.cs
// Manages the Billboards that are a part of Cellestial Bodies. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR


// default namespace

using System;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Manages the Billboards that are a part of Cellestial Bodies. Current functionality 
/// covers CameraFacing.
/// </summary>
public class SunBillboardManager : BillboardManager {

    private static System.Random rng = new System.Random(); // IMPROVE convert to RandomExtensions

    public Flare[] flares;
    public float flareIntensity = 1F;
    private Light flareLight;   // can be null if no flares are attached
    private float flareOriginalIntensity;

    protected override void InitializeOnStart() {
        base.InitializeOnStart();
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
            // avoid getting the flareLight prefab with Resources.Load("Lights/FlareLight")
            flareLight = Instantiate<Light>(UsefulPrefabs.currentInstance.flareLight);
            flareLight.transform.parent = billboardTransform;
            flareLight.transform.localPosition = Vector3.forward * 2;
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

    protected override void ProcessUpdate() {
        base.ProcessUpdate();
        if (flareLight) {
            VaryFlareIntensityByCameraDistance();
        }
    }

    private void VaryFlareIntensityByCameraDistance() {
        float flareIntensityFactor = Mathf.Pow(Mathf.Clamp01(-Vector3.Dot(cameraTransform.forward, billboardTransform.forward)), Constants.OneHundredPercent / flareIntensity);
        flareLight.intensity = flareOriginalIntensity * flareIntensityFactor;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

