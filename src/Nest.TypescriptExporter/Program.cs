﻿using System;
using System.IO;
using Nest.TypescriptGenerator.Touchups;

namespace Nest.TypescriptGenerator
{
	public static class Program
	{
		/// <summary>
		/// generate [definitionfile] [nest_source_folder]
		/// split [definitionfile] [nest_source_folder] [out_folder]
		/// </summary>
		public static int Main(string[] args)
		{
			const string defaultTsFile = "typedefinitions.ts";
			const string defaultNestSourceFolder = @"..\..\..\net-master\src\Nest";
			var command = args.Length > 0 ? args[0] : "generate";
			var definitionFile = args.Length > 1 ? args[1] : defaultTsFile;
			var nestSourceFolder = args.Length > 2 ? args[2] : defaultNestSourceFolder;
			switch (command)
			{
				case "generate": return Generate(definitionFile, nestSourceFolder);
				case "both":
				case "split":
					var outFolder = args.Length > 3 ? args[3] : @"..\..\..\elastic-client-generator\specification\specs2";
					var r = 0;
					if (command == "both") r += Generate(definitionFile, nestSourceFolder);
					r += Split(definitionFile, nestSourceFolder, outFolder);
					return r;
				default:
					Console.Error.WriteLine("Unknown command for generator, valid are generate|split");
					return 2;
			}
		}

		private static int Split(string definitionFile, string nestSourceFolder, string outFolder)
		{
			var restSpec = new RestSpec(nestSourceFolder);
			var splitter = new TypescriptDumpSplitter(definitionFile, restSpec, outFolder);
			return splitter.Split();
		}

		private static int Generate(string definitionFile, string nestSourceFolder)
		{
			var sourceDirectory = new CSharpSourceDirectory(nestSourceFolder);
			var typeInfoProvider = new CsharpTypeInfoProvider();
			var restSpec = new RestSpec(nestSourceFolder);
			var scriptGenerator = new ClientTypescriptGenerator(typeInfoProvider, sourceDirectory, restSpec);
			var generator = new ClientTypesExporter(typeInfoProvider, scriptGenerator);
			File.WriteAllText(definitionFile, generator.Generate());
			GenerateLineScrubber.LineBasedHacks(definitionFile);
			GeneratePrependDefinitions.PrependDefinitions(definitionFile);
			return 0;
		}
	}
}