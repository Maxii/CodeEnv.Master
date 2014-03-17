// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SectorPresenter.cs
// MVPresenter associated with a SectorView.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// MVPresenter associated with a SectorView.
    /// </summary>
    public class SectorPresenter : AItemPresenter {

        public new ISectorModel Model {
            get { return base.Model as ISectorModel; }
            protected set { base.Model = value; }
        }

        public SectorPresenter(IViewable view) : base(view) { }

        protected override IModel AcquireModelReference() {
            return _viewGameObject.GetSafeInterface<ISectorModel>();
        }

        protected override IGuiHudPublisher InitializeHudPublisher() {
            return new GuiHudPublisher<SectorData>(Model.Data);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}


