using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mesocyclone
{
    public sealed class TickSystem : MonoBehaviour
    {
        public static TickSystem Instance { get; private set; }

        List<Tickable> _tickables = new();
        public IReadOnlyList<Tickable> Tickables => _tickables;


        public void Register(Tickable Tickable)
        {
            if (!_tickables.Contains(Tickable))
                _tickables.Add(Tickable);
        }

        public void Unregister(Tickable Tickable)
        {
            if (_tickables.Contains(Tickable))
                _tickables.Remove(Tickable);
        }

        public void Unregister(UInt16 index) // unsigned short. Values between 0 and 65,535 . Should be enough for most cases, and saves memory over int
        {
            if (_tickables[(Int32)index] != null)
                _tickables.RemoveAt(index);
        }

        public void UnregisterAll()
        {
            _tickables.Clear();
        }


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            if (Instance == null)
            {
                GameObject tickSystem = new GameObject("Tick System");
                _ = tickSystem.AddComponent<TickSystem>();
            }
        }


        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            DontDestroyOnLoad(gameObject);
            Instance = this;
        }

        // TODO: figure out how to make this parallelized, or at least more efficient
        // can't use regular threading because Unity API calls must be made on the main thread
        // and Jobs requires NativeArray, which uses structs, and Tickable is a class
        // why am i a programmer
        // ts is too difficult for me
        void Update()
        {
            try
            {
                foreach (var tickable in _tickables)
                {
                    tickable.Accumulator += Time.deltaTime;
                    float interval = 1f / (tickable.TickRate * Time.timeScale);

                    // using while makes it so multiple due ticks catch up - i think so... 
                    // and even if it does, not sure how well it does performance wise
                    while (tickable.Accumulator >= interval)
                    {
                        tickable.Tick();
                        tickable.Accumulator -= interval;
                    }
                }
            }
            // definitely not an MSC reference
            catch
            {
                // your sins will not go unnoticed
                throw new Joar();
            }
        }

        void FixedUpdate()
        {
            try
            {
                foreach (var tickable in _tickables)
                {
                    tickable.FixedAccumulator += Time.fixedDeltaTime;
                    float interval = 1f / (tickable.TickRate * Time.timeScale);

                    while (tickable.FixedAccumulator >= interval)
                    {
                        tickable.FixedTick();
                        tickable.FixedAccumulator -= interval;
                    }
                }
            }
            catch
            {
                throw new Joar();
            }
        }
    }
}