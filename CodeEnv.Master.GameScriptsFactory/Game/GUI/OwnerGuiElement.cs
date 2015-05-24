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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// GuiElement handling the display and tooltip content for the Owner of an item.
/// </summary>
public class OwnerGuiElement : GuiElement, IComparable<OwnerGuiElement> {

    private static string _unknown = Constants.QuestionMark;

    protected override string TooltipContent { get { return "Owner custom tooltip placeholder"; } }

    private Player _owner;
    public Player Owner {
        get { return _owner; }
        set {
            _owner = value;
            OnOwnerSet();
        }
    }

    private UILabel _label;
    private UISprite _imageSprite;

    protected override void Awake() {
        base.Awake();
        Validate();
        _label = gameObject.GetSafeMonoBehaviourInChildren<UILabel>();
        _imageSprite = gameObject.GetSafeMonoBehavioursInChildren<UISprite>().Single(s => s.type == UIBasicSprite.Type.Simple);
    }

    void OnClick() {
        D.Warn("{0}.OnClick() not yet implemented. TODO: redirect to Owner diplomacy screen.", GetType().Name);
    }

    private void OnOwnerSet() {
        PopulateElementWidgets();
    }

    private void PopulateElementWidgets() {
        if (Owner != null) {
            _label.text = Owner.LeaderName;
            _label.color = Owner.Color.ToUnityColor();
            _imageSprite.spriteName = Owner.ImageFilename;
        }
        else {
            _label.text = _unknown;
            _label.color = TempGameValues.DisabledColor.ToUnityColor();
            _imageSprite.spriteName = "None";   // should show no sprite
        }
    }

    public override void Reset() {
        base.Reset();
        // not necessary as already ready for reuse when Owner next assigned
    }

    private void Validate() {
        if (elementID != GuiElementID.Owner) {
            D.Warn("{0}.ID = {1}. Fixing...", GetType().Name, elementID.GetName());
            elementID = GuiElementID.Owner;
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

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

