// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitElementPresenter.cs
// Abstract base MVPresenter associated with ElementViews.
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
///  Abstract base MVPresenter associated with ElementViews.
/// </summary>
public abstract class AUnitElementPresenter : AMortalItemPresenter {

    public new AUnitElementModel Model {
        get { return base.Model as AUnitElementModel; }
        protected set { base.Model = value; }
    }

    protected new IElementViewable View {
        get { return base.View as IElementViewable; }
    }

    protected ICommandViewable _commandView;

    public AUnitElementPresenter(IElementViewable view)
        : base(view) {
        GameObject viewParent = _viewGameObject.transform.parent.gameObject;
        _commandView = viewParent.GetSafeInterfaceInChildren<ICommandViewable>();
        // derived classes should call Subscribe() after they have acquired needed references
    }

    protected override void Subscribe() {
        base.Subscribe();
        View.onShowCompletion += Model.OnShowCompletion;
    }

    public bool IsCommandSelected {
        get { return (_commandView as ISelectable).IsSelected; }
        set { (_commandView as ISelectable).IsSelected = value; }
    }

    public void __SimulateAttacked() {
        Model.__SimulateAttacked();
    }

    // subscriptions contained completely within this gameobject (both subscriber
    // and subscribee) donot have to be cleaned up as all instances are destroyed

}

