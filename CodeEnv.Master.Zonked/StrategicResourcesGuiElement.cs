// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StrategicResourcesGuiElement.cs
// GuiElement handling the display and tooltip content for the Strategic Resources available to an Item. 
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
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// GuiElement handling the display and tooltip content for the Strategic Resources available to an Item. 
/// </summary>
[Obsolete]
public class StrategicResourcesGuiElement : GuiElement, IComparable<StrategicResourcesGuiElement> {

    private static string _labelFormat = Constants.FormatFloat_1DpMax;
    private static int _maxResourcesAllowed = 6;

    private ResourceYield? _resources;
    public ResourceYield? Resources {
        get { return _resources; }
        set {
            _resources = value;
            OnResourcesSet();
        }
    }

    // Each Resource container and the unknown label, when showing will have a tooltip 

    /// <summary>
    /// Lookup for Resource containers, keyed by the container's slot in the Element.
    /// </summary>
    private IDictionary<Slot, UIWidget> _resourceContainerLookup = new Dictionary<Slot, UIWidget>(_maxResourcesAllowed);

    /// <summary>
    /// Lookup for ResourceIDs, keyed by the Resource container's gameObject. 
    /// Used to show the right ResourceID tooltip when the container is hovered over.
    /// </summary>
    private IDictionary<GameObject, ResourceID> _resourceIDLookup = new Dictionary<GameObject, ResourceID>(_maxResourcesAllowed);
    private UILabel _unknownLabel;  // label has a preset '?'
    private int _resourcesCount = Constants.Zero;

    protected override void Awake() {
        base.Awake();
        InitializeReferences();
    }

    private void InitializeReferences() {
        // excludes the unknown widget label
        var resourceContainers = gameObject.GetSafeMonoBehavioursInImmediateChildrenOnly<UIWidget>().Where(w => w.GetComponent<UILabel>() == null);
        float avgLocalX = resourceContainers.Average(s => s.transform.localPosition.x);
        float avgLocalY = resourceContainers.Average(s => s.transform.localPosition.y);    // ~ 0F

        resourceContainers.ForAll(container => {
            Slot slot;
            bool toLeft = false;
            bool toTop = false;
            bool toBottom = false;

            var x = container.transform.localPosition.x;
            var y = container.transform.localPosition.y;
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
            InitializeResourceContainer(slot, container);
        });

        _unknownLabel = gameObject.GetSafeFirstMonoBehaviourInImmediateChildrenOnly<UILabel>();
        MyNguiEventListener.Get(_unknownLabel.gameObject).onTooltip += (go, show) => OnUnknownTooltip(show);
        NGUITools.SetActive(_unknownLabel.gameObject, false);
    }

    private void InitializeResourceContainer(Slot slot, UIWidget container) {
        _resourceContainerLookup.Add(slot, container);

        var eventListener = MyNguiEventListener.Get(container.gameObject);
        eventListener.onTooltip += (go, show) => OnResourceContainerTooltip(go, show);

        NGUITools.SetActive(container.gameObject, false);
    }

    private void OnResourceContainerTooltip(GameObject containerGo, bool show) {
        if (show) {
            var resourceID = _resourceIDLookup[containerGo];
            TooltipHudWindow.Show(new ResourceHudFormContent(resourceID));
        }
        else {
            TooltipHudWindow.Hide();
        }
    }

    private void OnUnknownTooltip(bool show) {
        if (show) {
            TooltipHudWindow.Show(new TextHudFormContent("Resource presence unknown"));
        }
        else {
            TooltipHudWindow.Hide();
        }
    }

    private void OnResourcesSet() {
        PopulateElementWidgets();
    }

    private void PopulateElementWidgets() {
        if (Resources.HasValue) {
            var strategicResourcesPresent = Resources.Value.ResourcesPresent.Where(res => res.GetResourceCategory() == ResourceCategory.Strategic).ToList();
            _resourcesCount = strategicResourcesPresent.Count;
            D.Assert(_resourcesCount <= _maxResourcesAllowed);
            for (int i = Constants.Zero; i < _resourcesCount; i++) {
                Slot slot = (Slot)i;
                UIWidget container = _resourceContainerLookup[slot];
                NGUITools.SetActive(container.gameObject, true);

                UISprite sprite = container.gameObject.GetSafeMonoBehaviourInChildren<UISprite>();
                var resourceID = strategicResourcesPresent[i];
                sprite.atlas = MyNguiUtilities.GetAtlas(resourceID.GetIconAtlasID());
                sprite.spriteName = resourceID.GetIconFilename();
                _resourceIDLookup.Add(container.gameObject, resourceID);

                var label = container.gameObject.GetSafeMonoBehaviourInChildren<UILabel>();
                label.text = _labelFormat.Inject(Resources.Value.GetYield(resourceID));
            }
        }
        else {
            NGUITools.SetActive(_unknownLabel.gameObject, true);
        }
    }

    public override void Reset() {
        base.Reset();
        _resourceContainerLookup.Values.ForAll(container => {
            NGUITools.SetActive(container.gameObject, false);
        });
        NGUITools.SetActive(_unknownLabel.gameObject, false);
        _resourceIDLookup.Clear();
    }

    protected override void Cleanup() {
        base.Cleanup();
        _resourceIDLookup.Keys.ForAll(containerGo => {
            MyNguiEventListener.Get(containerGo).onTooltip -= (go, show) => OnResourceContainerTooltip(go, show);
        });
        MyNguiEventListener.Get(_unknownLabel.gameObject).onTooltip -= (go, show) => OnUnknownTooltip(show);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IComparable<StrategicResourcesGuiElement> Members

    public int CompareTo(StrategicResourcesGuiElement other) {
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

