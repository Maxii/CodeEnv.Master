// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AGuiElement.cs
// Abstract AGuiWaitForInitializeMember that is uniquely identifiable by its GuiElementID. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Abstract AGuiWaitForInitializeMember that is uniquely identifiable by its GuiElementID. 
/// </summary>
public abstract class AGuiElement : AGuiWaitForInitializeMember {

    protected const string Unknown = Constants.QuestionMark;

    public abstract GuiElementID ElementID { get; }

    #region Debug

    protected override void __ValidateOnAwake() {
        base.__ValidateOnAwake();
        // 7.5.17 Removed requirement for a UIWidget so I can use GuiElement as an identifier for GuiWindows
        if (ElementID == default(GuiElementID)) {
            D.WarnContext(this, "{0}.{1} not set.", DebugName, typeof(GuiElementID).Name);
        }
    }

    #endregion

}

