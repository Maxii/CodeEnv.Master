// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Weapon.cs
// Data container class holding the characteristics of an Element's Weapon.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Data container class holding the characteristics of an Element's Weapon.
    /// </summary>
    public class Weapon : APropertyChangeTracking {

        static private string _toStringFormat = "{0}: Name[{1}], Operational[{2}], Strength[{3:0.#}], Range[{4:0.#}], ReloadPeriod[{5:0.#}], Size[{6:0.#}], Power[{7:0.#}]";

        private static string _nameFormat = "{0}_{1:0.#}";

        public event Action<Weapon> onIsOperationalChanged;

        public Guid ID { get; private set; }

        public Guid MonitorID { get; set; }

        private bool _isOperational;
        public bool IsOperational {
            get { return _isOperational; }
            set { SetProperty<bool>(ref _isOperational, value, "IsOperational", OnIsOperationalChanged); }
        }


        public string Name { get { return _nameFormat.Inject(_stat.BaseName, _stat.Strength.Combined); } }

        public CombatStrength Strength { get { return _stat.Strength; } }

        public float Range { get { return _stat.Range; } }

        public float ReloadPeriod { get { return _stat.ReloadPeriod; } }

        public float PhysicalSize { get { return _stat.ReloadPeriod; } }

        public float PowerRequirement { get { return _stat.PowerRequirement; } }

        private WeaponStat _stat;

        /// <summary>
        /// Initializes a new instance of the <see cref="Weapon"/> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        public Weapon(WeaponStat stat) {
            _stat = stat;
            ID = Guid.NewGuid();
        }

        private void OnIsOperationalChanged() {
            //if (IsOperational) {
            //    // just became operational again so reload
            //    InitiateReloadCycle();
            //}
            //else {
            //    // just lost operational status so kill any reload in process
            //    if (_reloadJob != null && _reloadJob.IsRunning) {
            //        _reloadJob.Kill();
            //    }
            //    IsReadyToFire = false;
            //}
            if (onIsOperationalChanged != null) {
                onIsOperationalChanged(this);
            }
        }

        //public bool IsReadyToFire { get; private set; }

        //public event Action<Weapon> onReadyToFire;

        //private Job _reloadJob;

        //private void InitiateReloadCycle() {
        //    _reloadJob = new Job(Reload(), toStart: true);
        //}

        //private IEnumerator Reload() {
        //    yield return new WaitForSeconds(ReloadPeriod);
        //    OnReloaded();
        //}

        //private void OnReloaded() {
        //    IsReadyToFire = true;
        //    if (onReadyToFire != null) {
        //        onReadyToFire(this);
        //    }
        //}

        //public void Fire(IElementTarget target) {
        //    D.Assert(IsOperational);
        //    D.Assert(IsReadyToFire);

        //    target.TakeHit(Strength);
        //    IsReadyToFire = false;
        //    InitiateReloadCycle();
        //}


        public override string ToString() {
            return _toStringFormat.Inject(GetType().Name, Name, IsOperational, Strength.Combined, Range, ReloadPeriod, PhysicalSize, PowerRequirement);
        }

    }
}

