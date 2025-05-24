using System;
using System.Collections.Generic;
using CesiumForUnity;
using DefaultNamespace;
using Unity.Mathematics;
using UnityEngine;
using Satellites.SGP;
using Satellites.SGP.Propagation;
using Satellites.SGP.TLE;

namespace Satellites
{
    public class SatelliteController : MonoBehaviour
    {
        public Sgp4 OrbitPropagator;
        public Tle Tle;
        public bool ShouldCalculateOrbit;
        private GameObject orbitGO;
        private LineRenderer orbitRenderer;

        public bool Initialize(Tle tle, GameObject[] satelliteModelPrefabs, Material globalSpaceMaterial)
        {
            OrbitPropagator = new Sgp4(tle);
            Tle = tle;
            ShouldCalculateOrbit = name == "7646 STARLETTE";
            return ApplyRandomModel(satelliteModelPrefabs, globalSpaceMaterial);
        }

        public bool ApplyRandomModel(GameObject[] satelliteModelPrefabs, Material globalSpaceMaterial)
        {
            if (!TryGetRandomModelPrefab(satelliteModelPrefabs, out var modelPrefab))
                return false;

            if (!TryApplyModel(modelPrefab, globalSpaceMaterial))
                return false;

            return true;
        }

        private bool TryGetRandomModelPrefab(GameObject[] prefabs, out GameObject prefab)
        {
            prefab = null;
            if (prefabs == null || prefabs.Length == 0)
            {
                Debug.LogWarning("Keine Satelliten-Modelle konfiguriert!");
                return false;
            }

            int randomIndex = UnityEngine.Random.Range(0, prefabs.Length);
            prefab = prefabs[randomIndex];
            return prefab != null;
        }

        private bool TryApplyModel(GameObject modelPrefab, Material globalSpaceMaterial)
        {
            var tempModel = Instantiate(modelPrefab);
            var modelMeshFilter = tempModel.GetComponent<MeshFilter>();
            var modelMeshRenderer = tempModel.GetComponent<MeshRenderer>();
            if (modelMeshFilter == null || modelMeshRenderer == null || modelMeshFilter.sharedMesh == null)
                return false;

            CopyMeshAndMaterials(modelMeshFilter, modelMeshRenderer, globalSpaceMaterial);
            Destroy(tempModel);
            return true;
        }

        private void CopyMeshAndMaterials(MeshFilter modelMeshFilter, MeshRenderer modelMeshRenderer,
            Material globalSpaceMaterial)
        {
            var meshFilter = GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>();
            var meshRenderer = GetComponent<MeshRenderer>() ?? gameObject.AddComponent<MeshRenderer>();

            meshFilter.mesh = modelMeshFilter.sharedMesh;
            NormalizeSatelliteSize(modelMeshFilter.sharedMesh);

            var materialController = GetComponent<SatelliteMaterialController>() ??
                                     gameObject.AddComponent<SatelliteMaterialController>();
            materialController.zoomController = FindObjectOfType<CesiumZoomController>();
            materialController.earthModeMaterials = modelMeshRenderer.sharedMaterials;
            materialController.spaceMaterial = globalSpaceMaterial;

            meshRenderer.enabled = true;
            if (materialController.zoomController && materialController.zoomController.targetCamera)
                materialController.UpdateMaterial();
            else
                meshRenderer.materials = modelMeshRenderer.sharedMaterials;
        }

        private void NormalizeSatelliteSize(Mesh mesh)
        {
            // Zielgröße für alle Satelliten (anpassen nach Bedarf)
            float targetSize = 40000f;

            // Berechne die größte Dimension des aktuellen Meshes
            Bounds bounds = mesh.bounds;
            float maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

            // Berechne den Skalierungsfaktor
            float scaleFactor = targetSize / maxDimension;

            // Wende die Skalierung an
            gameObject.transform.localScale = Vector3.one * scaleFactor; // Semikolon hinzugefügt
        }

        public void Update()
        {
            if (ShouldCalculateOrbit) CalculateOrbit();
            else if (orbitGO)
            {
                Destroy(orbitGO);
            }
        }

        private void CalculateOrbit()
        {
            CreateOrbitGo();

            var positions = CalculateNextPositions(TimeSpan.FromHours(12), TimeSpan.FromMinutes(1));

            orbitRenderer.positionCount = positions.Count;
            orbitRenderer.SetPositions(positions.ToArray());
        }

        private void CreateOrbitGo()
        {
            if (orbitGO) return;
            orbitGO = new GameObject("OrbitPath");

            orbitRenderer = orbitGO.AddComponent<LineRenderer>();

            orbitRenderer.startWidth = 5000f;
            orbitRenderer.endWidth = 5000f;
            orbitRenderer.material = new Material(Shader.Find("Sprites/Default"));
            orbitRenderer.startColor = Color.cyan;
            orbitRenderer.endColor = Color.cyan;
        }

        public List<Vector3> CalculateNextPositions(TimeSpan until, TimeSpan stepSize)
        {
            var positions = new List<Vector3>();
            for (TimeSpan i = TimeSpan.Zero; i < until; i = i.Add(stepSize))
            {
                var pos = OrbitPropagator.FindPosition(SatelliteManager.Instance.CurrentSimulatedTime.Add(i))
                    .ToSphericalEcef();
                var position = math.mul(SatelliteManager.Instance.cesiumGeoreference.ecefToLocalMatrix,
                    new double4(pos.ToDouble(), 1.0)).xyz;
                positions.Add(position.ToVector());
            }

            return positions;
        }
    }
}