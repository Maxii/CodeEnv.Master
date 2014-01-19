// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UniverseCenterPresenter.cs
// An MVPresenter associated with a UniverseCenter View.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// An MVPresenter associated with a UniverseCenter View.
/// </summary>
public class UniverseCenterPresenter : AFocusablePresenter {

    public new UniverseCenterItem Item {
        get { return base.Item as UniverseCenterItem; }
        protected set { base.Item = value; }
    }

    public UniverseCenterPresenter(IViewable view) : base(view) { }

    protected override AItem AcquireItemReference() {
        return UnityUtility.ValidateMonoBehaviourPresence<UniverseCenterItem>(_viewGameObject);
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        return new GuiHudPublisher<Data>(Item.Data);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

