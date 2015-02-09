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

        public new ICmdModel Model { get { return base.Model as ICmdModel; } }

        protected new AUnitCmdItemData Data { get { return base.Data as AUnitCmdItemData; } }

        protected new ICommandViewable View {
            get { return base.View as ICommandViewable; }
        }

        public AUnitCommandPresenter(ICommandViewable view)
            : base(view) {
            // derived classes should call Subscribe() after they have acquired needed references
        }

        protected override void Subscribe() {
            base.Subscribe();
            Model.onHQElementChanged += OnHQElementChanged;
            Data.onCompositionChanged += OnCompositionChanged;
        }

        protected override void CleanupOnDeath() {
            base.CleanupOnDeath();
            if ((View as ISelectable).IsSelected) {
                SelectionManager.Instance.CurrentSelection = null;
            }
        }

        public void OnPlayerIntelCoverageChanged() {
            Model.UnitElementModels.ForAll(e => e.Transform.GetSafeInterface<IElementViewable>().PlayerIntel.CurrentCoverage = View.PlayerIntel.CurrentCoverage);
            AssessCmdIcon();
        }

        private void OnCompositionChanged() {
            AssessCmdIcon();
        }

        private void OnHQElementChanged(IElementModel hqElementModel) {
            View.TrackingTarget = hqElementModel.Transform.GetSafeInterface<IWidgetTrackable>();
        }

        public virtual void OnIsSelectedChanged() {
            if ((View as ISelectable).IsSelected) {
                SelectionManager.Instance.CurrentSelection = View as ISelectable;
            }
            Model.UnitElementModels.ForAll(e => e.Transform.GetSafeInterface<IElementViewable>().AssessHighlighting());
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

