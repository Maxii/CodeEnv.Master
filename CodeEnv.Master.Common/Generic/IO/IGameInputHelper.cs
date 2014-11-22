// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IGameInputHelper.cs
// Interface allowing access to the associated Unity-compiled script. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using UnityEngine;

    /// <summary>
    /// Interface allowing access to the associated Unity-compiled script. 
    /// Typically, a static reference to the script is established by GameManager in References.cs, providing access to the script from classes located in pre-compiled assemblies.
    /// </summary>
    public interface IGameInputHelper {

        /// <summary>
        /// Gets the NguiMouseButton that is being used to generate the current event.
        /// Valid only within an Ngui UICamera-generated event.
        /// </summary>
        /// <returns></returns>
        NguiMouseButton CurrentMouseButton { get; }

        /// <summary>
        /// Tests whether the left mouse button is the current button that is being
        /// used to generate this event. Valid only within an Ngui UICamera-generated event.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the left mouseButton is the one that was used to generate the current event; otherwise, <c>false</c>.
        /// </returns>
        bool IsLeftMouseButton { get; }

        /// <summary>
        /// Tests whether the right mouse button is the current button that is being
        /// used to generate this event. Valid only within an Ngui UICamera-generated event.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the right mouseButton is the one that was used to generate the current event; otherwise, <c>false</c>.
        /// </returns>
        bool IsRightMouseButton { get; }

        /// <summary>
        /// Tests whether the middle mouse button is the current button that is being
        /// used to generate this event. Valid only within an Ngui UICamera-generated event.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the middle mouseButton is the one that was used to generate the current event; otherwise, <c>false</c>.
        /// </returns>
        bool IsMiddleMouseButton { get; }

        bool IsAnyKeyOrMouseButtonDown { get; }

        /// <summary>
        /// Determines whether any of the specified keys are being held down.
        /// </summary>
        /// <param name="keyHeldDown">The key held down.</param>
        /// <param name="keys">The keys.</param>
        /// <returns></returns>
        bool TryIsKeyHeldDown(out KeyCode keyHeldDown, params KeyCode[] keys);

        /// <summary>
        /// Determines whether any of the specified keys were pressed down this frame.
        /// </summary>
        /// <param name="keyDown">The key down.</param>
        /// <param name="keys">The keys.</param>
        /// <returns></returns>
        bool TryIsKeyDown(out KeyCode keyDown, params KeyCode[] keys);

        /// <summary>
        /// Generic notification function. Used in place of SendMessage to shorten the code and allow for more than one receiver.
        /// Derived from Ngui's UICamera.Notify() as sometimes UICamera.Notify was busy sending a previous message.
        /// </summary>
        /// <param name="go">The GameObject to notify.</param>
        /// <param name="methodName">Name of the method to call.</param>
        /// <param name="obj">Optional parameter associated with the method.</param>
        void Notify(GameObject go, string methodName, object obj = null);

    }
}

