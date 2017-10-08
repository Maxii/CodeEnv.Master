// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ResourcesGuiElement.cs
// AGuiElement that represents the Resources associated with an Item or Empire.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// AGuiElement that represents the Resources associated with an Item or Empire. Also handles unknown.
/// </summary>
public class ResourcesGuiElement : AGuiElement, IComparable<ResourcesGuiElement> {

    private const string LabelFormat = Constants.FormatInt_1DMin;

    /// <summary>
    /// The category of resources to be displayed. Use None if all resources present should be displayed.
    /// <remarks>This will cause these resources to be acquired from the provided ResourceYield.</remarks>
    /// </summary>
    [Tooltip("The category of resources to be displayed. Use None if all resources present should be displayed.")]
    [SerializeField]
    private ResourceCategory _resourceCategory = ResourceCategory.None;

#pragma warning disable 0649

    [SerializeField]
    private UIWidget[] _containers;

#pragma warning restore 0649

    private bool _isResourcesSet;   // can be a ResourceYield or null if unknown
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

    public override bool IsInitialized { get { return _isResourcesSet; } }

    /// <summary>
    /// Lookup for ResourceIDs, keyed by the Resource container's gameObject. 
    /// Used to show the right ResourceID tooltip when the container is hovered over.
    /// </summary>
    private IDictionary<GameObject, ResourceID> _resourceIDLookup;
    private UILabel _unknownLabel;  // label has a preset '?'
    private float _totalYield = Constants.ZeroF;

    protected override void InitializeValuesAndReferences() {
        _resourceIDLookup = new Dictionary<GameObject, ResourceID>(_containers.Length);
        _unknownLabel = gameObject.GetSingleComponentInImmediateChildren<UILabel>();
        MyEventListener.Get(_unknownLabel.gameObject).onTooltip += UnknownTooltipEventHandler;
        NGUITools.SetActive(_unknownLabel.gameObject, false);

        InitializeContainers();
    }

