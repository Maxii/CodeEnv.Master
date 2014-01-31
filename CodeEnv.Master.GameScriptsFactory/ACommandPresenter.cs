// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ACommandPresenter.cs
// COMMENT - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// COMMENT 
/// </summary>
public abstract class ACommandPresenter<ElementType> : AMortalFocusablePresenter where ElementType : AElement {

    public new ACommandItem<ElementType> Item {
        get { return base.Item as ACommandItem<ElementType>; }
        protected set { base.Item = value; }
    }

    protected new ICommandViewable View {
        get { return base.View as ICommandViewable; }
    }

    public ACommandPresenter(ICommandViewable view)
        : base(view) {
    }

    protected override void Subscribe() {
        base.Subscribe();
        _subscribers.Add(Item.SubscribeToPropertyChanged<ACommandItem<ElementType>, ElementType>(sb => sb.HQElement, OnHQElementChanged));
        //_subscribers.Add(Item.Data.SubscribeToPropertyChanged<StarbaseData, BaseComposition>(sbd => sbd.Composition, OnCompositionChanged));
        //_subscribers.Add(Item.SubscribeToPropertyChanged<StarbaseItem, StarbaseState>(sb => sb.CurrentState, OnStarbaseStateChanged));
        //View.onShowCompletion += Item.OnShowCompletion;
        Item.onElementDestroyed += OnElementDestroyed;
    }

    //private void OnStarbaseStateChanged() {
    //    StarbaseState state = Item.CurrentState;
    //    switch (state) {
    //        case StarbaseState.ShowDying:
    //            View.ShowDying();
    //            break;
    //        case StarbaseState.Dead:
    //        case StarbaseState.Dying:
    //        case StarbaseState.Idling:
    //        case StarbaseState.ProcessOrders:
    //            // do nothing
    //            break;
    //        case StarbaseState.None:
    //        default:
    //            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(state));
    //    }
    //}

    private void OnElementDestroyed(ElementType element) {
        if (element.gameObject.GetSafeInterface<ICameraFocusable>().IsFocus) {
            // our fleet's ship that was just destroyed was the focus, so change the focus to the fleet
            (View as ICameraFocusable).IsFocus = true;
        }
    }

    //public void __SimulateAllElementsAttacked() {
    //    Item.Elements.ForAll<AElement>(e => e.__SimulateAttacked());
    //}

    public void RequestContextMenu(bool isDown) {
        if (DebugSettings.Instance.AllowEnemyOrders || Item.Data.Owner.IsHuman) {
            CameraControl.Instance.ShowContextMenuOnPress(isDown);
        }
    }

    //protected override void OnItemDeath(ItemDeathEvent e) {
    //    if ((e.Source as StarbaseItem) == Item) {
    //        CleanupOnDeath();
    //    }
    //}

    protected override void CleanupOnDeath() {
        base.CleanupOnDeath();
        if ((View as ISelectable).IsSelected) {
            SelectionManager.Instance.CurrentSelection = null;
        }
    }

    //private void OnCompositionChanged() {
    //    AssessCmdIcon();
    //}

    public void OnPlayerIntelCoverageChanged() {
        Item.Elements.ForAll(e => e.gameObject.GetSafeInterface<IViewable>().PlayerIntel.CurrentCoverage = View.PlayerIntel.CurrentCoverage);
        AssessCmdIcon();
    }

    private void OnHQElementChanged() {
        View.TrackingTarget = GetTrackingTarget();
    }

    public virtual void OnIsSelectedChanged() {
        if ((View as ISelectable).IsSelected) {
            SelectionManager.Instance.CurrentSelection = View as ISelectable;
        }
        //Item.Elements.ForAll(s => s.gameObject.GetSafeMonoBehaviourComponent<FacilityView>().AssessHighlighting());
    }

    public Transform GetTrackingTarget() {
        return Item.HQElement.transform;
    }

    //private StarbaseIconFactory _iconFactory = StarbaseIconFactory.Instance;

    //private void AssessCmdIcon() {
    //    IIcon icon = _iconFactory.MakeInstance(Item.Data, View.PlayerIntel);
    //    View.ChangeCmdIcon(icon);
    //}

    protected void AssessCmdIcon() {
        IIcon icon = MakeCommandIconInstance();
        View.ChangeCmdIcon(icon);
    }

    protected abstract IIcon MakeCommandIconInstance();

}

