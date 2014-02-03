﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UniverseCenterPresenter.cs
// An MVPresenter associated with a UniverseCenter View.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// An MVPresenter associated with a UniverseCenter View.
/// </summary>
public class UniverseCenterPresenter : AFocusableItemPresenter {

    public new UniverseCenterModel Item {
        get { return base.Model as UniverseCenterModel; }
        protected set { base.Model = value; }
    }

    public UniverseCenterPresenter(IViewable view) : base(view) { }

    protected override AItemModel AcquireModelReference() {
        return UnityUtility.ValidateMonoBehaviourPresence<UniverseCenterModel>(_viewGameObject);
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        return new GuiHudPublisher<ItemData>(Model.Data);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

