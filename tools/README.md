# tools

| File | Purpose |
|---|---|
| `validate_project.py` | Static validation of the Unity project (meta files, GUID / fileID references, script ↔ class names, build settings, assembly definitions, Player prefab rules). Pure Python 3.8+, no dependencies. Run `python3 tools/validate_project.py --strict`; it is also executed by `.github/workflows/validate.yml`. |
| `known_external_guids.txt` | GUIDs of assets that live in Unity packages (Cinemachine, uGUI, …) and therefore are not in the repository. Add a line when a scene / prefab starts to reference a new package asset (the validator tells you which GUID is unknown). |
