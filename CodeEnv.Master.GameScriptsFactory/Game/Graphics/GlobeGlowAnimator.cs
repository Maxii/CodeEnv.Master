// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GlobeGlowAnimator.cs
// Runs the texture that simulates a 'glow' surrounding a globe.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

using System;
using UnityEngine;
using CodeEnv.Master.Common.Unity;
using CodeEnv.Master.Common;

/// <summary>
/// Runs the texture that simulates a 'glow' surrounding a globe.
/// </summary>
[Serializable]
public class GlobeGlowAnimator : MonoBehaviourBase {
    private static Vector2[] materialOrientationChoices = new Vector2[] {
        new Vector2(1, 1),
        new Vector2(1, -1),
        new Vector2(-1, -1),
        new Vector2(-1, 1)
    };

    public int rotationSpeedAndDirection = 2;   // TODO direction needs to be opposite the other Animator
    private Transform glowPanelTransform;

    void Awake() {
        glowPanelTransform = transform;
        UpdateRate = UpdateFrequency.Normal;
    }

    void Start() {
        RandomizeMaterialOrientation();
    }

    private void RandomizeMaterialOrientation() {
        Vector2 materialOrientation = RandomExtended<Vector2>.Choice(materialOrientationChoices);
        renderer.material.SetTextureScale(UnityConstants.MainDiffuseTexture, materialOrientation);
    }

    void Update() {
        if (ToUpdate()) {
            float adjDeltaTime = GameTime.DeltaTime * (int)UpdateRate;
            glowPanelTransform.Rotate(Vector3.up * adjDeltaTime * rotationSpeedAndDirection);
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }
}


