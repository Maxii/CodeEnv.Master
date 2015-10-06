// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: LOSMountPlaceholder.cs
// Placeholder for Line Of Sight Weapon Mounts. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Placeholder for Line Of Sight Weapon Mounts. 
/// </summary>
public class LOSMountPlaceholder : AMountPlaceholder {

    [Range(-20F, 70F)]
    [Tooltip("Minimum allowed elevation of the barrel in degrees. 0 elevation is horizontal to the plane of the placeholder.")]
    public float minimumBarrelElevation = 0F;

    protected override void Validate() {
        base.Validate();
        Arguments.ValidateForRange(minimumBarrelElevation, -20F, 70F);
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

