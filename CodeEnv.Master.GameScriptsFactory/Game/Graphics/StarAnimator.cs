// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarAnimator.cs
// Animates a Star's globe to simulate rotation.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Animates a Star's globe to simulate rotation.
/// </summary>
public class StarAnimator : AMonoBehaviourBase {

    [SerializeField]
    private GlobeMaterialAnimator primaryMaterialAnimator = new GlobeMaterialAnimator { xScrollSpeed = 0.015F, yScrollSpeed = 0.015F };
    [SerializeField]
    private GlobeMaterialAnimator optionalSecondMaterialAnimator = new GlobeMaterialAnimator { xScrollSpeed = -0.015F, yScrollSpeed = -0.015F };

    // Cached references
    private Material _primaryMaterial;
    private Material _optionalSecondMaterial;

    protected override void Awake() {
        base.Awake();
        UpdateRate = UpdateFrequency.Continuous;
    }

    protected override void Start() {
        base.Start();
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
        float adjustedDeltaTime = GameTime.DeltaTimeWithGameSpeed * (int)UpdateRate; //GameTime.TimeInCurrentSession;
        if (_primaryMaterial != null) {  // OPTIMIZE can remove. Only needed for testing
            primaryMaterialAnimator.Animate(_primaryMaterial, adjustedDeltaTime);
        }
        // Added for IOS compatibility? IMPROVE
        if (_optionalSecondMaterial != null) {
            optionalSecondMaterialAnimator.Animate(_optionalSecondMaterial, adjustedDeltaTime);
        }
    }

    [Serializable]
    public class GlobeMaterialAnimator {
        public float xScrollSpeed;
        public float yScrollSpeed;

        private float _x = Constants.ZeroF;
        private float _y = Constants.ZeroF;

        //internal void Animate(Material material, float time) {
        //    Vector2 textureOffset = new Vector2(xScrollSpeed * time % 1, yScrollSpeed * time % 1);
        //    material.SetTextureOffset(UnityConstants.MainDiffuseTexture, textureOffset);
        //    material.SetTextureOffset(UnityConstants.NormalMapTexture, textureOffset);
        //}

        internal void Animate(Material material, float deltaTime) {
            _x = (_x + (xScrollSpeed * deltaTime)) % 1;
            _y = (_y + (yScrollSpeed * deltaTime)) % 1;
            Vector2 textureOffset = new Vector2(_x, _y);
            material.SetTextureOffset(UnityConstants.MainDiffuseTexture, textureOffset);
            material.SetTextureOffset(UnityConstants.NormalMapTexture, textureOffset);
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

