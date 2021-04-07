# Teronis.ModuleInitializer.AssemblyLoader[.[..]]

> :warning: This approach I cannot recommend anymore. Instead I highly recommend to use the new module initializer feature in C# 9.

_Side note: These projects were part of [Teronis.DotNet](https://github.com/teroneko/Teronis.DotNet), but have been outsourced because they got involved with the Windows Defender of Azure DevOps and led to random build failures._

## About

These projects provide the functionality to auto-load a third party assembly in a customer assembly at compile time.
This happens by injecting/extending a module initializer with its content making a call to `Assembly.Load`.

## Projects

### Teronis.ModuleInitializer.AssemblyLoader.MSBuild [![Nuget](https://img.shields.io/nuget/vpre/Teronis.ModuleInitializer.AssemblyLoader.MSBuild)](https://www.nuget.org/packages/Teronis.ModuleInitializer.AssemblyLoader.MSBuild)

This projects is a wrapper for Teronis.ModuleInitializer.AssemblyLoader.Executable and serves as dependency for libaries that want to be injected by consumers.

### Teronis.ModuleInitializer.AssemblyLoader.Executable [![Nuget](https://img.shields.io/nuget/vpre/Teronis.ModuleInitializer.AssemblyLoader.Executable)](https://www.nuget.org/packages/Teronis.ModuleInitializer.AssemblyLoader.Executable)

Provides an executable that can be run to inject the assembly load related module initializer.

### Teronis.ModuleInitializer.AssemblyLoader [![Nuget](https://img.shields.io/nuget/vpre/Teronis.ModuleInitializer.AssemblyLoader)](https://www.nuget.org/packages/Teronis.ModuleInitializer.AssemblyLoader)

Provides the core logic to inject the assembly load related module initializer.

__Commonly Used Types:__
<br />AssemblyInitializerInjector

## Usage

Imagine you have a library called `Teronis.NetCoreApp.Identity.Bearer` and you want to force the consumer to auto-load the assembly of that library, then all you need to do is:

1\. Create a [build file](https://docs.microsoft.com/de-de/nuget/create-packages/creating-a-package#include-msbuild-props-and-targets-in-a-package) called `Teronis.NetCoreApp.Identity.Bearer.props` with following content:

```
<Project>

  <Target Name="_SetIdentityBearerModuleInitializerAssemblyLoaderInjectionTargetAssembly" BeforeTargets="BeforeCompile">    
    <ItemGroup>
      <ModuleInitializerAssemblyLoaderInjectionTargetAssemblies Include="@(IntermediateAssembly->'%(FullPath)')">
        <SourceAssembly>$(MSBuildThisFileDirectory)..\<path-to-dll></SourceAssembly>
      </ModuleInitializerAssemblyLoaderInjectionTargetAssemblies>
    </ItemGroup>
  </Target>
  
</Project>
```

This file can be tracked in package as following:

```
<ItemGroup>
  <None Include="build\*.props;build\*.targets" Pack="true" PackagePath="build" />
</ItemGroup>
```

2\. Add the Teronis.ModuleInitializer.AssemblyLoader.MSBuild as package dependency to your library.

```
<ItemGroup>
  <PackageReference Include="Teronis.ModuleInitializer.AssemblyLoader.MSBuild" Version="<version>" />
</ItemGroup>
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Thanks to

@Coen Goedegebure (https://www.coengoedegebure.com/module-initializers-in-dotnet/)
<br />@Adriano Carlos Verona (https://github.com/adrianoc/cecilifier)
<br />@Simon Cropp (https://github.com/Fody/ModuleInit)

for teaching me.