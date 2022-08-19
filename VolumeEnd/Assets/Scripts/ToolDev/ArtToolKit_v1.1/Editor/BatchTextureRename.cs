#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace ArtToolKit
{
    public class BatchTextureRename
    {
        //private static string assetPath = Application.dataPath;

        [MenuItem("Tools/BatchTool/批量克隆贴图并加分辨率后缀")]
        private static void BatchCloneTextureAndAddSizeSuffixName()
        {
            var objs = Selection.objects;
            CloneTexture(objs);
        }

        private static void CloneTexture(Object[] objs)
        {
            for (int i = 0; i < objs.Length; i++)
            {
                var obj = objs[i];
                string path = AssetDatabase.GetAssetPath(obj);
                for (int j = 0; j < ArtEditorUtil.sizeTyp.Length; j++)
                {
                    var suffix = ArtEditorUtil.sizeTyp[j];
                    AddSizeSuffix(path, suffix);
                }

                AssetDatabase.DeleteAsset(path);
            }
        }

        private static void AddSizeSuffix(string oldPath, int suffix)
        {
            var nameEx = FileNameEx(oldPath);
            if (oldPath.EndsWith(suffix + nameEx))
            {
                return;
            }

            var newPath = oldPath.Replace(nameEx, suffix + nameEx);
            AssetDatabase.CopyAsset(oldPath, newPath);
        }

        // [MenuItem("Tools/BatchTool/批量改实际尺寸后缀")]
        public static void AddTextureSizeSuffix()
        {
            Object[] objs = Selection.objects;

            //遍历所有
            for (int i = 0; i < objs.Length; i++)
            {
                string path = AssetDatabase.GetAssetPath(objs[i]);

                //获取贴图的尺寸
                int textureSize = 0;
                bool isContain = false;
                bool isSame = true;

                //先设置到最大的max size
                TextureImporter oldImporter = (TextureImporter) AssetImporter.GetAtPath(path);
                int oldSize = oldImporter.maxTextureSize;
                oldImporter.maxTextureSize = 16384;
                oldImporter.SaveAndReimport();

                //把实际的尺寸赋值给导入设置的maxsize
                textureSize = GetTextureSize(path);

                string suffixName = FileNameEx(path);
                string noSuffixPath = path.Replace(suffixName, "");
                string newPath = String.Empty;
                string newName = objs[i].name;

                //排除已经有后缀的情况
                for (int j = 0; j < ArtEditorUtil.sizeTyp.Length; j++)
                {

                    if (path.Contains("_" + ArtEditorUtil.sizeTyp[j]))
                    {
                        isContain = true;
                        if (ArtEditorUtil.sizeTyp[j] != textureSize) //已有后缀的情况下，如果更改了贴图实际尺寸，则要把现有的后缀去掉,不同后缀
                        {
                            isSame = false;
                            newName = objs[i].name.Replace("_" + ArtEditorUtil.sizeTyp[j].ToString(), "");
                            noSuffixPath = noSuffixPath.Replace("_" + ArtEditorUtil.sizeTyp[j].ToString(), "");
                        }

                        break;
                    }

                }

                //改成新后缀
                if (!isContain)
                {
                    newName = $"{newName}_{textureSize}{suffixName}";
                    newPath = $"{noSuffixPath}_{textureSize}{suffixName}";
                    AssetDatabase.RenameAsset(path, newName);
                    TextureImporter importer = (TextureImporter) AssetImporter.GetAtPath(newPath);
                    importer.maxTextureSize = textureSize;
                    importer.SaveAndReimport();
                }
                else
                {
                    if (!isSame)
                    {
                        //恢复原先的后缀
                        oldImporter.maxTextureSize = oldSize;

                        newName = $"{newName}_{textureSize}{suffixName}";
                        newPath = $"{noSuffixPath}_{textureSize}{suffixName}";
                        AssetDatabase.RenameAsset(path, newName);
                        TextureImporter importer = (TextureImporter) AssetImporter.GetAtPath(newPath);
                        importer.maxTextureSize = textureSize;
                        importer.SaveAndReimport();
                    }
                    else
                    {
                        //恢复原先的后缀
                        oldImporter.maxTextureSize = oldSize;
                        oldImporter.SaveAndReimport();
                    }
                }

            }
        }

        /// <summary>
        /// 获取贴图的尺寸
        /// </summary>
        private static int GetTextureSize(string path)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            int size = Mathf.Max(texture.width, texture.width);
            return size;
        }

        [MenuItem("Tools/BatchTool/批量设置贴图安卓平台的MaxSize")]
        private static void BatchSetPlatformMaxSizeTool()
        {
            var objs = Selection.objects;
            for (int i = 0; i < objs.Length; i++)
            {
                var obj = objs[i];

                var path = AssetDatabase.GetAssetPath(obj);
                TextureImporter importer = (TextureImporter) TextureImporter.GetAtPath(path);
                importer.maxTextureSize = GetTextureSize(path);

                var standalonePlatform = importer.GetPlatformTextureSettings("Standalone");
                standalonePlatform.overridden = true;
                // standalonePlatform.maxTextureSize = obj.name.EndsWith("_32");
                var strs = obj.name.Split(new char[1] {'_'});
                standalonePlatform.maxTextureSize = int.Parse(strs[strs.Length - 1]);

                var androidPlatform = importer.GetPlatformTextureSettings("Android");
                androidPlatform.overridden = true;
                int size = 32;
                if (androidPlatform.maxTextureSize != size)
                {
                    androidPlatform.maxTextureSize = importer.maxTextureSize / 2;
                }

                importer.SetPlatformTextureSettings(standalonePlatform);
                importer.SetPlatformTextureSettings(androidPlatform);
                importer.SaveAndReimport();
            }

        }

        // [MenuItem("Tools/BatchTool/批量设置SPE贴图Format")]
        public static void BatchSetSpeTextrueFormat()
        {
            var objs = Selection.objects;
            for (int i = 0; i < objs.Length; i++)
            {
                var obj = objs[i];
                if (obj is Texture)
                {
                    TextureImporterPlatformSettings standalonePlatform = null;
                    TextureImporterPlatformSettings androidPlatform = null;
                    TextureImporterPlatformSettings IosPlatform = null;

                    var path = AssetDatabase.GetAssetPath(obj);
                    TextureImporter importer = (TextureImporter) TextureImporter.GetAtPath(path);

                    if (importer.DoesSourceTextureHaveAlpha())
                    {
                        standalonePlatform =
                            TextureImporterPlatformSettings(importer, "Standalone", true, TextureImporterFormat.DXT5);
                        androidPlatform = TextureImporterPlatformSettings(importer, "Android", true,
                            TextureImporterFormat.ASTC_5x5);
                    }
                    else
                    {
                        standalonePlatform =
                            TextureImporterPlatformSettings(importer, "Standalone", true, TextureImporterFormat.DXT1);
                        androidPlatform = TextureImporterPlatformSettings(importer, "Android", true,
                            TextureImporterFormat.ASTC_5x5);
                    }
                    IosPlatform = TextureImporterPlatformSettings(importer, "iPhone", true, TextureImporterFormat.ASTC_5x5, androidPlatform.maxTextureSize);
    
                    importer.SetPlatformTextureSettings(IosPlatform);
                    importer.SetPlatformTextureSettings(standalonePlatform);
                    importer.SetPlatformTextureSettings(androidPlatform);
                    importer.SaveAndReimport();
                }
            }
        }
        
        public static void BatchSetIosTextureImport()
         {
             var objs = Selection.objects;
             for (int i = 0; i < objs.Length; i++)
             {
                 var obj = objs[i];
                 if (obj is Texture)
                 {
                     TextureImporterPlatformSettings IosPlatform = null;
                     TextureImporterPlatformSettings androidPlatform = null;
    
                     var path = AssetDatabase.GetAssetPath(obj);
                     TextureImporter importer = (TextureImporter) TextureImporter.GetAtPath(path);

                     androidPlatform = importer.GetPlatformTextureSettings("Android");
                     
                     IosPlatform = TextureImporterPlatformSettings(importer, "iPhone", true, TextureImporterFormat.ASTC_5x5, androidPlatform.maxTextureSize);

    
                     importer.SetPlatformTextureSettings(IosPlatform);
                     importer.SaveAndReimport();
                 }
             }
         }

        private static TextureImporterPlatformSettings TextureImporterPlatformSettings(TextureImporter importer,
            string platform, bool isOverride, TextureImporterFormat format)
        {
            var platformTextureSettings = importer.GetPlatformTextureSettings(platform);
            platformTextureSettings.overridden = isOverride;
            platformTextureSettings.format = format;
            return platformTextureSettings;
        }
        
        private static TextureImporterPlatformSettings TextureImporterPlatformSettings(TextureImporter importer,
            string platform, bool isOverride, TextureImporterFormat format, int size)
        {
            var platformTextureSettings = importer.GetPlatformTextureSettings(platform);
            platformTextureSettings.overridden = isOverride;
            platformTextureSettings.format = format;
            platformTextureSettings.maxTextureSize = size;
            return platformTextureSettings;
        }


        // [MenuItem("Tools/BatchTool/批量设置贴图预设")]
        // private static void BatchSetPresetTool()
        // {
        //     var m_preset = AssetPresetManager.GetPresetByCategory(AssetPreset.ECategory.Texture);
        //     AssetPresetManager assetPreset = AssetPresetManager.GetInstance(m_preset);
        //     var configurations = m_preset.Configurations;
        //     Object[] objs = Selection.objects;
        //
        //     //遍历所有
        //     for (int i = 0; i < objs.Length; i++)
        //     {
        //         string path = AssetDatabase.GetAssetPath(objs[i]);
        //         var textureGuid = AssetDatabase.GUIDFromAssetPath(path);
        //         configurations.AddEmptyTemplateAsset();
        //         assetPreset.OnFinalize();
        //         var tempAssets = configurations.TemplateAssets;
        //         tempAssets[tempAssets.Count - 1].AssetGUID = textureGuid.ToString();
        //         tempAssets[tempAssets.Count - 1].AssetName = objs[i].name;
        //         tempAssets[tempAssets.Count - 1].ShortKey = $"_{AssetDatabase.LoadAssetAtPath<Object>(path).name}";
        //     }
        //
        //     configurations.Save();
        // }

        /// <summary>
        /// 获取文件后缀名，如.PDF
        /// </summary>
        /// <param name="fileName"是传入的文件名></param>
        /// <returns></returns>
        public static string FileNameEx(string fileName)
        {
            int suff = fileName.Length - 1;
            while (suff >= 0 && (fileName[suff] != '.'))
                suff--;
            string suffixName = fileName.Remove(0, suff); //获取文件后缀名，如.PDF
            return suffixName;
        }
    }
}
#endif
