﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarAnimator.cs
// Animates a Star's globe.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Animates a Star's globe.
/// </summary>
public class StarAnimator : AMonoBehaviourBase {

    [SerializeField]
    private GlobeMaterialAnimator primaryMaterialAnimator = new GlobeMaterialAnimator { xScrollSpeed = 0.015F, yScrollSpeed = 0.015F };
    [SerializeField]
    private GlobeMaterialAnimator optionalSecondMaterialAnimator = new GlobeMaterialAnimator { xScrollSpeed = -0.015F, yScrollSpeed = -0.015F };

    // Cached references
    private Material _primaryMaterial;
    private Material _optionalSecondMaterial;

    void Awake() {
        UpdateRate = UpdateFrequency.Continuous;
    }

    void Start() {
        Renderer globeRenderer = GetComponentInImmediateChildren<MeshRenderer>();
        _primaryMaterial = globeRenderer.material;
        if (globeRenderer.materials.Length > 1) {
            _optionalSecondMaterial = globeRenderer.materials[1];
        }
    }

    void Update() {
        if (ToUpdate()) {
            AnimateGlobeRotation();
        }
    }

    private void AnimateGlobeRotation() {
        float time = GameTime.TimeInCurrentSession;
        if (_primaryMaterial != null) {  // OPTIMIZE can remove. Only needed for testing
            primaryMaterialAnimator.Animate(_primaryMaterial, time);
        }
        // Added for IOS compatibility? IMPROVE
        if (_optionalSecondMaterial != null) {
            optionalSecondMaterialAnimator.Animate(_optionalSecondMaterial, time);
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
