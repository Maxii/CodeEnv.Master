// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitCommandPresenter.cs
// Abstract, generic base MVPresenter associated with CommandViews.
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
/// Abstract, generic base MVPresenter associated with CommandViews.
/// </summary>
/// <typeparam name="UnitElementModelType">The Type of the derived AUnitElementModel.</typeparam>
public abstract class AUnitCommandPresenter<UnitElementModelType> : AMortalItemPresenter where UnitElementModelType : AUnitElementModel {

    public new AUnitCommandModel<UnitElementModelType> Model {
        get { return base.Model as AUnitCommandModel<UnitElementModelType>; }
        protected set { base.Model = value; }
    }

    protected new ICommandViewable View {
        get { return base.View as ICommandViewable; }
    }

    public AUnitCommandPresenter(ICommandViewable view)
        : base(view) {
        // derived classes should call Subscribe() after they have acquired needed references
    }

    protected override void Subscribe() {
        base.Subscribe();
        _subscribers.Add(Model.SubscribeToPropertyChanged<AUnitCommandModel<UnitElementModelType>, UnitElementModelType>(sb => sb.HQElement, OnHQElementChanged));
        Model.onSubordinateElementDeath += OnSubordinateElementDeath;
    }

    private void OnSubordinateElementDeath(UnitElementModelType element) {
        if (element.gameObject.GetSafeInterface<ICameraFocusable>().IsFocus) {
            // our element that was just destroyed was the focus, so change the focus to the command
            (View as ICameraFocusable).IsFocus = true;
        }
    }

    public void RequestContextMenu(bool isDown) {
        if (DebugSettings.Instance.AllowEnemyOrders || Model.Data.Owner.IsHuman) {
            CameraControl.Instance.ShowContextMenuOnPress(isDown);
        }
    }

    protected override void CleanupOnDeath() {
        base.CleanupOnDeath();
        if ((View as ISelectable).IsSelected) {
            SelectionManager.Instance.CurrentSelection = null;
        }
    }

    public void OnPlayerIntelCoverageChanged() {
        Model.Elements.ForAll(e => e.gameObject.GetSafeInterface<IViewable>().PlayerIntel.CurrentCoverage = View.PlayerIntel.CurrentCoverage);
        AssessCmdIcon();
    }

    private void OnHQElementChanged() {
        View.TrackingTarget = GetTrackingTarget();
    }

    public virtual void OnIsSelectedChanged() {
        if ((View as ISelectable).IsSelected) {
            SelectionManager.Instance.CurrentSelection = View as ISelectable;
        }
        Model.Elements.ForAll(e => e.gameObject.GetSafeInterface<IElementViewable>().AssessHighlighting());
    }

    public Transform GetTrackingTarget() {
        return Model.HQElement.transform;
    }

    protected void AssessCmdIcon() {
        IIcon icon = MakeCmdIconInstance();
        View.ChangeCmdIcon(icon);
    }

    protected abstract IIcon MakeCmdIconInstance();

    // subscriptions contained completely within this gameobject (both subscriber
    // and subscribee) donot have to be cleaned up as all instances are destroyed

}

