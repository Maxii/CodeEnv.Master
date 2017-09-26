// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AGuiElement.cs
// Abstract display element of the GUI that is uniquely identifiable by its GuiElementID.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract display element of the GUI that is uniquely identifiable by its GuiElementID. Also has
/// embedded text tooltip support. GuiElements typically have one or more UIWidget siblings
/// and/or children associated with them that they help identify and/or find.
/// </summary>
public abstract class AGuiElement : ATextTooltip {

    protected const string Unknown = Constants.QuestionMark;

    public abstract GuiElementID ElementID { get; }

    protected override void Awake() {
        base.Awake();
        __Validate();
    }

    /// <summary>
    /// Resets this GuiElement instance so it can be reused. 
    /// </summary>
    public abstract void ResetForReuse();

    #region Debug

    protected virtual void __Validate() {
        // 7.5.17 Removed requirement for a UIWidget so I can use GuiElement as an identifier for GuiWindows
        if (ElementID == default(GuiElementID)) {
            D.WarnContext(this, "{0}.{1} not set.", DebugName, typeof(GuiElementID).Name);
        }
    }

    #endregion


}

