// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: RareResourcesGuiElement.cs
// GuiElement handling the display and tooltip content for the RareResources available to an Item.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// GuiElement handling the display and tooltip content for the Resources available to an Item.
/// </summary>
[Obsolete]
public class RareResourcesGuiElement : GuiElement, IComparable<RareResourcesGuiElement> {

    private static string _labelFormat = Constants.FormatFloat_1DpMax;
    private static int _maxResourcesAllowed = 6;

    private RareResourceYield? _rareResources;
    public RareResourceYield? RareResources {
        get { return _rareResources; }
        set {
            _rareResources = value;
            OnResourcesSet();
        }
    }

    // No tooltip for this GuiElement as each Resource sprite showing will have a tooltip

    private IDictionary<Slot, UISprite> _spriteLookup = new Dictionary<Slot, UISprite>(_maxResourcesAllowed);
    private UILabel _unknownLabel;  // label has a preset '?'
    private int _resourcesCount = Constants.Zero;

    protected override void Awake() {
        base.Awake();
        InitializeReferences();
    }

    private void InitializeReferences() {
        var sprites = gameObject.GetSafeMonoBehavioursInChildren<UISprite>();
        float avgLocalX = sprites.Average(s => s.transform.localPosition.x);
        float avgLocalY = sprites.Average(s => s.transform.localPosition.y);    // ~ 0F

        sprites.ForAll(s => {
            Slot slot;
            bool toLeft = false;
            bool toTop = false;
            bool toBottom = false;

            var x = s.transform.localPosition.x;
            var y = s.transform.localPosition.y;
            toLeft = (x < avgLocalX) ? true : false;
            if (!Mathfx.Approx(y, avgLocalY, 1F)) {   // 1 Ngui virtual pixel of vertical leeway is plenty
                // this is not a middle sprite
                if (y > avgLocalY) {    // higher y values move the widget up
                    toTop = true;
                }
                else {
                    toBottom = true;
                }
            }

            if (toTop) {
                slot = toLeft ? Slot.TopLeft : Slot.TopRight;
            }
            else if (toBottom) {
                slot = toLeft ? Slot.BottomLeft : Slot.BottomRight;
            }
            else {
                slot = toLeft ? Slot.CenterLeft : Slot.CenterRight;
            }
            _spriteLookup.Add(slot, s);
            NGUITools.SetActive(s.gameObject, false);
        });

        _unknownLabel = gameObject.GetSafeFirstMonoBehaviourInImmediateChildrenOnly<UILabel>();
        NGUITools.SetActive(_unknownLabel.gameObject, false);
    }

    private void OnResourcesSet() {
        PopulateElementWidgets();
    }

    private void PopulateElementWidgets() {
        if (RareResources.HasValue) {
            var resourcesPresent = RareResources.Value.ResourcesPresent.ToList();
            _resourcesCount = resourcesPresent.Count;
            D.Assert(_resourcesCount <= _maxResourcesAllowed);
            for (int i = Constants.Zero; i < _resourcesCount; i++) {
                Slot slot = (Slot)i;
                UISprite sprite = _spriteLookup[slot];
                var resource = resourcesPresent[i];
                sprite.spriteName = resource.GetSpriteFilename();

                NGUITools.SetActive(sprite.gameObject, true);
                var label = sprite.gameObject.GetSafeMonoBehaviourInChildren<UILabel>();
                label.text = _labelFormat.Inject(RareResources.Value.GetYield(resource));
            }
        }
        else {
            NGUITools.SetActive(_unknownLabel.gameObject, true);
        }
    }

    public override void Reset() {
        base.Reset();
        _spriteLookup.Values.ForAll(sprite => {
            NGUITools.SetActive(sprite.gameObject, false);
        });
        NGUITools.SetActive(_unknownLabel.gameObject, false);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IComparable<RareResourcesGuiElement> Members

    public int CompareTo(RareResourcesGuiElement other) {
        return _resourcesCount.CompareTo(other._resourcesCount);
    }

    #endregion

    #region Nested Classes

    public enum Slot {
        // in the order they should be populated
        TopLeft = 0,
        CenterLeft = 1,
        BottomLeft = 2,
        TopRight = 3,
        CenterRight = 4,
        BottomRight = 5
    }

    #endregion

}

