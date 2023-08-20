// Copyright 2021 Crystal Ferrai
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.AssetRegistry;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Localization;
using CUE4Parse.UE4.Oodle.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Shaders;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.Wwise;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse_Conversion.Textures;
using Newtonsoft.Json;
using SkiaSharp;
using System.Text;

namespace Ue4Export
{
	/// <summary>
	/// Exports Ue4 assets to files
	/// </summary>
	internal class Exporter
	{
		private static readonly HashSet<string> sExtensionsToIgnore;

		private readonly string mGameDir;
		private readonly string mOutDir;

		private readonly Logger? mLogger;

		private readonly JsonSerializerSettings mJsonSettings;

		static Exporter()
		{
			// When exporting with wildcards, filter out uexp/ubulk files because they are not a valid export target and are always
			// paired with something that is valid like a uasset/umap
			sExtensionsToIgnore = new HashSet<string>()
			{
				".uexp",
				".ubulk"
			};
		}

		public Exporter(string gameDir, string outDir, Logger? logger)
		{
			mGameDir = gameDir;
			mOutDir = outDir;
			mLogger = logger;

			mJsonSettings = new JsonSerializerSettings()
			{
				Formatting = Formatting.Indented
			};
		}

		public bool Export(string assetPath, string rawAesKey)
		{
			bool success = true;

			using (var provider = new DefaultFileProvider(mGameDir, SearchOption.TopDirectoryOnly, false, new VersionContainer(EGame.GAME_UE4_25)))
			{
				provider.Initialize();

        var aes_key = new FAesKey(rawAesKey);

				foreach (var vfsReader in provider.UnloadedVfs)
				{
					provider.SubmitKey(vfsReader.EncryptionKeyGuid, aes_key);
				}
				provider.LoadLocalization(ELanguage.English);

        success = SaveText(provider, assetPath, false);
			}

			return success;
    }
		private bool SaveText(AbstractVfsFileProvider provider, string assetPath, bool isBulk = false)
		{
			string ext = GetTrimmedExtension(assetPath);
			string? text = null;

			switch (ext)
			{
				case "":
				case "uasset":
				case "umap":
					{
            var exports = provider.LoadObject(assetPath);
						text = JsonConvert.SerializeObject(exports, mJsonSettings);
						break;
					}
				case "ini":
				case "txt":
				case "log":
				case "po":
				case "bat":
				case "dat":
				case "cfg":
				case "ide":
				case "ipl":
				case "zon":
				case "xml":
				case "h":
				case "uproject":
				case "uplugin":
				case "upluginmanifest":
				case "csv":
				case "json":
				case "archive":
				case "manifest":
					text = Encoding.UTF8.GetString(provider.Files[assetPath].Read());
					break;
				case "locmeta":
					text = SerializeObject<FTextLocalizationMetaDataResource>(provider, assetPath);
					break;
				case "locres":
					text = SerializeObject<FTextLocalizationResource>(provider, assetPath);
					break;
				case "bin" when assetPath.Contains("AssetRegistry"):
					text = SerializeObject<FAssetRegistryState>(provider, assetPath);
					break;
				case "bnk":
				case "pck":
					text = SerializeObject<WwiseReader>(provider, assetPath);
					break;
				case "udic":
					text = SerializeObject<FOodleDictionaryArchive>(provider, assetPath);
					break;
				case "ushaderbytecode":
				case "ushadercode":
					text = SerializeObject<FShaderCodeArchive>(provider, assetPath);
					break;
				case "png":
				case "jpg":
				case "bmp":
				case "svg":
				case "ufont":
				case "otf":
				case "ttf":
				case "wem":
				default:
					if (!isBulk) mLogger?.Log(LogLevel.Warning, $"{assetPath} - This asset cannot be converted to Text.");
					return isBulk;
			}

      var dir = Path.GetDirectoryName(mOutDir);
			Directory.CreateDirectory(dir!);
			File.WriteAllText(mOutDir, text);

			return true;
		}

		private string SerializeObject<T>(AbstractVfsFileProvider provider, string assetPath)
		{
			using FArchive archive = provider.CreateReader(assetPath);
			object obj = Activator.CreateInstance(typeof(T), archive)!;
			return JsonConvert.SerializeObject(obj, mJsonSettings);
		}

		/// <summary>
		/// Returns the file extension from a path without a leading period
		/// </summary>
		private static string GetTrimmedExtension(string path)
		{
			string ext = Path.GetExtension(path);
			if (ext.StartsWith('.')) ext = ext[1..];
			return ext;
		}

		[Flags]
		private enum ExportFormats
		{
			None = 0x00,
			Raw = 0x01,
			Text = 0x02,
			Texture = 0x04
		}
	}
}
