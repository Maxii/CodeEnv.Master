﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SectorView.cs
// A class for managing the UI of a Sector.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// A class for managing the UI of a Sector.
/// </summary>
public class SectorView : AItemView {

    public SectorPresenter Presenter { get; private set; }

    protected override void Awake() {
        base.Awake();
        Subscribe();
        Radius = TempGameValues.SectorDiagonalLength * 0.5F;
    }

    protected override IIntel InitializePlayerIntel() {
        return new ImprovingIntel();
    }

    protected override void SubscribeToPlayerIntelCoverageChanged() {
        _subscribers.Add((PlayerIntel as ImprovingIntel).SubscribeToPropertyChanged<ImprovingIntel, IntelCoverage>(pi => pi.CurrentCoverage, OnPlayerIntelCoverageChanged));
    }

    // TODO meshes and animations need to be added to sectors
    // UNCLEAR include a separate CullingLayer for Sector meshes and animations?

    protected override void InitializePresenter() {
        Presenter = new SectorPresenter(this);
    }

    protected override void Cleanup() {
        base.Cleanup();
        Presenter.Dispose();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    //#region IViewable Members

    //protected float _radius;
    ///// <summary>
    ///// The [float] radius of this object in units measured as the distance from the 
    /////center to the min or max extent. As bounds is a bounding box it is the longest 
    ///// diagonal from the center to a corner of the box. Most of the time, the collider can be
    ///// used to calculate this size, assuming it doesn't change size dynmaically. 
    ///// Alternatively, a mesh can be used.
    ///// </summary>
    //public override float Radius {
    //    get {
    //        if (_radius == Constants.ZeroF) {
    //            _radius = TempGameValues.SectorDiagonalLength / 2F;
    //        }
    //        return _radius;
    //    }
    //}

    //#endregion

}

