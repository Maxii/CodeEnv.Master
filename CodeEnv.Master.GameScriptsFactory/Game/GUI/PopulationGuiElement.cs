// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PopulationGuiElement.cs
// AGuiElement that represents a Unit's Population.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// AGuiElement that represents a Unit's Population.
/// <remarks>IMPROVE Race distribution. Adopt StrengthGuiElement distribution approach.</remarks>
/// </summary>
public class PopulationGuiElement : AGuiElement, IComparable<PopulationGuiElement> {

    private const string PopValueFormat_Label = Constants.FormatInt_1DMin;
    private const string PopValueFormat_Tooltip = Constants.FormatFloat_1DpMax;
    private const string TotalTooltipFormat = "Total Population: {0}";

#pragma warning disable 0649

    [SerializeField]
    private UIWidget[] _containers;

#pragma warning restore 0649

    [Tooltip("Check to show the distribution of races in addition to the total population")]
    [SerializeField]
    private bool _showDistribution = false;

    [Tooltip("Check to show the icons in addition to the values")]
    [SerializeField]
    private bool _showIcons = true;

    private bool _isPopulationPropSet;   // can be a null if unknown
    private float? _population;
    public float? Population {
        get { return _population; }
        set {
            D.Assert(!_isPopulationPropSet); // occurs only once between Resets
            _population = value;
            PopulationPropSetHandler();
        }
    }

    public override GuiElementID ElementID { get { return GuiElementID.Population; } }

    public override bool IsInitialized { get { return _isPopulationPropSet; } }

    protected override void InitializeValuesAndReferences() {
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

    private void TotalContainerTooltipEventHandler(GameObject containerGo, bool show) {
        if (show) {
            string tooltipText = GetTotalContainerTooltipText();
            TooltipHudWindow.Instance.Show(tooltipText);
        }
        else {
            TooltipHudWindow.Instance.Hide();
        }
    }

    private void DistributionContainerTooltipEventHandler(GameObject containerGo, bool show) {
        if (show) {
            string tooltipText = __GetDistributionContainerTooltipText();
            TooltipHudWindow.Instance.Show(tooltipText);
        }
        else {
            TooltipHudWindow.Instance.Hide();
        }
    }

    private void PopulationPropSetHandler() {
        _isPopulationPropSet = true;
        if (IsInitialized) {
            PopulateMemberWidgetValues();
        }
    }

    #endregion

    protected override void PopulateMemberWidgetValues() {
        base.PopulateMemberWidgetValues();

        UIWidget totalContainer = _containers[0];
        NGUITools.SetActive(totalContainer.gameObject, true);
        UISprite totalIconSprite = totalContainer.gameObject.GetSingleComponentInChildren<UISprite>();
        if (_showIcons) {
            totalIconSprite.atlas = AtlasID.MyGui.GetAtlas();
            totalIconSprite.spriteName = TempGameValues.PopulationIconFilename;
        }
        else {
            NGUITools.SetActive(totalIconSprite.gameObject, false);
        }

        var totalLabel = totalContainer.gameObject.GetSingleComponentInChildren<UILabel>();
        totalLabel.text = Population.HasValue ? PopValueFormat_Label.Inject(Mathf.RoundToInt(Population.Value)) : Unknown;
    }

    private string GetTotalContainerTooltipText() {
        return TotalTooltipFormat.Inject(Population.HasValue ? PopValueFormat_Tooltip.Inject(Population) : Unknown);
    }

    private string __GetDistributionContainerTooltipText() { // TODO placeholder while only total population is used
        throw new NotImplementedException();
    }

    /// <summary>
    /// Resets this GuiElement instance so it can be reused.
    /// </summary>
    public override void ResetForReuse() {
        _containers.ForAll(container => {
            NGUITools.SetActive(container.gameObject, false);
        });
        _isPopulationPropSet = false;
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
        D.Assert(!_showDistribution);   // UNDONE race distribution functionality not yet implemented
    }


    #endregion

    #region IComparable<PopulationGuiElement> Members

    public int CompareTo(PopulationGuiElement other) {
        int result;
        if (!Population.HasValue) {
            result = !other.Population.HasValue ? Constants.Zero : Constants.MinusOne;
        }
        else {
            result = !other.Population.HasValue ? Constants.One : Population.Value.CompareTo(other.Population.Value);
        }
        return result;
    }

    #endregion
}

