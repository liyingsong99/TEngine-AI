using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HtmlToUGUI
{
    /// <summary>项目内可复用的 UGUI/TMP 烘焙入口；只在隔离场景中生成，不保存或修改用户场景。</summary>
    public static class HtmlToUGUIBakeApi
    {
        public const string DefaultConfigPath = "Assets/TEngine/Extension/HtmlToUGUI/HtmlToUGUIConfig.asset";

        [Serializable]
        public sealed class BakeResult
        {
            public string prefabPath;
            public string guid;
            public int nodeCount;
            public int generatedObjectCount;
            public int imageCount;
            public bool previewOnly;
            public bool overwritesExisting;
            public Vector2 referenceResolution;
        }

        /// <param name="canvasPathOrName">可选的已加载场景 Canvas 名称/层级路径，仅作为尺寸参考；空值创建临时 Canvas。</param>
        /// <param name="outputPrefabPath">Assets 下现有目录中的 .prefab 路径。</param>
        /// <param name="previewOnly">仅校验输入并返回写入范围，不创建资源。</param>
        /// <param name="overwrite">覆盖已有 Prefab 必须显式开启；保存会保留该资产 GUID。</param>
        public static BakeResult Bake(string json, string canvasPathOrName, string outputPrefabPath,
            string configPath = DefaultConfigPath, bool previewOnly = false, bool overwrite = false,
            bool legacyText = false, Font legacyFont = null)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("请在 Editor 空闲的 Edit Mode 烘焙。");
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("JSON 不能为空。", nameof(json));
            if (string.IsNullOrWhiteSpace(outputPrefabPath) || !outputPrefabPath.StartsWith("Assets/", StringComparison.Ordinal) ||
                outputPrefabPath.Contains("..") || outputPrefabPath.Contains('\\') || !outputPrefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("输出必须为 Assets 下的规范 .prefab 路径。", nameof(outputPrefabPath));
            string folder = Path.GetDirectoryName(outputPrefabPath).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(folder)) throw new DirectoryNotFoundException(folder);
            var config = AssetDatabase.LoadAssetAtPath<HtmlToUGUIConfig>(configPath);
            if (config == null) throw new ArgumentException("找不到 HtmlToUGUIConfig：" + configPath);
            var data = JsonConvert.DeserializeObject<UIDataNode>(json);
            if (data == null || string.IsNullOrWhiteSpace(data.name) || data.width <= 0 || data.height <= 0)
                throw new ArgumentException("JSON 根节点必须命名且尺寸大于零。");
            if (legacyText && legacyFont == null)
                throw new ArgumentException("UGUI Text 模式须显式传入工程现有字体。", nameof(legacyFont));
            if (!legacyText && (TMP_Settings.instance == null || TMP_Settings.defaultFontAsset == null))
                throw new InvalidOperationException("缺少 TMP Settings 或默认字体；请先导入 UGUI 包自带的 TMP Essential Resources，不会自动降级为 Legacy Text。");

            var result = new BakeResult {
                prefabPath = outputPrefabPath,
                guid = AssetDatabase.AssetPathToGUID(outputPrefabPath),
                previewOnly = previewOnly,
                overwritesExisting = File.Exists(outputPrefabPath),
                referenceResolution = data.schemaVersion >= 2
                    ? new Vector2(data.designWidth > 0 ? data.designWidth : data.width, data.designHeight > 0 ? data.designHeight : data.height)
                    : config.supportedResolutions[0].resolution
            };
            ValidateNodes(data, result);
            Canvas source = null;
            if (!string.IsNullOrWhiteSpace(canvasPathOrName))
            {
                var matches = Resources.FindObjectsOfTypeAll<Canvas>().Where(c =>
                    !EditorUtility.IsPersistent(c) && c.gameObject.scene.IsValid() &&
                    !EditorSceneManager.IsPreviewScene(c.gameObject.scene) &&
                    (c.name == canvasPathOrName || HierarchyPath(c.transform) == canvasPathOrName)).ToArray();
                if (matches.Length != 1) throw new ArgumentException("Canvas 必须唯一匹配：" + canvasPathOrName);
                source = matches[0];
            }
            if (previewOnly) return result;
            if (result.overwritesExisting && !overwrite)
                throw new InvalidOperationException("目标已存在；请先预览并明确传入 overwrite:true。");

            Scene previewScene = EditorSceneManager.NewPreviewScene();
            HtmlToUGUIBaker baker = null;
            GameObject host = null;
            try
            {
                host = new GameObject("HtmlToUGUI_PreviewCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
                SceneManager.MoveGameObjectToScene(host, previewScene);
                var canvas = host.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                ((RectTransform)host.transform).sizeDelta = source != null ? ((RectTransform)source.transform).rect.size : result.referenceResolution;
                baker = ScriptableObject.CreateInstance<HtmlToUGUIBaker>();
                baker.ConfigureForApi(config, legacyText);
                var root = HtmlToUGUIBaker.GenerateTree(baker, data, canvas);
                int uiLayer = source != null ? source.gameObject.layer : LayerMask.NameToLayer("UI");
                foreach (var node in root.GetComponentsInChildren<Transform>(true)) node.gameObject.layer = uiLayer;
                if (legacyText)
                    foreach (var text in root.GetComponentsInChildren<Text>(true)) text.font = legacyFont;
                // 保存窗口根，临时宿主 Canvas 不写入 Prefab。TEngine 在宿主 UICanvas 下加载窗口。
                root.AddComponent<Canvas>();
                root.AddComponent<GraphicRaycaster>();
                result.generatedObjectCount = root.GetComponentsInChildren<Transform>(true).Length;
                var saved = PrefabUtility.SaveAsPrefabAsset(root, outputPrefabPath, out bool success);
                if (!success || saved == null) throw new IOException("Prefab 保存失败：" + outputPrefabPath);
                result.guid = AssetDatabase.AssetPathToGUID(outputPrefabPath);
                return result;
            }
            finally
            {
                if (baker != null) UnityEngine.Object.DestroyImmediate(baker);
                if (host != null) UnityEngine.Object.DestroyImmediate(host);
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        private static string HierarchyPath(Transform node)
        {
            string path = node.name;
            while (node.parent != null) { node = node.parent; path = node.name + "/" + path; }
            return path;
        }

        private static void ValidateNodes(UIDataNode node, BakeResult result)
        {
            if (node == null || string.IsNullOrWhiteSpace(node.name) || node.name.Contains('/') || string.IsNullOrWhiteSpace(node.type))
                throw new ArgumentException("节点缺少有效 name/type。");
            result.nodeCount++;
            // 自动化只接收已导入的本地 Sprite，不在预览之外隐式复制图片或修改 importer。
            string image = !string.IsNullOrWhiteSpace(node.imageSrc) ? node.imageSrc : node.backgroundImageSrc;
            if (!string.IsNullOrWhiteSpace(image))
            {
                if (!(image.StartsWith("Assets/", StringComparison.Ordinal) || image.StartsWith("Packages/", StringComparison.Ordinal)) ||
                    AssetDatabase.LoadAssetAtPath<Sprite>(image) == null)
                    throw new ArgumentException("图片须为已导入的本地 Sprite 资产路径：" + image);
                result.imageCount++;
            }
            if (node.children == null) return;
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var child in node.children)
            {
                ValidateNodes(child, result);
                if (!names.Add(child.name)) throw new ArgumentException("同级节点重名：" + child.name);
            }
        }
    }
}
