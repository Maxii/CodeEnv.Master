// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Recycler.cs
// Recycler of GameObjects that allows reuse. Derived from P31ObjectRecycler.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using UnityEngine;

    /// <summary>
    /// Recycler of GameObjects that allows reuse. Derived from P31ObjectRecycler.
    /// 
    ///Basic usage with an example of a bullet.  Assume spawnerPrefab is a bullet prefab and recycler is an var.
    ///
    /// Start:
    ///                 recycler = new Recycler( spawnerPrefab, 10 );
    ///         
    ///  Shoot:
    ///               // Grab the next free object from our recycler
    ///                GameObject bullet = recycler.nextFree;
    ///               
    ///               // Make sure we had something before proceeding
    ///                if( bullet )
    ///                {
    ///                        // Shoot our bullet in a CoRoutine so we can destroy it a few seconds later
    ///                        StartCoroutine( shootBullet( bullet ) );
    ///                }
    ///        
    ///        ShootBullet Coroutine:
    ///                // Activate the bullet gameobject and prepare to apply forces
    ///                bullet.SetActive(true);
    ///                
    ///                // Wait for 3 seconds
    ///                yield return new WaitForSeconds( 3.0f );
    ///                
    ///                // Recycle the bullet which deactivates it
    ///                recycler.freeObject( bullet );
    /// </summary>
    public class Recycler {

        private GameObject[] _objectStore;
        private bool[] _availableObjects;
        private int _nextFreeLoopStart = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="Recycler"/> class.
        /// </summary>
        /// <param name="prefab">The prefab GameObject to replicate for reuse.</param>
        /// <param name="maxObjects">The number of objects to replicate.</param>
        public Recycler(GameObject prefab, int maxObjects) {
            _objectStore = new GameObject[maxObjects];
            _availableObjects = new bool[maxObjects];

            for (int i = 0; i < maxObjects; i++) {
                // Create a new instance and set ourself as the recycleBin
                GameObject goClone = Object.Instantiate(prefab) as GameObject;
                goClone.SetActive(false);

                // Add it to our objectStore and set it to available
                _objectStore.SetValue(goClone, i);
                _availableObjects[i] = true;
            }
        }

        /// <summary>
        /// Gets the next free object available for use or null if none are available.
        /// </summary>
        public GameObject NextFree {
            get {
                for (; _nextFreeLoopStart < _availableObjects.Length; _nextFreeLoopStart++) {
                    if (_availableObjects[_nextFreeLoopStart]) {
                        // Set the object to unavailable and return it
                        _availableObjects[_nextFreeLoopStart] = false;
                        return _objectStore.GetValue(_nextFreeLoopStart) as GameObject;
                    }
                }

                // We purposely do not reset our nextFreeLoopStart here because it will get reset when the next object gets freed
                return null;
            }
        }

        /// <summary>
        /// Frees the object for reuse. Must be called by any object that wants to be reused.
        /// </summary>
        /// <param name="objectToFree">The object to free.</param>
        public void FreeObject(GameObject objectToFree) {
            int index = System.Array.IndexOf(_objectStore, objectToFree);
            if (index >= 0) {
                // Reset the nextFreeLoopStart if this object has a lower index
                if (index < _nextFreeLoopStart)
                    _nextFreeLoopStart = index;

                // Make the object inactive
                objectToFree.gameObject.SetActive(false);

                // Set the object to available
                _availableObjects[index] = true;
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

