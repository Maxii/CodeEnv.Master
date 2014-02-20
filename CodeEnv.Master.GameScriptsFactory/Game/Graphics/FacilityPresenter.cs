// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityPresenter.cs
// An MVPresenter associated with a Facility View.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// An MVPresenter associated with a Facility View.
/// </summary>
public class FacilityPresenter : AUnitElementPresenter {

    public new FacilityModel Model {
        get { return base.Model as FacilityModel; }
        protected set { base.Model = value; }
    }

    public FacilityPresenter(IElementViewable view)
        : base(view) {
        Subscribe();
    }

    protected override AItemModel AcquireModelReference() {
        return UnityUtility.ValidateMonoBehaviourPresence<FacilityModel>(_viewGameObject);
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        var publisher = new GuiHudPublisher<FacilityData>(Model.Data);
        publisher.SetOptionalUpdateKeys(GuiHudLineKeys.Health);
        return publisher;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

