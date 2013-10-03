// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SelectionManager.cs
// Singleton. Selection Manager that keeps track of what is selected in the game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.GameContent;

    /// <summary>
    /// Singleton. Selection Manager that keeps track of what is selected in the game. Only one item can be selected at a time.
    /// </summary>
    public class SelectionManager : AGenericSingleton<SelectionManager> {

        private ISelectable _currentSelection;
        public ISelectable CurrentSelection {
            get { return _currentSelection; }
            set { SetProperty<ISelectable>(ref _currentSelection, value, "CurrentSelection", null, OnSelectionChanging); }
        }

        private SelectionManager() {
            Initialize();
        }

        protected override void Initialize() { }

        private void OnSelectionChanging(ISelectable newSelection) {
            if (CurrentSelection != null) {
                CurrentSelection.IsSelected = false;
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

