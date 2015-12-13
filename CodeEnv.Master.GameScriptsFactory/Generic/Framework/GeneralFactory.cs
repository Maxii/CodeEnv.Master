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
using UnityEngine;

/// <summary>
/// Singleton factory that makes miscellaneous instances that aren't made by either UnitFactory or SystemFactory.
/// </summary>
public class GeneralFactory : AGenericSingleton<GeneralFactory>, IGeneralFactory, IDisposable {

    private OrbitSimulator _orbiterPrefab;
    private MovingOrbitSimulator _movingOrbiterPrefab;
    private ShipOrbitSimulator _orbiterForShipsPrefab;
    private MovingShipOrbitSimulator _movingOrbiterForShipsPrefab;

    private GameObject _dynamicObjectsFolderGo;

    private GeneralFactory() {
        Initialize();
    }

    protected override void Initialize() {
        _orbiterPrefab = RequiredPrefabs.Instance.orbiter;
        _movingOrbiterPrefab = RequiredPrefabs.Instance.movingOrbiter;
        _orbiterForShipsPrefab = RequiredPrefabs.Instance.orbiterForShips;
        _movingOrbiterForShipsPrefab = RequiredPrefabs.Instance.movingOrbiterForShips;

        _dynamicObjectsFolderGo = DynamicObjectsFolder.Instance.gameObject;
    }

    /// <summary>
    /// Makes the appropriate instance of IOrbiter parented to <c>parent</c> and not yet enabled.
    /// </summary>
    /// <param name="parent">The GameObject the IOrbiter should be parented too.</param>
    /// <param name="isParentMobile">if set to <c>true</c> [is parent mobile].</param>
    /// <param name="isForShips">if set to <c>true</c> [is for ships].</param>
    /// <param name="orbitPeriod">The orbit period.</param>
    /// <param name="orbiterName">Name of the orbiter.</param>
    /// <returns></returns>
    public IOrbitSimulator MakeOrbitSimulatorInstance(GameObject parent, bool isParentMobile, bool isForShips, GameTimeDuration orbitPeriod, string orbiterName = "") {
        GameObject orbiterPrefab = null;
        if (isParentMobile) {
            orbiterPrefab = isForShips ? _movingOrbiterForShipsPrefab.gameObject : _movingOrbiterPrefab.gameObject;
        }
        else {
            orbiterPrefab = isForShips ? _orbiterForShipsPrefab.gameObject : _orbiterPrefab.gameObject;
        }
        string name = orbiterName.IsNullOrEmpty() ? orbiterPrefab.name : orbiterName;
        GameObject orbiterCloneGo = UnityUtility.AddChild(parent, orbiterPrefab);
        orbiterCloneGo.name = name;
        var orbiter = orbiterCloneGo.GetSafeInterface<IOrbitSimulator>();
        orbiter.OrbitPeriod = orbitPeriod;
        return orbiter;
    }

    /// <summary>
    /// Makes an instance of an explosion, scaled to work with the item it is being applied too.
    /// </summary>
    /// <param name="itemRadius">The item radius.</param>
    /// <param name="itemPosition">The item position.</param>
    /// <returns></returns>
    public ParticleSystem MakeExplosionInstance(float itemRadius, Vector3 itemPosition) {
        var explosionPrefab = RequiredPrefabs.Instance.explosion;
        var explosionClone = UnityUtility.AddChild(_dynamicObjectsFolderGo, explosionPrefab.gameObject);
        explosionClone.layer = (int)Layers.TransparentFX;
        explosionClone.transform.position = itemPosition;

        var explosionScaleControl = explosionClone.GetSafeComponent<VisualEffectScale>();
        explosionScaleControl.ItemRadius = itemRadius;
        return explosionClone.GetComponent<ParticleSystem>();
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
        destroyOnCompletion.effectType = DestroyEffectOnCompletion.EffectType.AudioSFX;
        return go;
    }

    /// <summary>
    /// Makes an instance of Ordnance.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    /// <param name="firingElement">The GameObject firing this ordnance.</param>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    public AOrdnance MakeOrdnanceInstance(AWeapon weapon, GameObject firingElement) {
        AOrdnance prefab;
        GameObject ordnanceGo;
        switch (weapon.DeliveryVehicleCategory) {
            case WDVCategory.Beam:
                prefab = RequiredPrefabs.Instance.beam;
                GameObject muzzle = (weapon.WeaponMount as ILOSWeaponMount).Muzzle;
                ordnanceGo = UnityUtility.AddChild(muzzle, prefab.gameObject);
                ordnanceGo.layer = (int)Layers.TransparentFX;
                break;
            case WDVCategory.Missile:
                prefab = RequiredPrefabs.Instance.missile;
                ordnanceGo = UnityUtility.AddChild(_dynamicObjectsFolderGo, prefab.gameObject);
                Physics.IgnoreCollision(ordnanceGo.GetComponent<Collider>(), firingElement.GetComponent<Collider>());
                Missile missile = ordnanceGo.GetSafeComponent<Missile>();
                missile.ElementVelocityAtLaunch = firingElement.GetComponent<Rigidbody>().velocity;
                ordnanceGo.layer = (int)Layers.Projectiles;
                break;
            case WDVCategory.Projectile:
                prefab = RequiredPrefabs.Instance.projectile;
                ordnanceGo = UnityUtility.AddChild(_dynamicObjectsFolderGo, prefab.gameObject);
                Physics.IgnoreCollision(ordnanceGo.GetComponent<Collider>(), firingElement.GetComponent<Collider>());
                ordnanceGo.layer = (int)Layers.Projectiles;
                break;
            case WDVCategory.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(weapon.DeliveryVehicleCategory));
        }
        ordnanceGo.transform.position = weapon.WeaponMount.MuzzleLocation;
        ordnanceGo.transform.rotation = Quaternion.LookRotation(weapon.WeaponMount.MuzzleFacing);
        return ordnanceGo.GetSafeComponent<AOrdnance>();    //ordnanceGo.GetSafeInterface<IOrdnance>();
    }

    private void Cleanup() {
        CallOnDispose();
        // other cleanup here including any tracking Gui2D elements
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IDisposable

    private bool _alreadyDisposed = false;
    protected bool _isDisposing = false;

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
    /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
    /// </summary>
    /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool isDisposing) {
        // Allows Dispose(isDisposing) to be called more than once
        if (_alreadyDisposed) {
            return;
        }

        _isDisposing = true;
        if (isDisposing) {
            // free managed resources here including unhooking events
            Cleanup();
        }
        // free unmanaged resources here

        _alreadyDisposed = true;
    }

    // Example method showing check for whether the object has been disposed
    //public void ExampleMethod() {
    //    // throw Exception if called on object that is already disposed
    //    if(alreadyDisposed) {
    //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
    //    }

    //    // method content here
    //}
    #endregion

}

