﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementCmdPresenter.cs
//  An MVPresenter associated with a Settlement View.
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
///  An MVPresenter associated with a Settlement View.
/// </summary>
public class SettlementCmdPresenter : AUnitCommandPresenter<FacilityModel> {

    public new SettlementCmdModel Model {
        get { return base.Model as SettlementCmdModel; }
        protected set { base.Model = value; }
    }

    public SettlementCmdPresenter(ICommandViewable view)
        : base(view) {
        Subscribe();
    }

    protected override AItemModel AcquireModelReference() {
        return UnityUtility.ValidateMonoBehaviourPresence<SettlementCmdModel>(_viewGameObject);
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        return new GuiHudPublisher<SettlementCmdData>(Model.Data);
    }

    protected override void Subscribe() {
        base.Subscribe();
        _subscribers.Add(Model.Data.SubscribeToPropertyChanged<SettlementCmdData, BaseComposition>(sbd => sbd.Composition, OnCompositionChanged));
    }

    private void OnCompositionChanged() {
        AssessCmdIcon();
    }

    protected override IIcon MakeCmdIconInstance() {
        return SettlementIconFactory.Instance.MakeInstance(Model.Data, View.PlayerIntel);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}
