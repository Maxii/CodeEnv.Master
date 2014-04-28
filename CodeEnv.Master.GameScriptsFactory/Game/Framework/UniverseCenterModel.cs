// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UniverseCenterModel.cs
// The Item at the center of the universe.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// The Item at the center of the universe.
/// </summary>
public class UniverseCenterModel : AItemModel, IUniverseCenterModel {

    public new ItemData Data {
        get { return base.Data as ItemData; }
        set { base.Data = value; }
    }

    protected override void Awake() {
        base.Awake();
        (collider as SphereCollider).radius = TempGameValues.UniverseCenterRadius;
        Radius = TempGameValues.UniverseCenterRadius;
        InitializeKeepoutCollider();
        Subscribe();
    }

    private void InitializeKeepoutCollider() {
        SphereCollider keepoutCollider = gameObject.GetComponentInImmediateChildren<SphereCollider>();
        D.Assert(keepoutCollider.gameObject.layer == (int)Layers.CelestialObjectKeepout);
        keepoutCollider.radius = Radius * TempGameValues.KeepoutRadiusMultiplier;
    }

    protected override void Initialize() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

