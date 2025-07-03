# Migration von GitLab zu GitHub

Dieses Repository wurde am 03.07.2025 von GitLab (https://git.uni-jena.de/se47toc/UnitySeminar) zu GitHub migriert.

## ⚠️ Wichtige Hinweise zur Migration

### Entfernte Inhalte
Während der Migration wurden folgende Ordner aus der Git-History entfernt, um die GitHub-Größenbeschränkungen einzuhalten:
- `MacOS_Built/`
- `Windows_Built/`  
- `Projektplanug/`

### Git LFS (Large File Storage)
Große Dateien werden nun über Git LFS verwaltet:
- **Cities-Datenbank:** `UnityProjekt/Assets/Data/Cities/cities.json` (189 MB)
- **3D-Modelle:** Alle `.glb` Dateien im `UnityProjekt/Assets/Modelle/` Ordner

### Fehlende Dateien
Einige Dateien, die in der Git-History gelöscht wurden (z.B. `random_satellite_11.glb`), sind nicht mehr im Repository vorhanden. Dies ist beabsichtigt und reduziert die Repository-Größe.

## Technische Details der Migration

### Verwendete Tools
- **git-filter-repo:** Zum Entfernen der Build-Ordner aus der Git-History
- **git-lfs:** Zur Verwaltung großer Dateien

### Migrations-Prozess
```bash
# 1. Repository klonen
git clone https://git.uni-jena.de/se47toc/UnitySeminar.git

# 2. Build-Ordner aus History entfernen
git filter-repo --path "MacOS_Built/" --invert-paths --force
git filter-repo --path "Windows_Built/" --invert-paths --force
git filter-repo --path "Projektplanung/" --invert-paths --force

# 3. Git LFS einrichten
git lfs install
git lfs track "UnityProjekt/Assets/Data/Cities/cities.json"
git lfs track "UnityProjekt/Assets/Modelle/**/*.glb"

# 4. Dateien zu LFS migrieren
git lfs migrate import --include="UnityProjekt/Assets/Data/Cities/cities.json" --include-ref=refs/heads/main
git lfs migrate import --include="UnityProjekt/Assets/Modelle/**/*.glb" --include-ref=refs/heads/main

# 5. Zu GitHub pushen
git remote add origin https://github.com/JanVogt06/SatTrak-SatelliteVisualization.git
git push -u origin main --force
```

## Repository klonen

Um dieses Repository zu klonen, stelle sicher, dass Git LFS installiert ist:

```bash
# Git LFS installieren (einmalig)
# macOS: brew install git-lfs
# Windows: Download von https://git-lfs.github.com/
# Linux: sudo apt-get install git-lfs

# LFS initialisieren
git lfs install

# Repository klonen
git clone https://github.com/JanVogt06/SatTrak-SatelliteVisualization.git
```
