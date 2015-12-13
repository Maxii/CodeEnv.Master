// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AInputManager.cs
// Singleton abstract base class that manages all user input.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Singleton abstract base class that manages all user input.
/// Mouse events originate from the Ngui event system.
/// </summary>
[Obsolete]
public abstract class AInputManager<T> : AMonoSingleton<T> where T : AMonoBase {

    /// <summary>
    /// The layers the UI EventDispatcher (2D) is allowed to 'see' when determining whether to raise an event.
    /// This covers the case when a Popup menu is open which can receive events, but the fixed UI elements should not.
    /// </summary>
    public static LayerMask UIEventDispatcherMask_PopupInputOnly { get { return _uiEventDispatcherMask_PopupInputOnly; } }
    private static LayerMask _uiEventDispatcherMask_PopupInputOnly = LayerMaskExtensions.CreateInclusiveMask(Layers.UIPopup);

    private GameInputMode _inputMode;
    /// <summary>
    /// The GameInputMode the game is currently operate in.
    /// </summary>
    public GameInputMode InputMode {
        get { return _inputMode; }
        set { SetProperty<GameInputMode>(ref _inputMode, value, "InputMode", OnInputModeChanged); }
    }

    /// <summary>
    /// The event dispatcher that sends events to UI objects.
    /// WARNING: This value is purposely null during scene transitions as otherwise,
    /// the instance provided would be from the previous scene with no warnings of such.
    /// </summary>
    public UICamera UIEventDispatcher { get; protected set; }

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        InitializeUIEventDispatcher();
    }

    protected virtual void InitializeUIEventDispatcher() {
        UIEventDispatcher = UIRoot.list.Single().gameObject.GetSafeMonoBehaviourComponentInChildren<UICamera>();
        UIEventDispatcher.eventType = UICamera.EventType.UI_2D;
        UIEventDispatcher.useKeyboard = true;
        UIEventDispatcher.useMouse = true;
        UIEventDispatcher.eventsGoToColliders = true;
    }

    protected abstract void OnInputModeChanged();

}

