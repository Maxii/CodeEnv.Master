// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: HudPublisher.cs
// Manages the content of the text that GuiCursorHud displays and provides
// some customization and access methods.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common.Unity {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Manages the content of the text that GuiCursorHud displays and provides
    /// some customization and access methods.
    /// </summary>
    public class HudPublisher {

        private GuiCursorHudText _guiCursorHudText;
        private Data _data;
        private GuiCursorHudLineKeys[] _optionalKeys;

        public HudPublisher(Data data) {
            _data = data;
        }

        /// <summary>
        /// Gets a new, updated or simply existing GuiCursorHudText instance containing 
        /// the text used by the GuiCursorHud display.
        /// </summary>
        /// <param name="intelLevel">The intel level.</param>
        /// <returns></returns>
        public GuiCursorHudText GetHudText(IntelLevel intelLevel) {        // OPTIMIZE Detect individual data property changes and replace them individually
            if (_guiCursorHudText == null || _guiCursorHudText.IntelLevel != intelLevel || _data.IsChanged) {
                // don't have the right version of GuiCursorHudText so make one
                _guiCursorHudText = GuiCursorHudTextFactory.MakeInstance(intelLevel, _data);
                _data.AcceptChanges();   // once we make a new one from current data, it is no longer dirty, if it ever was
            }
            else {
                // we have the right clean version so simply update the values that routinely change
                UpdateGuiCursorHudText(intelLevel, GuiCursorHudLineKeys.Distance);
                if (intelLevel == IntelLevel.OutOfDate) {
                    UpdateGuiCursorHudText(IntelLevel.OutOfDate, GuiCursorHudLineKeys.IntelState);
                }
                if (_optionalKeys != null) {
                    UpdateGuiCursorHudText(intelLevel, _optionalKeys);
                }
            }
            return _guiCursorHudText;
        }

        /// <summary>
        /// Clients can optionally provide additional GuiCursorHudLineKeys they wish to routinely update whenever GetHudText is called.
        /// LineKeys already automatically handled for all managers include Distance and IntelState.
        /// </summary>
        /// <param name="optionalKeys">The optional keys.</param>
        public void SetOptionalUpdateKeys(params GuiCursorHudLineKeys[] optionalKeys) {
            _optionalKeys = optionalKeys;
        }

        /// <summary>
        /// Updates the current GuiCursorHudText instance by replacing the lines identified by keys.
        /// </summary>
        /// <param name="intelLevel">The intel level.</param>
        /// <param name="keys">The line keys.</param>
        private void UpdateGuiCursorHudText(IntelLevel intelLevel, params GuiCursorHudLineKeys[] keys) {
            IColoredTextList coloredTextList;
            foreach (var key in keys) {
                coloredTextList = GuiCursorHudTextFactory.MakeInstance(key, intelLevel, _data);
                _guiCursorHudText.Replace(key, coloredTextList);
            }
        }

        //public void ClearCursorHUD() {
        //    _cursorHud.Clear();
        //}


        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

