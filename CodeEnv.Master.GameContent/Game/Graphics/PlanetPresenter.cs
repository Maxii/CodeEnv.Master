// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetPresenter.cs
// An MVPresenter associated with a PlanetView.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// An MVPresenter associated with a PlanetView.
    /// </summary>
    public class PlanetPresenter : AMortalItemPresenter {

        protected new PlanetData Data { get { return base.Data as PlanetData; } }

        public PlanetPresenter(IMortalViewable view)
            : base(view) {
            Subscribe();
        }

        protected override IGuiHudPublisher InitializeHudPublisher() {
            var publisher = new GuiHudPublisher<PlanetData>(Data);
            publisher.SetOptionalUpdateKeys(GuiHudLineKeys.Health);
            return publisher;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

