// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UniverseCenterPresenter.cs
// An MVPresenter associated with a UniverseCenter View.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// An MVPresenter associated with a UniverseCenter View.
    /// </summary>
    public class UniverseCenterPresenter : AFocusableItemPresenter {

        public new IUniverseCenterModel Model {
            get { return base.Model as IUniverseCenterModel; }
            protected set { base.Model = value; }
        }

        public UniverseCenterPresenter(IViewable view) : base(view) { }

        protected override IModel AcquireModelReference() {
            return _viewGameObject.GetSafeInterface<IUniverseCenterModel>();
        }

        protected override IGuiHudPublisher InitializeHudPublisher() {
            return new GuiHudPublisher<UniverseCenterData>(Model.Data);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

