# Changelog

## [1.0.1] - 2026-11-17
### Documentation
- Expanded `README.md` with architecture details, copy-pasteable C# usage and custom `IAssetImportRule` examples, core API table, sample and test references, and troubleshooting notes.
- Added XML documentation comments across all Editor and Sample C# types, methods, properties, and configuration fields.

## [1.0.0] - 2025-11-03
### Added
- `AssetImportPolicyAsset` ScriptableObject with non-destructive `RuleOverride<T>` configurations for Textures/Sprites/UI, 3D Models, and Audio clips.
- Starter presets for Mobile 2D/UI/Audio, PC 3D URP, and Pixel Art projects.
- `AssetPathMatcher` supporting folder prefixes, glob patterns, extension filters, and exclusion rules.
- `AssetImportRulesPostprocessor` hooking `OnPreprocessTexture`, `OnPreprocessModel`, and `OnPreprocessAudio` with label-based exemptions.
- `AssetComplianceAuditor`, `ImportAuditExporter` (Markdown and JSON), and optional `ImportAuditBuildGuard`.
- UI Toolkit `AssetImportRulesWindow` with Policy & Presets, Dry-Run Compliance Audit, and Path Rule Tester tabs.
- Extensible `IAssetImportRule` interface with `TypeCache` discovery, custom rule sample, and NUnit EditMode tests.
