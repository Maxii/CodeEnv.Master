// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiHudPublisher.cs
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
    public class GuiHudPublisher {

        private IGuiHud _guiCursorHud;
        private GuiHudText _guiCursorHudText;
        private Data _data;
        private GuiHudLineKeys[] _optionalKeys;

        public GuiHudPublisher(IGuiHud guiCursorHud, Data data) {
            _guiCursorHud = guiCursorHud;
            _data = data;
        }

        /// <summary>
        /// Displays a new, updated or already existing GuiCursorHudText instance containing 
        /// the text to display at the cursor.
        /// </summary>
        /// <param name="intelLevel">The intel level.</param>
        public void DisplayHudAtCursor(IntelLevel intelLevel) {
            _guiCursorHud.Set(GetHudText(intelLevel));
        }

        public void ClearHud() {
            _guiCursorHud.Clear();
        }

        private GuiHudText GetHudText(IntelLevel intelLevel) {        // OPTIMIZE Detect individual data property changes and replace them individually
            if (_guiCursorHudText == null || _guiCursorHudText.IntelLevel != intelLevel || _data.IsChanged) {
                // don't have the right version of GuiCursorHudText so make one
                _guiCursorHudText = GuiHudTextFactory.MakeInstance(intelLevel, _data);
                _data.AcceptChanges();   // once we make a new one from current data, it is no longer dirty, if it ever was
            }
            else {

                // IDEA: make this a coroutine and provide a stop coroutine method that the client can call when hover ends
                // we have the right clean version so simply update the values that routinely change
                UpdateGuiCursorHudText(intelLevel, GuiHudLineKeys.Distance);
                if (intelLevel == IntelLevel.OutOfDate) {
                    UpdateGuiCursorHudText(IntelLevel.OutOfDate, GuiHudLineKeys.IntelState);
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
        public void SetOptionalUpdateKeys(params GuiHudLineKeys[] optionalKeys) {
            _optionalKeys = optionalKeys;
        }

        /// <summary>
        /// Updates the current GuiCursorHudText instance by replacing the lines identified by keys.
        /// </summary>
        /// <param name="intelLevel">The intel level.</param>
        /// <param name="keys">The line keys.</param>
        private void UpdateGuiCursorHudText(IntelLevel intelLevel, params GuiHudLineKeys[] keys) {
            IColoredTextList coloredTextList;
            foreach (var key in keys) {
                coloredTextList = GuiHudTextFactory.MakeInstance(key, intelLevel, _data);
                _guiCursorHudText.Replace(key, coloredTextList);
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

