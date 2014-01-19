// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementPresenter.cs
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

/// <summary>
///  An MVPresenter associated with a Settlement View.
/// </summary>
public class SettlementPresenter : AMortalFocusablePresenter {

    public new SettlementItem Item {
        get { return base.Item as SettlementItem; }
        protected set { base.Item = value; }
    }

    protected new ISettlementViewable View {
        get { return base.View as ISettlementViewable; }
    }

    public SettlementPresenter(ISettlementViewable view)
        : base(view) {
        Subscribe();
    }

    protected override AItem AcquireItemReference() {
        return UnityUtility.ValidateMonoBehaviourPresence<SettlementItem>(_viewGameObject);
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        return new GuiHudPublisher<SettlementData>(Item.Data);
    }

    protected override void Subscribe() {
        base.Subscribe();
        _subscribers.Add(Item.SubscribeToPropertyChanged<SettlementItem, SettlementState>(sb => sb.CurrentState, OnSettlementStateChanged));
        View.onShowCompletion += Item.OnShowCompletion;
    }

    private void OnSettlementStateChanged() {
        SettlementState state = Item.CurrentState;
        switch (state) {
            case SettlementState.ShowDying:
                View.ShowDying();
                break;
            case SettlementState.Idling:
                // do nothing
                break;
            case SettlementState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(state));
        }
    }

    protected override void OnItemDeath(ItemDeathEvent e) {
        if ((e.Source as SettlementItem) == Item) {
            CleanupOnDeath();
        }
    }

    protected override void CleanupOnDeath() {
        base.CleanupOnDeath();
        // TODO initiate death of a settlement
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

