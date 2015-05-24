// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IMyNguiEventListener.cs
// Interface for easy access to MyNguiEventListener.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using UnityEngine;

    /// <summary>
    /// Interface for easy access to MyNguiEventListener.
    /// </summary>
    public interface IMyNguiEventListener {

        event Action<GameObject> onSubmit;
        event Action<GameObject> onClick;
        event Action<GameObject> onDoubleClick;
        event Action<GameObject, bool> onHover;
        event Action<GameObject, bool> onPress;
        event Action<GameObject, bool> onSelect;
        event Action<GameObject, float> onScroll;
        event Action<GameObject> onDragStart;
        event Action<GameObject, Vector2> onDrag;
        event Action<GameObject> onDragOver;
        event Action<GameObject> onDragOut;
        event Action<GameObject> onDragEnd;
        event Action<GameObject, GameObject> onDrop;
        event Action<GameObject, KeyCode> onKey;
        event Action<GameObject, bool> onTooltip;

    }
}

