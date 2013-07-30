// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IColoredTextList.cs
// Interface for strategy objects that provide lists of Colored Text for GuiCursorHudText.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common.Unity {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Interface for strategy objects that provide lists of Colored Text for GuiCursorHudText.
    /// </summary>
    public interface IColoredTextList {

        IList<ColoredText> GetList();
    }
}

