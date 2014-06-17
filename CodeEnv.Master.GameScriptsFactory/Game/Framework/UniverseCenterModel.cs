﻿// --------------------------------------------------------------------------------------------------------------------
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
public class UniverseCenterModel : AItemModel, IUniverseCenterModel, IUniverseCenterTarget, IOrbitable {

    public new ItemData Data {
        get { return base.Data as ItemData; }
        set { base.Data = value; }
    }

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void InitializeRadiiComponents() {
        var meshRenderer = gameObject.GetComponentInImmediateChildren<Renderer>();
        Radius = meshRenderer.bounds.size.x / 2F;    // half of the (length, width or height, all the same surrounding a sphere)
        D.Assert(Mathfx.Approx(Radius, TempGameValues.UniverseCenterRadius, 1F));    // 50
        (collider as SphereCollider).radius = Radius;

        SphereCollider keepoutCollider = gameObject.GetComponentInImmediateChildren<SphereCollider>();
        D.Assert(keepoutCollider.gameObject.layer == (int)Layers.CelestialObjectKeepout);
        keepoutCollider.radius = Radius * TempGameValues.KeepoutRadiusMultiplier;
        OrbitDistance = keepoutCollider.radius + 1F;
    }

    protected override void Initialize() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IOrbitable Members

    public float OrbitDistance { get; private set; }

    #endregion

}

