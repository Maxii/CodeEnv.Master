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

/// <summary>
/// An MVPresenter associated with a Facility View.
/// </summary>
public class FacilityPresenter : AMortalFocusablePresenter {

    public new FacilityItem Item {
        get { return base.Item as FacilityItem; }
        protected set { base.Item = value; }
    }

    //protected new IFacilityViewable View {
    //    get { return base.View as IFacilityViewable; }
    //}

    protected new IElementViewable View {
        get { return base.View as IElementViewable; }
    }


    //public FacilityPresenter(IFacilityViewable view)
    //    : base(view) {
    //    Subscribe();
    //}

    public FacilityPresenter(IElementViewable view)
        : base(view) {
        Subscribe();
    }


    protected override AItem AcquireItemReference() {
        return UnityUtility.ValidateMonoBehaviourPresence<FacilityItem>(_viewGameObject);
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        return new GuiHudPublisher<FacilityData>(Item.Data);
    }

    protected override void Subscribe() {
        base.Subscribe();
        _subscribers.Add(Item.SubscribeToPropertyChanged<FacilityItem, FacilityState>(sb => sb.CurrentState, OnSettlementStateChanged));
        View.onShowCompletion += Item.OnShowCompletion;
    }

    private void OnSettlementStateChanged() {
        FacilityState state = Item.CurrentState;
        switch (state) {
            case FacilityState.ShowDying:
                View.ShowDying();
                break;
            case FacilityState.Idling:
                // do nothing
                break;
            case FacilityState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(state));
        }
    }

    protected override void OnItemDeath(ItemDeathEvent e) {
        if ((e.Source as FacilityItem) == Item) {
            CleanupOnDeath();
        }
    }

    public void __SimulateAttacked() {
        Item.__SimulateAttacked();
    }

    protected override void CleanupOnDeath() {
        base.CleanupOnDeath();
        // TODO initiate death of a facility
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

