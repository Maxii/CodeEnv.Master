// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: EditModeController.cs
// Debug script that allows control of various settings during Edit Mode.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Linq;
using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Debug script that allows control of various settings during Edit Mode.
/// </summary>
[ExecuteInEditMode]
public class EditModeController : AMonoBase {

    public bool enableRenderers = false;

    void Update() {
        UpdateRenderers();
    }

    private void UpdateRenderers() {
        var renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Any()) {
            renderers.ForAll(r => {
                r.enabled = enableRenderers;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            });
        }
        // OPTIMIZE Can disable GridFramework Renderer to not show grid if choose to customize
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

