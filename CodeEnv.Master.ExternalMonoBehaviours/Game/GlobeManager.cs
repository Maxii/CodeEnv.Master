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
using CodeEnv.Master.Common.UI;

[Serializable]
[RequireComponent(typeof(SphereCollider), typeof(MeshRenderer))]
/// <summary>
/// Manages the spherical globes that are a part of Cellestial Bodies. Current functionality 
/// covers rotation animation, highlighting and raising FocusSelectedEvents.
/// </summary>
public class GlobeManager : MonoBehaviour {

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
        AnimateGlobeRotation();
    }

    private void AnimateGlobeRotation() {
        float time = Time.time;
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
        if (GameInput.IsMiddleMouseButtonDown()) {
            eventMgr.Raise<FocusSelectedEvent>(new FocusSelectedEvent(globeTransform));
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

