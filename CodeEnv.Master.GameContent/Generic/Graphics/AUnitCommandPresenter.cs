// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitCommandPresenter.cs
// Abstract base MVPresenter associated with CommandViews.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract base MVPresenter associated with CommandViews.
    /// </summary>
    public abstract class AUnitCommandPresenter : AMortalItemPresenter {

        public new ICommandModel Model {
            get { return base.Model as ICommandModel; }
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
            _subscribers.Add(Model.SubscribeToPropertyChanged<ICommandModel, IElementModel>(sb => sb.HQElement, OnHQElementChanged));
            Model.onElementsInitializationCompleted_OneShot += OnElementsInitializationCompleted;
            Model.onSubordinateElementDeath += OnSubordinateElementDeath;
            Model.Data.onCompositionChanged += OnCompositionChanged;
        }

        private void OnElementsInitializationCompleted() {
            View.Radius = GetHQElementRadius();
        }

        private void OnSubordinateElementDeath(IElementModel element) {
            if (element.Transform.GetSafeInterface<ICameraFocusable>().IsFocus) {
                // our element that was just destroyed was the focus, so change the focus to the command
                (View as ICameraFocusable).IsFocus = true;
            }
        }

        public void RequestContextMenu(bool isDown) {
            if (DebugSettings.Instance.AllowEnemyOrders || Model.Data.Owner.IsHuman) {
                _cameraControl.ShowContextMenuOnPress(isDown);
            }
        }

        protected override void CleanupOnDeath() {
            base.CleanupOnDeath();
            if ((View as ISelectable).IsSelected) {
                SelectionManager.Instance.CurrentSelection = null;
            }
        }

        public void OnPlayerIntelCoverageChanged() {
            Model.Elements.ForAll(e => e.Transform.GetSafeInterface<IViewable>().PlayerIntel.CurrentCoverage = View.PlayerIntel.CurrentCoverage);
            AssessCmdIcon();
        }

        private void OnCompositionChanged() {
            AssessCmdIcon();
        }

        private void OnHQElementChanged() {
            View.TrackingTarget = GetTrackingTarget();
            View.Radius = GetHQElementRadius();
        }

        public virtual void OnIsSelectedChanged() {
            if ((View as ISelectable).IsSelected) {
                SelectionManager.Instance.CurrentSelection = View as ISelectable;
            }
            Model.Elements.ForAll(e => e.Transform.GetSafeInterface<IElementViewable>().AssessHighlighting());
        }

        public Transform GetTrackingTarget() {
            return Model.HQElement.Transform;
        }

        public float GetHQElementRadius() {
            D.Assert(Model.HQElement.Radius != Constants.ZeroF, "{0}.Radius has not been initialized yet.".Inject(Model.HQElement.FullName));
            return Model.HQElement.Radius;
        }

        private void AssessCmdIcon() {
            IIcon icon = MakeCmdIconInstance();
            View.ChangeCmdIcon(icon);
        }

        protected abstract IIcon MakeCmdIconInstance();

        // subscriptions contained completely within this gameobject (both subscriber
        // and subscribee) donot have to be cleaned up as all instances are destroyed
    }
}

