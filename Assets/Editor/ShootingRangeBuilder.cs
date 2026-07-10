using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FPS.EditorTools
{
    /// Construit un stand de tir intérieur complet dans la scène ouverte.
    /// Menu : Tools > Shooting Range > Build (Rebuild). Idempotent : relancer reconstruit proprement.
    /// Axe de tir = +X (les cibles existantes font face à l'ouest). 7 couloirs répartis le long de Z.
    public static class ShootingRangeBuilder
    {
        const string MatFolder = "Assets/Materials/Range";
        const string TexFolder = "Assets/ThirdParty/Textures";

        // Dimensions intérieures (unités monde)
        const float XWest = 5f, XEast = 19f;
        const float ZSouth = -14.6f, ZNorth = 0.4f;
        const float H = 3.6f;   // hauteur sous plafond
        const float T = 0.3f;   // épaisseur murs
        const int Lanes = 7;
        const float LaneZ0 = -2.65f;   // centre du couloir 1
        const float LaneStep = 1.7f;   // écart entre couloirs
        const float FiringX = 9.75f;   // centre des séparateurs de box

        static Transform root;
        static int groundLayer;

        [MenuItem("Tools/Shooting Range/Build (Rebuild)")]
        public static void Build()
        {
            var rootGo = GameObject.Find("ShootingRange");
            if (rootGo == null)
            {
                rootGo = new GameObject("ShootingRange");
                Undo.RegisterCreatedObjectUndo(rootGo, "Range");
            }
            root = rootGo.transform;

            var ground = GameObject.Find("Ground");
            groundLayer = ground ? ground.layer : 0;

            Undo.SetCurrentGroupName("Build Shooting Range");

            RemoveOldPieces();
            var m = BuildMaterials();
            BuildStructure(m);
            BuildBooths(m);
            BuildDownrange(m);
            BuildLighting(m);
            BuildSignage(m);
            BuildProps(m);
            PlaceTargets();
            WarnIfBlocked();

            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            EditorSceneManager.MarkSceneDirty(rootGo.scene);
            Selection.activeGameObject = rootGo;
            Debug.Log("Stand de tir construit. Ctrl+Z pour annuler, Ctrl+S pour sauvegarder la scène.");
        }

        [MenuItem("Tools/Shooting Range/Remove")]
        public static void Remove()
        {
            var rootGo = GameObject.Find("ShootingRange");
            if (rootGo == null) return;
            root = rootGo.transform;
            Undo.SetCurrentGroupName("Remove Shooting Range");
            RemoveOldPieces();
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            EditorSceneManager.MarkSceneDirty(rootGo.scene);
        }

        static void RemoveOldPieces()
        {
            var doomed = new List<GameObject>();
            foreach (Transform c in root)
                if (c.name.StartsWith("Range_") || c.name.StartsWith("ConcreteWeatheredMossy"))
                    doomed.Add(c.gameObject);
            foreach (var go in doomed)
                Undo.DestroyObjectImmediate(go);
        }

        // ------------------------------------------------------------------ matériaux

        class RangeMats
        {
            public Material Wall, Floor, Ceiling, Frame, Table, Acoustic, Rubber, Backstop, Red, Yellow, Emissive, SignBack;
        }

        static RangeMats BuildMaterials()
        {
            var m = new RangeMats
            {
                Wall     = EnsureMat("RangeWall",     new Color(0.84f, 0.83f, 0.80f), 0.15f, 0f,   texKeyword: "concrete", tiling: 2f),
                Floor    = EnsureMat("RangeFloor",    new Color(0.56f, 0.56f, 0.54f), 0.45f, 0f,   texKeyword: "floor",    tiling: 3f),
                Ceiling  = EnsureMat("RangeCeiling",  new Color(0.17f, 0.18f, 0.20f), 0.10f, 0f),
                Frame    = EnsureMat("RangeFrame",    new Color(0.13f, 0.14f, 0.16f), 0.50f, 0.7f),
                Table    = EnsureMat("RangeTable",    new Color(0.35f, 0.37f, 0.41f), 0.35f, 0.3f),
                Acoustic = EnsureMat("RangeAcoustic", new Color(0.28f, 0.30f, 0.34f), 0.05f, 0f,   texKeyword: "acoustic", tiling: 2f),
                Rubber   = EnsureMat("RangeRubber",   new Color(0.22f, 0.23f, 0.24f), 0.10f, 0f,   texKeyword: "rubber",   tiling: 3f),
                Backstop = EnsureMat("RangeBackstop", new Color(0.18f, 0.29f, 0.24f), 0.15f, 0f),
                Red      = EnsureMat("RangeRed",      new Color(0.76f, 0.20f, 0.18f), 0.30f, 0f),
                Yellow   = EnsureMat("RangeYellow",   new Color(0.91f, 0.73f, 0.24f), 0.30f, 0f),
                Emissive = EnsureMat("RangeEmissive", Color.white, 0.5f, 0f, emission: Color.white * 2.2f),
                SignBack = EnsureMat("RangeSignBack", new Color(0.09f, 0.10f, 0.12f), 0.25f, 0.2f),
            };
            AssetDatabase.SaveAssets();
            return m;
        }

        static Material EnsureMat(string name, Color color, float smooth, float metal,
                                  Color? emission = null, string texKeyword = null, float tiling = 1f)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                AssetDatabase.CreateFolder("Assets", "Materials");
            if (!AssetDatabase.IsValidFolder(MatFolder))
                AssetDatabase.CreateFolder("Assets/Materials", "Range");

            string path = MatFolder + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            mat.SetFloat("_Smoothness", smooth);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smooth);
            mat.SetFloat("_Metallic", metal);

            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            // Upgrade optionnel : si des textures téléchargées existent sous Assets/ThirdParty/Textures,
            // elles sont branchées automatiquement (match par mot-clé dans le chemin).
            if (!string.IsNullOrEmpty(texKeyword))
            {
                var tex = FindTexture(texKeyword, normal: false);
                if (tex != null)
                {
                    mat.SetTexture("_BaseMap", tex);
                    if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
                    mat.SetColor("_BaseColor", Color.white);
                    mat.SetTextureScale("_BaseMap", Vector2.one * tiling);
                }
                var nrm = FindTexture(texKeyword, normal: true);
                if (nrm != null)
                {
                    mat.EnableKeyword("_NORMALMAP");
                    mat.SetTexture("_BumpMap", nrm);
                }
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Texture2D FindTexture(string keyword, bool normal)
        {
            if (!AssetDatabase.IsValidFolder(TexFolder)) return null;
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { TexFolder });
            foreach (var g in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                string lower = path.ToLowerInvariant();
                if (!lower.Contains(keyword)) continue;
                // Le sol vit dans un dossier "floor" : exclu des autres recherches
                // pour éviter que les murs récupèrent la texture du sol.
                if (keyword != "floor" && lower.Contains("floor")) continue;
                bool isNormal = lower.Contains("normal") || lower.Contains("_nor");
                bool isColor = lower.Contains("color") || lower.Contains("albedo") || lower.Contains("diff");
                if (normal ? isNormal : isColor)
                {
                    if (normal)
                    {
                        // Force le type d'import Normal Map, sinon URP l'interprète mal
                        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                        if (imp != null && imp.textureType != TextureImporterType.NormalMap)
                        {
                            imp.textureType = TextureImporterType.NormalMap;
                            imp.SaveAndReimport();
                        }
                    }
                    return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                }
            }
            return null;
        }

        // ------------------------------------------------------------------ helpers

        static Transform Group(string name)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Range");
            go.transform.SetParent(root, false);
            return go.transform;
        }

        static GameObject Box(string name, Transform parent, Vector3 center, Vector3 size, Material mat,
                              Vector3? euler = null, bool collider = true, bool shadows = true, string tag = "Wall")
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(go, "Range");
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = size;
            if (euler.HasValue) go.transform.localEulerAngles = euler.Value;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            if (!shadows) mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            if (!string.IsNullOrEmpty(tag)) go.tag = tag;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            return go;
        }

        // ------------------------------------------------------------------ construction

        static void BuildStructure(RangeMats m)
        {
            var s = Group("Range_Structure");
            float cx = (XWest + XEast) / 2f;
            float cz = (ZSouth + ZNorth) / 2f;
            float lenX = XEast - XWest + 2 * T;
            float lenZ = ZNorth - ZSouth + 2 * T;

            // Sol : dalle fine (0.06) → le joueur enjambe sans souci (step offset).
            // Même layer que Ground pour que le groundCheck de PlayerMovement fonctionne dedans.
            var floor = Box("Floor", s, new Vector3(cx, 0.03f, cz), new Vector3(lenX, 0.06f, lenZ), m.Floor);
            floor.layer = groundLayer;

            Box("Ceiling", s, new Vector3(cx, H + 0.075f, cz), new Vector3(lenX, 0.15f, lenZ), m.Ceiling);

            Box("Wall_North", s, new Vector3(cx, H / 2f, ZNorth + T / 2f), new Vector3(lenX, H, T), m.Wall);
            Box("Wall_South", s, new Vector3(cx, H / 2f, ZSouth - T / 2f), new Vector3(lenX, H, T), m.Wall);
            Box("Wall_East",  s, new Vector3(XEast + T / 2f, H / 2f, cz),  new Vector3(T, H, lenZ), m.Wall);

            // Mur ouest avec porte (entrée derrière la ligne de tir, comme un vrai stand)
            float doorZ = -7f, doorW = 1.8f, doorH = 2.4f;
            float wx = XWest - T / 2f;
            float nLen = (ZNorth + T) - (doorZ + doorW / 2f);
            float sLen = (doorZ - doorW / 2f) - (ZSouth - T);
            Box("Wall_West_N", s, new Vector3(wx, H / 2f, doorZ + doorW / 2f + nLen / 2f), new Vector3(T, H, nLen), m.Wall);
            Box("Wall_West_S", s, new Vector3(wx, H / 2f, doorZ - doorW / 2f - sLen / 2f), new Vector3(T, H, sLen), m.Wall);
            Box("Wall_West_Header", s, new Vector3(wx, doorH + (H - doorH) / 2f, doorZ), new Vector3(T, H - doorH, doorW), m.Wall);

            // Encadrement de porte sombre
            Box("DoorJamb_N", s, new Vector3(wx, doorH / 2f, doorZ + doorW / 2f + 0.05f), new Vector3(T + 0.06f, doorH, 0.1f), m.Frame);
            Box("DoorJamb_S", s, new Vector3(wx, doorH / 2f, doorZ - doorW / 2f - 0.05f), new Vector3(T + 0.06f, doorH, 0.1f), m.Frame);
            Box("DoorLintel", s, new Vector3(wx, doorH + 0.05f, doorZ), new Vector3(T + 0.06f, 0.1f, doorW + 0.2f), m.Frame);

            // Bande sombre derrière la zone tireurs
            Box("Wainscot_N", s, new Vector3(8f, 0.5f, ZNorth - 0.03f), new Vector3(6f, 1f, 0.06f), m.Acoustic);
            Box("Wainscot_S", s, new Vector3(8f, 0.5f, ZSouth + 0.03f), new Vector3(6f, 1f, 0.06f), m.Acoustic);

            // Panneaux acoustiques côté cibles
            for (int i = 0; i < 5; i++)
            {
                float px = 11.4f + i * 1.5f;
                Box("Acoustic_N_" + i, s, new Vector3(px, 1.9f, ZNorth - 0.04f), new Vector3(1.2f, 2f, 0.08f), m.Acoustic);
                Box("Acoustic_S_" + i, s, new Vector3(px, 1.9f, ZSouth + 0.04f), new Vector3(1.2f, 2f, 0.08f), m.Acoustic);
            }
        }

        static void BuildBooths(RangeMats m)
        {
            var b = Group("Range_Booths");

            // 8 séparateurs = 7 box de tir
            for (int i = 0; i <= Lanes; i++)
            {
                float z = LaneZ0 + LaneStep / 2f - i * LaneStep;
                Box("Divider_" + i, b, new Vector3(FiringX, 1.11f, z), new Vector3(1.7f, 2.1f, 0.08f), m.Frame);
            }

            // Table + panneau avant par couloir
            for (int i = 0; i < Lanes; i++)
            {
                float z = LaneZ0 - i * LaneStep;
                Box("Table_" + (i + 1), b, new Vector3(10.05f, 1.03f, z), new Vector3(0.8f, 0.06f, 1.5f), m.Table);
                Box("TableFront_" + (i + 1), b, new Vector3(10.42f, 0.53f, z), new Vector3(0.06f, 1.06f, 1.5f), m.Table);
            }

            float cz = (ZSouth + ZNorth) / 2f;
            Box("FiringLine", b, new Vector3(10.9f, 0.065f, cz), new Vector3(0.12f, 0.012f, ZNorth - ZSouth - 0.2f), m.Red, shadows: false);
            Box("HazardStrip", b, new Vector3(11.25f, 0.065f, cz), new Vector3(0.3f, 0.012f, ZNorth - ZSouth - 0.2f), m.Yellow, shadows: false);
        }

        static void BuildDownrange(RangeMats m)
        {
            var d = Group("Range_Downrange");
            float cz = (ZSouth + ZNorth) / 2f;

            // Tapis caoutchouc au sol côté cibles
            Box("RubberFloor", d, new Vector3(15.2f, 0.066f, cz), new Vector3(7.4f, 0.012f, ZNorth - ZSouth - 0.1f), m.Rubber, shadows: false);

            // Pare-balles incliné (le haut penche vers l'arrière)
            Box("Backstop", d, new Vector3(18.2f, 1.8f, cz), new Vector3(0.14f, 3.5f, ZNorth - ZSouth - 0.15f), m.Backstop,
                euler: new Vector3(0, 0, 14f));

            // Support + socle sous chaque cible
            for (int i = 0; i < Lanes; i++)
            {
                float z = LaneZ0 - i * LaneStep;
                Box("TargetStand_" + (i + 1), d, new Vector3(15.6f, 0.53f, z), new Vector3(0.12f, 0.94f, 0.12f), m.Frame);
                Box("TargetBase_" + (i + 1), d, new Vector3(15.6f, 0.09f, z), new Vector3(0.5f, 0.06f, 0.5f), m.Frame);
            }
        }

        static void BuildLighting(RangeMats m)
        {
            var l = Group("Range_Lighting");
            float cz = (ZSouth + ZNorth) / 2f;
            float stripLen = ZNorth - ZSouth - 1f;

            // Réglettes lumineuses émissives au plafond
            float[] stripX = { 7.2f, 9.7f, 12.2f, 14.7f, 17f };
            for (int i = 0; i < stripX.Length; i++)
                Box("LightStrip_" + i, l, new Vector3(stripX[i], H - 0.05f, cz), new Vector3(0.14f, 0.05f, stripLen), m.Emissive,
                    shadows: false);

            // Baffles plafond inclinés côté cibles (anti-ricochet, comme les vrais stands)
            float[] bafX = { 11.2f, 13f, 14.8f, 16.6f };
            for (int i = 0; i < bafX.Length; i++)
                Box("Baffle_" + i, l, new Vector3(bafX[i], 3.1f, cz), new Vector3(0.08f, 0.85f, stripLen), m.Ceiling,
                    euler: new Vector3(0, 0, -28f));

            // Vrais éclairages (3 rangées x 4)
            float[] rowX = { 7.5f, 11f, 15.5f };
            for (int r = 0; r < rowX.Length; r++)
                for (int i = 0; i < 4; i++)
                    Spot(l, new Vector3(rowX[r], H - 0.15f, -2.5f - i * 3.2f));
        }

        static void Spot(Transform parent, Vector3 pos)
        {
            var go = new GameObject("Spot_" + pos.x + "_" + pos.z);
            Undo.RegisterCreatedObjectUndo(go, "Range");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
            var li = go.AddComponent<Light>();
            li.type = LightType.Spot;
            li.spotAngle = 95f;
            li.range = 7f;
            li.intensity = 2.6f;
            li.color = new Color(1f, 0.96f, 0.88f);
            li.shadows = LightShadows.None;
        }

        static void BuildSignage(RangeMats m)
        {
            var sg = Group("Range_Signage");

            // Numéro au-dessus de chaque box (lisible depuis l'ouest, côté tireur)
            for (int i = 0; i < Lanes; i++)
            {
                float z = LaneZ0 - i * LaneStep;
                Box("LaneSignBack_" + (i + 1), sg, new Vector3(9.78f, 2.5f, z), new Vector3(0.05f, 0.42f, 0.42f), m.SignBack, collider: false);
                Sign(sg, (i + 1).ToString(), new Vector3(9.73f, 2.5f, z), new Vector3(0, 90f, 0), 4f, Color.white, 1.2f, 0.5f);
            }

            // Enseigne extérieure au-dessus de la porte
            Box("MainSignBack", sg, new Vector3(XWest - T - 0.05f, 3.05f, -7f), new Vector3(0.08f, 0.7f, 4.6f), m.SignBack, collider: false);
            Sign(sg, "STAND DE TIR", new Vector3(XWest - T - 0.11f, 3.05f, -7f), new Vector3(0, 90f, 0), 3f, new Color(1f, 0.85f, 0.3f), 4.4f, 0.7f);

            // Consigne de sécurité sur le mur nord, lisible depuis l'intérieur
            Sign(sg, "NE PAS FRANCHIR LA LIGNE ROUGE", new Vector3(8f, 2.2f, ZNorth - 0.08f), Vector3.zero, 1.6f,
                 new Color(0.9f, 0.25f, 0.2f), 5.5f, 0.5f);
        }

        // NB : si un texte apparaît en miroir, ajouter 180 au Y de sa rotation.
        static void Sign(Transform parent, string text, Vector3 pos, Vector3 euler, float fontSize, Color color, float w, float h)
        {
            var go = new GameObject("Sign_" + text);
            Undo.RegisterCreatedObjectUndo(go, "Range");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler;
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.rectTransform.sizeDelta = new Vector2(w, h);
        }

        static void BuildProps(RangeMats m)
        {
            var p = Group("Range_Props");
            Box("BackBench", p, new Vector3(5.75f, 0.45f, -2.8f), new Vector3(0.7f, 0.9f, 2.6f), m.Table);
            Box("Crate_A", p, new Vector3(5.7f, 1.08f, -2.2f), new Vector3(0.36f, 0.36f, 0.36f), m.Backstop);
            Box("Crate_B", p, new Vector3(5.75f, 1.05f, -3.1f), new Vector3(0.3f, 0.3f, 0.45f), m.Frame);
            Box("Crate_C", p, new Vector3(5.7f, 1.02f, -3.6f), new Vector3(0.26f, 0.26f, 0.26f), m.Red);
        }

        static void PlaceTargets()
        {
            var targets = new List<Transform>();
            foreach (Transform c in root)
                if (c.name.StartsWith("Target"))
                    targets.Add(c);

            // Conserve l'ordre gauche->droite existant
            targets = targets.OrderByDescending(t => t.position.z).ToList();

            for (int i = 0; i < targets.Count && i < Lanes; i++)
            {
                Undo.RecordObject(targets[i], "Move target");
                float z = LaneZ0 - i * LaneStep;
                targets[i].position = new Vector3(15.6f, 1.5f, z);
            }
        }

        static void WarnIfBlocked()
        {
            var cyl = GameObject.Find("Cylinder");
            if (cyl != null)
            {
                var pos = cyl.transform.position;
                if (pos.x > 4f && pos.x < 20f && pos.z > -15.5f && pos.z < 1.5f)
                    Debug.LogWarning("L'objet 'Cylinder' se trouve dans l'emprise du stand de tir — le déplacer ou le supprimer à la main.");
            }
        }
    }
}
