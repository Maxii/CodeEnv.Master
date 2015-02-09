﻿// --------------------------------------------------------------------------------------------------------------------
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

        protected new SectorItemData Data { get { return base.Data as SectorItemData; } }

        public SectorPresenter(IViewable view) : base(view) { }

        protected override IGuiHudPublisher InitializeHudPublisher() {
            return new GuiHudPublisher<SectorItemData>(Data);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}


