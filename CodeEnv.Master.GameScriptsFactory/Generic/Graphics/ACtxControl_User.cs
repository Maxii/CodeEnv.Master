// --------------------------------------------------------------------------------------------------------------------
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
using MoreLinq;
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
    /// Lookup table for IUnitTargets for this item, keyed by the ID of the item selected.
    /// </summary>
    protected static IDictionary<int, INavigable> _unitTargetLookup = new Dictionary<int, INavigable>();

    /// <summary>
    /// The directives available for execution when the user operator of the menu is the Item selected.
    /// </summary>
    protected abstract IEnumerable<T> UserMenuOperatorDirectives { get; }

    protected PlayerKnowledge _userKnowledge;
    private Stack<CtxMenu> _unusedSubMenus;

    public ACtxControl_User(GameObject ctxObjectGO, int uniqueSubmenusReqd, MenuPositionMode menuPosition)
        : base(ctxObjectGO, uniqueSubmenusReqd, menuPosition) {
        _userKnowledge = GameManager.Instance.UserAIManager.Knowledge;
        D.Assert(_userKnowledge != null);
    }

    protected override void PopulateMenu_UserMenuOperatorIsSelected() {
        base.PopulateMenu_UserMenuOperatorIsSelected();

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
            //D.Log("{0}.{1} disabled state is {2}.", GetType().Name, topLevelItem.text, topLevelItem.isDisabled);
            if (!topLevelItem.isSubmenu) {
                D.Assert(topLevelItem.id != -1);
                topLevelItem.id = _nextAvailableItemId;
                _directiveLookup.Add(topLevelItem.id, directive);
                _nextAvailableItemId++;
            }
            topLevelMenuItems.Add(topLevelItem);
            //D.Log("{0}.{1}.ItemID = {2}.", GetType().Name, topLevelItem.text, topLevelItem.id);
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
        IEnumerable<INavigable> targets;
        bool isSubmenuSupported = TryGetSubMenuUnitTargets_UserMenuOperatorIsSelected(directive, out targets);
        if (isSubmenuSupported) {
            // directive requires a submenu, although targets maybe empty
            var targetsStack = new Stack<INavigable>(targets);
            int submenuItemCount = targetsStack.Count;

            if (submenuItemCount > Constants.Zero) {
                submenuItemCount++; // make room for a Closest item
                var subMenu = _unusedSubMenus.Pop();
                subMenu.items = new CtxMenu.Item[submenuItemCount];
                for (int i = 0; i < submenuItemCount; i++) {
                    var target = i == 0 ? FindClosestTarget(ItemForDistanceMeasurements, targets) : targetsStack.Pop();
                    int subMenuItemId = i + _nextAvailableItemId; // submenu item IDs can't interfere with IDs already assigned

                    string textFormat = i == 0 ? SubmenuItemTextFormat_ClosestTarget : SubmenuItemTextFormat_Target;
                    subMenu.items[i] = new CtxMenu.Item() {
                        text = textFormat.Inject(target.DisplayName, GetDistanceTo(target)),
                        id = subMenuItemId
                    };
                    _unitTargetLookup.Add(subMenuItemId, target);
                    _directiveLookup.Add(subMenuItemId, directive);
                }
                topLevelItem.isSubmenu = true;
                topLevelItem.id = -1;  // needed to get item spacing right
                topLevelItem.submenu = subMenu;
                _nextAvailableItemId += submenuItemCount;
                return false;   // targets are present to populate the submenu so don't disable the toplevel item
            }
            topLevelItem.isSubmenu = true;
            topLevelItem.id = -1;   // needed to get item spacing right
            return true;    // targets are NOT present to populate the submenu so disable the toplevel item
        }
        return false;   // directive doesn't support a submenu so don't disable the toplevel item
    }

    /// <summary>
    /// Returns <c>true</c> if the menu item associated with this directive supports a submenu for listing target choices,
    /// <c>false</c> otherwise. If false, upon return the top level menu item will be disabled. Default implementation is false with no targets.
    /// </summary>
    /// <param name="directive">The directive.</param>
    /// <param name="targets">The targets for the submenu if any were found. Can be empty.</param>
    /// <returns></returns>
    protected virtual bool TryGetSubMenuUnitTargets_UserMenuOperatorIsSelected(T directive, out IEnumerable<INavigable> targets) {
        targets = Enumerable.Empty<INavigable>();
        return false;
    }

    protected override void HandleHideCtxMenu() {
        base.HandleHideCtxMenu();
        _unitTargetLookup.Clear();
    }

    private INavigable FindClosestTarget(AItem item, IEnumerable<INavigable> targets) {
        return targets.MinBy(t => Vector3.SqrMagnitude(t.Position - item.Position));
    }


}

