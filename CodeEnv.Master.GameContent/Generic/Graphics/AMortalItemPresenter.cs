﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMortalItemPresenter.cs
// An abstract base MVPresenter associated with AMortalItemView.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// An abstract base MVPresenter associated with AMortalItemView.
    /// </summary>
    public abstract class AMortalItemPresenter : AFocusableItemPresenter {

        public new IMortalModel Model { get { return base.Model as IMortalModel; } }

        protected new IMortalViewable View { get { return base.View as IMortalViewable; } }

        public AMortalItemPresenter(IMortalViewable view)
            : base(view) {
        }

        protected override void Subscribe() {
            base.Subscribe();
            Model.onDeathOneShot += OnDeath;
            Model.onShowAnimation += View.ShowAnimation;
            Model.onStopAnimation += View.StopAnimation;
            View.onShowCompletion += Model.OnShowCompletion;
        }

        protected virtual void OnDeath(IMortalModel itemModel) {
            // Equals avoids erroneous warning
            D.Assert(Model.Equals(itemModel), "{0} has erroneously received OnDeath from {1}.".Inject(Model.FullName, itemModel.FullName));
            CleanupOnDeath();
        }

        protected virtual void CleanupOnDeath() {
            View.OnDeath();
            if ((View as ICameraFocusable).IsFocus) {
                CleanupFocusOnDeath();
            }
            // UNDONE other cleanup needed if recycled
        }

        protected virtual void CleanupFocusOnDeath() {
            _cameraControl.CurrentFocus = null;
        }

        public void __SimulateAttacked() {
            Model.__SimulateAttacked();
        }

        // no need to unsubscribe from internal subscription to Model.onDeath
    }
}

