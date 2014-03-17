// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetoidPresenter.cs
// An MVPresenter associated with a PlanetoidView.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// An MVPresenter associated with a PlanetoidView.
    /// </summary>
    public class PlanetoidPresenter : AMortalItemPresenter {

        public new IPlanetoidModel Model {
            get { return base.Model as IPlanetoidModel; }
            protected set { base.Model = value; }
        }

        public PlanetoidPresenter(IMortalViewable view)
            : base(view) {
            Subscribe();
        }

        protected override IModel AcquireModelReference() {
            //return UnityUtility.ValidateMonoBehaviourPresence<PlanetoidModel>(_viewGameObject);
            return _viewGameObject.GetSafeInterface<IPlanetoidModel>();
        }

        protected override IGuiHudPublisher InitializeHudPublisher() {
            var publisher = new GuiHudPublisher<PlanetoidData>(Model.Data);
            publisher.SetOptionalUpdateKeys(GuiHudLineKeys.Health);
            return publisher;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

