using System.Collections.Generic;
using System;
using System.ComponentModel;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Mesocyclone.Data;

// this class contains all the components for an air cell
// alongside the authoring component that prefab has attached to automatically bind
// all the ECS components

namespace Mesocyclone.MesoDOTS
{
    #region ECS Component Data

    // base component for all air cells
    public struct AirCell : IComponentData
    {
        public int ID;
        public float3 CellCenter;
        public float Moles;
        public float Temperature;
        public float3 Velocity;
        public float3 Acceleration;
    }

    // the geometry / dimensions of the air cell
    public struct AirCellGeometry : IComponentData
    {
        public float CellStaticVolume;
        public float CellCircleArea;
        public float CellRadius;
        public float CellHeight;
    }

    // flag that marks that the cell needs to be initialized
    // pretty obvious
    public struct AirCellNeedsInitialization : IComponentData
    { }

    #endregion


    #region Authoring Component

    // the actual component attached to the GameObject prefab
    // which links all the ECS components on the entity-side
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public class AirCellAuthoring : MonoBehaviour
    {
        [Header("Main")]
        public int CellID;
        public Vector3 CellCenter;
        public float Moles = 1000f;
        public float Temperature = 300f;
        public Vector3 Velocity;
        public Vector3 Acceleration;

        [Header("Geometry")]
        public float CellStaticVolume;
        public float CellCircleArea;
        public float CellRadius;
        public float CellHeight;


        #region Baker

        [EditorBrowsable(EditorBrowsableState.Never)]
        public class Baker : Baker<AirCellAuthoring>
        {
            public override void Bake(AirCellAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                #region Struct Initialization on author cause for some fucking reason you can't assign default on decleration
                authoring.CellCenter = default;
                authoring.Velocity = default;
                authoring.Acceleration = default;
                #endregion

                AddComponent(entity, new AirCell
                {
                    ID = authoring.CellID,
                    CellCenter = authoring.CellCenter,
                    Moles = authoring.Moles,
                    Temperature = authoring.Temperature,
                    Velocity = authoring.Velocity,
                    Acceleration = authoring.Acceleration
                });

                AddComponent(entity, new AirCellGeometry
                {
                    CellStaticVolume = authoring.CellStaticVolume,
                    CellCircleArea = authoring.CellCircleArea,
                    CellRadius = authoring.CellRadius,
                    CellHeight = authoring.CellHeight
                });

                AddComponent(entity, new AirCellNeedsInitialization());
            }
        }

        #endregion
    }

    #endregion
}
