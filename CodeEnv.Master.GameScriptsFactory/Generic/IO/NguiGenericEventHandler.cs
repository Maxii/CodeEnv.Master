// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: NguiGenericEventHandler.cs
// Class that catches all Ngui events independant of whether they are consumed
// by other Gui and Game elements.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;

/// <summary>
/// Class that catches all Ngui events independant of whether they are consumed
/// by other Gui and Game elements.
/// </summary>
public class NguiGenericEventHandler : NguiFallthruEventHandler {

    protected override void InitializeOnStart() {
        UICamera.genericEventHandler = this.gameObject;
    }

    protected override void WriteMessageToConsole(string msg) {
        D.Error(msg);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    // prototype method for inspecting all events for camera control use
    // this filters out all events that occur on the Gui layer
    public bool __ValidateEventForCameraControllerUse() {
        bool isOnGuiLayer = UICamera.hoveredObject.layer == (int)Layers.Gui;
        if (isOnGuiLayer) {
            // the object under the mouse receiving the events is on the Gui layer,
            // so don't allow this event to propogate to our camera controls...
            return false;
        }
        return true;    // Is Gui layer the only criteria?
    }

}

