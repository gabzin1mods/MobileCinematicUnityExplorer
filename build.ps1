cd UniverseLib
.\build.ps1
cd ..

# ----------- Standalone Mono -----------
dotnet build src/CinematicUnityExplorer.sln -c Release_STANDALONE_Mono
$Path = "Release/CinematicUnityExplorer.Standalone.Mono"
# ILRepack
lib/ILRepack.exe /target:library /lib:lib/net35 /lib:$Path /internalize /out:$Path/CinematicUnityExplorer.Standalone.Mono.dll $Path/CinematicUnityExplorer.Standalone.Mono.dll $Path/mcs.dll $Path/Tomlet.dll
# (cleanup and move files)
Remove-Item $Path/Tomlet.dll
Remove-Item $Path/mcs.dll
Remove-Item $Path/../CinematicUnityExplorer.Standalone.Mono.zip -ErrorAction SilentlyContinue
compress-archive .\$Path\* $Path/../CinematicUnityExplorer.Standalone.Mono.zip


# ----------- Editor (mono) -----------
$Path1 = "Release/CinematicUnityExplorer.Standalone.Mono"
$Path2 = "UnityEditorPackage/Runtime"
Copy-Item $Path1/CinematicUnityExplorer.STANDALONE.Mono.dll -Destination $Path2
Copy-Item $Path1/UniverseLib.Mono.dll -Destination $Path2
Remove-Item Release/CinematicUnityExplorer.Editor.zip -ErrorAction SilentlyContinue
compress-archive .\UnityEditorPackage\*  Release/CinematicUnityExplorer.Editor.zip
