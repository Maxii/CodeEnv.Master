// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AGuiMenuPopupListBase.cs
// Abstract base class for popup lists that are elements of a menu with an Accept button. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;

/// <summary>
/// Abstract base class for popup lists that are elements of a menu with an Accept button. 
/// <remarks>Needed as derived child class AGuiMenuPopupList&lt;T&gt; is generic and can't be acquired by GetComponent()."/></remarks>
/// </summary>
public abstract class AGuiMenuPopupListBase : AGuiMenuElement {

    private const string DebugNameFormat = "{0}.{1}";

    private string _debugName;
    public override string DebugName {
        get {
            if (_debugName == null) {
                _debugName = DebugNameFormat.Inject(GetType().Name, ElementID.GetValueName());
            }
            return _debugName;
        }
    }

    /// <summary>
    /// The currently selected value of the PopupList as a string.
    /// <remarks>9.18.16 This property exists as Ngui 3.9.7 - 3.10.1 changed popupList.value 
    /// to only be valid during the onChange event. Previously, I was relying on popupList.value to hold
    /// the string value of the item selected from the list until the next time it was changed.</remarks>
    /// </summary>
    public string SelectedValue { get; protected set; }

    /// <summary>
    /// The currently selected value of the PopupList as a string after conversion, if needed.
    /// Default implementation returns SelectedValue. 
    /// <remarks>Derived classes should override if their selections need to be converted to another string value.
    /// Conversion is typically required when the popup list uses values from a PopupList-specific enum 
    /// that is a substitute for another primary enum used in the game. Such PopupList-specific
    /// enums typically exclude one or more values from the primary enum that shouldn't be
    /// shown in the list, and/or add the 'Random' choice.</remarks>
    /// </summary>
    public virtual string ConvertedSelectedValue { get { return SelectedValue; } }


}

