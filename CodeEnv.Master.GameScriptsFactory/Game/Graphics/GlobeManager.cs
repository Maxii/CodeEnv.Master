// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GlobeManager.cs
// Manages the spherical globes that are a part of Cellestial Bodies.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LEVEL_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Manages the spherical globes that are a part of Cellestial Bodies. Current functionality 
/// covers rotation animation, highlighting and raising FocusSelectedEvents.
/// </summary>
[Serializable, RequireComponent(typeof(SphereCollider), typeof(MeshRenderer))]
public class GlobeManager : MonoBehaviourBase {

    public GlobeMaterialAnimator primaryMaterialAnimator = new GlobeMaterialAnimator { xScrollSpeed = 0.015F, yScrollSpeed = 0.015F };
    public GlobeMaterialAnimator optionalSecondMaterialAnimator = new GlobeMaterialAnimator { xScrollSpeed = -0.015F, yScrollSpeed = -0.015F };

    // Cached references
    private GameEventManager eventMgr;
    private Transform globeTransform;
    private Material primaryMaterial;
    private Material optionalSecondMaterial;
    private Color startingColor;

    void Awake() {
        eventMgr = GameEventManager.Instance;
        globeTransform = transform;
        UpdateRate = UpdateFrequency.Continuous;
    }

    void Start() {
        Renderer globeRenderer = renderer;
        primaryMaterial = globeRenderer.material;
        startingColor = primaryMaterial.color;
        if (globeRenderer.materials.Length > 1) {
            optionalSecondMaterial = globeRenderer.materials[1];
        }
    }

    void Update() {
        if (ToUpdate()) {
            AnimateGlobeRotation();
        }
    }

    private void AnimateGlobeRotation() {
        float time = GameTime.TimeInCurrentSession; // TODO convert animation to __GameTime.DeltaTime * (int)UpdateRate
        primaryMaterialAnimator.Animate(primaryMaterial, time);
        // Added for IOS compatibility? IMPROVE
        if (optionalSecondMaterial != null) {
            optionalSecondMaterialAnimator.Animate(optionalSecondMaterial, time);
        }
    }

    void OnMouseEnter() {
        primaryMaterial.color = Color.black;    // IMPROVE need better highlighting
    }

    void OnMouseExit() {
        primaryMaterial.color = startingColor;
    }

    void OnMouseOver() {
        // Debug.Log("GlobeManager.OnMouseOver() called.");
        if (GameInput.IsMiddleMouseButtonClick()) {
            eventMgr.Raise<FocusSelectedEvent>(new FocusSelectedEvent(this, globeTransform));
        }
    }

    void OnBecameVisible() {
        //Debug.Log("A GlobeManager has become visible.");
        EnableAllScripts(true);
    }

    void OnBecameInvisible() {
        //Debug.LogWarning("A GlobeManager has become invisible.");        
        EnableAllScripts(false);
    }

    // IMPROVE disabling all the scripts in this Sun is not logically part of GlobeMgmt 
    // mission but this script needs a renderer to receive OnBecameInvisible()...
    private void EnableAllScripts(bool toEnable) {
        // acquiring all scripts in advance at startup results in some of them already being destroyed when called during exit
        MonoBehaviour[] allSunScripts = globeTransform.parent.GetComponentsInChildren<MonoBehaviour>();
        foreach (var s in allSunScripts) {
            s.enabled = toEnable;
        }
    }

    [Serializable]
    public class GlobeMaterialAnimator {
        public float xScrollSpeed = 0.015F;
        public float yScrollSpeed = 0.015F;

        internal void Animate(Material material, float time) {
            Vector2 textureOffset = new Vector2(xScrollSpeed * time % 1, yScrollSpeed * time % 1);
            material.SetTextureOffset(UnityConstants.MainDiffuseTexture, textureOffset);
            material.SetTextureOffset(UnityConstants.NormalMapTexture, textureOffset);
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

