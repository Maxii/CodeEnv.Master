// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarGlowAnimator.cs
// Runs the texture that simulates a 'glow' surrounding a globe.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_WARN
#define DEBUG_ERROR

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Runs the texture that simulates a 'glow' surrounding a globe.
/// </summary>
[Serializable]
public class StarGlowAnimator : AMonoBase {
    private static Vector2[] materialOrientationChoices = new Vector2[] {
        new Vector2(1, 1),
        new Vector2(1, -1),
        new Vector2(-1, -1),
        new Vector2(-1, 1)
    };

    public int rotationSpeedAndDirection = 2;   // TODO direction needs to be opposite the other Animator

    protected override void Awake() {
        base.Awake();
        UpdateRate = FrameUpdateFrequency.Frequent;
    }

    protected override void Start() {
        base.Start();
        RandomizeMaterialOrientation();
    }

    private void RandomizeMaterialOrientation() {
        Vector2 materialOrientation = RandomExtended<Vector2>.Choice(materialOrientationChoices);
        renderer.material.SetTextureScale(UnityConstants.MainDiffuseTexture, materialOrientation);
    }

    protected override void OccasionalUpdate() {
        base.OccasionalUpdate();
        float adjDeltaTime = GameTime.DeltaTimeWithGameSpeed * (int)UpdateRate;
        _transform.Rotate(Vector3.up * adjDeltaTime * rotationSpeedAndDirection);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }
}


