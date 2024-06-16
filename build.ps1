cd UniverseLib
.\build.ps1
cd ..

# ----------- Editor (mono) -----------
$Path1 = "Release/CinematicUnityExplorer.Standalone.Mono"
$Path2 = "UnityEditorPackage/Runtime"
Copy-Item $Path1/CinematicUnityExplorer.STANDALONE.Mono.dll -Destination $Path2
Copy-Item $Path1/UniverseLib.Mono.dll -Destination $Path2
Remove-Item Release/CinematicUnityExplorer.Editor.zip -ErrorAction SilentlyContinue
compress-archive .\UnityEditorPackage\*  Release/CinematicUnityExplorer.Editor.zip
