// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IColoredTextList.cs
// Interface for objects that contain a list of Colored Text elements for a Label.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System.Collections.Generic;

    /// <summary>
    /// Interface for objects that contain a list of Colored Text elements for a Label.
    /// </summary>
    public interface IColoredTextList {

        /// <summary>
        /// Readonly. The list of <c>ColoredText</c> elements.
        /// </summary>
        IList<ColoredText> List { get; }

        /// <summary>
        /// Readonly. The array of  elements as a <c>string</c> array.
        /// </summary>
        string[] TextElements { get; }

    }
}

