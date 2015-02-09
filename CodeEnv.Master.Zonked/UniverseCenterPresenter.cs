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

        protected new UniverseCenterItemData Data { get { return base.Data as UniverseCenterItemData; } }

        public UniverseCenterPresenter(IViewable view) : base(view) { }

        protected override IGuiHudPublisher InitializeHudPublisher() {
            return new GuiHudPublisher<UniverseCenterItemData>(Data);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

