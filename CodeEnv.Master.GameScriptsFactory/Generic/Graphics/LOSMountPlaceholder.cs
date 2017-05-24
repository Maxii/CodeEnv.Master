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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Placeholder for Line Of Sight Weapon Mounts. 
/// </summary>
public class LOSMountPlaceholder : AMountPlaceholder {

    //[FormerlySerializedAs("minimumBarrelElevation")]
    [Range(-20F, 70F)]
    [Tooltip("Minimum allowed elevation of the barrel in degrees. 0 elevation is horizontal to the plane of the placeholder.")]
    [SerializeField]
    private float _minBarrelElevation = Constants.ZeroF;

    public float MinimumBarrelElevation { get { return _minBarrelElevation; } }

    protected override void Validate() {
        base.Validate();
        Utility.ValidateForRange(_minBarrelElevation, TempGameValues.MinimumBarrelElevationRange);
    }

    protected override void Cleanup() { }


}

