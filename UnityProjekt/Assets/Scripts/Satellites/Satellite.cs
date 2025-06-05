using System;
using Satellites.SGP.Propagation;
using Satellites.SGP.TLE;
using UnityEngine;
using UnityEngine.Serialization;

namespace Satellites
{
    public class Satellite : MonoBehaviour
    {
        [FormerlySerializedAs("controller")] public SatelliteOrbit orbit;
        [FormerlySerializedAs("materialController")] public SatelliteModelController modelController;
        public Sgp4 OrbitPropagator;
        public Tle Tle;
        
        public bool IsISS { get; private set; }
        
        public bool Init(Tle tle, GameObject[] satelliteModelPrefabs, Material globalSpaceMaterial)
        {
            name = tle.NoradNumber + " " + tle.Name;
            Tle = tle;
        
            // ISS identifizieren
            IsISS = tle.NoradNumber == 25544;
        
            OrbitPropagator = new Sgp4(tle);
            orbit.Initialize(OrbitPropagator);
            return modelController.SetModel(satelliteModelPrefabs, globalSpaceMaterial, IsISS);
        }

        // Start is called before the first frame update
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
