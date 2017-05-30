// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AHull.cs
// Abstract base class for an element hull.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Abstract base class for an element hull.
/// </summary>
public abstract class AHull : AMonoBase, IHull {

    public string DebugName { get { return GetType().Name; } }

    //[FormerlySerializedAs("hullMesh")]
    [SerializeField]
    private Transform _hullMesh = null;

    public Transform HullMesh { get { return _hullMesh; } }

    protected abstract int MaxAllowedLosWeapons { get; }

    protected abstract int MaxAllowedLaunchedWeapons { get; }

    protected sealed override void Awake() {
        base.Awake();
        Validate();
    }

    protected virtual void Validate() {
        D.AssertNotNull(_hullMesh);
        /*****************************************************************************************************************************
                    * TODO Can't do this test now as multiple HullCategories use the same hull mesh. To avoid excess work positioning
                    * mount placeholders on hull meshes, I've only made one version of each hull with prepositioned mount placeholders.
                    *****************************************************************************************************************************/
        //var losMountPlaceholders = gameObject.GetSafeMonoBehavioursInChildren<LOSMountPlaceholder>();
        //D.Assert(losMountPlaceholders.Count() == MaxAllowedLosWeapons);
        //var missileMountPlaceholders = gameObject.GetSafeMonoBehavioursInChildren<MissileMountPlaceholder>();
        //D.Assert(missileMountPlaceholders.Count() == MaxAllowedMissileWeapons);
    }

    public sealed override string ToString() {
        return DebugName;
    }
}

