// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StrengthGuiElement.cs
// AGuiElement that represents the CombatStrength of a MortalItem. Handles Offensive or Defensive and Unknown strength.
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
/// AGuiElement that represents the CombatStrength of a MortalItem. Handles Offensive or Defensive and Unknown strength.
/// </summary>
public class StrengthGuiElement : AGuiElement, IComparable<StrengthGuiElement> {

    private const string StrengthValueFormat_Label = Constants.FormatInt_1DMin;
    private const string StrengthValueFormat_Tooltip = Constants.FormatFloat_1DpMax;
    private const string TooltipFormat = "{0}[{1}]";
    private const string UnknownTooltipFormat = "{0} Unknown";

#pragma warning disable 0649

    [SerializeField]
    private UIWidget[] _containers;

#pragma warning restore 0649

    [Tooltip("Check to show the distribution across WDVCategory in addition to the total strength")]
    [SerializeField]
    private bool _showDistribution = false;

    [Tooltip("Check to show the icons in addition to the values")]
    [SerializeField]
    private bool _showIcons = true;

    [Tooltip("The unique ID of this Strength GuiElement")]
    [SerializeField]
    private GuiElementID _elementID = GuiElementID.None;
    public override GuiElementID ElementID { get { return _elementID; } }

    private bool _isStrengthPropSet;
    private CombatStrength? _strength;  // can be null (unknown), default (no strength) or has strength
    public CombatStrength? Strength {
        get { return _strength; }
        set {
            D.Assert(!_isStrengthPropSet);  // only occurs once between Resets
            _strength = value;
            StrengthPropSetHandler();  // SetProperty() only calls handler when changed
        }
    }

    protected override string TooltipContent { get { return ElementID.GetValueName(); } }

    public override bool IsInitialized { get { return _isStrengthPropSet; } }

    /// <summary>
    /// Lookup for Weapon's EquipmentCategory, keyed by the category container's gameObject. 
    /// Used to show the right tooltip when the container is hovered over.
    /// </summary>
    private IDictionary<GameObject, EquipmentCategory> _strengthCatLookup;
    private UILabel _unknownLabel;

    protected override void InitializeValuesAndReferences() {
        _strengthCatLookup = new Dictionary<GameObject, EquipmentCategory>(_containers.Length);

        _unknownLabel = gameObject.GetSingleComponentInImmediateChildren<UILabel>(includeInactive: true);
        if (_unknownLabel.gameObject.activeSelf) {   // 10.21.17 If initially inactive, this usage can't result in unknown
            MyEventListener.Get(_unknownLabel.gameObject).onTooltip += UnknownTooltipEventHandler;
            NGUITools.SetActive(_unknownLabel.gameObject, false);
        }
        InitializeContainers();
    }

    private void InitializeContainers() {
        bool isFirstContainer = true;
        foreach (var container in _containers) {
            if (isFirstContainer) {
                MyEventListener.Get(container.gameObject).onTooltip += TotalContainerTooltipEventHandler;
                isFirstContainer = false;
            }
            else {
                if (_showDistribution) {
                    MyEventListener.Get(container.gameObject).onTooltip += DistributionContainerTooltipEventHandler;
                }
            }
            NGUITools.SetActive(container.gameObject, false);
        }
    }

    #region Event and Property Change Handlers

    private void UnknownTooltipEventHandler(GameObject go, bool show) {
        if (show) {
            TooltipHudWindow.Instance.Show(UnknownTooltipFormat.Inject(ElementID.GetValueName()));
        }
        else {
            TooltipHudWindow.Instance.Hide();
        }
    }

    private void TotalContainerTooltipEventHandler(GameObject containerGo, bool show) {
        if (show) {
            string tooltipText = TooltipFormat.Inject("Total: ", StrengthValueFormat_Tooltip.Inject(Strength.Value.TotalDeliveryStrength));
            TooltipHudWindow.Instance.Show(tooltipText);
        }
        else {
            TooltipHudWindow.Instance.Hide();
        }
    }

    private void DistributionContainerTooltipEventHandler(GameObject containerGo, bool show) {
        if (show) {
            ////var wdvCategory = _wdvCategoryLookup[containerGo];
            ////string tooltipText = GetTooltipText(wdvCategory);
            var category = _strengthCatLookup[containerGo];
            string tooltipText = GetTooltipText(category);
            TooltipHudWindow.Instance.Show(tooltipText);
        }
        else {
            TooltipHudWindow.Instance.Hide();
        }
    }

