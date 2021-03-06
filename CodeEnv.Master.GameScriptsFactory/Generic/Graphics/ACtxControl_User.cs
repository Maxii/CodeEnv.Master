﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ACtxControl_User.cs
// Abstract, generic base class for CtxControls where the MenuOperator (Cmd, Element, Planetoid, etc.) is owned by the User. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract, generic base class for CtxControls where the MenuOperator (Cmd, Element, Planetoid, etc.) is owned by the User. 
/// User versions have submenus full of potential targets.
/// </summary>
/// <typeparam name="T">The enum type of Directives used by the MenuOperator.</typeparam>
public abstract class ACtxControl_User<T> : ACtxControl where T : struct {

    /// <summary>
    /// The format for the closest target menu item - Closest: TargetName(targetDistance);
    /// </summary>
    private const string SubmenuItemTextFormat_ClosestTarget = "Closest: {0}({1:0.})";

    /// <summary>
    /// The format for a target menu item - TargetName(targetDistance);
    /// </summary>
    private const string SubmenuItemTextFormat_Target = "{0}({1:0.})";

    /// <summary>
    /// Lookup table for IUnitTargets for this item, keyed by the ID of the submenu item selected.
    /// </summary>
    protected static IDictionary<int, INavigableDestination> _unitSubmenuTgtLookup = new Dictionary<int, INavigableDestination>();

    /// <summary>
    /// The directives available for execution when the user operator of the menu is the Item selected.
    /// </summary>
    protected abstract IEnumerable<T> UserMenuOperatorDirectives { get; }

    protected override bool SelectedItemMenuHasContent { get { return UserMenuOperatorDirectives.Any(); } }

    protected UserPlayerKnowledge _userKnowledge;
    protected UserAIManager _userAIMgr;
    private Stack<CtxMenu> _unusedSubMenus;

    public ACtxControl_User(GameObject ctxObjectGO, int uniqueSubmenusReqd, MenuPositionMode menuPosition)
        : base(ctxObjectGO, uniqueSubmenusReqd, menuPosition) {
        _userAIMgr = _gameMgr.UserAIManager;
        _userKnowledge = _userAIMgr.Knowledge;
        D.AssertNotNull(_userKnowledge);
    }

    protected override void PopulateMenu_MenuOperatorIsSelected() {
        base.PopulateMenu_MenuOperatorIsSelected();

        _unusedSubMenus = new Stack<CtxMenu>(_availableSubMenus);
        var topLevelMenuItems = new List<CtxMenu.Item>();
        foreach (T directive in UserMenuOperatorDirectives) {
            var topLevelItem = new CtxMenu.Item() {
                text = Enums<T>.GetName(directive)
                // setting the ID value is deferred until we know whether it is a submenu (in which case it is set to -1)
            };

            topLevelItem.isDisabled = IsUserMenuOperatorMenuItemDisabledFor(directive);
            if (!topLevelItem.isDisabled) {
                topLevelItem.isDisabled = TryPopulateItemSubMenu_UserMenuOperatorIsSelected(topLevelItem, directive);
            }
            //D.Log("{0}.{1} disabled state is {2}.", DebugName, topLevelItem.text, topLevelItem.isDisabled);
            if (!topLevelItem.isSubmenu) {
                D.AssertNotEqual(Constants.MinusOne, topLevelItem.id);
                topLevelItem.id = _nextAvailableItemId;
                _directiveLookup.Add(topLevelItem.id, directive);
                _nextAvailableItemId++;
            }
            topLevelMenuItems.Add(topLevelItem);
            //D.Log("{0}.{1}.ItemID = {2}.", DebugName, topLevelItem.text, topLevelItem.id);
        }
        _ctxObject.menuItems = topLevelMenuItems.ToArray();
    }

    /// <summary>
    /// Returns the initial disabled state of the MenuOperator menu item associated with this directive prior to attempting to
    /// populate a submenu for the same menu item. Default implementation returns false, aka not disabled.
    /// </summary>
    /// <param name="directive">The directive.</param>
    /// <returns></returns>
    protected virtual bool IsUserMenuOperatorMenuItemDisabledFor(T directive) {
        return false;
    }

