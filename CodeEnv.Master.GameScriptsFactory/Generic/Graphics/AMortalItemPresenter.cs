// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMortalItemPresenter.cs
// An abstract base MVPresenter associated with AMortalItem.
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
/// An abstract base MVPresenter associated with AMortalItem.
/// </summary>
public abstract class AMortalItemPresenter : AFocusableItemPresenter {

    public new AMortalItemModel Model {
        get { return base.Model as AMortalItemModel; }
        protected set { base.Model = value; }
    }

    public AMortalItemPresenter(IViewable view) : base(view) { }

    protected override void Subscribe() {
        base.Subscribe();
        Model.onItemDeath += OnDeath;
    }

    protected virtual void OnDeath(AMortalItemModel itemModel) {
        D.Assert(Model == itemModel, "{0} has erroneously received OnDeath from {1}.".Inject(Model.Data.Name, itemModel.Data.Name));
        CleanupOnDeath();
    }

    protected virtual void CleanupOnDeath() {
        if ((View as ICameraFocusable).IsFocus) {
            CameraControl.Instance.CurrentFocus = null;
        }
        // UNDONE other cleanup needed if recycled
    }

    // no need to unsubscribe from internal subscription to Model.onDeath

}

