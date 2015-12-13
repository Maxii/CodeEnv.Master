// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MortalDetectionHandler.cs
// Component that handles detection events for IDetectable items that are mortal.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Component that handles detection events for IDetectable items that are mortal.
    /// </summary>
    [System.Obsolete]
    public class MortalDetectionHandler : DetectionHandler {

        public MortalDetectionHandler(IMortalItem item)
            : base(item) {
            Subscribe();
        }

        private void Subscribe() {
            (_item as IMortalItem).onDeathOneShot += OnDeath;
        }

        private void OnDeath(IMortalItem item) {
            _gameMgr.GetPlayerKnowledge(item.Owner).OnItemDeath(item);// FIXME player is wrong
        }

        protected override void Unsubscribe() {
            base.Unsubscribe();
            (_item as IMortalItem).onDeathOneShot -= OnDeath;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

