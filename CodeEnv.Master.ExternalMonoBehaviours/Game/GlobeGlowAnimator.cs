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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using CodeEnv.Master.Common.Unity;
using CodeEnv.Master.Common;

/// <summary>
/// Runs the texture that simulates a 'glow' surrounding a globe.
/// </summary>
public class GlobeGlowAnimator : MonoBehaviour {

    public int rotationSpeedAndDirection = 2;
    private Transform glowPanelTransform;

    void Awake() {
        glowPanelTransform = transform;
    }

    void Start() {
        RandomizeRotationStartDegrees();
    }

    private void RandomizeRotationStartDegrees() {
        System.Random rng = new System.Random();
        int xVector2 = (rng.Next(-1, 1) < 0 ? -1 : 1);
        int yVector2 = (rng.Next(-1, 1) < 0 ? -1 : 1);

        renderer.material.SetTextureScale(UnityConstants.MainDiffuseTexture, new Vector2(xVector2, yVector2));
    }

    void Update() {
        glowPanelTransform.Rotate(Vector3.up * Time.deltaTime * rotationSpeedAndDirection);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }
}