    private void InitializeContainers() {
        foreach (var container in _containers) {
            MyEventListener.Get(container.gameObject).onTooltip += ResourceContainerTooltipEventHandler;
            NGUITools.SetActive(container.gameObject, false);
        }
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
            TooltipHudWindow.Instance.Show("Resources unknown");
        }
        else {
            TooltipHudWindow.Instance.Hide();
        }
    }

    private void ResourcesPropSetHandler() {
        _isResourcesSet = true;
        if (IsInitialized) {
            PopulateMemberWidgetValues();
        }
    }

    #endregion

    protected override void PopulateMemberWidgetValues() {
        base.PopulateMemberWidgetValues();
        if (!Resources.HasValue) {
            HandleValuesUnknown();
            return;
        }

        IList<ResourceID> resourcesPresent;
        if (_resourceCategory == ResourceCategory.None) {
            resourcesPresent = Resources.Value.ResourcesPresent.ToList();
        }
        else {
            resourcesPresent = Resources.Value.ResourcesPresent.Where(res => res.GetResourceCategory() == _resourceCategory).ToList();
        }
        int resourcesCount = resourcesPresent.Count;
        float cumYield = Constants.ZeroF;
        for (int i = Constants.Zero; i < resourcesCount; i++) {
            UIWidget container = _containers[i];
            NGUITools.SetActive(container.gameObject, true);

            UISprite sprite = container.gameObject.GetSingleComponentInChildren<UISprite>();
            var resourceID = resourcesPresent[i];
            sprite.atlas = resourceID.GetIconAtlasID().GetAtlas();
            sprite.spriteName = resourceID.GetIconFilename();
            _resourceIDLookup.Add(container.gameObject, resourceID);

            float yield = Resources.Value.GetYield(resourceID);
            cumYield += yield;
            var label = container.gameObject.GetSingleComponentInChildren<UILabel>();
            label.text = LabelFormat.Inject(Mathf.RoundToInt(yield));
        }
        _totalYield = cumYield;
        D.Assert(_totalYield >= Constants.ZeroF);
    }

    private void HandleValuesUnknown() {
        NGUITools.SetActive(_unknownLabel.gameObject, true);
    }

    /// <summary>
    /// Resets this GuiElement instance so it can be reused.
    /// </summary>
    public override void ResetForReuse() {
        _containers.ForAll(container => {
            NGUITools.SetActive(container.gameObject, false);
        });
        NGUITools.SetActive(_unknownLabel.gameObject, false);
        _resourceIDLookup.Clear();
        _isResourcesSet = false;
        _totalYield = Constants.ZeroF;
    }

    #region Cleanup

    private void Unsubscribe() {
        foreach (var container in _containers) {
            MyEventListener.Get(container.gameObject).onTooltip -= ResourceContainerTooltipEventHandler;
        }
        MyEventListener.Get(_unknownLabel.gameObject).onTooltip -= UnknownTooltipEventHandler;
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    #endregion

    #region Debug

    protected override void __ValidateOnAwake() {
        base.__ValidateOnAwake();
        Utility.ValidateNotNullOrEmpty<UIWidget>(_containers);
        foreach (var container in _containers) {
            D.AssertNotNull(container);
        }
    }

    #endregion

    #region IComparable<ResourcesGuiElement> Members

    public int CompareTo(ResourcesGuiElement other) {
        int result;
        if (_totalYield == Constants.ZeroF) {
            result = other._totalYield == Constants.ZeroF ? Constants.Zero : Constants.MinusOne;
        }
        else if (Resources == null) {
            // an unknown yield (Resources == null) sorts higher than a yield that is Zero
            result = other.Resources == null ? Constants.Zero : (other._totalYield == Constants.ZeroF) ? Constants.One : Constants.MinusOne;
        }
        else {
            result = (other._totalYield == Constants.ZeroF || other.Resources == null) ? Constants.One : _totalYield.CompareTo(other._totalYield);
        }
        return result;
    }

    #endregion

    #region Archive

    // Approach that used fixed locations of containers

    //private const string LabelFormat = Constants.FormatFloat_1DpMax;
    //private const int MaxResourcesAllowed = 6;

    /// <summary>
    /// Lookup for Resource containers, keyed by the container's slot in the Element.
    /// </summary>
    //private IDictionary<Slot, UIWidget> _resourceContainerLookup = new Dictionary<Slot, UIWidget>(MaxResourcesAllowed, SlotEqualityComparer.Default);

    //private IDictionary<GameObject, ResourceID> _resourceIDLookup = new Dictionary<GameObject, ResourceID>(MaxResourcesAllowed);

    //protected override void InitializeValuesAndReferences() {
    //    _unknownLabel = gameObject.GetSingleComponentInImmediateChildren<UILabel>();

    //    var resourceContainers = gameObject.GetSafeComponentsInImmediateChildren<UIWidget>().Except(_unknownLabel);
    //    float avgLocalX = resourceContainers.Average(s => s.transform.localPosition.x);
    //    float avgLocalY = resourceContainers.Average(s => s.transform.localPosition.y);    // ~ 0F

    //    resourceContainers.ForAll(container => {
    //        Slot slot;
    //        bool toLeft = false;
    //        bool toTop = false;
    //        bool toBottom = false;

    //        var x = container.transform.localPosition.x;
    //        var y = container.transform.localPosition.y;
    //        toLeft = (x < avgLocalX) ? true : false;
    //        if (!Mathfx.Approx(y, avgLocalY, 1F)) {   // 1 Ngui virtual pixel of vertical leeway is plenty
    //            // this is not a middle sprite
    //            if (y > avgLocalY) {    // higher y values move the widget up
    //                toTop = true;
    //            }
    //            else {
    //                toBottom = true;
    //            }
    //        }

    //        if (toTop) {
    //            slot = toLeft ? Slot.TopLeft : Slot.TopRight;
    //        }
    //        else if (toBottom) {
    //            slot = toLeft ? Slot.BottomLeft : Slot.BottomRight;
    //        }
    //        else {
    //            slot = toLeft ? Slot.CenterLeft : Slot.CenterRight;
    //        }
    //        InitializeResourceContainer(slot, container);
    //    });

    //    MyEventListener.Get(_unknownLabel.gameObject).onTooltip += UnknownTooltipEventHandler;
    //    NGUITools.SetActive(_unknownLabel.gameObject, false);
    //}

    //private void InitializeResourceContainer(Slot slot, UIWidget container) {
    //    _resourceContainerLookup.Add(slot, container);

    //    var eventListener = MyEventListener.Get(container.gameObject);
    //    eventListener.onTooltip += ResourceContainerTooltipEventHandler;

    //    NGUITools.SetActive(container.gameObject, false);
    //}

    //protected override void PopulateMemberWidgetValues() {
    //    base.PopulateMemberWidgetValues();
    //    if (!Resources.HasValue) {
    //        HandleValuesUnknown();
    //        return;
    //    }

    //    var resourcesPresent = Resources.Value.ResourcesPresent.Where(res => res.GetResourceCategory() == _resourceCategory).ToList();
    //    _resourcesCount = resourcesPresent.Count;
    //    D.Assert(_resourcesCount <= MaxResourcesAllowed);
    //    for (int i = Constants.Zero; i < _resourcesCount; i++) {
    //        Slot slot = (Slot)i;
    //        UIWidget container = _resourceContainerLookup[slot];
    //        NGUITools.SetActive(container.gameObject, true);

    //        UISprite sprite = container.gameObject.GetSingleComponentInChildren<UISprite>();
    //        var resourceID = resourcesPresent[i];
    //        sprite.atlas = resourceID.GetIconAtlasID().GetAtlas();
    //        sprite.spriteName = resourceID.GetIconFilename();
    //        _resourceIDLookup.Add(container.gameObject, resourceID);

    //        var label = container.gameObject.GetSingleComponentInChildren<UILabel>();
    //        label.text = LabelFormat.Inject(Resources.Value.GetYield(resourceID));
    //    }
    //}

    //public override void ResetForReuse() {
    //    _resourceContainerLookup.Values.ForAll(container => {
    //        NGUITools.SetActive(container.gameObject, false);
    //    });
    //    NGUITools.SetActive(_unknownLabel.gameObject, false);
    //    _resourceIDLookup.Clear();
    //    _isResourcesSet = false;
    //}


    //private enum Slot {
    //    // in the order they should be populated
    //    TopLeft = 0,
    //    CenterLeft = 1,
    //    BottomLeft = 2,
    //    TopRight = 3,
    //    CenterRight = 4,
    //    BottomRight = 5
    //}

    ///// <summary>
    ///// IEqualityComparer for Slot. 
    ///// <remarks>For use when Slot is used as a Dictionary key as it avoids boxing from use of object.Equals.</remarks>
    ///// </summary>
    //private class SlotEqualityComparer : IEqualityComparer<Slot> {

    //    public static readonly SlotEqualityComparer Default = new SlotEqualityComparer();

    //    public string DebugName { get { return GetType().Name; } }

    //    public override string ToString() {
    //        return DebugName;
    //    }

    //    #region IEqualityComparer<Slot> Members

    //    public bool Equals(Slot value1, Slot value2) {
    //        return value1 == value2;
    //    }

    //    public int GetHashCode(Slot value) {
    //        return value.GetHashCode();
    //    }

    //    #endregion

    //}

    #endregion

}

