# Satellite Tracker Unity

Ein interaktives 3D-Satelliten-Visualisierungsprojekt für Unity, das Cesium for Unity nutzt, um Satelliten in Echtzeit
auf einem virtuellen Globus zu verfolgen.

## 🌍 Features

- **Echtzeit-Satellitenverfolgung**: Visualisierung von über 5000 aktiven Satelliten mit TLE-Daten von Celestrak
- **Interaktive Kamerasteuerung**: Nahtloser Übergang zwischen Weltraum- und Erdansicht
- **ISS-Tracking**: Spezielle Hervorhebung und Quick-Access für die Internationale Raumstation
- **Heatmap-Visualisierung**: Darstellung der Satellitendichte auf der Erdoberfläche
- **Ortssuche**: Schnelle Navigation zu Städten weltweit mit GeoNames-Datenbank
- **Satellitensuche**: Durchsuchbare Liste aller verfolgten Satelliten
- **Performance-optimiert**: GPU-Instancing und Job-System für flüssige Darstellung

## 🚀 Installation

1. **Klonen des Git Repository**

- https://git.uni-jena.de/se47toc/UnitySeminar

2. **Öffenen des Projektes in Unity**

- Unity Hub öffnen und Projekt hinzufügen
- Download und Zuweisung der korrekten Unity Version

## 🎮 Bedienung

### Kamerasteuerung

**Space-Modus (Weltraumansicht)**:

- Linke Maustaste gedrückt halten + Bewegen: Globus rotieren
- Mausrad: Zoom

**Earth-Modus (Nahansicht)**:

- ESC: Zwischen Inspektions- und Kameramodus wechseln
- Im Kameramodus:
    - W/A/S/D: Vorwärts/Links/Rückwärts/Rechts
    - Q/E: Hoch/Runter
    - Maus: Umsehen
    - Shift: Schnellere Bewegung
    - Mausrad: Vorwärts/Rückwärts
    - R: Zurück zur Ausgangsposition

### UI-Elemente

- **Space-Button**: Zurück zur Weltraumansicht
- **Suchleiste**: Orte auf der Erde suchen
- **Satellite List**: Liste aller Satelliten durchsuchen
- **ISS-Button**: Schnellzugriff auf die ISS
- **Show/Hide Toggle**: Satelliten ein-/ausblenden
- **Time Multiplier**: Simulationsgeschwindigkeit anpassen
- **Altitude Slider**: Satelliten nach Höhe filtern

## 🏗️ Projektstruktur

### Hauptkomponenten

- **SatelliteManager**: Zentrale Verwaltung aller Satelliten
- **CesiumZoomController**: Steuerung der Kameraübergänge
- **FreeFlyCamera**: First-Person-Kamerasteuerung
- **GlobeRotationController**: Orbit-Kamerasteuerung um die Erde
- **HeatmapController**: Visualisierung der Satellitendichte
- **SearchPanelController**: Satelliten-Suchfunktion
- **GeoNamesSearchFromJSON**: Ortssuche auf der Erde

### Satelliten-System

- **Satellite**: Basis-Komponente für jeden Satelliten
- **SatelliteOrbit**: Bahnberechnungen (SGP4-Algorithmus)
- **SatelliteModelController**: Verwaltung der 3D-Modelle
- **MoveSatelliteJobParallelForTransform**: Performance-optimierte Positionsaktualisierung

## ⚙️ Konfiguration

### SatelliteManager

- `timeMultiplier`: Simulationsgeschwindigkeit (1 = Echtzeit, 60 = 1 Minute/Sekunde)
- `satelliteModelPrefabs`: Array mit verfügbaren Satellitenmodellen
- `issModelPrefab`: Spezielles Modell für die ISS
- `globalSpaceMaterial`: Material für Satelliten in der Weltraumansicht

### CesiumZoomController

- `earthFov`: Field of View für Erdansicht (Standard: 60°)
- `spaceFov`: Field of View für Weltraumansicht (Standard: 80°)
- `fovTransitionDuration`: Dauer des Übergangs (Standard: 2.3s)

### HeatmapController

- `InfluenceRadius`: Einflussbereich jedes Satelliten (Standard: 1.000 km)
- `MaxDensityCount`: Maximale Dichte für Farbskalierung

## 📡 Datenquellen

- **TLE-Daten**: Automatischer Download von [Celestrak](https://celestrak.org/)
- **Ortsdatenbank**: GeoNames JSON-Datei mit weltweiten Städten
- **Aktualisierung**: TLE-Daten werden alle 12 Stunden gecacht