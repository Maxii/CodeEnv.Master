// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AForm.cs
// Abstract base class for Forms. A Form supervises a collection of UIWidgets
// in an arrangement that can be displayed by a HudWindow.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract base class for Forms. A Form supervises a collection of UIWidgets
/// in an arrangement that can be displayed by a AGuiWindow. AForms are
/// populated with content to display by feeding them Text, Reports or individual
/// values (e.g. a ResourceTooltipForm is fed a ResourceID, displaying values derived from
/// the ResourceID in a TooltipHudWindow).
/// <remarks>6.17.17 Many AForms are shown in HUDs and the FormID is used by the HUD 
/// to pick the form to show. Some AForms don't lend themselves to HUD displays due to 
/// the way the form is structured. TableRowForms are the primary example as Rows can be
/// quite long. In this case the FormID is currently not used.</remarks>
/// </summary>
public abstract class AForm : AMonoBase {

    public string DebugName { get { return GetType().Name; } }

    public abstract FormID FormID { get; }

    private bool _isInitialized;

    protected sealed override void Awake() {
        base.Awake();
        __ValidateOnAwake();
        InitializeValuesAndReferences();
        _isInitialized = true;
    }

    protected abstract void InitializeValuesAndReferences();

    /// <summary>
    /// Populates this form with its values in preparation for its AFormWindow showing it.
    /// <remarks>Most forms need to populate the key Property that provides data to the form prior to this method being called. 
    /// The exception is ATableForm which acquires its own data when this method is called.</remarks>
    /// </summary>
    public abstract void PopulateValues();

    protected abstract void AssignValuesToMembers();

    /// <summary>
    /// Resets this form in preparation for reuse.
    /// <remarks>If this is not done, then incoming content that is the same as existing 
    /// content will not trigger OnChange initialization.</remarks>
    /// </summary>
    public void ResetForReuse() {
        if (_isInitialized) {
            //D.Log("{0}.ResetForReuse() called.", DebugName);
            ResetForReuse_Internal();
        }
    }

    /// <summary>
    /// Resets this Form by nulling the existing content (Text, Reports, etc.) of the form. Any AGuiElements present as well as non-AGuiElements
    /// present will also have their content reset.
    /// <remarks>Called only if the form has already been initialized, aka the forms references to its AGuiElements and non-AGuiElements have been set.</remarks>
    /// <remarks>Warning: Do not null references to AGuiElements or non-AGuiElements as they will not be reacquired when the form is reused.</remarks>
    /// </summary>
    protected abstract void ResetForReuse_Internal();

    public sealed override string ToString() {
        return DebugName;
    }

    #region Debug

    protected virtual void __ValidateOnAwake() {
        D.AssertNotDefault((int)FormID);
    }

    #endregion

}

