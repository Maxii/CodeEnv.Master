// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ACtxControl_Player.cs
//  Abstract, generic base class for Player versions of CtxControls.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract, generic base class for Player versions of CtxControls. Player versions have
/// MenuOperatorAccess submenus full of potential targets.
/// </summary>
/// <typeparam name="T">The enum type of Directives used by the MenuOperator.</typeparam>
public abstract class ACtxControl_Player<T> : ACtxControl where T : struct {

    /// <summary>
    /// The directives available for execution when accessing the context menu of the selected item.
    /// </summary>
    protected abstract IEnumerable<T> SelectedItemDirectives { get; }

    /// <summary>
    /// Gets the Item to measure from when determining which IUnitTargets are closest.
    /// </summary>
    protected abstract ADiscernibleItem ItemForFindClosest { get; }

    private Stack<CtxMenu> _unusedSubMenus;

    public ACtxControl_Player(GameObject ctxObjectGO) : base(ctxObjectGO) { }

    protected override void PopulateMenu_SelectedItemAccess() {
        base.PopulateMenu_SelectedItemAccess();

        _unusedSubMenus = new Stack<CtxMenu>(_availableSubMenus);
        var topLevelMenuItems = new List<CtxMenu.Item>();
        foreach (T directive in SelectedItemDirectives) {
            var topLevelItem = new CtxMenu.Item() {
                text = Enums<T>.GetName(directive)
                // setting the ID value is deferred until we know whether it is a submenu (in which case it is set to -1)
            };

            topLevelItem.isDisabled = IsSelectedItemMenuItemDisabled(directive);
            if (!topLevelItem.isDisabled) {
                topLevelItem.isDisabled = TryPopulateItemSubMenu_SelectedItemAccess(topLevelItem, directive);
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
    /// Returns the initial disabled state of the SelectedItem menu item associated with this directive prior to attempting to
    /// populate a submenu for the same menu item. Default implementation returns false, aka not disabled.
    /// </summary>
    /// <param name="directive">The directive.</param>
    /// <returns></returns>
    protected virtual bool IsSelectedItemMenuItemDisabled(T directive) {
        return false;
    }

    /// <summary>
    /// Tries to populate a submenu for the provided topLevelItem, if appropriate. Returns <c>true</c> if the topLevelItem
    /// should be disabled, false otherwise.
    /// </summary>
    /// <param name="topLevelItem">The item.</param>
    /// <param name="directive">The directive.</param>
    /// <returns></returns>
    private bool TryPopulateItemSubMenu_SelectedItemAccess(CtxMenu.Item topLevelItem, T directive) {
        IEnumerable<IUnitAttackableTarget> targets;
        if (TryGetSubMenuUnitTargets_SelectedItemAccess(directive, out targets)) {
            // directive requires a submenu, although targets maybe empty
            var targetsStack = new Stack<IUnitAttackableTarget>(targets);
            int submenuItemCount = targetsStack.Count;

            if (submenuItemCount > Constants.Zero) {
                submenuItemCount++; // make room for a Closest item
                var subMenu = _unusedSubMenus.Pop();
                subMenu.items = new CtxMenu.Item[submenuItemCount];
                for (int i = 0; i < submenuItemCount; i++) {
                    var target = i == 0 ? ItemForFindClosest.FindClosest(targets) : targetsStack.Pop();
                    int subMenuItemId = i + _nextAvailableItemId; // submenu item IDs can't interfere with IDs already assigned
                    subMenu.items[i] = new CtxMenu.Item() {
                        text = i == 0 ? "Closest" : target.DisplayName,
                        id = subMenuItemId
                    };
                    _unitTargetLookup.Add(subMenuItemId, target);
                    _directiveLookup.Add(subMenuItemId, directive);
                }
                topLevelItem.isSubmenu = true;
                topLevelItem.id = -1;  // needed to get item spacing right
                topLevelItem.submenu = subMenu;
                _nextAvailableItemId += submenuItemCount;
                return false;   // targets are present in the submenu so don't disable
            }
            topLevelItem.isSubmenu = true;
            topLevelItem.id = -1;   // needed to get item spacing right
            return true;    // targets are NOT present in the submenu so disable
        }
        return false;   // directive doesn't use a submenu so don't disable
    }

    /// <summary>
    /// Returns <c>true</c> if the item associated with this directive can have a submenu and targets, 
    /// <c>false</c> otherwise. Returns the targets for the subMenu if any were found. Default implementation is false and none.
    /// </summary>
    /// <param name="directive">The directive.</param>
    /// <param name="targets">The targets.</param>
    /// <returns></returns>
    protected virtual bool TryGetSubMenuUnitTargets_SelectedItemAccess(T directive, out IEnumerable<IUnitAttackableTarget> targets) {
        targets = Enumerable.Empty<IUnitAttackableTarget>();
        return false;
    }

}

