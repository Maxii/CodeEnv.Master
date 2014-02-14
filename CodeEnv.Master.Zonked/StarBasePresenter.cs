// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarBasePresenter.cs
// An MVPresenter associated with a StarBaseView.
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
/// An MVPresenter associated with a StarBaseView.
/// </summary>
public class StarBasePresenter : AMortalItemPresenter {

    public new StarBaseItem Item {
        get { return base.Model as StarBaseItem; }
        protected set { base.Model = value; }
    }

    protected new IStarBaseViewable View {
        get { return base.View as IStarBaseViewable; }
    }

    public StarBasePresenter(IStarBaseViewable view)
        : base(view) {
        Subscribe();
    }

    protected override AItemModel AcquireModelReference() {
        return UnityUtility.ValidateMonoBehaviourPresence<StarBaseItem>(_viewGameObject);
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        return new GuiHudPublisher<StarBaseData>(Item.Data);
    }

    protected override void Subscribe() {
        base.Subscribe();
        _subscribers.Add(Item.SubscribeToPropertyChanged<StarBaseItem, StarbaseState>(sb => sb.CurrentState, OnStarBaseStateChanged));
        View.onShowCompletion += Item.OnShowCompletion;
    }

    private void OnStarBaseStateChanged() {
        StarbaseState state = Item.CurrentState;
        switch (state) {
            case StarbaseState.ShowDying:
                View.ShowDying();
                break;
            case StarbaseState.Idling:
                // do nothing
                break;
            case StarbaseState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(state));
        }
    }

    public void OnPressWhileSelected(bool isDown) {
        OnPressRequestContextMenu(isDown);
    }

    private void OnPressRequestContextMenu(bool isDown) {
        if (DebugSettings.Instance.AllowEnemyOrders || Item.Data.Owner.IsHuman) {
            _cameraControl.Instance.ShowContextMenuOnPress(isDown);
        }
    }

    protected override void OnItemDeath(MortalItemDeathEvent e) {
        if ((e.Source as StarBaseItem) == Item) {
            CleanupOnDeath();
        }
    }

    protected override void CleanupOnDeath() {
        base.CleanupOnDeath();
        if ((View as ISelectable).IsSelected) {
            SelectionManager.Instance.CurrentSelection = null;
        }
    }

    public void OnIsSelectedChanged() {
        if ((View as ISelectable).IsSelected) {
            SelectionManager.Instance.CurrentSelection = View as ISelectable;
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

