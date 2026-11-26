using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Ukubola.Player;
using Ukubola.Rendering;
using Setup = Ukubola.EditorTools.UkubolaRetroSetup;

namespace Ukubola.EditorTools
{
    public static class UkubolaRetroValidator
    {
        [MenuItem("Tools/UKUBOLA/Phase 02/Validate Retro Lab", false, 32)]
        public static void Validate()
        {
            if (!Setup.CanRun()) return;
            try
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                CheckScene(Setup.OpenInternal());
                Debug.Log("[UKUBOLA P02] VALIDATION PASSED | 0 errors. Play checks still required.");
            }
            catch (Exception ex) { Debug.LogError("[UKUBOLA P02] VALIDATION FAILED: " + ex.Message); }
        }

        internal static void CheckScene(Scene scene)
        {
            var errors = new List<string>();
            void Check(bool ok, string message) { if (!ok) errors.Add(message); }
            Check(scene.IsValid() && scene.isLoaded && scene.path == Setup.LabPath, "Invalid RetroLab scene.");
            var displays = Setup.Find<RetroDisplay>(scene);
            var players = Setup.Find<PlayerView>(scene);
            Check(displays.Length == 1 && displays[0].isActiveAndEnabled, "Exactly one active RetroDisplay required.");
            Check(players.Length == 1 && players[0].isActiveAndEnabled, "Exactly one active PlayerView required.");
            Check(Setup.Find<PlayerMotor>(scene).Count(m => m.isActiveAndEnabled) == 1, "Exactly one active PlayerMotor required.");
            Check(Setup.Find<PlayerInputReader>(scene).Count(m => m.isActiveAndEnabled) == 1, "Exactly one active PlayerInputReader required.");
            Check(Setup.Find<CharacterController>(scene).Count(c => c.enabled && c.gameObject.activeInHierarchy) == 1, "Exactly one active CharacterController required.");
            var cameras = Setup.Find<Camera>(scene).Where(c => c.isActiveAndEnabled).ToArray();
            Check(cameras.Length == 2, "Exactly two active cameras required (world and output).");
            Check(cameras.Count(c => c.CompareTag("MainCamera")) == 1, "Only the world camera may be MainCamera.");
            var listeners = Setup.Find<AudioListener>(scene).Where(a => a.isActiveAndEnabled).ToArray();
            Check(listeners.Length == 1, "Exactly one active AudioListener required.");
            Camera world = null;
            if (displays.Length == 1)
            {
                var d = displays[0]; world = d.WorldCamera; var output = d.OutputCamera; var target = d.Target;
                Check(world && output && world != output && cameras.Contains(world) && cameras.Contains(output), "Display cameras missing, inactive or identical.");
                Check(players.Length == 1 && world && players[0].PlayerCamera == world, "Retro display must use PlayerView camera.");
                Check(target && AssetDatabase.GetAssetPath(target) == Setup.TargetPath && target.width == 320 && target.height == 200 && target.depth >= 16 && target.filterMode == FilterMode.Point && target.wrapMode == TextureWrapMode.Clamp && target.antiAliasing == 1 && !target.useMipMap && !target.useDynamicScale, "RenderTexture must be the 320x200 point-filtered asset with depth, no MSAA or dynamic scaling.");
                if (world)
                {
                    Check(world.targetTexture == target && target, "World camera target is incorrect.");
                    Check(world.CompareTag("MainCamera") && listeners.Length == 1 && listeners[0].gameObject == world.gameObject, "Keep MainCamera and AudioListener on player camera.");
                    Check((world.cullingMask & (1 << LayerMask.NameToLayer("World"))) != 0 && (world.cullingMask & 1) != 0, "World camera must see World and Default layers.");
                    CheckCamera(world, errors);
                }
                if (output)
                {
                    Check(output.targetTexture == null && output.cullingMask == 0 && !output.CompareTag("MainCamera") && output.targetDisplay == 0 && output.clearFlags == CameraClearFlags.SolidColor && output.backgroundColor == Color.black, "Output camera must clear screen black and render no scene geometry.");
                    CheckCamera(output, errors);
                }
                Check(d.Canvas && d.Canvas.isActiveAndEnabled && d.Canvas.renderMode == RenderMode.ScreenSpaceOverlay && d.Canvas.targetDisplay == 0, "Missing overlay Canvas.");
                Check(d.Image && d.Image.isActiveAndEnabled && d.Canvas && d.Image.transform.parent == d.Canvas.transform && d.Image.texture == target && !d.Image.raycastTarget, "Missing display RawImage or wrong texture/raycast settings.");
            }
            var billboards = Setup.Find<Billboard>(scene);
            Check(billboards.Length == 2 && billboards.All(b => b.isActiveAndEnabled && world && b.Viewer == world && b.transform.parent != null && b.GetComponent<SpriteRenderer>() != null), "Two billboard visual children must reference the player camera.");
            var directional = Setup.Find<DirectionalSprite>(scene);
            Check(directional.Length == 1, "One directional sprite demo required.");
            if (directional.Length == 1)
            {
                var d = directional[0];
                Check(d.isActiveAndEnabled && d.Actor && d.Actor == d.transform.parent && d.Viewer == world && d.Renderer == d.GetComponent<SpriteRenderer>() && d.GetComponent<Billboard>(), "Directional sprite references are inconsistent.");
                Check(d.Frames != null && d.Frames.Length == 8 && d.Frames.All(f => f) && d.Frames.Distinct().Count() == 8, "Expected eight distinct sprite assets.");
                if (d.Frames != null && d.Frames.Length == 8)
                    for (int i = 0; i < 8; i++) Check(d.Frames[i] && AssetDatabase.GetAssetPath(d.Frames[i]) == Setup.SpriteDir + "Dummy_" + i + ".png", "Incorrect directional frame order at " + i);
            }
            var spriteRenderers = Setup.Find<SpriteRenderer>(scene);
            Check(spriteRenderers.Length == 2, "Expected two sprite renderers.");
            foreach (var sr in spriteRenderers)
                Check(sr.sprite && sr.sharedMaterial && sr.sharedMaterial.shader && sr.sharedMaterial.shader.name == "UKUBOLA/SpriteCutout" && sr.sharedMaterial.shader.isSupported && !ShaderUtil.ShaderHasError(sr.sharedMaterial.shader), "Sprite material or sprite missing: " + sr.name);
            foreach (string name in new[] { "Wall", "Floor", "Accent" })
                CheckTexture(Setup.TextureDir + name + ".png", false, errors);
            for (int i = 0; i < 8; i++) CheckTexture(Setup.SpriteDir + "Dummy_" + i + ".png", true, errors);
            CheckTexture(Setup.SpriteDir + "Beacon.png", true, errors);
            var renderers = Setup.Find<MeshRenderer>(scene).Where(r => r.gameObject.activeInHierarchy).ToArray();
            Check(renderers.Length >= 10, "Original arena geometry missing.");
            foreach (var r in renderers)
                Check(r.sharedMaterial && r.sharedMaterial.shader && r.sharedMaterial.shader.name == "Universal Render Pipeline/Unlit" && r.sharedMaterial.GetTexture("_BaseMap") && AssetDatabase.GetAssetPath(r.sharedMaterial).StartsWith(Setup.Generated + "/", StringComparison.Ordinal), "Missing retro material: " + r.name);
            var contexts = Setup.Find<LevelContext>(scene);
            Check(contexts.Length == 1 && contexts[0].LevelId == "RETRO_LAB", "LevelContext ID is incorrect.");
            if (contexts.Length == 1) errors.AddRange(contexts[0].RebuildRegistry());
            Check(Setup.Find<GameSession>(scene).Count(s => s.isActiveAndEnabled) == 1, "Missing GameSession.");
            foreach (var t in Setup.Find<Transform>(scene))
                Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0, "Missing script: " + t.name);
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
        }
        private static void CheckCamera(Camera camera, List<string> errors)
        {
            var data = camera.GetComponent<UniversalAdditionalCameraData>();
            if (camera.allowHDR || camera.allowMSAA || camera.allowDynamicResolution || !data || data.renderType != CameraRenderType.Base || data.renderPostProcessing || data.antialiasing != AntialiasingMode.None)
                errors.Add("Camera render settings invalid: " + camera.name);
        }
        private static void CheckTexture(string path, bool sprite, List<string> errors)
        {
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var i = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!t || !i || t.width != 32 || t.height != (sprite ? 48 : 32) || t.filterMode != FilterMode.Point || i.textureCompression != TextureImporterCompression.Uncompressed || i.mipmapEnabled == sprite || i.wrapMode != (sprite ? TextureWrapMode.Clamp : TextureWrapMode.Repeat) || i.textureType != (sprite ? TextureImporterType.Sprite : TextureImporterType.Default))
            { errors.Add("Texture import settings invalid: " + path); return; }
            if (sprite)
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (!s || Mathf.Abs(s.pixelsPerUnit - 32f) > .01f || (s.pivot - new Vector2(16, 0)).sqrMagnitude > .01f)
                    errors.Add("Sprite must have bottom-center pivot and 32 PPU: " + path);
            }
        }
    }
}
