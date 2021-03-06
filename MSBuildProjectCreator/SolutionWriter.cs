﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using JustDecompile.Tools.MSBuildProjectBuilder.FilePathsServices;
using System.IO;
using Mono.Cecil.AssemblyResolver;
using Telerik.JustDecompiler.Languages;

namespace JustDecompile.Tools.MSBuildProjectBuilder
{
	internal class SolutionWriter
	{
		private readonly AssemblyDefinition assembly;
		private readonly string targetDir;
		private readonly string solutionFileName;
		private readonly Dictionary<ModuleDefinition, string> modulesProjectsRelativePaths;
		private readonly Dictionary<ModuleDefinition, Guid> modulesProjectsGuids;
		private readonly Guid languageGuid;
        private readonly TargetPlatform targetPlatform;
        private readonly VisualStudioVersion visualStudioVersion;

		internal SolutionWriter(AssemblyDefinition assembly, TargetPlatform targetPlatform, string targetDir, string solutionFileName, 
			Dictionary<ModuleDefinition, string> modulesProjectsRelativePaths, Dictionary<ModuleDefinition, Guid> modulesProjectsGuids,
            VisualStudioVersion visualStudioVersion, ILanguage language)
		{
			this.assembly = assembly;
            this.targetPlatform = targetPlatform;
			this.targetDir = targetDir;
			this.solutionFileName = solutionFileName;
			this.modulesProjectsRelativePaths = modulesProjectsRelativePaths;
			this.modulesProjectsGuids = modulesProjectsGuids;
            if (language is Telerik.JustDecompiler.Languages.CSharp.CSharp)
            {
                this.languageGuid = new Guid(WinRTProjectBuilder.CSharpGUID);
            }
            else if (language is Telerik.JustDecompiler.Languages.VisualBasic.VisualBasic)
            {
                this.languageGuid = new Guid(WinRTProjectBuilder.VisualBasicGUID);
            }
            else
            {
                throw new NotSupportedException();
            }

            this.visualStudioVersion = visualStudioVersion;
		}
		
		public void WriteSolutionFile()
		{
			string solutionPath = Path.Combine(targetDir, solutionFileName);

			using (StreamWriter writer = new StreamWriter(solutionPath))
			{
                string formatVersion = string.Empty;
                if (this.visualStudioVersion == VisualStudioVersion.VS2010)
                {
                    formatVersion = "11.00";
                }
                else if (this.visualStudioVersion == VisualStudioVersion.VS2012 ||
                         this.visualStudioVersion == VisualStudioVersion.VS2013 ||
                         this.visualStudioVersion == VisualStudioVersion.VS2015)
                {
                    formatVersion = "12.00";
                }
                else
                {
                    throw new NotImplementedException();
                }

                writer.WriteLine("Microsoft Visual Studio Solution File, Format Version {0}", formatVersion);

                string visualStudioVersionString;
                if (this.visualStudioVersion == VisualStudioVersion.VS2015)
                {
                    visualStudioVersionString = "14";
                }
                else
                {
                    visualStudioVersionString = this.visualStudioVersion.ToFriendlyString();
                }

                writer.WriteLine("# Visual Studio " + visualStudioVersionString);

                if (this.visualStudioVersion == VisualStudioVersion.VS2013 ||
                    this.visualStudioVersion == VisualStudioVersion.VS2015)
                {
                    string visualStudioLongVersionString = string.Empty;
                    if (this.visualStudioVersion == VisualStudioVersion.VS2013)
                    {
                        visualStudioLongVersionString = "12.0.21005.1";
                    }
                    else
                    {
                        visualStudioLongVersionString = "14.0.24720.0";
                    }
                    
                    writer.WriteLine("VisualStudioVersion = " + visualStudioLongVersionString);
                    writer.WriteLine("MinimumVisualStudioVersion = 10.0.40219.1");
                }

				foreach (ModuleDefinition module in this.assembly.Modules)
				{
					WriteProjectInfo(module, writer);
				}

				writer.WriteLine("Global");

				WriteSolutionConfigurationPlatforms(writer);

                WriteProjectConfigurationPlatforms(writer);

				WriteSolutionProperties(writer);

				writer.WriteLine("EndGlobal");
			}
		}

		private void WriteSolutionConfigurationPlatforms(StreamWriter writer)
		{
            writer.WriteLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
            this.WriteSolutionConfigurations(writer);
			writer.WriteLine("\tEndGlobalSection");
		}

