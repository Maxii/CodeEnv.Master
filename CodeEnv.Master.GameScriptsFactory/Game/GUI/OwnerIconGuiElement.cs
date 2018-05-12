// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: OwnerIconGuiElement.cs
// AIconGuiElement that represents the Owner of an item. Also handles no owner and unknown.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// AIconGuiElement that represents the Owner of an item. Also handles no owner and unknown.
/// </summary>
public class OwnerIconGuiElement : AIconGuiElement, IComparable<OwnerIconGuiElement> {

    private const string DebugNameFormat = "{0}[{1}]";

    public override GuiElementID ElementID { get { return GuiElementID.Owner; } }

    public override string DebugName {
        get {
            string ownerName = Owner != null ? Owner.LeaderName : "Unknown Owner";
            return DebugNameFormat.Inject(GetType().Name, ownerName);
        }
    }
    private bool _isOwnerSet;   // reqd as Owner can be Player, NoPlayer or null if unknown
    private Player _owner;
    public Player Owner {
        get { return _owner; }
        set {
            D.Assert(!_isOwnerSet); // only happens once between Resets
            _owner = value;
            OwnerPropSetHandler();  // SetProperty() only calls handler when changed
        }
    }


    public override bool IsInitialized { get { return _isOwnerSet; } }

    protected override string TooltipContent { get { return Owner != null ? Owner.LeaderName.SurroundWith(Owner.Color) : "Unknown Owner"; } }

    private UILabel _iconImageNameLabel;

    protected override UISprite AcquireIconImageSprite() {
        // Handles case where an ImageFrame is used
        UISprite immediateChildSprite = gameObject.GetSingleComponentInImmediateChildren<UISprite>();
        return immediateChildSprite.transform.childCount == Constants.Zero ? immediateChildSprite : immediateChildSprite.gameObject.GetSingleComponentInImmediateChildren<UISprite>();
    }

    protected override void AcquireAdditionalWidgets() {
        _iconImageNameLabel = gameObject.GetSingleComponentInChildren<UILabel>();
    }

    protected override void PopulateMemberWidgetValues() {
        base.PopulateMemberWidgetValues();
        if (Owner == null) {
            HandleValuesUnknown();
            return;
        }

        _iconImageSprite.atlas = Owner.LeaderImageAtlasID.GetAtlas();
        _iconImageSprite.spriteName = Owner.LeaderImageFilename;
        _iconImageNameLabel.text = Owner.LeaderName.SurroundWith(Owner.Color);
    }

    #region Event and Property Change Handlers

    private void OwnerPropSetHandler() {
        _isOwnerSet = true;
        if (IsInitialized) {
            PopulateMemberWidgetValues();
            Show();
        }
    }

    #endregion

    protected override void HandleGuiElementHovered(bool isOver) {
        if (isOver) {
            HoveredHudWindow.Instance.Show(DebugName);
        }
        else {
            HoveredHudWindow.Instance.Hide();
        }
    }

    protected override void HandleValuesUnknown() {
        base.HandleValuesUnknown();
        _iconImageNameLabel.text = Unknown;
    }

    public override void ResetForReuse() {
        base.ResetForReuse();
        _isOwnerSet = false;
        if (_iconImageNameLabel != null) {
            _iconImageNameLabel.text = null;
        }
    }

    protected override void Cleanup() { }

    #region IComparable<OwnerIconGuiElement> Members

    public int CompareTo(OwnerIconGuiElement other) {
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

