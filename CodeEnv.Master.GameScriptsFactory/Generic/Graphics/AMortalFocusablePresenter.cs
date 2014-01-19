// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMortalFocusablePresenter.cs
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
public abstract class AMortalFocusablePresenter : AFocusablePresenter {

    public new AMortalItem Item {
        get { return base.Item as AMortalItem; }
        protected set { base.Item = value; }
    }

    protected GameEventManager _eventMgr;

    public AMortalFocusablePresenter(IViewable view)
        : base(view) {
        _eventMgr = GameEventManager.Instance;
    }

    protected override void Subscribe() {
        base.Subscribe();
        _eventMgr.AddListener<ItemDeathEvent>(this, OnItemDeath);
    }

    public bool IsDetectable {
        get { return true; }
        // TODO assess detectability of this view for the HumanPlayer
        // include Item.StealthMode, Item.Owner and whether in range
        // of humanPlayer's sensors
    }

    protected abstract void OnItemDeath(ItemDeathEvent e);

    protected virtual void CleanupOnDeath() {
        if ((View as ICameraFocusable).IsFocus) {
            CameraControl.Instance.CurrentFocus = null;
        }
    }

    protected override void Unsubscribe() {
        base.Unsubscribe();
        _eventMgr.RemoveListener<ItemDeathEvent>(this, OnItemDeath);
    }

}

