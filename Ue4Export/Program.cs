﻿// Copyright 2021 Crystal Ferrai
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

namespace Ue4Export
{
	internal class Program
	{
		static int Main(string[] args)
		{
			if (args.Length != 4)
			{
				Console.WriteLine("Exports a set of files from a UE4 game. Usage:\n\nUe4Export [game asset path] [asset path] [output directory] [aes key]\n\n  game asset path = path to a directory containing .pak files for a game\n\n  asset path = asset path to export. See readme.md for more details.\n\n  output directory = directory to output exported assets");
				return 0;
			}

			ConsoleLogger logger = new();

			string gameDir = args[0];
			if (!Directory.Exists(gameDir))
			{
				logger.Log(LogLevel.Fatal, $"Could not access game directory \"{gameDir}\"");
				return 1;
			}

			string assetPath = args[1];
			string outDir = args[2];
      string rawAesKey = args[3];

			Exporter exporter = new Exporter(gameDir, outDir, logger);
			bool success = exporter.Export(assetPath, rawAesKey);

			if (!success)
			{
				logger.Log(LogLevel.Warning, "One or more assets failed to export.");
			}

			logger.Log(LogLevel.Important, "\nExports complete.");

			// Pause if debugger attached
			//if (System.Diagnostics.Debugger.IsAttached) Console.ReadKey();

			return success ? 0 : 2;
		}
	}
}