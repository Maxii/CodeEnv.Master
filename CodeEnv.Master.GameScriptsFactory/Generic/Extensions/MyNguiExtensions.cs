// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MyNguiExtensions.cs
// Extensions for (or that return) Ngui Types. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;

/// <summary>
/// Extensions for (or that return) Ngui Types. 
/// Warning: Cannot locate in a pre-compiled assembly as Ngui Types are only present in script form.
/// </summary>
public static class MyNguiExtensions {

    /// <summary>
    /// Gets the Ngui UIAtlas ID'd by atlasID.
    /// </summary>
    /// <param name="atlasID">The atlas ID.</param>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    public static UIAtlas GetAtlas(this AtlasID atlasID) {
        switch (atlasID) {
            case AtlasID.Fleet:
                return RequiredPrefabs.Instance.fleetIconAtlas;
            case AtlasID.Contextual:
                return RequiredPrefabs.Instance.contextualAtlas;
            case AtlasID.MyGui:
                return RequiredPrefabs.Instance.myGuiAtlas;
            case AtlasID.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(atlasID));
        }

    }

}

