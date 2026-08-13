using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

// base class for all performance overloads

namespace Mesocyclone
{
    /// <summary>
    /// last resort for humanity
    /// </summary>
    public class PerformanceOverloadException : Exception
    {
        private static bool? _hasAsciiBellSupport;

        /// <summary>
        /// Whether the curent OS/terminal setup can reliably fire an ASCII BEL (\a)
        /// <br/><br/>
        /// Mac pretty much always can (i think, don't @ me i've never used mac before ;-;).
        /// Linux depends on whether the user has the required package and/or the terminal is wired up to anything for it to work.
        /// </summary>
        public static bool hasAsciiBellSupport
        {
            get
            {
                if (_hasAsciiBellSupport.HasValue)
                    return _hasAsciiBellSupport.Value;
                
                switch (UnityEngine.Application.platform)
                {
                    case RuntimePlatform.OSXPlayer:
                    case RuntimePlatform.OSXEditor:
                        _hasAsciiBellSupport = true;
                        break;
                    
                    case RuntimePlatform.LinuxPlayer:
                    case RuntimePlatform.LinuxEditor:
                        _hasAsciiBellSupport = CheckLinuxBellSupport();
                        break;
                    
                    default:
                        _hasAsciiBellSupport = false;
                        break;
                }

                return _hasAsciiBellSupport.Value;
            }
        }

        private static bool CheckLinuxBellSupport()
        {
            try
            {
                ProcessStartInfo PSI = new
                {
                    FileName = "which",
                    Arguments = "beep",
                    RedirectStandardOutput = true,
                    UserShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(PSI))
                {
                    process.WaitForExit(500);
                    return process.ExitCode == 0;
                }
            }
            catch
            {
                // "whitch" itself is missing, or couldn't create and/or start a process, so prolly no support
                return false;
            }
        }

        /// <summary>
        /// Use this to call the exception instead of manually throwing it, since immediately throwing the exveption might not casue anything to happen.
        /// </summary>
        public static void Call()
        {
            Alert();
            throw new PerformanceOverloadException();
        }

        private static void Alert()
        {   
            // kill me
            switch (UnityEngine.Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    try
                    {
                        System.Console.Beep();
                    }
                    catch
                    {
                        // no console to beep, so, uhhh, don't do anything ig. Idk any workarounds for this bullshit OS
                    }
                    break;
                
                // why do we still even use the term OS X? we're on MacOS 26 not 10 -.-
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    System.Console.Write("\a");
                
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.LinuxEditor:
                    if (hasAsciiBellSupport)
                        System.Console.Write("\a");
                    else
                        PlayLinuxSystemSound();
                    break;
                
                default: break;
            }
        }

        private static void PlayLinuxSystemSound()
        {
            const string soundPath = "/usr/share/sounds/freedesktop/stereo/suspend-error.oga";

            if (!File.Exists(soundPath))
                return;
            
            string[] players = ["paplay", "aplay"];

            foreach (var player in players)
            {
                try
                {
                    Process.Start
                    (
                        new ProcessStartInfo
                        {
                            // still no idea why none of these variables appear for me :/
                            FileName = player,
                            Arguments = $"\"{soundPath}\"", // i think this is how you write it
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    );
                    return; // use the first one that launches
                }
                catch
                {
                    // try the next player
                }
            }
        }

        private PerformanceOverloadException() : base("Game has overwhelmed all device resources, crash stages.")
        {
            UnityEngine.Debug.LogError(base.Message);
            UnityEngine.Debug.LogException(this);
            UnityEngine.Application.Quit();
        }
    }
}