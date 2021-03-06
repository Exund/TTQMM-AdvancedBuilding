﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Harmony;
using Nuterra.BlockInjector;
using ModHelper.Config;
using Nuterra.NativeOptions;


namespace Exund.AdvancedBuilding
{
    public class AdvancedBuildingMod
    {
        public static string PreciseSnapshotsFolder = Path.Combine(Application.dataPath, "../PreciseSnapshots");
        private static GameObject _holder;
        internal static AssetBundle assetBundle;
        internal static RuntimeGizmos.TransformGizmo transformGizmo;
        internal static ModConfig config = new ModConfig();
        internal static string asm_path = Assembly.GetExecutingAssembly().Location.Replace("Exund.AdvancedBuilding.dll", "");

        public static void Load()
        {
            var harmony = HarmonyInstance.Create("exund.advancedbuilding");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            assetBundle = AssetBundle.LoadFromFile(asm_path + "Assets/runtimegizmos");

            try
            {
                _holder = new GameObject();
                _holder.AddComponent<AdvancedEditor>();
                _holder.AddComponent<LoadWindow>();
                UnityEngine.Object.DontDestroyOnLoad(_holder);

                transformGizmo = Singleton.cameraTrans.gameObject.AddComponent<RuntimeGizmos.TransformGizmo>();
                transformGizmo.enabled = false;

                config.TryGetConfig<float>("position_step", ref AdvancedEditor.position_step);
                config.TryGetConfig<float>("rotation_step", ref AdvancedEditor.rotation_step);
                config.TryGetConfig<float>("scale_step", ref AdvancedEditor.scale_step);
                config.TryGetConfig<bool>("open_inventory", ref AdvancedEditor.open_inventory);
                config.TryGetConfig<bool>("global_filters", ref AdvancedEditor.global_filters);
                var key = (int)AdvancedEditor.block_picker_key;
                config.TryGetConfig<int>("block_picker_key", ref key);
                AdvancedEditor.block_picker_key = (KeyCode)key;

                config.TryGetConfig<bool>("clearOnCollapse", ref PaletteTextFilter.clearOnCollapse);


                string modName = "Advanced Building";
                OptionKey blockPickerKey = new OptionKey("Block Picker activation key", modName, AdvancedEditor.block_picker_key);
                blockPickerKey.onValueSaved.AddListener(() =>
                {
                    AdvancedEditor.block_picker_key = blockPickerKey.SavedValue;
                    config["block_picker_key"] = (int)AdvancedEditor.block_picker_key;
                });

                OptionToggle globalFilterToggle = new OptionToggle("Block Picker - Use global filters", modName, AdvancedEditor.global_filters);
                globalFilterToggle.onValueSaved.AddListener(() =>
                {
                    AdvancedEditor.global_filters = globalFilterToggle.SavedValue;
                    config["global_filters"] = AdvancedEditor.global_filters;
                });

                OptionToggle openInventoryToggle = new OptionToggle("Block Picker - Automatically open the inventory when picking a block", modName, AdvancedEditor.open_inventory);
                openInventoryToggle.onValueSaved.AddListener(() =>
                {
                    AdvancedEditor.open_inventory = openInventoryToggle.SavedValue;
                    config["open_inventory"] = AdvancedEditor.open_inventory;
                });

                OptionToggle clearOnCollapse = new OptionToggle("Block Search - Clear filter when closing inventory", modName, PaletteTextFilter.clearOnCollapse);
                clearOnCollapse.onValueSaved.AddListener(() =>
                {
                    PaletteTextFilter.clearOnCollapse = clearOnCollapse.SavedValue;
                    config["clearOnCollapse"] = PaletteTextFilter.clearOnCollapse;
                    config.WriteConfigJsonFile();
                });

                if (!Directory.Exists(PreciseSnapshotsFolder))
                {
                    Directory.CreateDirectory(PreciseSnapshotsFolder);
                }

                new BlockPrefabBuilder(BlockTypes.GSOBlock_111, true)
                    .SetBlockID(7020)
                    .SetName("Reticule Research Hadamard Superposer")
                    .SetDescription("This block can register quantum fluctuations applied on the tech's blocks and stabilize them during the snapshot process.\n\n<b>Warning</b>: Can cause temporary quantum jumps if it enters in contact with zero-stasis gluons.\nUsed to activate Advanced Building.")
                    .SetFaction(FactionSubTypes.EXP)
                    .SetCategory(BlockCategories.Accessories)
                    .SetRarity(BlockRarity.Rare)
                    .SetPrice(58860)
                    .SetRecipe(new Dictionary<ChunkTypes, int> {
                        { ChunkTypes.SeedAI, 5 }
                    })
                    .SetModel(GameObjectJSON.MeshFromFile(asm_path + "Assets/hadamard_superposer.obj"), true, GameObjectJSON.GetObjectFromGameResources<Material>("RR_Main"))
                    .SetIcon(GameObjectJSON.ImageFromFile(asm_path + "Assets/hadamard_superposer.png"))
                    .AddComponent<ModuleOffgridStore>()
                    .RegisterLater();

            } 
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static float NumberField(float value)
        {
            var h = GUILayout.Height(25f);
            float.TryParse(GUILayout.TextField(value.ToString(), h), out float val);
            val = (float)Math.Round(val, 6);
            if (val != value)
            {
                GUI.changed = true;
            }

            return val;
        }

        public static float NumberField(float value, float interval)
        {
            var h = GUILayout.Height(25f);
            var w = GUILayout.Width(25f);

            GUILayout.BeginHorizontal(h);
            float.TryParse(GUILayout.TextField(value.ToString(), h), out float val);
            if (GUILayout.Button("+", w, h)) val += interval;
            if (GUILayout.Button("-", w, h)) val -= interval;
            GUILayout.EndHorizontal();
            val = (float)Math.Round(val, 6);
            if (val != value)
            {
                GUI.changed = true;
            }

            return val;
        }

        private static class Patches
        {
            [HarmonyPatch(typeof(TankPreset.BlockSpec), "InitFromBlockState")]
            private static class BlockSpec_InitFromBlockState
            {
                static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
                {
                    var codes = new List<CodeInstruction>(instructions);
                    var niv = codes.FindIndex(op => op.opcode == OpCodes.Newobj);
                    codes[niv - 2].operand = typeof(TankBlock).GetProperty("cachedLocalPosition", BindingFlags.Instance | BindingFlags.Public).GetGetMethod(false);
                    codes[niv - 1] = new CodeInstruction(OpCodes.Nop);

                    /*foreach (var ci in codes)
                    {
                        try
                        {
                            Console.WriteLine(ci.opcode.ToString() + "\t" + ci.operand.ToString());
                        } catch {
                            Console.WriteLine(ci.opcode.ToString());
                        }
                    }*/
                    return codes;
                }
            }

            [HarmonyPatch(typeof(ManPointer), "OnMouse")]
            private static class ManControllerTechBuilder_SpawnNewPaintingBlock
            {
                static void Prefix()
                {
                    if (Input.GetMouseButton(0) && Input.GetKey(AdvancedEditor.block_picker_key) && ManPlayer.inst.PaletteUnlocked)
                    {
                        ManPointer.inst.ChangeBuildMode((ManPointer.BuildingMode)10);
                    }
                }
            }

            private static class UIPaletteBlockSelect_Patches
            {
                [HarmonyPatch(typeof(UIPaletteBlockSelect), "BlockFilterFunction")]
                private static class BlockFilterFunction
                {
                    static void Postfix(ref BlockTypes blockType, ref bool __result)
                    {
                        if(__result)
                        {
                            __result = PaletteTextFilter.BlockFilterFunction(blockType);
                        }
                    }
                }

                [HarmonyPatch(typeof(UIPaletteBlockSelect), "OnPool")]
                private static class OnPool
                {
                    static void Postfix(ref UIPaletteBlockSelect __instance)
                    {
                        PaletteTextFilter.Init(__instance);
                    }
                }

                [HarmonyPatch(typeof(UIPaletteBlockSelect), "Collapse")]
                private static class Collapse
                {
                    static void Postfix(ref bool __result)
                    {
                        PaletteTextFilter.OnPaletteCollapse(__result);
                    }
                }
            }

            [HarmonyPatch(typeof(ManPauseGame), "TogglePauseMenu")]
            private static class ManPauseGame_TogglePauseMenu
            {
                static bool Prefix()
                {
                    return PaletteTextFilter.PreventPause();
                }
            }
        }
    }
}
