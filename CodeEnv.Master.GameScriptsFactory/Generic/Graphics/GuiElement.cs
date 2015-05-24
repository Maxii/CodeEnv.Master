// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiElement.cs
// Simple instantiable class that holds the ID for a Gui Element.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Simple instantiable class that holds the ID for a Gui Element.
/// </summary>
public class GuiElement : AGuiTooltip {

    public GuiElementID elementID;

    protected override void Awake() {
        base.Awake();
        Validate();
    }

    /// <summary>
    /// Resets this GuiElement instance so it can be reused. 
    /// Default implementation does nothing.
    /// </summary>
    public virtual void Reset() { }

    private void Validate() {
        UnityUtility.ValidateMonoBehaviourPresence<UIWidget>(gameObject);
        D.LogContext(elementID == default(GuiElementID), "{0}.GuiElementID not set.".Inject(gameObject.name), gameObject);
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

