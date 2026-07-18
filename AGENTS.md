# Repository Guidelines

## Project Structure & Module Organization
This repository is the Unity `Assets` tree for RandomWorld. Gameplay code lives in `Scripts/2D`, with feature areas such as `Character`, `Domain`, `Tool`, `UI`, `Enum`, and `MVC`. Keep pure rules and calculations in `Scripts/2D/Domain/**` when possible, and keep Unity object, prefab, Photon, and scene access in adapter/manager layers. Third-party or imported code is under `Scripts/Reference`; avoid changing it unless the fix is intentionally vendor-specific.

Unity content is organized in `Scenes`, `Resources`, `ResourcesLocal`, `Materials`, `Animation`, `URP`, `TextMesh Pro`, `StreamingAssets`, and `AddressableAssetsData`. Preserve and commit matching `.meta` files for every asset change.

## Build, Test, and Development Commands
Use the Unity Editor to open the project root that contains this `Assets` folder. Build player targets from Unity Build Settings; existing runnable output and related files are kept under `Build`.

Useful repository commands:

```powershell
git status
git config core.hooksPath .githooks
powershell -NoProfile -ExecutionPolicy Bypass -File .\.gitarchive\Set-ArchivePassword.ps1
```

After prefab or Addressables changes, rebuild the relevant AssetBundles/Addressables before validating gameplay.

## Coding Style & Naming Conventions
C# code uses namespace `LAB2D`, four-space indentation, braces on their own lines, and PascalCase for classes, methods, properties, constants, and enum values. Private fields commonly use camelCase; prefer `this.` for instance member access in touched files when it matches local style. Keep `MonoBehaviour` classes focused on Unity lifecycle and scene wiring; put deterministic logic into small service/tool classes for easier review.

## Testing Guidelines
No dedicated test folder is currently present in `Assets`. Validate changes in the Unity Editor with Play Mode and scene-specific checks. For pure logic in `Domain` or `Tool`, add focused Unity Test Runner coverage when introducing new behavior, using descriptive names such as `DamageCalculatorTests` and `ApplyDefense_ClampsToMinimumDamage`.

## Commit & Pull Request Guidelines
Recent history uses short, imperative summaries, often conventional-style prefixes such as `refactor(WaveBoss): ...` or `refactor ...`. Prefer `feat(scope): summary`, `fix(scope): summary`, or `refactor(scope): summary` for clarity.

Pull requests should describe gameplay impact, list validation performed, mention affected scenes/prefabs/assets, and include screenshots or short clips for UI or visual changes. Link issues when applicable and call out any required AssetBundle or Addressables rebuild.

## Configuration & Asset Safety
Do not commit local-only secrets or machine-specific settings. Always keep `.meta` files with assets, avoid renaming assets outside Unity, and review prefab/scene diffs carefully before committing.
