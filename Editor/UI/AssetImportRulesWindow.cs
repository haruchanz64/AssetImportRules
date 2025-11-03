using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AssetImportRules.Editor.Core;
using AssetImportRules.Editor.Pipeline;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AssetImportRules.Editor.UI
{
    /// <summary>
    /// UI Toolkit workbench for configuring AssetImportPolicyAsset rules (Sprites, UI, Textures,
    /// 3D Models, Audio), running dry-run compliance audits with property diffs, batch re-importing,
    /// and testing glob/folder path patterns.
    /// </summary>
    public sealed class AssetImportRulesWindow : EditorWindow
    {
        private AssetImportPolicyAsset _policy;
        private ImportComplianceReport _lastReport;
        private int _activeTab;

        private Button[] _tabButtons;
        private ScrollView _body;
        private Label _statusLabel;

        // Audit tab state
        private TextField _rootFolderField;
        private DropdownField _categoryFilter;
        private VisualElement _auditKpiRow;
        private VisualElement _auditResultsContainer;

        // Matcher tab state
        private TextField _testPathField;
        private VisualElement _testPathOutput;

        [MenuItem("Tools/AssetImportRules/Import Policy Workbench", false, 10)]
        public static void ShowWindow()
        {
            var window = GetWindow<AssetImportRulesWindow>("AssetImportRules");
            window.minSize = new Vector2(380f, 340f);
            window.Show();
        }

        public void CreateGUI()
        {
            _policy = AssetComplianceAuditor.GetActivePolicy();

            rootVisualElement.Clear();
            rootVisualElement.AddToClassList("air-root");
            AttachStyles(rootVisualElement);

            // Header
            var header = new VisualElement();
            header.AddToClassList("air-header");
            var title = new Label("AssetImportRules — Import Policy & Compliance Workbench");
            title.AddToClassList("air-title");
            header.Add(title);
            header.Add(new Button(RunDryRun) { text = "Run Dry-Run Audit" });
            rootVisualElement.Add(header);

            // Tabs
            var tabs = new VisualElement();
            tabs.AddToClassList("air-tabs");
            _tabButtons = new[]
            {
                CreateTabButton("Policy & Presets", 0),
                CreateTabButton("Compliance Audit", 1),
                CreateTabButton("Path Rule Tester", 2)
            };
            for (int i = 0; i < _tabButtons.Length; i++) tabs.Add(_tabButtons[i]);
            rootVisualElement.Add(tabs);

            // Body
            _body = new ScrollView(ScrollViewMode.Vertical);
            _body.AddToClassList("air-body");
            rootVisualElement.Add(_body);

            // Status bar
            _statusLabel = new Label(_policy != null ? $"Active policy: {_policy.name}" : "No AssetImportPolicyAsset selected. Create or assign one in Policy & Presets.");
            _statusLabel.AddToClassList("air-status");
            rootVisualElement.Add(_statusLabel);

            SelectTab(0);
        }

        private Button CreateTabButton(string label, int index)
        {
            var btn = new Button(() => SelectTab(index)) { text = label };
            btn.AddToClassList("air-tab-btn");
            return btn;
        }

        private void SelectTab(int index)
        {
            _activeTab = index;
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                _tabButtons[i].EnableInClassList("air-tab-btn--active", i == index);
            }

            _body.Clear();
            switch (index)
            {
                case 0: BuildPolicyTab(); break;
                case 1: BuildAuditTab(); break;
                case 2: BuildPathTesterTab(); break;
            }
        }

        private void BuildPolicyTab()
        {
            var policyCard = new VisualElement();
            policyCard.AddToClassList("air-card");
            policyCard.Add(MakeSectionTitle("Active Import Policy"));

            var policyField = new ObjectField("Policy Asset")
            {
                objectType = typeof(AssetImportPolicyAsset),
                value = _policy
            };
            policyField.RegisterValueChangedCallback(evt =>
            {
                _policy = evt.newValue as AssetImportPolicyAsset;
                AssetComplianceAuditor.SetActivePolicy(_policy);
                SetStatus(_policy != null ? $"Active policy set to '{_policy.name}'." : "Cleared active policy.");
                SelectTab(0);
            });
            policyCard.Add(policyField);

            var presetRow = new VisualElement();
            presetRow.AddToClassList("air-row");
            presetRow.Add(new Button(() => CreateStarterPolicy("Mobile2D_UI", p => p.ApplyMobile2DAndUiPreset())) { text = "Create Mobile 2D/UI/Audio Policy" });
            presetRow.Add(new Button(() => CreateStarterPolicy("PC3D_URP", p => p.ApplyPc3DUrpPreset())) { text = "Create PC 3D URP Policy" });
            presetRow.Add(new Button(() => CreateStarterPolicy("PixelArt", p => p.ApplyPixelArtPreset())) { text = "Create Pixel Art Policy" });
            policyCard.Add(presetRow);

            // Pre-build guard toggles
            var preBuildToggle = new Toggle("Run Pre-Build Compliance Audit") { value = ImportAuditBuildGuard.Enabled };
            var failBuildToggle = new Toggle("Fail Build on Non-Compliant Assets") { value = ImportAuditBuildGuard.FailOnNonCompliant };
            failBuildToggle.SetEnabled(ImportAuditBuildGuard.Enabled);
            preBuildToggle.RegisterValueChangedCallback(evt =>
            {
                ImportAuditBuildGuard.Enabled = evt.newValue;
                failBuildToggle.SetEnabled(evt.newValue);
            });
            failBuildToggle.RegisterValueChangedCallback(evt => ImportAuditBuildGuard.FailOnNonCompliant = evt.newValue);
            policyCard.Add(preBuildToggle);
            policyCard.Add(failBuildToggle);
            _body.Add(policyCard);

            if (_policy != null)
            {
                var inspectorCard = new VisualElement();
                inspectorCard.AddToClassList("air-card");
                inspectorCard.Add(MakeSectionTitle($"Policy Rules ({_policy.textureRules.Count} Texture/UI, {_policy.modelRules.Count} Model, {_policy.audioRules.Count} Audio)"));
                var inspector = new InspectorElement(_policy);
                inspectorCard.Add(inspector);
                _body.Add(inspectorCard);
            }

            // Custom rules discovered via TypeCache
            var customCard = new VisualElement();
            customCard.AddToClassList("air-card");
            var customRules = AssetComplianceAuditor.DiscoverCustomRules();
            customCard.Add(MakeSectionTitle($"Discovered Custom Rules (IAssetImportRule: {customRules.Count})"));
            if (customRules.Count == 0)
            {
                var empty = new Label("No custom IAssetImportRule classes found. Implement IAssetImportRule in any Editor assembly to extend checks.");
                empty.AddToClassList("air-muted");
                customCard.Add(empty);
            }
            else
            {
                for (int i = 0; i < customRules.Count; i++)
                {
                    var r = customRules[i];
                    var lbl = new Label($"[{r.Priority}] {r.RuleId} — {r.DisplayName}");
                    lbl.AddToClassList("air-mono");
                    customCard.Add(lbl);
                }
            }
            _body.Add(customCard);
        }

        private void BuildAuditTab()
        {
            var ctrlCard = new VisualElement();
            ctrlCard.AddToClassList("air-card");
            ctrlCard.Add(MakeSectionTitle("Dry-Run Compliance Audit"));

            var row = new VisualElement();
            row.AddToClassList("air-row");
            _rootFolderField = new TextField("Root Folder") { value = "Assets" };
            _rootFolderField.style.flexGrow = 1f;
            _rootFolderField.style.minWidth = 180f;
            _categoryFilter = new DropdownField("Category", new List<string> { "All", "TextureOrSprite", "Model", "Audio", "Custom" }, 0);
            _categoryFilter.style.flexGrow = 1f;
            _categoryFilter.style.minWidth = 180f;
            _categoryFilter.RegisterValueChangedCallback(_ => RenderAuditResults());
            row.Add(_rootFolderField);
            row.Add(_categoryFilter);
            ctrlCard.Add(row);

            var btnRow = new VisualElement();
            btnRow.AddToClassList("air-row");
            btnRow.Add(new Button(RunDryRun) { text = "Scan Project (Dry Run)" });
            btnRow.Add(new Button(FixAllNonCompliant) { text = "Apply & Reimport All" });
            btnRow.Add(new Button(CopyAuditMarkdown) { text = "Copy Markdown" });
            btnRow.Add(new Button(ExportAuditJson) { text = "Export JSON..." });
            ctrlCard.Add(btnRow);
            _body.Add(ctrlCard);

            _auditKpiRow = new VisualElement();
            _auditKpiRow.AddToClassList("air-kpi-row");
            _body.Add(_auditKpiRow);

            _auditResultsContainer = new VisualElement();
            _body.Add(_auditResultsContainer);

            RenderAuditResults();
        }

        private void BuildPathTesterTab()
        {
            var card = new VisualElement();
            card.AddToClassList("air-card");
            card.Add(MakeSectionTitle("Path Pattern & Rule Match Tester"));

            _testPathField = new TextField("Sample Asset Path") { value = "Assets/UI/Icons/play_button.png" };
            _testPathField.RegisterValueChangedCallback(_ => EvaluateTestPath());
            card.Add(_testPathField);

            var quickRow = new VisualElement();
            quickRow.AddToClassList("air-row");
            quickRow.Add(new Button(() => _testPathField.value = "Assets/UI/HUD/health_bar.png") { text = "UI PNG" });
            quickRow.Add(new Button(() => _testPathField.value = "Assets/Art/Sprites/hero_sheet.png") { text = "Sprite PNG" });
            quickRow.Add(new Button(() => _testPathField.value = "Assets/Art/Models/crate.fbx") { text = "3D FBX" });
            quickRow.Add(new Button(() => _testPathField.value = "Assets/Audio/SFX/hit_01.wav") { text = "SFX WAV" });
            quickRow.Add(new Button(() => _testPathField.value = "Assets/Audio/Music/theme.ogg") { text = "Music OGG" });
            card.Add(quickRow);

            _testPathOutput = new VisualElement();
            card.Add(_testPathOutput);
            _body.Add(card);

            EvaluateTestPath();
        }

        private void CreateStarterPolicy(string suffix, Action<AssetImportPolicyAsset> configure)
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create AssetImportPolicyAsset",
                $"AssetImportPolicy_{suffix}",
                "asset",
                "Choose a location inside Assets to store the import policy.");
            if (string.IsNullOrEmpty(path)) return;

            var asset = CreateInstance<AssetImportPolicyAsset>();
            configure(asset);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            _policy = asset;
            AssetComplianceAuditor.SetActivePolicy(_policy);
            EditorGUIUtility.PingObject(asset);
            SetStatus($"Created starter policy at {path}");
            SelectTab(0);
        }

        private void RunDryRun()
        {
            if (_policy == null)
            {
                SetStatus("Assign or create an AssetImportPolicyAsset first.");
                return;
            }

            string folder = _rootFolderField != null ? _rootFolderField.value : "Assets";
            _lastReport = AssetComplianceAuditor.RunDryRunAudit(_policy, folder);
            SetStatus($"Dry run complete: {_lastReport.complianceScore}/100 ({_lastReport.nonCompliantAssets} non-compliant out of {_lastReport.matchedAssets} matched).");
            if (_activeTab != 1) SelectTab(1);
            else RenderAuditResults();
        }

        private void RenderAuditResults()
        {
            if (_auditKpiRow == null || _auditResultsContainer == null) return;
            _auditKpiRow.Clear();
            _auditResultsContainer.Clear();

            if (_lastReport == null)
            {
                var hint = new Label("Click 'Scan Project (Dry Run)' to inspect Textures, Sprites, 3D Models, and Audio clips against the active policy.");
                hint.AddToClassList("air-muted");
                _auditResultsContainer.Add(hint);
                return;
            }

            AddKpi(_auditKpiRow, "Compliance Score", $"{_lastReport.complianceScore} / 100");
            AddKpi(_auditKpiRow, "Scanned", _lastReport.totalScannedAssets.ToString());
            AddKpi(_auditKpiRow, "Matched Rules", _lastReport.matchedAssets.ToString());
            AddKpi(_auditKpiRow, "Non-Compliant", _lastReport.nonCompliantAssets.ToString());
            AddKpi(_auditKpiRow, "Exempt", _lastReport.exemptedAssets.ToString());

            string filter = _categoryFilter != null ? _categoryFilter.value : "All";
            int shown = 0;

            for (int i = 0; i < _lastReport.nonCompliantItems.Count; i++)
            {
                var item = _lastReport.nonCompliantItems[i];
                if (filter != "All" && !string.Equals(item.category.ToString(), filter, StringComparison.Ordinal))
                {
                    continue;
                }
                shown++;

                var box = new VisualElement();
                box.AddToClassList("air-diff-item");

                var topRow = new VisualElement();
                topRow.AddToClassList("air-row");
                var pathLbl = new Label($"{item.assetPath}  [{item.category}]  ({item.matchedRuleNames})");
                pathLbl.AddToClassList("air-mono");
                pathLbl.style.flexGrow = 1f;

                string capturedPath = item.assetPath;
                var pingBtn = new Button(() =>
                {
                    var obj = AssetDatabase.LoadMainAssetAtPath(capturedPath);
                    if (obj != null)
                    {
                        Selection.activeObject = obj;
                        EditorGUIUtility.PingObject(obj);
                    }
                }) { text = "Ping" };

                var fixBtn = new Button(() =>
                {
                    AssetComplianceAuditor.ApplyPolicyToAssetPaths(_policy, new[] { capturedPath });
                    RunDryRun();
                }) { text = "Fix" };

                topRow.Add(pathLbl);
                topRow.Add(pingBtn);
                topRow.Add(fixBtn);
                box.Add(topRow);

                for (int d = 0; d < item.diffs.Count; d++)
                {
                    var diff = item.diffs[d];
                    var diffLbl = new Label($"  • {diff.propertyName}: {diff.currentValue}  ->  {diff.expectedValue}  ({diff.ruleName})");
                    diffLbl.AddToClassList("air-muted");
                    box.Add(diffLbl);
                }

                _auditResultsContainer.Add(box);
            }

            if (shown == 0)
            {
                var ok = new Label("All matched assets in this filter comply with the active import policy.");
                ok.AddToClassList("air-mono");
                _auditResultsContainer.Add(ok);
            }
        }

        private void FixAllNonCompliant()
        {
            if (_policy == null || _lastReport == null || _lastReport.nonCompliantItems.Count == 0)
            {
                SetStatus("Nothing to fix. Run a dry-run audit first.");
                return;
            }

            int updated = AssetComplianceAuditor.ApplyPolicyToAssetPaths(
                _policy,
                _lastReport.nonCompliantItems.Select(x => x.assetPath));
            SetStatus($"Applied import rules and re-imported {updated} asset(s).");
            RunDryRun();
        }

        private void EvaluateTestPath()
        {
            if (_testPathOutput == null) return;
            _testPathOutput.Clear();

            if (_policy == null)
            {
                _testPathOutput.Add(new Label("No active policy selected."));
                return;
            }

            string samplePath = _testPathField.value;
            if (AssetComplianceAuditor.IsAssetExempt(samplePath, _policy))
            {
                var exempt = new Label($"EXEMPT: '{samplePath}' matches a global ignore pattern or carries the exempt label.");
                exempt.AddToClassList("air-mono");
                _testPathOutput.Add(exempt);
                return;
            }

            var tex = AssetComplianceAuditor.MatchTextureRules(samplePath, _policy);
            var mod = AssetComplianceAuditor.MatchModelRules(samplePath, _policy);
            var aud = AssetComplianceAuditor.MatchAudioRules(samplePath, _policy);

            if (tex.Count == 0 && mod.Count == 0 && aud.Count == 0)
            {
                var none = new Label($"No built-in Texture, Model, or Audio rule matches '{samplePath}'.");
                none.AddToClassList("air-muted");
                _testPathOutput.Add(none);
                return;
            }

            foreach (var r in tex) _testPathOutput.Add(new Label($"[Texture/Sprite] {r.ruleName} (priority {r.priority}, pattern '{r.pathPattern}')"));
            foreach (var r in mod) _testPathOutput.Add(new Label($"[3D Model] {r.ruleName} (priority {r.priority}, pattern '{r.pathPattern}')"));
            foreach (var r in aud) _testPathOutput.Add(new Label($"[Audio] {r.ruleName} (priority {r.priority}, pattern '{r.pathPattern}')"));
        }

        private void CopyAuditMarkdown()
        {
            if (_lastReport == null) return;
            EditorGUIUtility.systemCopyBuffer = ImportAuditExporter.ToMarkdown(_lastReport);
            SetStatus("Copied Markdown compliance report to clipboard.");
        }

        private void ExportAuditJson()
        {
            if (_lastReport == null) return;
            string path = EditorUtility.SaveFilePanel("Export Compliance Report JSON", Application.dataPath, "import_compliance_report", "json");
            if (string.IsNullOrEmpty(path)) return;
            File.WriteAllText(path, ImportAuditExporter.ToJson(_lastReport, true));
            SetStatus($"Exported JSON report to {path}");
        }

        private void SetStatus(string message)
        {
            if (_statusLabel != null) _statusLabel.text = message;
        }

        private static Label MakeSectionTitle(string text)
        {
            var lbl = new Label(text);
            lbl.AddToClassList("air-section-title");
            return lbl;
        }

        private static void AddKpi(VisualElement parent, string label, string value)
        {
            var box = new VisualElement();
            box.AddToClassList("air-kpi");
            var l = new Label(label);
            l.AddToClassList("air-kpi-label");
            var v = new Label(value);
            v.AddToClassList("air-kpi-value");
            box.Add(l);
            box.Add(v);
            parent.Add(box);
        }

        private static void AttachStyles(VisualElement target)
        {
            const string pkgPath = "Packages/com.haruchanz64.asset-import-rules/Editor/UI/AssetImportRulesStyles.uss";
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(pkgPath);
            if (sheet == null)
            {
                string[] guids = AssetDatabase.FindAssets("AssetImportRulesStyles t:StyleSheet");
                if (guids.Length > 0)
                {
                    sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }
            if (sheet != null) target.styleSheets.Add(sheet);
        }
    }
}
