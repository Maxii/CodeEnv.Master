// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IHasContextMenu.cs
// Interface indicating who has a Contextual Context Menu.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface indicating who has a Contextual Context Menu.
    /// </summary>
    public interface IHasContextMenu {

        /// <summary>
        /// Temporary method that validates that the attached CtxObject has it's CtxMenu set as Prefabs don't
        /// appear to hold onto the setting. Also checks that there is a Collider present which is also manditory
        /// for Context Menus to work.
        /// </summary>
        void __ValidateCtxObjectSettings();

        /// <summary>
        /// Called when a mouse button is pressed or unpressed on this gameobject. The object
        /// must forward the call to CtxPickHandler on the mainCamera for the ContextMenus to work.
        /// </summary>
        /// <param name="isDown">if set to <c>true</c> equivalent to Input.onMouseDown, false to onMouseUp.</param>
        void OnPress(bool isDown);

    }
}

