using System.Collections.Generic;
using UnityEngine;

namespace Mesocyclone
{
    public sealed class TickSystem : MonoBehaviour
    {
        public static TickSystem Instance { get; private set; }

        private readonly List<Tickable> _tickables = new();
        public IReadOnlyList<Tickable> Tickables => _tickables;

        public void Register(Tickable Tickable)
        {
            if (!_tickables.Contains(Tickable))
                _tickables.Add(Tickable);
        }

        public void Unregister(Tickable Tickable)
        {
            if (_tickables.Contains(Tickable))
                _ = _tickables.Remove(Tickable);
        }

        public void Unregister(ushort index) // unsigned short. Values between 0 and 65,535 . Should be enough for most cases, and saves memory over int
        {
            if (_tickables[index] != null)
                _tickables.RemoveAt(index);
        }

        public void UnregisterAll()
        {
            _tickables.Clear();
        }


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            if (Instance == null)
            {
                GameObject tickSystem = new("Tick System");
                _ = tickSystem.AddComponent<TickSystem>();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            DontDestroyOnLoad(gameObject);
            Instance = this;
        }

        // can't use regular threading because Unity API calls must be made on the main thread
        // and Jobs requires NativeArray, which uses structs, and Tickable is a class
        // why am i a programmer
        // ts is too difficult for me
        private void Update()
        {
            foreach (var tickable in _tickables)
                try
                { tickable.Tick(); } // definitely not an MSC reference
                catch //your sins will not go unnoticed
                { throw new Joar("Tick: " + tickable.name); }
        }

        private void FixedUpdate()
        {
            foreach (var tickable in _tickables)
                try
                { tickable.FixedTick(); }
                catch
                { throw new Joar("FixedTick: " + tickable.name); }
        }

        private void LateUpdate()
        {
            foreach (var tickable in _tickables)
                try
                { tickable.LateTick(); }
                catch
                { throw new Joar("LateTick: " + tickable.name); }
        }
    }
}