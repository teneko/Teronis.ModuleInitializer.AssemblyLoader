﻿// Copyright (c) Teroneko.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MethodBody = Mono.Cecil.Cil.MethodBody;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using Mono.Cecil.Pdb;
using Mono.Cecil.Rocks;
using System.IO;
using Teronis.ModuleInitializer.AssemblyLoader.Utils;
using Teronis.ModuleInitializer.AssemblyLoader.Extensions;
using Teronis.ModuleInitializer.AssemblyLoader;

namespace Teronis.ModuleInitializer.AssemblyLoader
{
    public class AssemblyLoaderInjector
    {
        private const string cctorName = ".cctor";
        private const MethodAttributes cctorAttributes = MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;

        public static AssemblyLoaderInjector Default = new AssemblyLoaderInjector();

        private static TypeDefinition findModuleClass(ModuleDefinition moduleDefinition) =>
            moduleDefinition.Types.FirstOrDefault(x => x.Name == "<Module>")
            ?? throw new InjectionException("The module does not contain a module class.");

        private static MethodDefinition findOrCreateCctor(TypeDefinition moduleClass, TypeSystem typeSystem)
        {
            var cctor = moduleClass.Methods.FirstOrDefault(x => x.Name == cctorName);

            if (cctor == null) {
                cctor = new MethodDefinition(cctorName, cctorAttributes, typeSystem.Void);
                cctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                moduleClass.Methods.Add(cctor);
            }

            return cctor;
        }

        private static void injectAssemblyLoad(MethodBody methodBody, string sourceAssemblyPath, Func<MethodDefinition, MethodReference> resolveMethod)
        {
            methodBody.SimplifyMacros();

            var returnInstructions = methodBody.Instructions
                .Where(x => x.OpCode == OpCodes.Ret)
                .ToList();

            using var sourceAssembly = AssemblyPathUtils.ReadAssemblyFromPath(sourceAssemblyPath, false);
            var sourceAssemblyMainModule = sourceAssembly.MainModule;

            var foundMethodInitializerMethods = ModuleDefinitionUtils.FindModuleInitializerMethods(sourceAssemblyMainModule)
                .Select(resolveMethod);

            foreach (var instruction in returnInstructions) {
                var instructions = new List<Instruction>(foundMethodInitializerMethods.Select(x => Instruction.Create(OpCodes.Call, x))) {
                    Instruction.Create(OpCodes.Ret)
                };

                methodBody.Instructions.Replace(instruction, instructions);
            }

            methodBody.OptimizeMacros();
        }

        private static void writeTargetInjectionAssembly(AssemblyDefinition assembly, string assemblyPath, string? keyFile, bool writeSymbols)
        {
            var writeParams = new WriterParameters();

            if (writeSymbols) {
                writeParams.WriteSymbols = true;
                writeParams.SymbolWriterProvider = new PdbWriterProvider();
            }

            if (keyFile != null) {
                writeParams.StrongNameKeyPair = new StrongNameKeyPair(File.ReadAllBytes(keyFile));
            }

            assembly.Write(assemblyPath, writeParams);
        }

        public void InjectAssemblyInitializer(string injectionTargetAssemblyPath, string sourceAssemblyPath, string? keyFile)
        {
            injectionTargetAssemblyPath = injectionTargetAssemblyPath
                ?? throw new ArgumentNullException(nameof(injectionTargetAssemblyPath));

            sourceAssemblyPath = sourceAssemblyPath
                ?? throw new ArgumentNullException(nameof(sourceAssemblyPath));

            var injectionTargetAssemblyCopyPath = Path.GetTempFileName();

            void deleteInjectionTargetAssemblyCopyFile() =>
                File.Delete(injectionTargetAssemblyCopyPath);

            try {
                // Copy original injection target assembly to temporary assembly path.
                File.Copy(injectionTargetAssemblyPath, injectionTargetAssemblyCopyPath, true);
                var assemblyPdbFilePath = AssemblyPathUtils.GetPdbFilePathOrDefault(injectionTargetAssemblyCopyPath);
                var assemblyPdbFilePathIsExisting = !string.IsNullOrEmpty(assemblyPdbFilePath);

                var injectionTargetAssembly = AssemblyPathUtils.ReadAssemblyFromPath(injectionTargetAssemblyCopyPath,
                    readSymbols: assemblyPdbFilePathIsExisting);

                using var injectionTargetAssemblyMainModule = injectionTargetAssembly.MainModule;
                var injectionTargetAssemblyMainModuleClass = findModuleClass(injectionTargetAssemblyMainModule);
                var injectionTargetAssemblyMainModuleClassCctor = findOrCreateCctor(injectionTargetAssemblyMainModuleClass, injectionTargetAssemblyMainModule.TypeSystem);
                var injectionTargetAssemblyMainModuleClassCctorBody = injectionTargetAssemblyMainModuleClassCctor.Body;

                injectAssemblyLoad(injectionTargetAssemblyMainModuleClassCctorBody,
                    sourceAssemblyPath,
                    injectionTargetAssemblyMainModule.ImportReference);

                // AssemblyDefinition.Write(..) behaviour is to overwrite path if existing.
                writeTargetInjectionAssembly(injectionTargetAssembly, injectionTargetAssemblyPath, keyFile,
                    writeSymbols: assemblyPdbFilePathIsExisting);

                injectionTargetAssembly.Dispose();
                deleteInjectionTargetAssemblyCopyFile();
            } catch (Exception error) {
                try {
                    deleteInjectionTargetAssemblyCopyFile();
                } catch (Exception deleteFileError) {
                    throw new AggregateException(error, deleteFileError);
                }

                throw error;
            }
        }
    }
}
