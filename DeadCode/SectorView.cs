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

    // Sectors donot have colliders. Context menu actuation comes from SectorExaminer

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override IIntel InitializePlayerIntel() {
        return new ImprovingIntel();
    }

    protected override void InitializeVisualMembers() {
        //TODO meshes and animations need to be added to sectors
        // UNCLEAR include a separate CullingLayer for Sector meshes and animations?   
    }

    protected override void SubscribeToPlayerIntelCoverageChanged() {
        _subscribers.Add((PlayerIntel as ImprovingIntel).SubscribeToPropertyChanged<ImprovingIntel, IntelCoverage>(pi => pi.CurrentCoverage, OnPlayerIntelCoverageChanged));
    }

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

}

