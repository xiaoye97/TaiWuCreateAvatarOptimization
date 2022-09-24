using System;
using System.IO;
using HarmonyLib;
using UnityEngine;
using UICommon.Character.Avatar;
using System.Collections.Generic;
using TaiwuModdingLib.Core.Plugin;
using GameData.Domains.Character.AvatarSystem;

namespace TaiWuCreateAvatarOptimization
{
    [PluginConfig("TaiWuCreateAvatarOptimization", "宵夜97", "1.1.0")]
    public class CreateAvatarOptimization : TaiwuRemakePlugin
    {
        private Harmony harmony;

        // 缓存
        public static Dictionary<string, Sprite> SmallCache;

        public static Dictionary<string, Sprite> NormalCache;
        public static Dictionary<string, Sprite> BigCache;

        /// <summary>
        /// Mod被关闭
        /// </summary>
        public override void Dispose()
        {
            // Mod被关闭时取消Patch
            if (harmony != null)
            {
                harmony.UnpatchSelf();
                harmony = null;
            }
        }

        /// <summary>
        /// Mod初始化
        /// </summary>
        public override void Initialize()
        {
            SmallCache = new Dictionary<string, Sprite>();
            NormalCache = new Dictionary<string, Sprite>();
            BigCache = new Dictionary<string, Sprite>();
            harmony = Harmony.CreateAndPatchAll(typeof(CreateAvatarOptimization));
            // 延迟1秒搜索资源，延迟主要是为了等待其他Mod开启完毕，这样才能搜索到其他Mod下的图片素材。
            DelayMono.DelayDo(SearchAssets, 1f);
        }

        #region 替换原图
        [HarmonyPostfix, HarmonyPatch(typeof(AvatarAtlasAssets), "GetSpriteArray")]
        public static void AvatarAtlasAssets_GetSpriteArray(AvatarAtlasAssets __instance, byte avatarId, string spriteName, ref Sprite[] __result)
        {
            if (__result != null && __result.Length == 3)
            {
                if (__result[0] != null)
                {
                    string name = __result[0].name.Replace("(Clone)", "");
                    if (BigCache.ContainsKey(name))
                    {
                        __result[0] = BigCache[name];
                        //Debug.Log($"AvatarAtlasAssets_GetSpriteArray Big 替换了 {name}");
                    }
                }
                if (__result[1] != null)
                {
                    string name = __result[1].name.Replace("(Clone)", "");
                    if (NormalCache.ContainsKey(name))
                    {
                        __result[1] = NormalCache[name];
                        //Debug.Log($"AvatarAtlasAssets_GetSpriteArray Normal 替换了 {name}");
                    }
                }
                if (__result[2] != null)
                {
                    string name = __result[2].name.Replace("(Clone)", "");
                    if (SmallCache.ContainsKey(name))
                    {
                        __result[2] = SmallCache[name];
                        //Debug.Log($"AvatarAtlasAssets_GetSpriteArray Small 替换了 {name}");
                    }
                }
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(AvatarManager), "GetSprite")]
        public static void AvatarManager_GetSprite(AvatarManager __instance, int size, ref Sprite __result)
        {
            if (__result == null) return;
            if (size == 0)
            {
                string name = __result.name.Replace("(Clone)", "");
                if (BigCache.ContainsKey(name))
                {
                    __result = BigCache[name];
                    //Debug.Log($"AvatarManager_GetSprite Big 替换了 {name}");
                }
            }
            if (size == 1)
            {
                string name = __result.name.Replace("(Clone)", "");
                if (NormalCache.ContainsKey(name))
                {
                    __result = NormalCache[name];
                    //Debug.Log($"AvatarManager_GetSprite Normal 替换了 {name}");
                }
            }
            if (size == 2)
            {
                string name = __result.name.Replace("(Clone)", "");
                if (SmallCache.ContainsKey(name))
                {
                    __result = SmallCache[name];
                    //Debug.Log($"AvatarManager_GetSprite Small 替换了 {name}");
                }
            }
        }
        #endregion

