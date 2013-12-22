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
public class StarBasePresenter : AMortalFocusablePresenter {

    public new StarBaseItem Item {
        get { return base.Item as StarBaseItem; }
        protected set { base.Item = value; }
    }

    protected new IStarBaseViewable View {
        get { return base.View as IStarBaseViewable; }
    }

    public StarBasePresenter(IStarBaseViewable view)
        : base(view) {
        Subscribe();
    }

    protected override AItem InitilizeItemLinkage() {
        return UnityUtility.ValidateMonoBehaviourPresence<StarBaseItem>(_viewGameObject);
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        return new GuiHudPublisher<StarBaseData>(Item.Data);
    }

    protected override void Subscribe() {
        base.Subscribe();
        _subscribers.Add(Item.SubscribeToPropertyChanged<StarBaseItem, StarBaseState>(sb => sb.CurrentState, OnStarBaseStateChanged));
        View.onShowCompletion += Item.OnShowCompletion;
    }

    private void OnStarBaseStateChanged() {
        StarBaseState state = Item.CurrentState;
        switch (state) {
            case StarBaseState.ShowDying:
                View.ShowDying();
                break;
            case StarBaseState.Idling:
                // do nothing
                break;
            case StarBaseState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(state));
        }
    }

    public void OnPressWhileSelected(bool isDown) {
        OnPressRequestContextMenu(isDown);
    }

    private void OnPressRequestContextMenu(bool isDown) {
        if (DebugSettings.Instance.AllowEnemyOrders || Item.Data.Owner.IsHuman) {
            CameraControl.Instance.ShowContextMenuOnPress(isDown);
        }
    }

    protected override void OnItemDeath(ItemDeathEvent e) {
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

