// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MoonPresenter.cs
// An MVPresenter associated with a MoonView.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// An MVPresenter associated with a MoonView.
    /// </summary>
    public class MoonPresenter : AMortalItemPresenter {

        public new IMoonModel Model {
            get { return base.Model as IMoonModel; }
            protected set { base.Model = value; }
        }

        protected new MoonData Data { get { return base.Data as MoonData; } }

        public MoonPresenter(IMortalViewable view)
            : base(view) {
            Subscribe();
        }

        protected override IModel AcquireModelReference() {
            return _viewGameObject.GetSafeInterface<IMoonModel>();
        }

        protected override IGuiHudPublisher InitializeHudPublisher() {
            var publisher = new GuiHudPublisher<MoonData>(Data);
            publisher.SetOptionalUpdateKeys(GuiHudLineKeys.Health);
            return publisher;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

