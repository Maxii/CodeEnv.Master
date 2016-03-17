// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarFlare.cs
// Varies a Star's flare style and intensity as a function of distance from the camera.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Varies a Star's flare style and intensity as a function of distance from the camera.
/// </summary>
[System.Obsolete]
public class StarFlare : AMonoBase {

    private static System.Random rng = new System.Random(); // IMPROVE convert to RandomExtensions

    public Flare[] flares;
    public float flareIntensity = 1F;
    private Light _flareLight;   // can be null if no flares are attached
    private float _originalIntensity;
    private Transform _mainCamera;

    protected override void Awake() {
        base.Awake();
        _mainCamera = Camera.main.transform;
        UpdateRate = FrameUpdateFrequency.Seldom;
    }

    protected override void Start() {
        base.Start();
        if (Utility.CheckForContent<Flare>(flares)) {
            // if flares doesn't contain any flares, it means I don't want to use any right now
            CreateFlare();
        }
    }

    private void CreateFlare() {
        Light[] lights = gameObject.GetComponentsInChildren<Light>();
        Utility.ValidateNotNullOrEmpty<Light>(lights);

        int lightCount = lights.Length;
        if (lightCount == 1) {
            // there is only the primary light attached, so I need to create another for the flare
            // avoid getting the flareLight prefab with Resources.Load("Lights/FlareLight")
            GameObject flareLightGo = UnityUtility.AddChild(transform.gameObject, UsefulTools.Instance.flareLight.gameObject);
            _flareLight = flareLightGo.GetComponent<Light>();
            float starRadius = gameObject.GetSingleComponentInParents<StarItem>().Radius;
            Vector3 flareLightLocationBehindStar = Vector3.forward * (starRadius + 2F);
            flareLightGo.transform.localPosition = flareLightLocationBehindStar;
        }
        else if (lightCount == 2) {
            _flareLight = lights[1];
        }
        else {
            D.Warn("There are more lights attached than needed: {0}".Inject(lightCount));
            _flareLight = lights[1];
        }
        _flareLight.range = Constants.ZeroF;
        _flareLight.flare = flares[rng.Next(flares.Length)];
        _originalIntensity = _flareLight.intensity;
    }

    protected override void OccasionalUpdate() {
        base.OccasionalUpdate();
        if (_flareLight != null) {
            VaryFlareIntensityByCameraDistance();
        }
    }

    private void VaryFlareIntensityByCameraDistance() {
        float flareIntensityFactor = Mathf.Pow(Mathf.Clamp01(-Vector3.Dot(_mainCamera.forward, transform.forward)), Constants.OneHundredPercent / flareIntensity);
        _flareLight.intensity = _originalIntensity * flareIntensityFactor;
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

