﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ColoredTextList_String.cs
// String version of class for strategy objects that provide lists of Colored Text for Huds.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// String version of class for strategy objects that provide lists of Colored Text for Huds.
    /// </summary>
    [System.Obsolete]
    public class ColoredTextList_String : ColoredTextList {

        public ColoredTextList_String(params string[] values) {
            foreach (string v in values) {
                _list.Add(new ColoredText(v));
            }
        }

    }
}

