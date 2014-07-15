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

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    ///  Abstract base MVPresenter associated with ElementViews.
    /// </summary>
    public abstract class AUnitElementPresenter : AMortalItemPresenter {

        public new IElementModel Model { get { return base.Model as IElementModel; } }

        protected new IElementViewable View { get { return base.View as IElementViewable; } }

        protected ICommandViewable _commandView;

        public AUnitElementPresenter(IElementViewable view)
            : base(view) {
            // derived classes should call Subscribe() after they have acquired needed references
        }

        protected override void Subscribe() {
            base.Subscribe();
            Model.onCommandChanged += OnCommandChanged;
        }

        public bool IsCommandSelected {
            get { return (_commandView as ISelectable).IsSelected; }
            set { (_commandView as ISelectable).IsSelected = value; }
        }

        private void OnCommandChanged(ICmdModel cmdModel) {
            //D.Log("{0}.{1}.OnCommandChanged() called.", Model.FullName, GetType().Name);
            _commandView = cmdModel.Transform.gameObject.GetSafeInterface<ICommandViewable>();
        }

        protected override void CleanupFocusOnDeath() {
            // no need to execute base.CleanupFocusOnDeath() as making the command the focus here will notify the camera
            (_commandView as ICameraFocusable).IsFocus = true;
        }

        // subscriptions contained completely within this gameobject (both subscriber
        // and subscribee) donot have to be cleaned up as all instances are destroyed
    }
}