    private void StrengthPropSetHandler() {
        _isStrengthPropSet = true;
        if (IsInitialized) {
            PopulateMemberWidgetValues();
        }
    }

    #endregion

    protected override void PopulateMemberWidgetValues() {
        base.PopulateMemberWidgetValues();

        if (!Strength.HasValue) {
            HandleValuesUnknown();
            return;
        }

        UIWidget totalContainer = _containers[0];
        NGUITools.SetActive(totalContainer.gameObject, true);
        UISprite totalIconSprite = totalContainer.gameObject.GetSingleComponentInChildren<UISprite>();
        if (_showIcons) {
            totalIconSprite.atlas = Strength.Value.Mode.GetIconAtlasID().GetAtlas();
            totalIconSprite.spriteName = Strength.Value.Mode.GetIconFilename();
        }
        else {
            NGUITools.SetActive(totalIconSprite.gameObject, false);
        }
        UILabel totalIconLabel = totalContainer.gameObject.GetSingleComponentInChildren<UILabel>();
        totalIconLabel.text = StrengthValueFormat_Label.Inject(Mathf.RoundToInt(Strength.Value.TotalDeliveryStrength.__Total));

        if (_showDistribution) {
            EquipmentCategory[] weapCatsToShow = TempGameValues.WeaponEquipCats;

            int showCount = weapCatsToShow.Length;
            D.Assert(_containers.Length >= showCount);
            for (int i = Constants.One; i < showCount; i++) {
                UIWidget container = _containers[i];
                NGUITools.SetActive(container.gameObject, true);

                UISprite iconSprite = container.gameObject.GetSingleComponentInChildren<UISprite>();
                EquipmentCategory categoryToShow = weapCatsToShow[i];
                if (_showIcons) {
                    iconSprite.atlas = categoryToShow.__GetIconAtlasID().GetAtlas();
                    iconSprite.spriteName = categoryToShow.__GetIconFilename();
                }
                else {
                    NGUITools.SetActive(iconSprite.gameObject, false);
                }
                _strengthCatLookup.Add(container.gameObject, categoryToShow);

                var weapCatStrength = Strength.Value.GetStrength(categoryToShow);

                string strengthText = StrengthValueFormat_Label.Inject(Mathf.RoundToInt(weapCatStrength.__Total));
                var strengthLabel = container.gameObject.GetSingleComponentInChildren<UILabel>();
                strengthLabel.text = strengthText;
            }
        }
    }

    private string GetTooltipText(EquipmentCategory category) {
        float catStrengthValue = Strength.Value.GetStrength(category).__Total;
        return TooltipFormat.Inject(category.GetValueName(), StrengthValueFormat_Tooltip.Inject(catStrengthValue));
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
        _strengthCatLookup.Clear();
        _isStrengthPropSet = false;
    }

    #region Cleanup

    private void Unsubscribe() {
        bool isFirstContainer = true;
        foreach (var container in _containers) {
            if (isFirstContainer) {
                MyEventListener.Get(container.gameObject).onTooltip -= TotalContainerTooltipEventHandler;
                isFirstContainer = false;
            }
            else {
                if (_showDistribution) {
                    MyEventListener.Get(container.gameObject).onTooltip -= DistributionContainerTooltipEventHandler;
                }
            }
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
        D.Assert(ElementID == GuiElementID.OffensiveStrength || ElementID == GuiElementID.DefensiveStrength, ElementID.GetValueName());
        Utility.ValidateNotNullOrEmpty<UIWidget>(_containers);
        foreach (var container in _containers) {
            D.AssertNotNull(container);
        }
        if (_showDistribution) {
            D.Assert(_containers.Count() >= Constants.One + 4); // 1 Total + 4 EquipmentCategories that are also Weapons
            ////D.Assert(_containers.Count() >= Constants.One + Enums<WDVCategory>.GetValues(excludeDefault: true).Count()); // 1 Total + 4 WDVCategories
        }
    }

    #endregion

    #region IComparable<StrengthGuiElement2> Members

    public int CompareTo(StrengthGuiElement other) {
        int result;
        if (!Strength.HasValue) {
            result = !other.Strength.HasValue ? Constants.Zero : Constants.MinusOne;
        }
        else {
            result = !other.Strength.HasValue ? Constants.One : Strength.Value.CompareTo(other.Strength.Value);
        }
        return result;
    }

    #endregion
}