        /// <summary>
        /// 搜索资源
        /// </summary>
        public static void SearchAssets()
        {
            Debug.Log("开始加载捏人优化资源");
            SmallCache = new Dictionary<string, Sprite>();
            NormalCache = new Dictionary<string, Sprite>();
            BigCache = new Dictionary<string, Sprite>();
            // 遍历所有开启的Mod
            foreach (var enabledMod in ModManager.EnabledMods)
            {
                var info = ModManager.GetModInfo(enabledMod);
                var path = ModManager.GetModPath(info);
                DirectoryInfo modDir = new DirectoryInfo(path);
                if (modDir.Exists)
                {
                    // 在Mod文件夹中找CreateAvatarOptimizationPackage
                    var paks = modDir.GetDirectories("CreateAvatarOptimizationPackage");
                    if (paks != null && paks.Length > 0)
                    {
                        var pakDir = paks[0];
                        var smallDir = new DirectoryInfo(pakDir + "/Small");
                        var normalDir = new DirectoryInfo(pakDir + "/Normal");
                        var bigDir = new DirectoryInfo(pakDir + "/Big");
                        if (!smallDir.Exists)
                        {
                            Debug.LogWarning($"[捏人优化]在Mod {info.Title} 中找到了CreateAvatarOptimizationPackage文件夹，但是CreateAvatarOptimizationPackage文件夹中没有Small文件夹，已略过此Mod");
                            continue;
                        }
                        if (!normalDir.Exists)
                        {
                            Debug.LogWarning($"[捏人优化]在Mod {info.Title} 中找到了CreateAvatarOptimizationPackage文件夹，但是CreateAvatarOptimizationPackage文件夹中没有Normal文件夹，已略过此Mod");
                            continue;
                        }
                        if (!bigDir.Exists)
                        {
                            Debug.LogWarning($"[捏人优化]在Mod {info.Title} 中找到了CreateAvatarOptimizationPackage文件夹，但是CreateAvatarOptimizationPackage文件夹中没有Big文件夹，已略过此Mod");
                            continue;
                        }
                        var smallPNGs = smallDir.GetFiles("*.png");
                        var normalPNGs = normalDir.GetFiles("*.png");
                        var bigPNGs = bigDir.GetFiles("*.png");
                        // 遍历并加载图片
                        if (smallPNGs != null && smallPNGs.Length > 0)
                        {
                            foreach (var file in smallPNGs)
                            {
                                var sprite = LoadTextureToSprite(file.FullName);
                                if (sprite != null)
                                {
                                    string name = file.Name.Replace(".png", "");
                                    sprite.name = name;
                                    SmallCache[name] = sprite;
                                    //Debug.Log($"[捏人优化]加载了图片 {name}");
                                }
                            }
                        }
                        if (normalPNGs != null && normalPNGs.Length > 0)
                        {
                            foreach (var file in normalPNGs)
                            {
                                var sprite = LoadTextureToSprite(file.FullName);
                                if (sprite != null)
                                {
                                    string name = file.Name.Replace(".png", "");
                                    sprite.name = name;
                                    NormalCache[name] = sprite;
                                    //Debug.Log($"[捏人优化]加载了图片 {name}");
                                }
                            }
                        }
                        if (bigPNGs != null && bigPNGs.Length > 0)
                        {
                            foreach (var file in bigPNGs)
                            {
                                var sprite = LoadTextureToSprite(file.FullName);
                                if (sprite != null)
                                {
                                    string name = file.Name.Replace(".png", "");
                                    sprite.name = name;
                                    BigCache[name] = sprite;
                                    //Debug.Log($"[捏人优化]加载了图片 {name}");
                                }
                            }
                        }
                    }
                }
            }
            Debug.Log("加载完毕");
        }

        /// <summary>
        /// 加载图片
        /// </summary>
        public static Sprite LoadTextureToSprite(string texPath)
        {
            if (File.Exists(texPath))
            {
                try
                {
                    using (FileStream fileStream = new FileStream(texPath, FileMode.Open))
                    {
                        byte[] array = new byte[fileStream.Length];
                        fileStream.Read(array, 0, array.Length);
                        fileStream.Close();
                        Texture2D texture2D = new Texture2D(1024, 1024, TextureFormat.ARGB32, false);
                        texture2D.LoadImage(array);
                        return Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), Vector2.zero);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[捏人优化]加载图片失败，图片:{texPath}，异常信息:{ex.Message}");
                }
            }
            return null;
        }
    }
}