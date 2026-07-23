#undef SHOW_PROCESS_INFO

// this is fucking useless

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MCCustom
{
    /// <summary>
    /// Re-Format / Wrapper of Unity's Coroutine system for my (half bread) personal liking :]
    /// </summary>
    public sealed class Process
    {
        #region Variables

#nullable enable // in case this isn't disabled

        /// <summary>The Coroutine masked as the Process that runs the Routine</summary>
        Coroutine? coroutine;

#nullable disable

        /// <summary>The IEnumerator Routine with all the logic that should be ran</summary>
        Func<IEnumerator> routine;

        /// <summary>True if the Process is running</summary>
        public bool isRunning => coroutine != null;

        #endregion

        #region Constructor

        /// <summary>Initializes the Process</summary>
        /// <param name="routine">Dictates the routine that the Process should handle</param>
        public Process(Func<IEnumerator> routine) => this.routine = routine;

        /// <summary>Starts the Process, thus starting the routine</summary>
        /// <param name="silent">If True Surpresses Debug Logging</param>
        public void Start(bool silent = false)
        {
            if (coroutine != null)
            {
#if SHOW_PROCESS_INFO
                if (!silent) Debug.Log($"Attempting to start process (=> {routine}) that's already running : EXITING...");
#endif

                return;
            }

            coroutine = AudioManager.Instance.StartCoroutine(routine()); // didn't have any other singleton :/

#if SHOW_PROCESS_INFO
            if (!silent) Debug.Log($"Starting Process {coroutine} => {routine}");
#endif
        }

        #endregion

        #region Public Functions

        /// <summary>Ends the Process, thus stopping the routine</summary>
        /// <param name="silent">If True Surpresses Debug Logging</param>
        public void Stop(bool silent = false)
        {
            if (coroutine == null)
            {
#if SHOW_PROCESS_INFO
                if (!silent) Debug.Log($"Attempting to stop a process (=> {routine}) that's already null : EXITING...");
#endif

                return;
            }

#if SHOW_PROCESS_INFO
            if (!silent) Debug.Log($"Stopping Process => {routine}");
#endif

            AudioManager.Instance.StopCoroutine(coroutine);
            coroutine = null;
        }

        /// <summary>Restarts the Process : If the process is running it'll stop then start again</summary>
        public void Restart()
        {
            if (coroutine == null)
            {
#if SHOW_PROCESS_INFO
                Debug.Log($"Attempting to restart a process (=> {routine}) that's null : RUNNING '{coroutine}.Start()'...");
#endif

                Start(true);
                return;
            }

            // Restart
            Stop(true); // silent
            Start(true); // silent

#if SHOW_PROCESS_INFO
            Debug.Log($"Restarting Process => {routine}");
#endif
        }

        #endregion
    }
}