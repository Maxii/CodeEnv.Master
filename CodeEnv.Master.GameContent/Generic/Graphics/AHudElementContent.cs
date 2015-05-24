// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ATooltipContent.cs
// Abstract base class for contents associated with customized Hud elements.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Abstract base class for contents associated with customized Hud elements.
    /// </summary>
    public abstract class AHudElementContent {

        public HudElementID ElementID { get; private set; }

        public AHudElementContent(HudElementID elementID) {
            ElementID = elementID;
        }
    }
}

