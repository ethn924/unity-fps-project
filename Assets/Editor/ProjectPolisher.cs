using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace FPS.EditorTools
{
    /// Améliorations graphiques et optimisations de performance.
    /// Menus : Tools > Shooting Range > Polish Graphics et Tools > Optimize > ...
    public static class ProjectPolisher
    {
        const string PipelinePath = "Assets/Settings/PC_RPAsset.asset";
        const string RendererPath = "Assets/Settings/PC_Renderer.asset";
        const string ProfilePath = "Assets/Scenes/SampleScene/Global Volume Profile.asset";

        [MenuItem("Tools/Shooting Range/Polish Graphics")]
        public static void PolishGraphics()
        {
            UsePcPipelineEverywhere();
            TuneAmbientOcclusion();
            TuneVolumeProfile();
            AssetDatabase.SaveAssets();
            Debug.Log("[Polish] Graphismes ajustés : pipeline PC (Forward+, SSAO), couleurs, bloom doux, vignette légère.");
        }

        // Le projet utilisait les pipelines par défaut (URPDefaultResources) : ombres à 200 m,
        // shadowmaps 4096, pas de SSAO. Le pipeline PC du template est mieux réglé :
        // Forward+, SSAO, ombres 50 m, GPU Resident Drawer. On l'assigne à tous les niveaux.
        static void UsePcPipelineEverywhere()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null) { Debug.LogError("[Polish] PC_RPAsset introuvable."); return; }

            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
                QualitySettings.streamingMipmapsActive = true; // grosses textures chargées progressivement
            }
            QualitySettings.SetQualityLevel(current, false);

            // La copie opaque de l'écran n'est utilisée par aucun shader de la scène : coût inutile.
            // Biais d'ombre : les valeurs du template (0.1/0.5) provoquent de l'acné d'ombre
            // (stries/taches) sur les murs éclairés en rasant — valeurs par défaut URP rétablies.
            // GPU Resident Drawer : source connue d'objets qui scintillent/permutent
            // sur certaines configs — coupé, le SRP Batcher suffit largement ici.
            var so = new SerializedObject(pipeline);
            so.FindProperty("m_RequireOpaqueTexture").boolValue = false;
            so.FindProperty("m_ShadowDepthBias").floatValue = 1f;
            so.FindProperty("m_ShadowNormalBias").floatValue = 1f;
            so.FindProperty("m_GPUResidentDrawerMode").intValue = 0;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(pipeline);
        }

        // SSAO désactivé : son bruit peint des taches mouvantes sur les grandes surfaces
        // unies (murs du stand) — l'effet cause plus de dégâts visuels qu'il n'apporte.
        static void TuneAmbientOcclusion()
        {
            var feature = AssetDatabase.LoadAllAssetsAtPath(RendererPath)
                .OfType<ScriptableObject>()
                .FirstOrDefault(o => o != null && o.GetType().Name == "ScreenSpaceAmbientOcclusion");
            if (feature == null) { Debug.LogWarning("[Polish] SSAO introuvable sur PC_Renderer."); return; }

            var so = new SerializedObject(feature);
            so.FindProperty("m_Active").boolValue = false;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(feature);
        }

        // Réglages sobres : l'objectif est un rendu plus riche sans agresser les yeux
        static void TuneVolumeProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null) { Debug.LogWarning("[Polish] Volume profile de la scène introuvable."); return; }

            // Un peu plus de contraste, saturation quasi neutre : trop de saturation
            // fait ressortir les taches vertes/brunes du béton (effet "feuillage")
            if (profile.TryGet(out ColorAdjustments color))
            {
                color.contrast.Override(6f);
                color.saturation.Override(2f);
            }

            // Halo doux limité aux surfaces vraiment lumineuses (réglettes du plafond)
            if (profile.TryGet(out Bloom bloom))
            {
                bloom.threshold.Override(1f);
                bloom.intensity.Override(0.3f);
                bloom.scatter.Override(0.6f);
            }

            // Vignette légère : concentre le regard sans assombrir les bords
            if (profile.TryGet(out Vignette vignette))
                vignette.intensity.Override(0.16f);

            // Balance des blancs légèrement chaude : ambiance intérieure
            if (!profile.TryGet(out WhiteBalance whiteBalance))
            {
                whiteBalance = profile.Add<WhiteBalance>(false);
                AssetDatabase.AddObjectToAsset(whiteBalance, profile);
            }
            whiteBalance.temperature.Override(6f);

            foreach (var component in profile.components) EditorUtility.SetDirty(component);
            EditorUtility.SetDirty(profile);
        }

        // Habille le compteur de munitions : panneau sombre arrondi + liseré doré,
        // typographie centrée. Idempotent : relancer réutilise le panneau existant.
        [MenuItem("Tools/Shooting Range/Polish HUD")]
        public static void PolishHud()
        {
            var manager = Object.FindAnyObjectByType<AmmoManager>();
            var tmp = manager != null ? manager.ammoDisplay : null;
            if (tmp == null) { Debug.LogError("[HUD] AmmoManager.ammoDisplay introuvable dans la scène."); return; }

            var canvas = tmp.GetComponentInParent<Canvas>();
            Undo.SetCurrentGroupName("Polish HUD");

            // Panneau de fond, ancré en bas à droite
            var existing = canvas.transform.Find("AmmoPanel");
            GameObject panelGo;
            if (existing != null) panelGo = existing.gameObject;
            else
            {
                panelGo = new GameObject("AmmoPanel", typeof(RectTransform), typeof(Image));
                Undo.RegisterCreatedObjectUndo(panelGo, "HUD");
                panelGo.transform.SetParent(canvas.transform, false);
            }
            var rt = (RectTransform)panelGo.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-28f, 28f);
            rt.sizeDelta = new Vector2(190f, 72f);

            var img = panelGo.GetComponent<Image>();
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.type = Image.Type.Sliced;
            img.color = new Color(0.05f, 0.06f, 0.08f, 0.62f);

            // Liseré doré en bas du panneau (rappel de la signalétique du stand)
            var accentTr = rt.Find("AmmoAccent");
            GameObject accentGo;
            if (accentTr != null) accentGo = accentTr.gameObject;
            else
            {
                accentGo = new GameObject("AmmoAccent", typeof(RectTransform), typeof(Image));
                Undo.RegisterCreatedObjectUndo(accentGo, "HUD");
                accentGo.transform.SetParent(rt, false);
            }
            var art = (RectTransform)accentGo.transform;
            art.anchorMin = new Vector2(0f, 0f);
            art.anchorMax = new Vector2(1f, 0f);
            art.pivot = new Vector2(0.5f, 0f);
            art.anchoredPosition = new Vector2(0f, 4f);
            art.sizeDelta = new Vector2(-24f, 3f);
            accentGo.GetComponent<Image>().color = new Color(0.85f, 0.68f, 0.25f, 0.9f);

            // Texte déplacé dans le panneau, centré, typo affirmée
            Undo.RecordObject(tmp.rectTransform, "HUD");
            Undo.RecordObject(tmp, "HUD");
            tmp.rectTransform.SetParent(rt, false);
            tmp.rectTransform.anchorMin = Vector2.zero;
            tmp.rectTransform.anchorMax = Vector2.one;
            tmp.rectTransform.offsetMin = new Vector2(10f, 6f);
            tmp.rectTransform.offsetMax = new Vector2(-10f, -6f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.margin = Vector4.zero;               // marges résiduelles = texte décentré
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.fontSize = 42f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.enableAutoSizing = false;
            tmp.color = Color.white;
            // Aperçu hors Play : Weapon.UpdateHUD écrase ce texte en jeu
            tmp.text = "8<size=55%><color=#9A9A9A> / 32</color></size>";

            EditorSceneManager.MarkSceneDirty(panelGo.scene);
            Debug.Log("[HUD] Compteur de munitions habillé (panneau + typo).");
        }

        // Plafonne les textures des packs tiers à 1024 px : moins de VRAM, éditeur plus fluide.
        // Le pack Infima (arme vue de très près) est exclu pour garder sa netteté.
        [MenuItem("Tools/Optimize/Compress ThirdParty Textures (max 1024)")]
        public static void CompressThirdPartyTextures()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/ThirdParty" });
            int changed = 0;
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.Contains("Infima")) continue;
                    // Textures CC0 du stand : vues de près sur de grandes surfaces, on garde le 2K
                    if (path.Contains("ThirdParty/Textures")) continue;

                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null) continue;

                    bool dirty = false;
                    if (importer.maxTextureSize > 1024) { importer.maxTextureSize = 1024; dirty = true; }
                    if (importer.mipmapEnabled && !importer.streamingMipmaps) { importer.streamingMipmaps = true; dirty = true; }
                    if (importer.textureCompression == TextureImporterCompression.Uncompressed)
                    {
                        importer.textureCompression = TextureImporterCompression.Compressed;
                        dirty = true;
                    }

                    if (dirty) { importer.SaveAndReimport(); changed++; }
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }

            Debug.Log($"[Optimize] {changed} texture(s) plafonnée(s) à 1024 px avec streaming mipmaps. " +
                      "Le réimport peut prendre quelques minutes la première fois.");
        }

        // Rend leur résolution 2K aux textures CC0 du stand (murs/sol vus de près),
        // si un passage de compression précédent les avait plafonnées à 1024.
        [MenuItem("Tools/Optimize/Restore Range Textures (2048)")]
        public static void RestoreRangeTextures()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/ThirdParty/Textures" });
            int changed = 0;
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("_Unused")) continue;
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                // 2048 px + format BC7 : dégradés bien plus propres que le DXT par défaut
                var pc = importer.GetPlatformTextureSettings("Standalone");
                bool dirty = importer.maxTextureSize < 2048 || !pc.overridden || pc.format != TextureImporterFormat.BC7;
                if (!dirty) continue;

                importer.maxTextureSize = 2048;
                pc.overridden = true;
                pc.maxTextureSize = 2048;
                pc.format = TextureImporterFormat.BC7;
                importer.SetPlatformTextureSettings(pc);
                importer.SaveAndReimport();
                changed++;
            }
            Debug.Log($"[Optimize] {changed} texture(s) du stand en 2048 px + BC7.");
        }
    }
}
