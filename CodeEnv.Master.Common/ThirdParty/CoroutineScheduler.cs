﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CoroutineScheduler.cs
// A simple coroutine scheduler.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections;

    ///<example>
    ///My usage notes: Power the scheduler with my own
    ///UnityUpdateEvent timed to occur at the end of all
    ///other updates - use ExecutionOrder on a dedicated
    ///GameObject with the FramesLastUpdate script
    ///attached.
    ///
    /// private int _lastFrameCountAtUpdate;
    ///
    /// private void OnUpdate(UnityUpdateEvent e) {
    ///   int frameCount = Time.frameCount;
    ///    if (frameCount != _lastFrameCountAtUpdate) {
    ///        _coroutineScheduler.UpdateAllCoroutines(frameCount, Time.time);
    ///        _lastFrameCountAtUpdate = frameCount;
    ///    }
    ///}
    /// </example>

    /// <summary>
    /// CoroutineScheduler.cs
    /// 
    /// Port of the Javascript version from 
    /// http://www.unifycommunity.com/wiki/index.php?title=CoroutineScheduler
    /// 
    /// Linked list node type used by coroutine scheduler to track scheduling of coroutines.
    /// 
    /// 
    /// BMBF Researchproject http://playfm.htw-berlin.de
    /// PlayFM - Serious Games für den IT-gestützten Wissenstransfer im Facility Management 
    ///	Gefördert durch das bmb+f - Programm Forschung an Fachhochschulen profUntFH
    ///	
    ///	<author>Frank.Otto@htw-berlin.de</author>
    ///
    /// 
    /// A simple coroutine scheduler. Coroutines can yield until the next update
    /// "yield;", until a given number of updates "yield anInt", until a given
    /// amount of seconds "yield aFloat;", or until another coroutine has finished
    /// "yield scheduler.StartCoroutine(Coroutine())".
    /// 
    /// Multiple scheduler instances are supported and can be very useful. A
    /// coroutine running under one scheduler can yield (wait) for a coroutine
    /// running under a completely different scheduler instance.
    /// 
    /// Unity's YieldInstruction classes are not used because I cannot
    /// access their internal data needed for scheduling. Semantics are slightly
    /// different from Unity's scheduler. For example, in Unity if you start a
    /// coroutine it will run up to its first yield immediately, while in this
    /// scheduler it will not run until the next time UpdateAllCoroutines is called.
    /// This feature allows any code to start coroutines at any time, while
    /// making sure the started coroutines only run at specific times.
    /// 
    /// You should not depend on update order between coroutines running on the same
    /// update. For example, StartCoroutine(A), StartCoroutine(B), StartCoroutine(C)
    /// where A, B, C => while(true) { print(A|B|C); yield; }, do not expect "ABC" or
    /// "CBA" or any other specific ordering.
    /// </summary>
    [Obsolete]
    public class CoroutineScheduler {

        private CoroutineNode _firstNode = null;
        private int _currentFrame;
        private float _currentTime;

        /// <summary>
        /// Starts a coroutine, the coroutine does not run immediately but on the
        /// next call to UpdateAllCoroutines. The execution of a coroutine can
        /// be paused at any point using the yield statement. The yield return value
        /// specifies when the coroutine is resumed.
        /// </summary>
        /// <param name="fiber">The fiber.</param>
        /// <returns></returns>
        public CoroutineNode StartCoroutine(IEnumerator fiber) {
            // if function does not have a yield, fiber will be null and we no-op
            if (fiber == null) {
                return null;
            }
            // create coroutine node and run until we reach first yield
            CoroutineNode coroutine = new CoroutineNode(fiber);
            AddCoroutine(coroutine);
            return coroutine;
        }

        /// <summary>
        /// Stops all coroutines running on this behaviour. Use of this method is
        /// discouraged, think of a natural way for your coroutines to finish
        /// on their own instead of being forcefully stopped before they finish.
        /// If you need finer control over stopping coroutines you can use multiple
        /// schedulers.
        /// </summary>
        public void StopAllCoroutines() {
            _firstNode = null;
        }

        /// <summary>
        /// Returns true if this scheduler has any coroutines. You can use this to
        /// check if all coroutines have finished or been stopped.
        /// </summary>
        /// <returns></returns>
        public bool HasCoroutines() {
            return _firstNode != null;
        }

        /// <summary>
        /// Runs all active coroutines until their next yield. Caller must provide
        /// the current frame and time. This allows for schedulers to run under
        /// frame and time regimes other than the Unity's main game loop.
        /// </summary>
        /// <param name="frame">The frame.</param>
        /// <param name="time">The time.</param>
        public void UpdateAllCoroutines(int frame, float time) {
            _currentFrame = frame;
            _currentTime = time;
            CoroutineNode coroutine = this._firstNode;
            while (coroutine != null) {
                // store listNext before coroutine finishes and is removed from the list
                CoroutineNode listNext = coroutine.listNext;

                if (coroutine.waitForFrame > 0 && frame >= coroutine.waitForFrame) {
                    coroutine.waitForFrame = -1;
                    UpdateCoroutine(coroutine);
                }
                else if (coroutine.waitForTime > 0.0f && time >= coroutine.waitForTime) {
                    coroutine.waitForTime = -1.0f;
                    UpdateCoroutine(coroutine);
                }
                else if (coroutine.waitForCoroutine != null && coroutine.waitForCoroutine.finished) {
                    coroutine.waitForCoroutine = null;
                    UpdateCoroutine(coroutine);
                }
                else if (coroutine.waitForFrame == -1 && coroutine.waitForTime == -1.0f && coroutine.waitForCoroutine == null) {
                    // initial update
                    UpdateCoroutine(coroutine);
                }
                coroutine = listNext;
            }
        }

        /**
       * Executes coroutine until next yield. If coroutine has finished, flags
       * it as finished and removes it from scheduler list.
       */
        private void UpdateCoroutine(CoroutineNode coroutine) {
            IEnumerator fiber = coroutine.fiber;
            if (coroutine.fiber.MoveNext()) {
                System.Object yieldCommand = fiber.Current == null ? (System.Object)1 : fiber.Current;

                if (yieldCommand.GetType() == typeof(int)) {
                    coroutine.waitForFrame = (int)yieldCommand;
                    coroutine.waitForFrame += (int)_currentFrame;
                }
                else if (yieldCommand.GetType() == typeof(float)) {
                    coroutine.waitForTime = (float)yieldCommand;
                    coroutine.waitForTime += (float)_currentTime;
                }
                else if (yieldCommand.GetType() == typeof(CoroutineNode)) {
                    coroutine.waitForCoroutine = (CoroutineNode)yieldCommand;
                }
                else {
                    throw new System.ArgumentException("CoroutineScheduler: Unexpected coroutine yield type: " + yieldCommand.GetType());
                }
            }
            else {
                // coroutine finished
                coroutine.finished = true;
                RemoveCoroutine(coroutine);
            }
        }

        private void AddCoroutine(CoroutineNode coroutine) {

            if (this._firstNode != null) {
                coroutine.listNext = this._firstNode;
                _firstNode.listPrevious = coroutine;
            }
            _firstNode = coroutine;
        }

        private void RemoveCoroutine(CoroutineNode coroutine) {
            if (this._firstNode == coroutine) {
                // remove first
                this._firstNode = coroutine.listNext;
            }
            else {
                // not head of list
                if (coroutine.listNext != null) {
                    // remove between
                    coroutine.listPrevious.listNext = coroutine.listNext;
                    coroutine.listNext.listPrevious = coroutine.listPrevious;
                }
                else if (coroutine.listPrevious != null) {
                    // and listNext is null
                    coroutine.listPrevious.listNext = null;
                    // remove last
                }
            }
            coroutine.listPrevious = null;
            coroutine.listNext = null;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