    /// <summary>
    /// Tries to populate a submenu for the provided topLevelItem, if appropriate. Returns <c>true</c> 
    /// if the topLevel MenuItem should be disabled, false otherwise.
    /// </summary>
    /// <param name="topLevelItem">The item.</param>
    /// <param name="directive">The directive.</param>
    /// <returns></returns>
    private bool TryPopulateItemSubMenu_UserMenuOperatorIsSelected(CtxMenu.Item topLevelItem, T directive) {
        IEnumerable<INavigableDestination> targets;
        bool isSubmenuSupported = TryGetSubMenuUnitTargets_UserMenuOperatorIsSelected(directive, out targets);
        if (isSubmenuSupported) {
            // directive requires a submenu, although targets may be empty
            var targetsStack = new Stack<INavigableDestination>(targets);
            int submenuItemCount = targetsStack.Count;

            //D.Log("{0}: _unusedSubMenu count = {1}.", DebugName, _unusedSubMenus.Count);
            if (submenuItemCount > Constants.Zero) {
                submenuItemCount++; // make room for a Closest item
                var subMenu = _unusedSubMenus.Pop();
                subMenu.items = new CtxMenu.Item[submenuItemCount];
                for (int i = 0; i < submenuItemCount; i++) {
                    var target = i == 0 ? GameUtility.GetClosest(PositionForDistanceMeasurements, targets) : targetsStack.Pop();
                    int subMenuItemId = i + _nextAvailableItemId; // submenu item IDs can't interfere with IDs already assigned

                    string textFormat = i == 0 ? SubmenuItemTextFormat_ClosestTarget : SubmenuItemTextFormat_Target;
                    string tgtNameText = GetTargetNameTextForSubmenuItem(directive, target);
                    subMenu.items[i] = new CtxMenu.Item() {
                        text = textFormat.Inject(tgtNameText, GetDistanceTo(target)),
                        id = subMenuItemId
                    };
                    _unitSubmenuTgtLookup.Add(subMenuItemId, target);
                    _directiveLookup.Add(subMenuItemId, directive);
                }
                topLevelItem.isSubmenu = true;
                topLevelItem.id = -1;  // needed to get item spacing right
                topLevelItem.submenu = subMenu;
                _nextAvailableItemId += submenuItemCount;
                return false;   // targets are present to populate the submenu so don't disable the top-level item
            }
            topLevelItem.isSubmenu = true;
            topLevelItem.id = -1;   // needed to get item spacing right
            return true;    // targets are NOT present to populate the submenu so disable the top-level item
        }
        return false;   // directive doesn't support a submenu so don't disable the top-level item
    }

    /// <summary>
    /// Gets the target name text for this directive and target to be used in a submenu item.
    /// <remarks>Allows derived classes to incorporate color or other characteristics into the name displayed as a target in the submenu.</remarks>
    /// </summary>
    /// <param name="directive">The directive.</param>
    /// <param name="target">The target.</param>
    /// <returns></returns>
    protected virtual string GetTargetNameTextForSubmenuItem(T directive, INavigableDestination target) {
        return target.Name;
    }

    /// <summary>
    /// Returns <c>true</c> if the menu item associated with this directive supports a submenu for listing target choices,
    /// <c>false</c> otherwise. If false, upon return the top level menu item will be disabled. Default implementation is false with no targets.
    /// <remarks>The return value answers the question "Does the directive support submenus?" It does not mean "Are there any targets 
    /// in the submenu?" so don't return targets.Any()!</remarks>
    /// </summary>
    /// <param name="directive">The directive.</param>
    /// <param name="targets">The targets for the submenu if any were found. Can be empty.</param>
    /// <returns></returns>
    protected virtual bool TryGetSubMenuUnitTargets_UserMenuOperatorIsSelected(T directive, out IEnumerable<INavigableDestination> targets) {
        targets = Enumerable.Empty<INavigableDestination>();
        bool doesDirectiveSupportSubmenus = false;
        return doesDirectiveSupportSubmenus;
    }

    protected override void HandleHideCtxMenu() {
        base.HandleHideCtxMenu();
        _unitSubmenuTgtLookup.Clear();
    }

    #region Debug

    /// <summary>
    /// Validates that the unique submenus Qty reqd value submitted equals the number of subMenus actually required
    /// as determined by TryGetSubMenuUnitTargets_UserMenuOperatorIsSelected().
    /// <remarks>6.20.18 Replaced use of TryGetSubMenuUnitTargets_UserMenuOperatorIsSelected(directive) with 
    /// __IsSubmenuSupportedFor(directive) to allow TryGet... to fully implement Asserts and Knowledge checks.
    /// This was mandated when I realized that this control is initialized when an AI version of the control is swapped
    /// out for this User version just before the Item owner is changed to the User. When TryGet... was used, it
    /// went to PlayerKnowledge to acquire targets, but as the menuOperatorItem's owner had not yet changed, it was
    /// the wrong PlayerKnowledge.</remarks>
    /// </summary>
    [System.Diagnostics.Conditional("DEBUG")]
    protected void __ValidateUniqueSubmenuQtyReqd() {
        int submenusReqd = Constants.Zero;
        foreach (var directive in UserMenuOperatorDirectives) {
            if (__IsSubmenuSupportedFor(directive)) {
                submenusReqd++;
            }
        }
        D.AssertEqual(submenusReqd, _uniqueSubmenuQtyReqd, "{0}: Erroneous number of Reqd Submenus specified {1}.".Inject(DebugName, _uniqueSubmenuQtyReqd));
    }

    protected abstract bool __IsSubmenuSupportedFor(T directive);

    #endregion

}

