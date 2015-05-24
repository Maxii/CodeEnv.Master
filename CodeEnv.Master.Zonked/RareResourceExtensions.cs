// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: RareResourceExtensions.cs
// Extension methods for RareResource values.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Extension methods for RareResource values.
    /// TODO Externalize values in XML.
    /// </summary>
    [Obsolete]
    public static class RareResourceExtensions {

        public static string GetSpriteFilename(this RareResourceID resource) {
            switch (resource) {
                case RareResourceID.Titanium:
                case RareResourceID.Duranium:
                case RareResourceID.Unobtanium:
                    return "Stats1_12";
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(resource));
            }
        }

    }
}

