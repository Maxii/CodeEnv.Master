﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarItem.cs
// The data-holding class for all Stars in the game.
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
/// The data-holding class for all Stars in the game.
/// </summary>
public class StarModel : AItemModel, IStarModel {

    public new StarData Data {
        get { return base.Data as StarData; }
        set { base.Data = value; }
    }

    protected override void Awake() {
        base.Awake();
        (collider as SphereCollider).radius = TempGameValues.StarRadius;
        Radius = TempGameValues.StarRadius;
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

