// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMortalItemPresenter.cs
// An abstract base MVPresenter associated with AMortalItemView.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// An abstract base MVPresenter associated with AMortalItemView.
/// </summary>
public abstract class AMortalItemPresenter : AFocusableItemPresenter {

    public new AMortalItemModel Model {
        get { return base.Model as AMortalItemModel; }
        protected set { base.Model = value; }
    }

    protected new IMortalViewable View {
        get { return base.View as IMortalViewable; }
    }

    public AMortalItemPresenter(IMortalViewable view)
        : base(view) {
        IsAlive = true;
    }

    public bool IsAlive { get; private set; }

    protected override void Subscribe() {
        base.Subscribe();
        Model.onItemDeath += OnDeath;
        Model.onShowAnimation += View.ShowAnimation;
        Model.onStopAnimation += View.StopAnimation;
        View.onShowCompletion += Model.OnShowCompletion;
    }

    protected virtual void OnDeath(IMortalModel itemModel) {
        // Equals avoids erroneous warning
        D.Assert(Model.Equals(itemModel), "{0} has erroneously received OnDeath from {1}.".Inject(Model.Data.Name, itemModel.Name));
        CleanupOnDeath();
    }

    protected virtual void CleanupOnDeath() {
        IsAlive = false;
        View.AssessDiscernability();
        CleanupFocusOnDeath();
        // UNDONE other cleanup needed if recycled
    }

    protected virtual void CleanupFocusOnDeath() {
        var focusableView = View as ICameraFocusable;
        if (focusableView.IsFocus) {
            _cameraControl.CurrentFocus = null;
        }
    }

    public void __SimulateAttacked() {
        Model.__SimulateAttacked();
    }

    // no need to unsubscribe from internal subscription to Model.onDeath

}

