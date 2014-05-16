﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipPresenter.cs
// An MVPresenter associated with a ShipView.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// An MVPresenter associated with a ShipView.
    /// </summary>
    public class ShipPresenter : AUnitElementPresenter {

        public new IShipModel Model {
            get { return base.Model as IShipModel; }
            protected set { base.Model = value; }
        }

        public ShipPresenter(IElementViewable view)
            : base(view) {
            Subscribe();
        }

        protected override IModel AcquireModelReference() {
            return _viewGameObject.GetSafeInterface<IShipModel>();
        }

        protected override IGuiHudPublisher InitializeHudPublisher() {
            var hudPublisher = new GuiHudPublisher<ShipData>(Model.Data);
            hudPublisher.SetOptionalUpdateKeys(GuiHudLineKeys.Speed, GuiHudLineKeys.Health);
            return hudPublisher;
        }

        public bool IsHQElement {
            get { return Model.IsHQElement; }
        }

        public Reference<float> GetShipSpeedReference() {
            return new Reference<float>(() => Model.Data.CurrentSpeed);
        }

        public void OnIsSelected() {
            SelectionManager.Instance.CurrentSelection = View as ISelectable;
        }

        protected override void CleanupOnDeath() {
            base.CleanupOnDeath();
            if ((View as ISelectable).IsSelected) {
                SelectionManager.Instance.CurrentSelection = null;
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}
