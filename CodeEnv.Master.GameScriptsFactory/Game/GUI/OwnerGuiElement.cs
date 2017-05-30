// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: OwnerGuiElement.cs
// GuiElement handling the display and tooltip content for the Owner of an item.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// GuiElement handling the display and tooltip content for the Owner of an item.
/// </summary>
public class OwnerGuiElement : AImageGuiElement, IComparable<OwnerGuiElement> {

    private bool _isOwnerSet;
    private Player _owner;
    public Player Owner {
        get { return _owner; }
        set {
            D.Assert(!_isOwnerSet);    // only happens once between Resets
            _owner = value;
            OwnerPropSetHandler();  // SetProperty() only calls handler when changed
        }
    }

    public override GuiElementID ElementID { get { return GuiElementID.Owner; } }

    protected override bool AreAllValuesSet { get { return _isOwnerSet; } }

    #region Event and Property Change Handlers

    private void ClickEventHandler() {
        D.Warn("{0}.OnClick() not yet implemented.", GetType().Name);
        //TODO: redirect to Owner diplomacy screen.
    }

    private void OwnerPropSetHandler() {
        _isOwnerSet = true;
        if (AreAllValuesSet) {
            PopulateElementWidgets();
        }
    }

    void OnClick() {
        ClickEventHandler();
    }


    #endregion

    protected override void PopulateElementWidgets() {
        if (Owner == null) {
            HandleValuesUnknown();
            return;
        }

        AtlasID imageAtlasID = Owner.LeaderImageAtlasID;
        string imageFilename = Owner.LeaderImageFilename;
        string leaderName_Colored = Owner.LeaderName.SurroundWith(Owner.Color);

        switch (_widgetsPresent) {
            case WidgetsPresent.Image:
                PopulateImageValues(imageFilename, imageAtlasID);
                _tooltipContent = "Owner custom tooltip placeholder";
                break;
            case WidgetsPresent.Label:
                _imageNameLabel.text = leaderName_Colored;
                break;
            case WidgetsPresent.Both:
                PopulateImageValues(imageFilename, imageAtlasID);
                _imageNameLabel.text = leaderName_Colored;
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_widgetsPresent));
        }
    }

    public override void Reset() {
        _isOwnerSet = false;
    }

    protected override void Cleanup() { }


    #region IComparable<OwnerGuiElement> Members

    public int CompareTo(OwnerGuiElement other) {
        int result;
        var noPlayer = TempGameValues.NoPlayer;
        if (Owner == noPlayer) {
            result = other.Owner == noPlayer ? Constants.Zero : Constants.MinusOne;
        }
        else if (Owner == null) {
            // an unknown owner (owner == null) sorts higher than an owner that is known to be None
            result = other.Owner == null ? Constants.Zero : (other.Owner == noPlayer) ? Constants.One : Constants.MinusOne;
        }
        else {
            result = (other.Owner == noPlayer || other.Owner == null) ? Constants.One : Owner.LeaderName.CompareTo(other.Owner.LeaderName);
        }
        return result;
    }

    #endregion

}

