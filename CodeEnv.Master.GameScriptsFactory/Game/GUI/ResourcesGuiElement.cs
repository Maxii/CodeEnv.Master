// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ResourcesGuiElement.cs
// GuiElement handling the display and tooltip content for the Resources available to an Item. 
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
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// GuiElement handling the display and tooltip content for the Resources available to an Item. 
/// OPTIMIZE Reduce number of sprites and labels
/// </summary>
public class ResourcesGuiElement : AGuiElement, IComparable<ResourcesGuiElement> {

    private static string _labelFormat = Constants.FormatFloat_1DpMax;
    private static int _maxResourcesAllowed = 6;

    /// <summary>
    /// The category of resources to be displayed.
    /// This will cause these resources to be acquired from the provided ResourceYield.
    /// </summary>
    //[FormerlySerializedAs("resourceCategory")]
    [Tooltip("The category of resources to be displayed")]
    [SerializeField]
    private ResourceCategory _resourceCategory = ResourceCategory.None;

    private bool _isResourcesSet;
    private ResourceYield? _resources;
    public ResourceYield? Resources {
        get { return _resources; }
        set {
            D.Assert(!_isResourcesSet); // occurs only once between Resets
            _resources = value;
            ResourcesPropSetHandler();
        }
    }

    public override GuiElementID ElementID { get { return GuiElementID.Resources; } }

    private bool AreAllValuesSet { get { return _isResourcesSet; } }

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
        InitializeValuesAndReferences();
    }

    private void InitializeValuesAndReferences() {
        _unknownLabel = gameObject.GetSingleComponentInImmediateChildren<UILabel>();

        var resourceContainers = gameObject.GetSafeComponentsInImmediateChildren<UIWidget>().Except(_unknownLabel);
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

        MyEventListener.Get(_unknownLabel.gameObject).onTooltip += UnknownTooltipEventHandler;
        NGUITools.SetActive(_unknownLabel.gameObject, false);
    }

    private void InitializeResourceContainer(Slot slot, UIWidget container) {
        _resourceContainerLookup.Add(slot, container);

        var eventListener = MyEventListener.Get(container.gameObject);
        eventListener.onTooltip += ResourceContainerTooltipEventHandler;

        NGUITools.SetActive(container.gameObject, false);
    }

    #region Event and Property Change Handlers

    private void ResourceContainerTooltipEventHandler(GameObject containerGo, bool show) {
        if (show) {
            var resourceID = _resourceIDLookup[containerGo];
            TooltipHudWindow.Instance.Show(resourceID);
        }
        else {
            TooltipHudWindow.Instance.Hide();
        }
    }

    private void UnknownTooltipEventHandler(GameObject go, bool show) {
        if (show) {
            TooltipHudWindow.Instance.Show("Resource presence unknown");
        }
        else {
            TooltipHudWindow.Instance.Hide();
        }
    }

    private void ResourcesPropSetHandler() {
        _isResourcesSet = true;
        if (AreAllValuesSet) {
            PopulateElementWidgets();
        }
    }

    #endregion

    private void PopulateElementWidgets() {
        if (!Resources.HasValue) {
            HandleValuesUnknown();
            return;
        }

        var resourcesPresent = Resources.Value.ResourcesPresent.Where(res => res.GetResourceCategory() == _resourceCategory).ToList();
        _resourcesCount = resourcesPresent.Count;
        D.Assert(_resourcesCount <= _maxResourcesAllowed);
        for (int i = Constants.Zero; i < _resourcesCount; i++) {
            Slot slot = (Slot)i;
            UIWidget container = _resourceContainerLookup[slot];
            NGUITools.SetActive(container.gameObject, true);

            UISprite sprite = container.gameObject.GetSingleComponentInChildren<UISprite>();
            var resourceID = resourcesPresent[i];
            sprite.atlas = resourceID.GetIconAtlasID().GetAtlas();
            sprite.spriteName = resourceID.GetIconFilename();
            _resourceIDLookup.Add(container.gameObject, resourceID);

            var label = container.gameObject.GetSingleComponentInChildren<UILabel>();
            label.text = _labelFormat.Inject(Resources.Value.GetYield(resourceID));
        }
    }

    private void HandleValuesUnknown() {
        NGUITools.SetActive(_unknownLabel.gameObject, true);
    }

    /// <summary>
    /// Resets this GuiElement instance so it can be reused.
    /// </summary>
    public override void Reset() {
        _resourceContainerLookup.Values.ForAll(container => {
            NGUITools.SetActive(container.gameObject, false);
        });
        NGUITools.SetActive(_unknownLabel.gameObject, false);
        _resourceIDLookup.Clear();
        _isResourcesSet = false;
    }

    protected override void Validate() {
        base.Validate();
        D.Assert(_resourceCategory != ResourceCategory.None, "{0}.{1} has not been set.".Inject(GetType().Name, typeof(ResourceCategory).Name));
    }

    protected override void Cleanup() {
        _resourceIDLookup.Keys.ForAll(containerGo => {
            MyEventListener.Get(containerGo).onTooltip -= ResourceContainerTooltipEventHandler;
        });
        MyEventListener.Get(_unknownLabel.gameObject).onTooltip -= UnknownTooltipEventHandler;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IComparable<ResourcesGuiElement> Members

    public int CompareTo(ResourcesGuiElement other) {
        return _resourcesCount.CompareTo(other._resourcesCount);
    }

    #endregion

    #region Nested Classes

    private enum Slot {
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