        protected virtual void WriteSolutionConfigurations(StreamWriter writer)
        {
            string mainModuleArchitecture = Utilities.GetModuleArchitecturePropertyValue(this.assembly.MainModule, true);

            writer.WriteLine("\t\tDebug|" + mainModuleArchitecture + " = Debug|" + mainModuleArchitecture);
            writer.WriteLine("\t\tRelease|" + mainModuleArchitecture + " = Release|" + mainModuleArchitecture);
        }

        private void WriteProjectConfigurationPlatforms(StreamWriter writer)
		{
			writer.WriteLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");

			foreach (ModuleDefinition module in this.assembly.Modules)
			{
				Guid moduleProjectGuid;
				if (!modulesProjectsGuids.TryGetValue(module, out moduleProjectGuid))
				{
					throw new Exception("Module project guid not found in modules projects guid map.");
				}

				string moduleProjectGuidString = moduleProjectGuid.ToString().ToUpper();
                this.WriteProjectConfigurations(writer, module, moduleProjectGuidString);
			}

			writer.WriteLine("\tEndGlobalSection");
		}

        protected virtual void WriteProjectConfigurations(StreamWriter writer, ModuleDefinition module, string moduleProjectGuidString)
        {
            string mainModuleArchitecture = Utilities.GetModuleArchitecturePropertyValue(this.assembly.MainModule, true);
            string moduleArchitecture = Utilities.GetModuleArchitecturePropertyValue(module, true);

            writer.WriteLine("\t\t{" + moduleProjectGuidString + "}.Debug|" + mainModuleArchitecture + ".ActiveCfg = Debug|" + moduleArchitecture);
            writer.WriteLine("\t\t{" + moduleProjectGuidString + "}.Debug|" + mainModuleArchitecture + ".Build.0 = Debug|" + moduleArchitecture);

            writer.WriteLine("\t\t{" + moduleProjectGuidString + "}.Release|" + mainModuleArchitecture + ".ActiveCfg = Release|" + moduleArchitecture);
            writer.WriteLine("\t\t{" + moduleProjectGuidString + "}.Release|" + mainModuleArchitecture + ".Build.0 = Release|" + moduleArchitecture);
        }

		private void WriteSolutionProperties(StreamWriter writer)
		{
			writer.WriteLine("\tGlobalSection(SolutionProperties) = preSolution");
			writer.WriteLine("\t\tHideSolutionNode = FALSE");
			writer.WriteLine("\tEndGlobalSection");
		}

		private string GetModuleName(ModuleDefinition module)
		{
			return Utilities.IsMainModule(module) ? this.assembly.Name.Name : Utilities.GetNetmoduleName(module);
		}

		private void WriteMainModuleDependencies(StreamWriter writer)
		{
			foreach (ModuleDefinition module in this.assembly.Modules)
			{
				if (!Utilities.IsMainModule(module))
				{
					Guid moduleProjectGuid;
					if (!modulesProjectsGuids.TryGetValue(module, out moduleProjectGuid))
					{
						throw new Exception("Module project guid not found in modules projects guid map.");
					}

					string moduleProjectGuidString = moduleProjectGuid.ToString().ToUpper();

					writer.WriteLine("\tProjectSection(ProjectDependencies) = postProject");
					writer.WriteLine("\t\t{" + moduleProjectGuidString + "} = {" + moduleProjectGuidString + "}");
					writer.WriteLine("\tEndProjectSection");
				}
			}
		}

		private void WriteProjectInfo(ModuleDefinition module, StreamWriter writer)
		{
			string moduleName = GetModuleName(module);
			
			string moduleRelativeFilePath;
			if (!modulesProjectsRelativePaths.TryGetValue(module, out moduleRelativeFilePath))
			{
				throw new Exception("Module project file path not found in modules projects filepaths map.");
			}

			Guid moduleProjectGuid;
			if (!modulesProjectsGuids.TryGetValue(module, out moduleProjectGuid))
			{
				throw new Exception("Module project guid not found in modules projects guid map.");
			}

			string moduleProjectGuidString = moduleProjectGuid.ToString().ToUpper();

			writer.WriteLine("Project(\"{" + languageGuid.ToString().ToUpper() + "}\") = \"" + moduleName + "\", \"" + moduleRelativeFilePath + "\", \"{" + moduleProjectGuidString + "}\"");

			if (Utilities.IsMainModule(module))
			{
				WriteMainModuleDependencies(writer);
			}

			writer.WriteLine("EndProject");
		}
	}
}
