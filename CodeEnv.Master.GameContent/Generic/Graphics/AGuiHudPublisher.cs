// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AGuiHudPublisher.cs
// Abstract base class for GuiHudPublisher&lt;DataType&gt; that makes a static
// field available across all variations.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Abstract base class for GuiHudPublisher&lt;DataType&gt; that makes a static
    /// field available across all variations.
    /// </summary>
    public abstract class AGuiHudPublisher {

        protected static IGuiHud _guiCursorHud;

        public static void SetGuiCursorHud(IGuiHud guiHud) {
            _guiCursorHud = guiHud;
        }

    }
}

