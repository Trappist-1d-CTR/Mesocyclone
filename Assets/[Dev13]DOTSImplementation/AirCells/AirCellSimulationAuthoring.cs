using System;
using System.ComponentModel;
using UnityEngine;
using Unity.Entites;

namespace Mesocyclone
{
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public class AirCellSimulationAuthoring : MonoBehaviour
    {
        public float TimeScale = 1f;
        public float DistanceScale = 1f;
        public float GravityScale = 1f;
        public Vector3 DronePosition;
        public float CdTest;
        public List<Vector3> StartingGrid;
        public float MoleTest;
        public float TempTest;
        public Vector3 VelTest;
        public Vector3 CenterTest;
        public GameObject Prefab;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Intialize()
        {
            GameObject simulator = new("Air Cell Simulator");
            simulator.AddComponent<AirCellSimulationAuthoring>();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public class Baker : Baker<AirCellSimulationAuthoring>
        {
            public override void Bake(AirCellSimulationAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);

                DependsOn(authoring.Prefab);
                Entity prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic);

                AddComponent(entity, new AirCellSimulation
                {
                    TimeScale = authoring.TimeScale,
                    DistanceScale = authoring.DistanceScale,
                    GravityScale = authoring.GravityScale,
                    DronePosition = authoring.DronePosition,
                    CdTest = authoring.CdTest,
                    StartingGrid = authoring.StartingGrid,
                    MoleTest = authoring.MoleTest,
                    TempTest = authoring.TempTest,
                    VelTest = authoring.VelTest,
                    CenterTest = authoring.CenterTest,
                    Prefab = prefab
                });
            }
        }
    }
}