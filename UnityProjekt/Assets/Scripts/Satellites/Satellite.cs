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
        public SatelliteMaterialController materialController;
        public Sgp4 OrbitPropagator;
        public Tle Tle;
        
        public bool Init(Tle tle, GameObject[] satelliteModelPrefabs, Material globalSpaceMaterial)
        {
            name = tle.NoradNumber + " " + tle.Name;
            Tle = tle;
            OrbitPropagator = new Sgp4(tle);
            orbit.Initialize(OrbitPropagator);
            return materialController.SetModel(satelliteModelPrefabs, globalSpaceMaterial);
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
