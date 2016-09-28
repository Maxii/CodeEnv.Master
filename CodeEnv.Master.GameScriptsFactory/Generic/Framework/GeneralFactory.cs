// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GeneralFactory.cs
// Singleton factory that makes miscellaneous instances that aren't made by either UnitFactory or SystemFactory.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using PathologicalGames;
using UnityEngine;

/// <summary>
/// Singleton factory that makes miscellaneous instances that aren't made by either UnitFactory or SystemFactory.
/// </summary>
public class GeneralFactory : AGenericSingleton<GeneralFactory>, IGeneralFactory, IDisposable {

    [Obsolete]
    private OrbitSimulator _immobileCelestialOrbitSimPrefab;
    [Obsolete]
    private MobileOrbitSimulator _mobileCelestialOrbitSimPrefab;
    private ShipCloseOrbitSimulator _immobileShipOrbitSimPrefab;
    private MobileShipCloseOrbitSimulator _mobileShipOrbitSimPrefab;

    private GameObject _dynamicObjectsFolderGo;

    private GeneralFactory() {
        Initialize();
    }

    protected sealed override void Initialize() {
        _immobileCelestialOrbitSimPrefab = RequiredPrefabs.Instance.orbitSimulator;
        _mobileCelestialOrbitSimPrefab = RequiredPrefabs.Instance.mobileOrbitSimulator;
        _immobileShipOrbitSimPrefab = RequiredPrefabs.Instance.shipCloseOrbitSimulator;
        _mobileShipOrbitSimPrefab = RequiredPrefabs.Instance.mobileShipCloseOrbitSimulator;

        _dynamicObjectsFolderGo = DynamicObjectsFolder.Instance.gameObject;
    }

    /// <summary>
    /// Installs the provided orbitingObject into orbit around the OrbitedObject held by orbitData.
    /// </summary>
    /// <param name="orbitingGo">The orbiting GameObject.</param>
    /// <param name="orbitData">The orbit slot.</param>
    [Obsolete]
    public void InstallCelestialItemInOrbit(GameObject orbitingGo, OrbitData orbitData) {
        GameObject orbitSimPrefab = orbitData.IsOrbitedItemMobile ? _mobileCelestialOrbitSimPrefab.gameObject : _immobileCelestialOrbitSimPrefab.gameObject;
        GameObject orbitSimGo = UnityUtility.AddChild(orbitData.OrbitedItem, orbitSimPrefab);
        var orbitSim = orbitSimGo.GetSafeComponent<OrbitSimulator>();
        orbitSim.OrbitData = orbitData;
        orbitSimGo.name = orbitingGo.name + Constants.Space + typeof(OrbitSimulator).Name;
        UnityUtility.AttachChildToParent(orbitingGo, orbitSimGo);
        orbitingGo.transform.localPosition = GenerateRandomLocalPositionWithinSlot(orbitData);
    }

    /// <summary>
    /// Generates a random local position within the orbit slot at <c>MeanDistance</c> from the body orbited.
    /// Use to set the local position of the orbiting object once attached to the orbiter.
    /// </summary>
    /// <returns></returns>
    [Obsolete]
    private Vector3 GenerateRandomLocalPositionWithinSlot(OrbitData orbitData) {
        Vector2 pointOnCircle = RandomExtended.PointOnCircle(orbitData.MeanRadius);
        return new Vector3(pointOnCircle.x, Constants.ZeroF, pointOnCircle.y);
    }

    /// <summary>
    /// Makes and returns an instance of IShipCloseOrbitSimulator for this OrbitData.
    /// </summary>
    /// <param name="closeOrbitData">The orbit data.</param>
    /// <returns></returns>
    public IShipCloseOrbitSimulator MakeShipCloseOrbitSimulatorInstance(OrbitData closeOrbitData) {
        GameObject orbitSimPrefab = closeOrbitData.IsOrbitedItemMobile ? _mobileShipOrbitSimPrefab.gameObject : _immobileShipOrbitSimPrefab.gameObject;
        GameObject orbitSimGo = UnityUtility.AddChild(closeOrbitData.OrbitedItem, orbitSimPrefab);
        var orbitSim = orbitSimGo.GetSafeComponent<ShipCloseOrbitSimulator>();
        orbitSim.OrbitData = closeOrbitData;
        IShipCloseOrbitable closeOrbitableItem = closeOrbitData.OrbitedItem.GetComponent<IShipCloseOrbitable>();
        orbitSimGo.name = closeOrbitableItem.FullName + Constants.Space + typeof(ShipCloseOrbitSimulator).Name;  // OPTIMIZE
        return orbitSim;
    }

    /// <summary>
    /// Makes a GameObject that will auto destruct when its AudioSource (added by client) finishes playing. The position
    /// is important as the AudioSFX playing is 3D. Being too far away from the AudioListener on the MainCamera
    /// will result in no audio. Parented to the DynamicObjectsFolder.
    /// </summary>
    /// <param name="name">The name to apply to the gameObject.</param>
    /// <param name="position">The position to locate the gameObject.</param>
    /// <returns></returns>
    public GameObject MakeAutoDestruct3DAudioSFXInstance(string name, Vector3 position) {
        var go = new GameObject(name);
        UnityUtility.AttachChildToParent(go, _dynamicObjectsFolderGo);
        go.transform.position = position;
        go.layer = (int)Layers.TransparentFX;
        var destroyOnCompletion = go.AddComponent<DestroyEffectOnCompletion>();
        destroyOnCompletion.KindOfEffect = DestroyEffectOnCompletion.EffectType.AudioSFX;
        return go;
    }

    private void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IDisposable

    private bool _alreadyDisposed = false;

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose() {

        Dispose(true);

        // This object is being cleaned up by you explicitly calling Dispose() so take this object off
        // the finalization queue and prevent finalization code from 'disposing' a second time
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources.
    /// </summary>
    /// <param name="isExplicitlyDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool isExplicitlyDisposing) {
        if (_alreadyDisposed) { // Allows Dispose(isExplicitlyDisposing) to mistakenly be called more than once
            D.Warn("{0} has already been disposed.", GetType().Name);
            return; //throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
        }

        if (isExplicitlyDisposing) {
            // Dispose of managed resources here as you have called Dispose() explicitly
            Cleanup();
            CallOnDispose();
        }

        // Dispose of unmanaged resources here as either 1) you have called Dispose() explicitly so
        // may as well clean up both managed and unmanaged at the same time, or 2) the Finalizer has
        // called Dispose(false) to cleanup unmanaged resources

        _alreadyDisposed = true;
    }

    #endregion


}

