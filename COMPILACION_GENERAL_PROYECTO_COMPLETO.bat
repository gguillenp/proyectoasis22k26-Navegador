@echo off
setlocal EnableExtensions DisableDelayedExpansion
:: Compilador autonomo: conservar este BAT dentro del repositorio completo.
:: No necesita ejecutar otros BAT ni editar rutas de usuario.
set "SEGURIDAD_ROOT=%~dp0"
set "SEGURIDAD_SELF=%~f0"
set "SEGURIDAD_NAME=%~nx0"
set "AUTO=0"
set "LISTA_PS="
set "AYUDAS_PS="
set "MODO_PS="
set "COLOR_PS="
set "HILOS_PS="

:ARGUMENTOS
if "%~1"=="" goto COMPILAR
if /i "%~1"=="/auto" (
    set "AUTO=1"
    shift
    goto ARGUMENTOS
)
if /i "%~1"=="/listar" (
    set "LISTA_PS=-SoloListar"
    set "AUTO=1"
    shift
    goto ARGUMENTOS
)
if /i "%~1"=="/rebuild" (
    set "MODO_PS=-RecompilarTodo"
    shift
    goto ARGUMENTOS
)
if /i "%~1"=="/sin-color" (
    set "COLOR_PS=-SinColor"
    shift
    goto ARGUMENTOS
)
if /i "%~1"=="/secuencial" (
    set "HILOS_PS=-Secuencial"
    shift
    goto ARGUMENTOS
)
if /i "%~1"=="/con-ayudas" (
    set "AYUDAS_PS=-CompilarAyudas"
    shift
    goto ARGUMENTOS
)
if /i "%~1"=="/sin-ayudas" (
    set "AYUDAS_PS="
    shift
    goto ARGUMENTOS
)
if /i "%~1"=="/?" goto AYUDA
echo [ERROR] Opcion desconocida: "%~1"
set "BUILD_EXIT=1"
goto FIN

:COMPILAR
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -Command "$texto=[IO.File]::ReadAllText($env:SEGURIDAD_SELF); $marca=[regex]::Match($texto,'(?m)^:__SEGURIDAD_POWERSHELL__\r?$'); if(-not $marca.Success){Write-Error 'Falta el compilador incluido en este BAT.';exit 1}; & ([scriptblock]::Create($texto.Substring($marca.Index+$marca.Length))) -RepoRoot $env:SEGURIDAD_ROOT %LISTA_PS% %AYUDAS_PS% %MODO_PS% %COLOR_PS% %HILOS_PS%"
set "BUILD_EXIT=%ERRORLEVEL%"
goto FIN

:AYUDA
echo Uso: %SEGURIDAD_NAME% [/auto] [/listar] [/rebuild] [/secuencial] [/sin-color] [/con-ayudas ^| /sin-ayudas]
echo.
echo Compila todos los componentes y modulos de codigo en Debug.
echo Sin opciones: compilacion incremental y pausa al terminar.
echo /auto        Termina sin pausar. Codigo 0: correcto; 1: error.
echo /listar      Muestra los proyectos sin ejecutar MSBuild ni modificar ayudas.
echo /rebuild     Restaura paquetes y recompila todos los proyectos desde cero.
echo /secuencial  Compila un proyecto a la vez en lugar de usar todos los nucleos.
echo /sin-color   Usa texto simple, sin colores ni barra interactiva.
echo /con-ayudas  Compila tambien los archivos HHP con HTML Help Workshop.
echo /sin-ayudas  Conserva las ayudas existentes. Es el modo predeterminado.
echo.
echo Compila por lotes en paralelo y omite los proyectos sin cambios.
echo No crea unidades de disco. Solo si Windows no tiene activadas las rutas
echo largas usa un enlace de carpeta temporal de ruta corta y lo quita al terminar.
echo Busca el repositorio desde la ubicacion del BAT, sin rutas de usuario fijas.
echo Requiere Windows, Visual Studio 2022 o Build Tools actualizado con
echo desarrollo de escritorio .NET, .NET Framework 4.8 y SDK .NET 8 o posterior
echo compatible con MSBuild. NuGet puede necesitar Internet.
echo Variables opcionales: MSBUILD_MANUAL, HHC_MANUAL y COMPILACION_NUCLEOS
echo (cuantos proyectos compilar a la vez; por defecto, todos los nucleos).
set "BUILD_EXIT=0"
set "AUTO=1"

:FIN
if "%AUTO%"=="0" pause
endlocal & exit /b %BUILD_EXIT%

:__SEGURIDAD_POWERSHELL__
param(
    [switch]$SoloListar,
    [switch]$RecompilarTodo,
    [switch]$SinColor,
    [switch]$CompilarAyudas,
    [switch]$Secuencial,
    [string]$RepoRoot
)

# Coordinador compatible con Windows PowerShell 5.1.
# Compila por lotes: cada lote reune los proyectos cuyas dependencias ya estan listas
# y se entrega a una sola llamada de MSBuild en paralelo. Los proyectos sin cambios
# desde su ultima compilacion correcta no se vuelven a compilar.
$ErrorActionPreference = 'Stop'
$repo = $null
$script:globalLog = $null
$script:logWriter = $null
$script:failedFile = $null
$script:compileAlias = $null
$script:referenceOverrides = New-Object 'System.Collections.Generic.List[object]'
$script:restoreForImports = $false
$script:batFiles = New-Object 'System.Collections.Generic.List[string]'
$script:solutionCache = @{}
$script:legacyRelease = $false
$script:resourceTimes = @{}
$script:outputTimes = New-Object 'System.Collections.Generic.Dictionary[string,object]' ([StringComparer]::OrdinalIgnoreCase)
$script:cycles = New-Object 'System.Collections.Generic.List[object]'
$script:timer = [Diagnostics.Stopwatch]::StartNew()
$script:useColor = -not $SinColor -and -not [Console]::IsOutputRedirected -and -not $env:NO_COLOR
$script:skipFolders = '^(bin|obj|packages|logs|node_modules|\.git|\.vs)$'
$script:pivotName = 'CapaVista_Seguridad'
$utf8 = New-Object Text.UTF8Encoding($false)

function Get-RepositoryRoot([string]$StartDirectory) {
    if ([string]::IsNullOrWhiteSpace($StartDirectory)) { throw 'No se recibio la ubicacion del BAT.' }
    $cursor = Get-Item -LiteralPath $StartDirectory
    while ($cursor) {
        if ((Test-Path -LiteralPath (Join-Path $cursor.FullName 'codigo\componentes') -PathType Container) -and
            (Test-Path -LiteralPath (Join-Path $cursor.FullName 'codigo\modulos') -PathType Container)) {
            return $cursor.FullName.TrimEnd('\', '/')
        }
        $cursor = $cursor.Parent
    }
    throw 'No se encontro el repositorio: faltan codigo\componentes y codigo\modulos. Coloca este BAT dentro de una copia completa del proyecto.'
}

function Write-Status([string]$Message, [ConsoleColor]$Color = [ConsoleColor]::Gray) {
    if ($Message -match '\[ERROR') { $Color = [ConsoleColor]::Red }
    elseif ($Message -match '\[(AVISO|BLOQUEADO|SIN PROYECTOS)') { $Color = [ConsoleColor]::Yellow }
    elseif ($Message -match '\[OK\]') { $Color = [ConsoleColor]::Green }
    if ($script:useColor) { Write-Host $Message -ForegroundColor $Color }
    else { Write-Host $Message }
    if ($script:logWriter) { $script:logWriter.WriteLine($Message) }
}

function Add-GlobalLog([string]$Text) {
    # El registro general se mantiene abierto: reabrirlo en cada linea cuesta varios segundos.
    if ($script:logWriter -and $Text) { $script:logWriter.WriteLine($Text) }
}

function Add-GlobalLogFile([string]$Path) {
    if ([IO.File]::Exists($Path)) { Add-GlobalLog ([IO.File]::ReadAllText($Path)) }
}

function Write-Header([int]$Count) {
    $mode = 'Incremental'
    if ($RecompilarTodo) { $mode = 'Recompilacion completa' }
    Write-Status ''
    Write-Status '  ================================================================' Cyan
    Write-Status '       SEGURIDAD  |  COMPILADOR DEL PROYECTO' Cyan
    Write-Status '  ================================================================' Cyan
    Write-Status ('  {0}  |  Debug  |  {1} proyectos' -f $mode, $Count) White
    Write-Status ''
}

function Write-ProjectResult($Project) {
    $label = 'OK'
    if ($Project.State -eq 'Error') { $label = 'ERROR' }
    elseif ($Project.State -eq 'Bloqueado') { $label = 'BLOQUEADO' }
    $script:finished++
    $percent = [int][Math]::Floor(100 * $script:finished / $script:total)
    $note = ''
    if ($Project.State -eq 'Correcto' -and $Project.Mode -eq 'Sin cambios') { $note = '  sin cambios' }
    Write-Status ('[{0}] {1,3}/{2} {3,3}%  {4} > {5}  ({6:N1}s){7}' -f
        $label, $Project.Index, $script:total, $percent, $Project.Group, $Project.Name, $Project.Duration, $note)
    if ($script:useColor) {
        Write-Progress -Activity 'Compilando componentes y modulos' -Status ($Project.Group + ' > ' + $Project.Name) -PercentComplete ([Math]::Min(100, $percent))
    }
}

function Write-BuildSummary($Plan, [string]$Directory) {
    $ok = @($Plan | Where-Object State -eq 'Correcto').Count
    $errors = @($Plan | Where-Object State -eq 'Error').Count
    $blocked = @($Plan | Where-Object State -eq 'Bloqueado').Count
    $skipped = @($Plan | Where-Object { $_.State -eq 'Correcto' -and $_.Mode -eq 'Sin cambios' }).Count
    Write-Status ''
    Write-Status '  ================================================================' Cyan
    Write-Status ('  RESULTADO   {0}/{1} correctos | {2} errores | {3} bloqueados' -f $ok, $Plan.Count, $errors, $blocked) White
    Write-Status ('  COMPILADOS  {0} | SIN CAMBIOS {1}' -f ($ok - $skipped), $skipped) White
    Write-Status ('  DURACION    {0:00}:{1:00}:{2:00}' -f [Math]::Floor($script:timer.Elapsed.TotalHours), $script:timer.Elapsed.Minutes, $script:timer.Elapsed.Seconds) White
    Write-Status '  ----------------------------------------------------------------' DarkGray
    Write-Status ('  {0,-27} {1,6} {2,7} {3,10}' -f 'COMPONENTE / MODULO', 'OK', 'ERROR', 'BLOQUEADO')
    foreach ($group in $Plan | Group-Object Group | Sort-Object Name) {
        $correct = @($group.Group | Where-Object State -eq 'Correcto').Count
        $failed = @($group.Group | Where-Object State -eq 'Error').Count
        $waiting = @($group.Group | Where-Object State -eq 'Bloqueado').Count
        Write-Status ('  {0,-27} {1,6} {2,7} {3,10}' -f $group.Name, $correct, $failed, $waiting)
    }
    Write-Status '  ================================================================' Cyan
    Write-Status ''
    Write-Status "Registros: $Directory"
    Write-Status 'Detalle: resultados.csv | resumen_modulos.csv | fallidos.txt'
}

function Remove-UntrackedVentasPackages([string]$RepositoryRoot) {
    # NuGet restaura packages.config dentro del repo. Buscar todas las carpetas
    # packages de Ventas porque la estructura puede variar entre clones.
    $ventasRoot = Join-Path $RepositoryRoot 'codigo\modulos\ventas'
    if (-not (Test-Path -LiteralPath $ventasRoot -PathType Container)) {
        Write-Status '[AVISO] No se encontro el area de Ventas; no se limpiaron paquetes.'
        return
    }
    $git = Get-Command git -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $git) { Write-Status '[AVISO] Git no esta disponible; se conservan los paquetes locales de Ventas.'; return }
    $rootLength = $RepositoryRoot.TrimEnd([char]92).Length
    $packageRoots = New-Object 'System.Collections.Generic.List[string]'
    $pendingDirectories = New-Object 'System.Collections.Generic.Queue[string]'
    $pendingDirectories.Enqueue($ventasRoot)
    while ($pendingDirectories.Count -gt 0) {
        $currentDirectory = $pendingDirectories.Dequeue()
        foreach ($directory in Get-ChildItem -LiteralPath $currentDirectory -Directory -Force) {
            if ($directory.Attributes -band [IO.FileAttributes]::ReparsePoint) { continue }
            if ($directory.Name -ieq 'packages') {
                $packageRoots.Add($directory.FullName)
                continue
            }
            if ($directory.Name -match '^(bin|obj|logs|node_modules|\.git|\.vs)$') { continue }
            $pendingDirectories.Enqueue($directory.FullName)
        }
    }
    if ($packageRoots.Count -eq 0) {
        Write-Status '[LIMPIEZA] No se encontraron carpetas packages dentro de Ventas.'
        return
    }
    foreach ($packageRoot in $packageRoots) {
        $relativeRoot = $packageRoot.Substring($rootLength).TrimStart([char]92).Replace([char]92, [char]47)
        $tracked = @(& $git.Source -C $RepositoryRoot ls-files -- $relativeRoot 2>$null)
        if ($LASTEXITCODE -ne 0) {
            Write-Status "[AVISO] No se pudo consultar Git para $relativeRoot; se conserva."
            continue
        }
        $trackedSet = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
        foreach ($path in $tracked) { [void]$trackedSet.Add($path) }
        $removed = 0
        foreach ($file in Get-ChildItem -LiteralPath $packageRoot -File -Recurse -Force) {
            $relative = $file.FullName.Substring($rootLength).TrimStart([char]92).Replace([char]92, [char]47)
            if (-not $trackedSet.Contains($relative)) {
                try {
                    Remove-Item -LiteralPath $file.FullName -Force -ErrorAction Stop
                    $removed++
                } catch {
                    Write-Status "[AVISO] No se pudo retirar $($file.FullName): $($_.Exception.Message)"
                }
            }
        }
        Get-ChildItem -LiteralPath $packageRoot -Directory -Recurse -Force |
            Sort-Object { $_.FullName.Length } -Descending |
            Where-Object { -not (Get-ChildItem -LiteralPath $_.FullName -Force | Select-Object -First 1) } |
            ForEach-Object { Remove-Item -LiteralPath $_.FullName -Force -ErrorAction SilentlyContinue }
        if ($removed -gt 0) { Write-Status "[LIMPIEZA] $removed archivo(s) no versionado(s) retirado(s) de $relativeRoot." }
        else { Write-Status "[LIMPIEZA] Sin archivos no versionados en $relativeRoot." }
    }
}

function Get-SourceProjects([string]$Directory) {
    # Podar paquetes y salidas antes de recorrerlos; no seguir enlaces.
    $queue = New-Object 'System.Collections.Generic.Queue[string]'
    $queue.Enqueue($Directory)
    while ($queue.Count -gt 0) {
        foreach ($entry in (New-Object IO.DirectoryInfo($queue.Dequeue())).EnumerateFileSystemInfos()) {
            if ($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) { continue }
            if ($entry.Attributes -band [IO.FileAttributes]::Directory) {
                if ($entry.Name -notmatch $script:skipFolders) { $queue.Enqueue($entry.FullName) }
            } elseif ($entry.Name -like '*.csproj') { $entry }
            elseif ($entry.Name -like '*.bat') { $script:batFiles.Add($entry.FullName) }
            elseif ($entry.Name -match '^Directory\.Build\.(props|targets)$') { $script:restoreForImports = $true }
        }
    }
}

function Expand-ProjectPath([string]$Path, [string]$Directory) {
    return $Path.Replace('$(Configuration)', 'Debug').Replace('$(Platform)', 'AnyCPU').Replace('$(MSBuildProjectDirectory)', $Directory)
}

function Get-ProjectInfo([IO.FileInfo]$File) {
    # Una sola pasada por el XML del proyecto.
    $xml = New-Object Xml.XmlDocument
    $xml.Load($File.FullName)
    $root = $xml.DocumentElement
    $assemblyName = $null; $type = $null; $framework = $null
    $isSdk = $root.HasAttribute('Sdk')
    $hasPackageReference = $false
    $unknownImport = $false
    $gathers = $false
    $projectReferences = New-Object 'System.Collections.Generic.List[string]'
    $references = New-Object 'System.Collections.Generic.List[object]'
    # Archivos enlazados desde fuera de la carpeta: la revision de "sin cambios" debe verlos.
    $linked = New-Object 'System.Collections.Generic.List[string]'
    $opaque = $false
    $prefix = $File.DirectoryName.TrimEnd('\') + '\'
    # Los filtros XPath dejan en PowerShell solo los nodos que interesan.
    foreach ($node in $root.SelectNodes("*[local-name()='PropertyGroup']/*[local-name()='AssemblyName' or local-name()='OutputType' or local-name()='TargetFramework']")) {
        $name = $node.LocalName
        if ($name -eq 'AssemblyName') { if ($null -eq $assemblyName) { $assemblyName = $node.InnerText.Trim() } }
        elseif ($name -eq 'OutputType') { if ($null -eq $type) { $type = $node.InnerText.Trim() } }
        elseif ($null -eq $framework) { $framework = $node.InnerText.Trim() }
    }
    foreach ($node in $xml.SelectNodes("//*[local-name()='Reference' or local-name()='ProjectReference' or local-name()='PackageReference' or local-name()='Import' or local-name()='Sdk' or local-name()='MSBuild']")) {
        $name = $node.LocalName
        if ($name -eq 'Reference') {
            if ($node.ParentNode.LocalName -ne 'ItemGroup') { continue }
            $hint = $null
            foreach ($child in $node.ChildNodes) { if ($child.LocalName -eq 'HintPath') { $hint = $child.InnerText; break } }
            if ($hint -and $hint -match '(?i)[\\/]bin[\\/]Release[\\/]') { $script:legacyRelease = $true }
            $identity = $node.GetAttribute('Include')
            $references.Add(@($identity, $identity.Split(',')[0].Trim(), $hint))
        } elseif ($name -eq 'ProjectReference') { $projectReferences.Add($node.GetAttribute('Include')) }
        elseif ($name -eq 'PackageReference') { $hasPackageReference = $true }
        elseif ($name -eq 'MSBuild') { if ($node.ParentNode.LocalName -eq 'Target') { $gathers = $true } }
        elseif ($name -eq 'Sdk') { $isSdk = $true }
        else {
            if ($node.HasAttribute('Sdk')) { $isSdk = $true }
            if ($node.HasAttribute('Project')) {
                # Los demas imports pueden introducir PackageReference indirectamente.
                $path = $node.GetAttribute('Project')
                $known = $path -match '^\$\(MSBuild(ExtensionsPath[^)]*|ToolsPath|BinPath|FrameworkToolsPath)\)[\\/]' -and
                    $path -match '[\\/]Microsoft\.(Common\.props|CSharp\.targets)$'
                if (-not $known) { $unknownImport = $true }
            }
        }
    }
    $outside = "//*[local-name()='ItemGroup']/*[(local-name()='Compile' or local-name()='EmbeddedResource' or local-name()='None' or local-name()='Content' or local-name()='Resource' or local-name()='Page' or local-name()='ApplicationDefinition' or local-name()='AdditionalFiles') and (contains(@Include,'..') or contains(@Include,'(') or contains(@Include,':') or contains(@Include,'%') or contains(@Include,'*') or contains(@Include,'?') or contains(@Include,';') or starts-with(@Include,'\') or starts-with(@Include,'/'))]"
    foreach ($node in $xml.SelectNodes($outside)) {
        $include = Expand-ProjectPath $node.GetAttribute('Include') $File.DirectoryName
        if ($include.IndexOf('$(') -ge 0 -or $include.IndexOf('@(') -ge 0 -or $include.IndexOf('%(') -ge 0) { $opaque = $true; continue }
        if ($include.IndexOfAny([char[]]'*?;') -ge 0) {
            if ($include.IndexOf('..') -ge 0 -or [IO.Path]::IsPathRooted($include)) { $opaque = $true }
            continue
        }
        try {
            $full = [IO.Path]::GetFullPath([IO.Path]::Combine($File.DirectoryName, [Uri]::UnescapeDataString($include)))
        } catch { $opaque = $true; continue }
        if (-not $full.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { $linked.Add($full) }
    }
    if (-not $assemblyName) { $assemblyName = $File.BaseName }
    if (-not $type) {
        $type = 'Library'
        if ($root.GetAttribute('Sdk') -match '\.Web$') { $type = 'Exe' }
    }
    $rank = 2
    if ($File.BaseName -like 'CapaModelo_*') { $rank = 0 }
    elseif ($File.BaseName -like 'CapaControlador_*') { $rank = 1 }
    elseif ($File.BaseName -like 'CapaVista_*') { $rank = 3 }
    if ($type -match '^(WinExe|Exe)$') { $rank = 4 }
    $needsRestore = [IO.File]::Exists((Join-Path $File.DirectoryName 'packages.config')) -or $isSdk -or $hasPackageReference -or $unknownImport
    $extension = '.dll'
    # Un ejecutable SDK para .NET Framework (net48) tambien genera .exe; para .NET moderno, .dll.
    if ($type -match '^(WinExe|Exe)$' -and (-not $isSdk -or $framework -match '^net4')) { $extension = '.exe' }
    $outputDirectory = Join-Path $File.DirectoryName 'bin\Debug'
    if ($isSdk -and $framework) { $outputDirectory = Join-Path $outputDirectory $framework }
    $relative = $File.FullName.Substring($repo.Length + 1)
    [pscustomobject]@{
        Path = $File.FullName; Directory = $File.DirectoryName
        Relative = $relative
        Name = $File.BaseName; Assembly = $assemblyName; Rank = $rank
        SortKey = ('{0}|{1}' -f $rank, $relative)
        Output = Join-Path $outputDirectory ($assemblyName + $extension)
        Group = (($relative -split '[\\/]')[1..2] -join '\')
        LastErrors = @(); ReferenceErrors = New-Object 'System.Collections.Generic.List[string]'
        State = 'Pendiente'; Attempts = 0; Duration = 0.0; Log = ''; Mode = ''; Index = 0
        SharedOutput = $false; SharedDirectory = $false; NeedsRestore = [bool]$needsRestore
        Sdk = [bool]($isSdk -or $hasPackageReference)
        ProjectReferences = $projectReferences; References = $references
        Linked = $linked; Opaque = $opaque; Force = $false; Cycle = $null; Gathers = $gathers
        Dependencies = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    }
}

function Test-DependsOn($Start, [string]$TargetPath, $ByPath) {
    # Indica si $Start llega a $TargetPath siguiendo dependencias.
    $seen = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    $stack = New-Object 'System.Collections.Generic.Stack[string]'
    $stack.Push($Start.Path)
    while ($stack.Count -gt 0) {
        $current = $stack.Pop()
        if (-not $seen.Add($current)) { continue }
        foreach ($dependency in $ByPath[$current].Dependencies) {
            if ($dependency -ieq $TargetPath) { return $true }
            $stack.Push($dependency)
        }
    }
    return $false
}

function Get-Cycles($Projects, $ByPath) {
    # Componentes fuertemente conexos (Tarjan, iterativo) con mas de un proyecto.
    $index = @{}; $low = @{}; $onStack = @{}
    $stack = New-Object 'System.Collections.Generic.Stack[string]'
    $result = New-Object 'System.Collections.Generic.List[object]'
    $counter = 0
    foreach ($root in $Projects) {
        if ($index.ContainsKey($root.Path)) { continue }
        $work = New-Object 'System.Collections.Generic.Stack[object]'
        $work.Push(@($root.Path, 0))
        while ($work.Count -gt 0) {
            $frame = $work.Pop()
            $node = [string]$frame[0]; $position = [int]$frame[1]
            if ($position -eq 0) {
                $index[$node] = $counter; $low[$node] = $counter; $counter++
                $stack.Push($node); $onStack[$node] = $true
            }
            $children = @($ByPath[$node].Dependencies)
            $descended = $false
            for ($i = $position; $i -lt $children.Count; $i++) {
                $child = [string]$children[$i]
                if (-not $index.ContainsKey($child)) {
                    $work.Push(@($node, ($i + 1)))
                    $work.Push(@($child, 0))
                    $descended = $true
                    break
                } elseif ($onStack[$child]) {
                    $low[$node] = [Math]::Min($low[$node], $index[$child])
                }
            }
            if ($descended) { continue }
            if ($low[$node] -eq $index[$node]) {
                $members = New-Object 'System.Collections.Generic.List[string]'
                do {
                    $member = $stack.Pop(); $onStack[$member] = $false
                    $members.Add($member)
                } while ($member -ne $node)
                if ($members.Count -gt 1) { $result.Add($members) }
            }
            if ($work.Count -gt 0) {
                $parent = [string]$work.Peek()[0]
                $low[$parent] = [Math]::Min($low[$parent], $low[$node])
            }
        }
    }
    return ,$result
}

function Get-BuildPlan($Projects) {
    $byPath = @{}
    $byAssembly = @{}
    foreach ($project in $Projects) {
        $byPath[$project.Path] = $project
        if (-not $byAssembly.ContainsKey($project.Assembly)) { $byAssembly[$project.Assembly] = New-Object 'System.Collections.Generic.List[object]' }
        $byAssembly[$project.Assembly].Add($project)
    }
    $neighbors = New-Object 'System.Collections.Generic.List[object]'
    foreach ($project in $Projects) {
        foreach ($raw in $project.ProjectReferences) {
            $include = Expand-ProjectPath $raw $project.Directory
            if ($include.IndexOf('$(') -ge 0) {
                $project.ReferenceErrors.Add("ProjectReference con propiedades no resueltas: $include")
                continue
            }
            $path = [IO.Path]::GetFullPath([IO.Path]::Combine($project.Directory, $include))
            if ($byPath.ContainsKey($path) -and $path -ne $project.Path) { [void]$project.Dependencies.Add($path) }
            else { $project.ReferenceErrors.Add("ProjectReference fuera del plan: $include") }
        }
        foreach ($reference in $project.References) {
            $name = $reference[1]; $hint = $reference[2]
            $internal = $byAssembly.ContainsKey($name)
            # La mayoria son ensamblados del sistema sin HintPath: no hay nada que resolver.
            if (-not $internal -and (-not $hint -or $hint.IndexOf('\bin\', [StringComparison]::OrdinalIgnoreCase) -lt 0)) { continue }
            $hintPath = $null
            if ($hint) {
                $hintText = Expand-ProjectPath $hint $project.Directory
                if ($hintText.IndexOf('$(') -lt 0) {
                    try { $hintPath = [IO.Path]::GetFullPath([IO.Path]::Combine($project.Directory, $hintText.Trim())) } catch { $hintPath = $null }
                }
            }
            if (-not $internal) {
                # DLL de terceros tomada del bin de otro proyecto: ese proyecto debe compilar antes.
                if ($hintPath) { $neighbors.Add(@($project, $hintPath)) }
                continue
            }
            $candidates = [Collections.Generic.List[object]]::new()
            foreach ($candidate in $byAssembly[$name]) { if ($candidate.Path -ne $project.Path) { $candidates.Add($candidate) } }
            $producers = [Collections.Generic.List[object]]::new()
            if ($hintPath) {
                foreach ($candidate in $candidates) {
                    if ($hintPath.StartsWith($candidate.Directory + '\bin\', [StringComparison]::OrdinalIgnoreCase)) { $producers.Add($candidate) }
                }
            }
            if ($producers.Count -eq 0 -and $candidates.Count -gt 0) {
                $single = $true
                foreach ($candidate in $candidates) { if ($candidate.Output -ine $candidates[0].Output) { $single = $false; break } }
                if ($single) { $producers = $candidates }
            }
            if ($producers.Count -eq 0) {
                $project.ReferenceErrors.Add("Referencia interna ambigua: $name. Revise HintPath.")
                continue
            }
            foreach ($dependency in $producers) { [void]$project.Dependencies.Add($dependency.Path) }
            $script:referenceOverrides.Add([pscustomobject]@{
                Project = $project.Path; Identity = $reference[0]; Output = $producers[0].Output
            })
        }
    }
    foreach ($group in $Projects | Group-Object Output | Where-Object Count -gt 1) {
        foreach ($project in $group.Group) { $project.SharedOutput = $true }
    }
    # Dependencias de orden que no vienen de una referencia directa; nunca deben crear un ciclo.
    foreach ($pair in $neighbors) {
        $consumer = $pair[0]; $hintPath = [string]$pair[1]
        if ([IO.File]::Exists($hintPath)) { continue }
        foreach ($owner in $Projects) {
            if ($owner.Path -eq $consumer.Path) { continue }
            if ($hintPath.StartsWith($owner.Directory + '\bin\', [StringComparison]::OrdinalIgnoreCase) -and
                -not (Test-DependsOn $owner $consumer.Path $byPath)) {
                [void]$consumer.Dependencies.Add($owner.Path)
            }
        }
    }
    # Un proyecto que llama a MSBuild sobre otros (el ejecutable de Ventas reune las DLL de su
    # modulo y de los componentes) espera a todas las bibliotecas de su modulo y de componentes.
    foreach ($project in $Projects) {
        if (-not $project.Gathers) { continue }
        foreach ($library in $Projects) {
            if ($library.Path -eq $project.Path -or $library.Rank -eq 4 -or $library.Name -notlike 'Capa*') { continue }
            if ($library.Group -ne $project.Group -and $library.Group -notlike 'componentes\*') { continue }
            if (-not (Test-DependsOn $library $project.Path $byPath)) { [void]$project.Dependencies.Add($library.Path) }
        }
    }
    # Dos proyectos en la misma carpeta comparten obj: se encadenan para no compilar a la vez.
    foreach ($group in $Projects | Group-Object Directory | Where-Object Count -gt 1) {
        $ordered = @($group.Group | Sort-Object Relative)
        foreach ($project in $ordered) { $project.SharedDirectory = $true }
        for ($i = 1; $i -lt $ordered.Count; $i++) {
            if (-not (Test-DependsOn $ordered[$i - 1] $ordered[$i].Path $byPath)) { [void]$ordered[$i].Dependencies.Add($ordered[$i - 1].Path) }
            elseif (-not (Test-DependsOn $ordered[$i] $ordered[$i - 1].Path $byPath)) { [void]$ordered[$i - 1].Dependencies.Add($ordered[$i].Path) }
        }
    }
    # Ciclos: el unico que se sabe resolver es el de CapaVista_Seguridad con las vistas que abre.
    foreach ($members in (Get-Cycles $Projects $byPath)) {
        $set = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
        foreach ($member in $members) { [void]$set.Add($member) }
        $pivot = $null
        foreach ($member in $members) { if ($byPath[$member].Name -eq $script:pivotName) { $pivot = $byPath[$member] } }
        $cycle = [pscustomobject]@{ Members = $set; Pivot = $pivot; Started = $false; SeedReady = $false; SeedBuilt = $false }
        foreach ($member in $members) { $byPath[$member].Cycle = $cycle }
        $script:cycles.Add($cycle)
        if (-not $pivot) {
            $names = @($members | ForEach-Object { $byPath[$_].Name } | Sort-Object) -join ', '
            foreach ($member in $members) { $byPath[$member].ReferenceErrors.Add("Dependencia circular sin resolver entre: $names") }
        }
    }
    foreach ($cycle in $script:cycles) {
        if (-not $cycle.Pivot) { continue }
        # Quien usa una vista del ciclo espera a la DLL completa del pivote, no a la semilla.
        foreach ($project in $Projects) {
            if ($cycle.Members.Contains($project.Path)) { continue }
            foreach ($dependency in @($project.Dependencies)) {
                if ($cycle.Members.Contains($dependency)) { [void]$project.Dependencies.Add($cycle.Pivot.Path); break }
            }
        }
    }
    # Orden topologico. Dentro del ciclo, la dependencia hacia el pivote la cubre la semilla.
    $waiting = @{}
    $dependents = @{}
    foreach ($project in $Projects) { $waiting[$project.Path] = 0; $dependents[$project.Path] = New-Object 'System.Collections.Generic.List[object]' }
    foreach ($project in $Projects) {
        foreach ($dependency in $project.Dependencies) {
            if ($project.Cycle -and $project.Cycle.Pivot -and $project.Cycle.Pivot.Path -eq $dependency -and $project.Path -ne $dependency) { continue }
            $waiting[$project.Path]++
            $dependents[$dependency].Add($project)
        }
    }
    $ready = New-Object 'System.Collections.Generic.List[object]'
    $left = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    foreach ($project in $Projects) {
        [void]$left.Add($project.Path)
        if ($waiting[$project.Path] -eq 0) { $ready.Add($project) }
    }
    $ordered = New-Object 'System.Collections.Generic.List[object]'
    while ($left.Count -gt 0) {
        if ($ready.Count -eq 0) {
            # Ciclo sin resolver: se registra en orden y quedara como error.
            foreach ($project in $Projects) { if ($left.Contains($project.Path)) { $ready.Add($project) } }
        }
        $best = 0
        for ($i = 1; $i -lt $ready.Count; $i++) {
            if ([string]::Compare($ready[$i].SortKey, $ready[$best].SortKey, [StringComparison]::OrdinalIgnoreCase) -lt 0) { $best = $i }
        }
        $project = $ready[$best]
        $ready.RemoveAt($best)
        if (-not $left.Remove($project.Path)) { continue }
        $ordered.Add($project)
        foreach ($dependent in $dependents[$project.Path]) {
            $waiting[$dependent.Path]--
            if ($waiting[$dependent.Path] -eq 0 -and $left.Contains($dependent.Path)) { $ready.Add($dependent) }
        }
    }
    for ($i = 0; $i -lt $ordered.Count; $i++) { $ordered[$i].Index = $i + 1 }
    return $ordered.ToArray()
}

function Find-Tool([string]$Kind) {
    $manual = [Environment]::GetEnvironmentVariable($Kind + '_MANUAL')
    if ($manual) {
        if (-not (Test-Path -LiteralPath $manual -PathType Leaf)) { throw "La ruta $($Kind)_MANUAL no existe: $manual" }
        return [IO.Path]::GetFullPath($manual)
    }
    $exe = 'MSBuild.exe'
    if ($Kind -eq 'HHC') { $exe = 'hhc.exe' }
    $command = Get-Command $exe -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    $programDirs = @([Environment]::GetFolderPath('ProgramFilesX86'), [Environment]::GetFolderPath('ProgramFiles')) |
        Where-Object { $_ } | Select-Object -Unique
    if ($Kind -eq 'HHC') {
        if ($command) { return $command.Source }
        foreach ($directory in $programDirs) {
            $candidate = Join-Path $directory 'HTML Help Workshop\hhc.exe'
            if (Test-Path -LiteralPath $candidate -PathType Leaf) { return $candidate }
        }
        throw 'No se encontro hhc.exe. Instala HTML Help Workshop o define HHC_MANUAL para usar /con-ayudas.'
    }
    foreach ($directory in $programDirs) {
        $vswhere = Join-Path $directory 'Microsoft Visual Studio\Installer\vswhere.exe'
        if (Test-Path -LiteralPath $vswhere -PathType Leaf) {
            $candidates = @(& $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe')
            foreach ($candidate in $candidates) {
                if (Test-Path -LiteralPath $candidate -PathType Leaf) { return $candidate }
            }
        }
    }
    if ($command) { return $command.Source }
    foreach ($directory in $programDirs) {
        foreach ($edition in @('Community', 'Professional', 'Enterprise', 'BuildTools')) {
            $candidate = Join-Path $directory "Microsoft Visual Studio\2022\$edition\MSBuild\Current\Bin\MSBuild.exe"
            if (Test-Path -LiteralPath $candidate -PathType Leaf) { return $candidate }
        }
    }
    throw 'No se encontro MSBuild. Instala Visual Studio 2022/Build Tools con escritorio .NET y .NET Framework 4.8, o define MSBUILD_MANUAL. Blazor necesita SDK .NET 8 o posterior compatible.'
}

function Get-JunctionTarget([string]$Path) {
    # Devuelve el destino si la ruta es un enlace de carpeta; $null si no lo es.
    $item = Get-Item -LiteralPath $Path -Force -ErrorAction SilentlyContinue
    if (-not $item -or -not ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) { return $null }
    $target = @($item.Target) | Select-Object -First 1
    if (-not $target) { return '' }
    return ([string]$target).TrimEnd('\')
}

function Test-LongPaths {
    try {
        return ((Get-ItemProperty -LiteralPath 'HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem' -Name LongPathsEnabled -ErrorAction Stop).LongPathsEnabled -eq 1)
    } catch { return $false }
}

function Initialize-CompileAlias($Projects) {
    # Las rutas del proyecto rozan MAX_PATH: la relativa mas larga pasa de 235 caracteres.
    # Con las rutas largas de Windows activadas, MSBuild compila con las rutas reales y solo
    # importa que la carpeta de cada proyecto no llegue a 259. Si no estan activadas, se usa un
    # enlace de carpeta de ruta corta. Nunca se crea una unidad de disco (antes se usaba SUBST).
    $deepest = 0
    foreach ($project in $Projects) { if ($project.Directory.Length -gt $deepest) { $deepest = $project.Directory.Length } }
    if (Test-LongPaths) {
        if ($deepest -lt 240) {
            Write-Status 'Rutas largas de Windows activadas: se compila con las rutas reales, sin disco ni enlace temporal.'
            return
        }
    } elseif ($repo.Length + 245 -le 259) { return }
    if ($repo.StartsWith('\\')) {
        Write-Status '[AVISO] El repositorio esta en una ruta de red: no se puede acortar. Si aparecen errores de ruta larga, copielo a una carpeta local corta.'
        return
    }
    $hasher = [Security.Cryptography.SHA256]::Create()
    try { $key = ([BitConverter]::ToString($hasher.ComputeHash($utf8.GetBytes($repo.ToLowerInvariant())))).Replace('-', '').Substring(0, 6).ToLowerInvariant() }
    finally { $hasher.Dispose() }
    # El nombre sale de la ruta del repositorio: es el mismo en cada corrida y MSBuild conserva su incremental.
    $base = Join-Path ([IO.Path]::GetPathRoot($repo)) ('_s' + $key)
    foreach ($alias in @($base, ($base + 'a'), ($base + 'b'))) {
        if ($alias.Length + 4 -ge $repo.Length) { break }
        try {
            if (Test-Path -LiteralPath $alias) {
                $target = Get-JunctionTarget $alias
                # Un enlace que quedo de una corrida interrumpida se reutiliza; lo ajeno no se toca.
                if ($null -eq $target -or $target -ine $repo) { continue }
            } else {
                [void](New-Item -ItemType Junction -Path $alias -Target ([Management.Automation.WildcardPattern]::Escape($repo)) -ErrorAction Stop)
                try { (Get-Item -LiteralPath $alias -Force).Attributes = [IO.FileAttributes]::Directory -bor [IO.FileAttributes]::Hidden -bor [IO.FileAttributes]::ReparsePoint } catch {}
            }
            if ((Get-JunctionTarget $alias) -ieq $repo -and (Test-Path -LiteralPath (Join-Path $alias 'codigo') -PathType Container)) {
                $script:compileAlias = $alias
                Write-Status "Enlace temporal de ruta corta (una carpeta, no un disco): $alias -> $repo"
                return
            }
            [IO.Directory]::Delete($alias, $false)
        } catch { continue }
    }
    Write-Status '[AVISO] No se pudo crear el enlace de ruta corta. Se usaran las rutas originales; NuGet podria informar rutas largas.'
}

function Remove-CompileAlias {
    # Quita solo el enlace, nunca su contenido: el destino es el repositorio.
    if (-not $script:compileAlias) { return }
    try {
        if ((Get-JunctionTarget $script:compileAlias) -ieq $repo) { [IO.Directory]::Delete($script:compileAlias, $false) }
    } catch { Write-Warning "No se pudo quitar el enlace temporal $script:compileAlias" }
}

function Get-CompilePath([string]$Path) {
    if ($script:compileAlias) {
        if ($Path.Equals($repo, [StringComparison]::OrdinalIgnoreCase)) { return $script:compileAlias }
        if ($Path.StartsWith($repo + '\', [StringComparison]::OrdinalIgnoreCase) -or
            $Path.StartsWith($repo + '/', [StringComparison]::OrdinalIgnoreCase)) {
            return $script:compileAlias + $Path.Substring($repo.Length)
        }
    }
    return $Path
}

function ConvertTo-MSBuildLiteral([string]$Value) {
    return [regex]::Replace($Value, '[%$@''();?*]', {
        param($match)
        '%' + ([int][char]$match.Value).ToString('X2')
    })
}

function New-LegacyBuildTargets([string]$LogDirectory) {
    # MSBuildWarningsAsErrors va aqui y no solo en la linea de comandos: los nodos en paralelo
    # no reciben /warnAsError y dejarian pasar una referencia que no se resolvio.
    # El ejecutable de Ventas compila por su cuenta decenas de proyectos ajenos; en la
    # compilacion general ya estan compilados, asi que solo se reunen sus bibliotecas.
    $content = @'
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Target Name="SeguridadExcluirClass1Ausente" BeforeTargets="CoreCompile">
    <ItemGroup>
      <Compile Remove="Class1.cs" Condition="!Exists('$(MSBuildProjectDirectory)\Class1.cs')" />
    </ItemGroup>
  </Target>
  <PropertyGroup Condition="'$(SeguridadSemilla)' != 'true'">
    <MSBuildWarningsAsErrors>$(MSBuildWarningsAsErrors);MSB3245;MSB3243</MSBuildWarningsAsErrors>
  </PropertyGroup>
  <Target Name="SeguridadReunirBibliotecas" AfterTargets="Build" Condition="'$(VentasDependenciasPreparadas)' == 'true' And '$(ProyectoCompilacionVentas)' != '' And Exists('$(ProyectoCompilacionVentas)')">
    <MSBuild Projects="$(ProyectoCompilacionVentas)" Targets="ReunirBibliotecas" Properties="$(PropiedadesPreparacionVentas)" RemoveProperties="$(PropiedadesSolucionVentas);SeguridadMarca" />
  </Target>
  <Target Name="SeguridadMarcarCorrecto" AfterTargets="Build" Condition="'$(SeguridadMarca)' != ''">
    <WriteLinesToFile File="$(SeguridadMarca)" Lines="ok" Overwrite="true" />
  </Target>
</Project>
'@
    $text = [Text.StringBuilder]::new($content.Substring(0, $content.LastIndexOf('</Project>')))
    [void]$text.AppendLine('  <Target Name="SeguridadResolverReferenciasInternas" BeforeTargets="ResolveAssemblyReferences">')
    [void]$text.AppendLine('    <ItemGroup>')
    foreach ($reference in $script:referenceOverrides) {
        $projectPath = [Security.SecurityElement]::Escape((ConvertTo-MSBuildLiteral (Get-CompilePath $reference.Project)))
        $identity = [Security.SecurityElement]::Escape((ConvertTo-MSBuildLiteral $reference.Identity))
        $outputLiteral = [Security.SecurityElement]::Escape((ConvertTo-MSBuildLiteral (Get-CompilePath $reference.Output)))
        # Si la DLL del productor aun no existe, se conserva el HintPath original del proyecto.
        [void]$text.Append('      <Reference Condition="&apos;$(MSBuildProjectFullPath)&apos; == &apos;').Append($projectPath)
        [void]$text.Append('&apos; And &apos;%(Reference.Identity)&apos; == &apos;').Append($identity)
        [void]$text.Append('&apos; And Exists(&apos;').Append($outputLiteral).Append('&apos;)"><HintPath>').Append($outputLiteral)
        [void]$text.AppendLine('</HintPath><SpecificVersion>False</SpecificVersion></Reference>')
    }
    [void]$text.AppendLine('    </ItemGroup>')
    [void]$text.AppendLine('  </Target>')
    [void]$text.AppendLine('</Project>')
    $content = $text.ToString()

    $hasher = [Security.Cryptography.SHA256]::Create()
    try { $key = ([BitConverter]::ToString($hasher.ComputeHash($utf8.GetBytes($content)))).Replace('-', '').Substring(0, 16) }
    finally { $hasher.Dispose() }
    $cache = Join-Path $repo 'logs\build_seguridad_global\cache'
    [void][IO.Directory]::CreateDirectory($cache)
    $path = Join-Path $cache ('ajustes_' + $key + '.targets')
    if (-not (Test-Path -LiteralPath $path) -or [IO.File]::ReadAllText($path) -cne $content) {
        [IO.File]::WriteAllText($path, $content, $utf8)
    }
    # MSBuild recompila un proyecto si algun archivo importado es mas nuevo que su DLL. Con una
    # fecha antigua fija, regenerar este archivo no provoca recompilaciones en cascada; los
    # cambios reales de referencias los detecta MSBuild por su cuenta.
    $old = New-Object DateTime(2001, 1, 1, 0, 0, 0, [DateTimeKind]::Utc)
    if ([IO.File]::GetLastWriteTimeUtc($path) -ne $old) { [IO.File]::SetLastWriteTimeUtc($path, $old) }
    Copy-Item -LiteralPath $path -Destination (Join-Path $LogDirectory 'ajustes_referencias.targets') -Force
    return $path
}

function Get-SolutionDirectory([string]$Directory) {
    if ($script:solutionCache.ContainsKey($Directory)) { return $script:solutionCache[$Directory] }
    $found = $Directory
    $cursor = Get-Item -LiteralPath $Directory
    while ($cursor -and ($cursor.FullName -eq $repo -or
            $cursor.FullName.StartsWith($repo + '\', [StringComparison]::OrdinalIgnoreCase))) {
        if (Get-ChildItem -LiteralPath $cursor.FullName -Filter '*.sln' -File | Select-Object -First 1) { $found = $cursor.FullName; break }
        if ($cursor.FullName -eq $repo) { break }
        $cursor = $cursor.Parent
    }
    $script:solutionCache[$Directory] = $found
    return $found
}

function Invoke-Logged([string]$Executable, [string[]]$Arguments, [string]$LogPath) {
    $saved = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $global:LASTEXITCODE = $null
        & $Executable @Arguments 2>&1 | Out-File -LiteralPath $LogPath -Encoding utf8 -ErrorAction Stop
        $code = $global:LASTEXITCODE
    } finally { $ErrorActionPreference = $saved }
    Add-GlobalLogFile $LogPath
    if ($null -eq $code) { throw "No se pudo iniciar: $Executable. Log: $LogPath" }
    return $code
}

function Read-SharedText([string]$Path) {
    # Lee un archivo que MSBuild puede tener abierto para escritura.
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return '' }
    $stream = New-Object IO.FileStream($Path, [IO.FileMode]::Open, [IO.FileAccess]::Read, ([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete))
    try {
        $reader = New-Object IO.StreamReader($stream, $utf8)
        try { return $reader.ReadToEnd() } finally { $reader.Dispose() }
    } finally { $stream.Dispose() }
}

function Invoke-MSBuild([string[]]$Arguments, [string]$ResponseFile, [string]$WorkingDirectory, [scriptblock]$OnTick) {
    # Los argumentos van en un archivo de respuesta: una linea por argumento, entre comillas.
    # Con BOM, para que MSBuild lea bien rutas con acentos o enie.
    $lines = foreach ($argument in $Arguments) { '"' + $argument.Replace('"', '\"') + '"' }
    [IO.File]::WriteAllLines($ResponseFile, [string[]]$lines, (New-Object Text.UTF8Encoding($true)))
    $info = New-Object Diagnostics.ProcessStartInfo
    $info.FileName = $msbuild
    $info.Arguments = '@"' + $ResponseFile + '"'
    $info.WorkingDirectory = $WorkingDirectory
    $info.UseShellExecute = $false
    # Sin ventana propia: todo lo que hace falta queda en los registros (/noconlog + -flp).
    $info.CreateNoWindow = $true
    $process = [Diagnostics.Process]::Start($info)
    if (-not $process) { throw "No se pudo iniciar MSBuild: $msbuild" }
    try {
        while (-not $process.WaitForExit(250)) { if ($OnTick) { & $OnTick } }
        $process.WaitForExit()
        return $process.ExitCode
    } finally {
        # Con Ctrl+C este MSBuild no recibe la senal por no tener consola: se cierra aqui.
        try { if (-not $process.HasExited) { $process.Kill() } } catch {}
        $process.Dispose()
    }
}

function Get-StartFailure([string]$ResponseFile) {
    # Si MSBuild no creo su registro es que rechazo la linea de comandos: se repite la llamada
    # solo para leer el mensaje, que en ese caso sale por la consola.
    $saved = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        return @(& $msbuild ('@' + $ResponseFile) 2>&1 | Select-Object -First 6 | ForEach-Object { ([string]$_).Trim() } | Where-Object { $_ })
    } catch { return @() }
    finally { $ErrorActionPreference = $saved }
}

function Get-ErrorsByProject([string]$ErrorLog) {
    # Cada linea del log de errores termina con "[ruta del proyecto]".
    $map = @{}
    foreach ($line in (Read-SharedText $ErrorLog) -split "`r?`n") {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $key = ''
        $match = [regex]::Match($line, '\[([^\[\]]+?proj)(::[^\]]*)?\]\s*$')
        if (-not $match.Success) {
            # Un proyecto con XML invalido no trae el sufijo: el archivo del error es el proyecto.
            $match = [regex]::Match($line, '^\s*(?:\d+>)?([A-Za-z]:\\[^:*?"<>|]+?proj)\(\d+(?:,\d+)?\)\s*:\s*error\s')
        }
        if ($match.Success) { $key = $match.Groups[1].Value.ToLowerInvariant() }
        if (-not $map.ContainsKey($key)) { $map[$key] = New-Object 'System.Collections.Generic.List[string]' }
        if (-not $map[$key].Contains($line.Trim())) { $map[$key].Add($line.Trim()) }
    }
    return $map
}

function Copy-LegacyDlls($Project) {
    # Solo se usa si algun proyecto del repositorio toma una DLL desde bin\Release.
    $debug = Join-Path $Project.Directory 'bin\Debug'
    if (-not (Test-Path -LiteralPath $debug -PathType Container)) { return }
    $dlls = @(Get-ChildItem -LiteralPath $debug -Filter '*.dll' -File)
    if ($dlls.Count -eq 0) { return }
    $release = Join-Path $Project.Directory 'bin\Release'
    [void][IO.Directory]::CreateDirectory($release)
    foreach ($dll in $dlls) {
        $destination = Join-Path $release $dll.Name
        if (Test-Path -LiteralPath $destination -PathType Leaf) {
            $existing = Get-Item -LiteralPath $destination
            if ($existing.Length -eq $dll.Length -and $existing.LastWriteTimeUtc -eq $dll.LastWriteTimeUtc) { continue }
        }
        Copy-Item -LiteralPath $dll.FullName -Destination $destination -Force
    }
}

function Get-StampPath($Project) {
    $name = 'compilacion_ok.stamp'
    if ($Project.SharedDirectory) { $name = 'compilacion_ok.' + $Project.Name + '.stamp' }
    return Join-Path $Project.Directory ('obj\' + $name)
}

function Get-OutputTime($Project) {
    # Fecha de la DLL o EXE del proyecto; MinValue si no existe. Se recuerda hasta que el proyecto se compile.
    $cached = $script:outputTimes[$Project.Output]
    if ($null -ne $cached) { return $cached }
    $time = [DateTime]::MinValue
    if ([IO.File]::Exists($Project.Output)) { $time = [IO.File]::GetLastWriteTimeUtc($Project.Output) }
    $script:outputTimes[$Project.Output] = $time
    return $time
}

function Test-UpToDate($Project) {
    # Como la revision rapida de Visual Studio: si nada cambio desde la ultima
    # compilacion correcta, no se llama a MSBuild.
    if ($RecompilarTodo -or $Project.Force -or $Project.Opaque) { return $false }
    $stamp = Get-StampPath $Project
    if (-not [IO.File]::Exists($stamp) -or (Get-OutputTime $Project) -eq [DateTime]::MinValue) { return $false }
    $limit = [IO.File]::GetLastWriteTimeUtc($stamp)
    foreach ($dependency in $Project.Dependencies) {
        $producer = $byPath[$dependency]
        # Dentro de un ciclo cada proyecto se compila despues de otro: se evalua el ciclo entero.
        if ($Project.Cycle -and $producer.Cycle -eq $Project.Cycle) { continue }
        if ($producer.Output -ieq $Project.Output) { continue }
        $built = Get-OutputTime $producer
        if ($built -eq [DateTime]::MinValue -or $built -gt $limit) { return $false }
    }
    foreach ($file in $Project.Linked) {
        if (-not [IO.File]::Exists($file) -or [IO.File]::GetLastWriteTimeUtc($file) -gt $limit) { return $false }
    }
    # Imagenes e iconos de recursos\ se enlazan desde los .resx y no aparecen en el .csproj.
    foreach ($area in @('', $Project.Group.ToLowerInvariant())) {
        if ($script:resourceTimes.ContainsKey($area) -and $script:resourceTimes[$area] -gt $limit) { return $false }
    }
    # Borrar o agregar un archivo cambia la fecha de su carpeta: asi tambien se detectan los borrados.
    if ([IO.Directory]::GetLastWriteTimeUtc($Project.Directory) -gt $limit) { return $false }
    $queue = [Collections.Generic.Queue[string]]::new()
    $queue.Enqueue($Project.Directory)
    while ($queue.Count -gt 0) {
        foreach ($entry in [IO.DirectoryInfo]::new($queue.Dequeue()).GetFileSystemInfos()) {
            if ($entry.LastWriteTimeUtc -gt $limit) {
                # La fecha de una carpeta cambia al crear o borrar archivos dentro; bin y obj no cuentan.
                if (-not ($entry.Attributes -band [IO.FileAttributes]::Directory) -or $entry.Name -notmatch $script:skipFolders) { return $false }
            }
            if (($entry.Attributes -band [IO.FileAttributes]::Directory) -and $entry.Name -notmatch $script:skipFolders -and
                -not ($entry.Attributes -band [IO.FileAttributes]::ReparsePoint)) { $queue.Enqueue($entry.FullName) }
        }
    }
    return $true
}

function Get-ResourceTimes {
    # Fecha del archivo mas reciente de recursos\, por area (modulos\ventas, componentes\seguridad...).
    $times = @{}
    $root = Join-Path $repo 'recursos'
    if (-not (Test-Path -LiteralPath $root -PathType Container)) { return $times }
    $queue = New-Object 'System.Collections.Generic.Queue[string]'
    $queue.Enqueue($root)
    while ($queue.Count -gt 0) {
        foreach ($entry in (New-Object IO.DirectoryInfo($queue.Dequeue())).EnumerateFileSystemInfos()) {
            if ($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) { continue }
            if ($entry.Attributes -band [IO.FileAttributes]::Directory) { $queue.Enqueue($entry.FullName); continue }
            $parts = $entry.FullName.Substring($root.Length + 1).Split([char]92)
            $area = ''
            if ($parts.Length -ge 3) { $area = ($parts[0] + [char]92 + $parts[1]).ToLowerInvariant() }
            if (-not $times.ContainsKey($area) -or $entry.LastWriteTimeUtc -gt $times[$area]) { $times[$area] = $entry.LastWriteTimeUtc }
        }
    }
    return $times
}

function Test-NeedsRestore($Project, [int]$Round) {
    # NuGet solo cuando faltan paquetes: restaurar lo ya restaurado cuesta casi medio segundo
    # por proyecto. En los reintentos siempre, por si el error era de un paquete.
    if ($RecompilarTodo -or $Round -gt 1) { return $true }
    if ($Project.Sdk) {
        if (-not [IO.File]::Exists((Join-Path $Project.Directory 'obj\project.assets.json'))) { return $true }
        # Un restore fallido tambien deja project.assets.json; el resultado esta en project.nuget.cache.
        $cache = Join-Path $Project.Directory 'obj\project.nuget.cache'
        if ([IO.File]::Exists($cache) -and [IO.File]::ReadAllText($cache) -match '"success"\s*:\s*false') { return $true }
    }
    $config = Join-Path $Project.Directory 'packages.config'
    if ([IO.File]::Exists($config)) {
        $packages = Join-Path (Get-SolutionDirectory $Project.Directory) 'packages'
        $document = New-Object Xml.XmlDocument
        $document.Load($config)
        foreach ($package in $document.SelectNodes("//*[local-name()='package']")) {
            $folder = Join-Path $packages ($package.GetAttribute('id') + '.' + $package.GetAttribute('version'))
            if (-not [IO.Directory]::Exists($folder)) { return $true }
            # Una carpeta vacia no es un paquete instalado.
            $any = $false
            foreach ($entry in [IO.Directory]::EnumerateFileSystemEntries($folder)) { $any = $true; break }
            if (-not $any) { return $true }
        }
    }
    return $false
}

function Get-SolutionProperty($Project) {
    $solution = (Get-SolutionDirectory $Project.Directory).TrimEnd('\', '/') + '/'
    return 'SolutionDir=' + (ConvertTo-MSBuildLiteral (Get-CompilePath $solution))
}

function New-BatchProject([string]$Path, $Entries) {
    # Proyecto envoltorio: una sola llamada de MSBuild compila en paralelo todos los proyectos del lote.
    $text = New-Object Text.StringBuilder
    [void]$text.AppendLine('<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003" DefaultTargets="Lote">')
    [void]$text.AppendLine('  <ItemGroup>')
    foreach ($entry in $Entries) {
        [void]$text.AppendLine(('    <{0} Include="{1}"><AdditionalProperties>{2}</AdditionalProperties></{0}>' -f $entry.Kind,
            [Security.SecurityElement]::Escape((ConvertTo-MSBuildLiteral (Get-CompilePath $entry.Project.Path))),
            [Security.SecurityElement]::Escape($entry.Properties)))
    }
    [void]$text.AppendLine('  </ItemGroup>')
    [void]$text.AppendLine('  <Target Name="Lote">')
    [void]$text.AppendLine('    <MSBuild Projects="@(Compilar)" Targets="Build" BuildInParallel="true" ContinueOnError="true" Condition="''@(Compilar)'' != ''''" />')
    [void]$text.AppendLine('    <MSBuild Projects="@(Recompilar)" Targets="Rebuild" BuildInParallel="true" ContinueOnError="true" Condition="''@(Recompilar)'' != ''''" />')
    [void]$text.AppendLine('  </Target>')
    [void]$text.AppendLine('</Project>')
    [IO.File]::WriteAllText($Path, $text.ToString(), $utf8)
}

function New-RestoreProject([string]$Path, $Projects) {
    # NuGet no admite dos restauraciones a la vez hacia la misma carpeta packages: los proyectos
    # de una misma solucion van en serie y las soluciones distintas en paralelo. Para eso el
    # envoltorio se llama a si mismo una vez por grupo.
    $groups = @{}
    $self = [Security.SecurityElement]::Escape((ConvertTo-MSBuildLiteral (Get-CompilePath $Path)))
    $text = New-Object Text.StringBuilder
    [void]$text.AppendLine('<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003" DefaultTargets="Lote">')
    [void]$text.AppendLine('  <ItemGroup>')
    foreach ($project in $Projects) {
        $solution = (Get-SolutionDirectory $project.Directory).ToLowerInvariant()
        if (-not $groups.ContainsKey($solution)) {
            $groups[$solution] = $groups.Count + 1
            [void]$text.AppendLine(('    <Grupo Include="{0}"><AdditionalProperties>SeguridadGrupo={1}</AdditionalProperties></Grupo>' -f $self, $groups[$solution]))
        }
        [void]$text.AppendLine(('    <Restaurar Include="{0}"><AdditionalProperties>{1}</AdditionalProperties><Grupo>{2}</Grupo></Restaurar>' -f
            [Security.SecurityElement]::Escape((ConvertTo-MSBuildLiteral (Get-CompilePath $project.Path))),
            [Security.SecurityElement]::Escape((Get-SolutionProperty $project)), $groups[$solution]))
    }
    [void]$text.AppendLine('  </ItemGroup>')
    [void]$text.AppendLine('  <Target Name="Lote">')
    [void]$text.AppendLine('    <MSBuild Projects="@(Grupo)" Targets="RestaurarGrupo" BuildInParallel="true" ContinueOnError="true" />')
    [void]$text.AppendLine('  </Target>')
    [void]$text.AppendLine('  <Target Name="RestaurarGrupo">')
    [void]$text.AppendLine('    <ItemGroup>')
    [void]$text.AppendLine('      <DelGrupo Include="@(Restaurar)" Condition="''%(Restaurar.Grupo)'' == ''$(SeguridadGrupo)''" />')
    [void]$text.AppendLine('    </ItemGroup>')
    [void]$text.AppendLine(('    <MSBuild Projects="@(DelGrupo)" Targets="Restore" BuildInParallel="false" ContinueOnError="true" Properties="RestorePackagesConfig=true;NuGetAudit=false;MSBuildRestoreSessionId={0}" />' -f [guid]::NewGuid().ToString()))
    [void]$text.AppendLine('  </Target>')
    [void]$text.AppendLine('</Project>')
    [IO.File]::WriteAllText($Path, $text.ToString(), $utf8)
}

function Complete-Project($Project, [string]$State, [string[]]$Errors) {
    if ($Project.Mode -ne 'Sin cambios') { [void]$script:outputTimes.Remove($Project.Output) }
    $Project.State = $State
    $Project.LastErrors = @($Errors)
    if ($State -eq 'Correcto') {
        if ($Project.Mode -ne 'Sin cambios') {
            if ($script:legacyRelease) { Copy-LegacyDlls $Project }
            [void][IO.Directory]::CreateDirectory((Join-Path $Project.Directory 'obj'))
            [IO.File]::WriteAllText((Get-StampPath $Project), (Get-Date -Format o), $utf8)
        }
    } elseif ($State -eq 'Error') {
        Remove-Item -LiteralPath (Get-StampPath $Project) -Force -ErrorAction SilentlyContinue
    }
    Write-ProjectResult $Project
    if ($State -eq 'Error') {
        $Project.LastErrors | Select-Object -First 3 | ForEach-Object { Write-Status ('  ' + $_) }
    }
}

function Invoke-Batch($Projects, [int]$Round) {
    $script:batchNumber++
    $label = 'ronda_{0}_lote_{1:D2}' -f $Round, $script:batchNumber
    $markers = Join-Path $logDirectory 'marcas'
    [void][IO.Directory]::CreateDirectory($markers)
    $nodes = [Math]::Min($script:maxNodes, $Projects.Count)
    if ($Round -gt 1) { $nodes = 1 }
    $common = @('/nologo', '/noconlog', '/nr:false', '/p:Configuration=Debug', '/p:BuildProjectReferences=false',
        '/p:VentasDependenciasPreparadas=true',
        ('/p:CustomAfterMicrosoftCommonTargets=' + (ConvertTo-MSBuildLiteral (Get-CompilePath $legacyTargets))))
    $workingDirectory = Get-CompilePath $repo
    $restoreErrors = @{}
    $restore = @($Projects | Where-Object { ($_.NeedsRestore -or $script:restoreForImports) -and (Test-NeedsRestore $_ $Round) })
    if ($restore.Count -gt 0) {
        # NuGet va en una llamada propia y sin reutilizar nodos, para que la compilacion evalue lo restaurado.
        $restoreProject = Join-Path $logDirectory ($label + '_restaurar.proj')
        New-RestoreProject $restoreProject $restore
        $restoreLog = Join-Path $logDirectory ($label + '_restaurar.log')
        $restoreErrorLog = Join-Path $logDirectory ($label + '_restaurar_errores.log')
        Write-Status ("Lote {0}: restaurando paquetes NuGet de {1} proyecto(s)." -f $script:batchNumber, $restore.Count)
        [void](Invoke-MSBuild ($common + @((Get-CompilePath $restoreProject), ('/m:' + $nodes),
            ('-flp:logfile=' + (Get-CompilePath $restoreLog) + ';verbosity=minimal;encoding=UTF-8'),
            ('-flp1:logfile=' + (Get-CompilePath $restoreErrorLog) + ';errorsonly;verbosity=minimal;encoding=UTF-8'))) (Join-Path $logDirectory ($label + '_restaurar.rsp')) $workingDirectory $null)
        Add-GlobalLogFile $restoreLog
        $restoreErrors = Get-ErrorsByProject $restoreErrorLog
    }
    $entries = New-Object 'System.Collections.Generic.List[object]'
    $markerOf = @{}
    foreach ($project in $Projects) {
        $marker = Join-Path $markers ('r{0}_{1:D3}.ok' -f $Round, $project.Index)
        Remove-Item -LiteralPath $marker -Force -ErrorAction SilentlyContinue
        $markerOf[$project.Path] = $marker
        $kind = 'Compilar'
        $project.Mode = 'Build'
        if ($RecompilarTodo -or $project.SharedOutput -or ($project.Cycle -and $project.Cycle.SeedBuilt -and $project.Cycle.Pivot -eq $project)) {
            $kind = 'Recompilar'; $project.Mode = 'Rebuild'
        }
        $project.Attempts++
        $entries.Add([pscustomobject]@{ Kind = $kind; Project = $project
            Properties = ((Get-SolutionProperty $project) + ';SeguridadMarca=' + (ConvertTo-MSBuildLiteral (Get-CompilePath $marker))) })
    }
    $batchProject = Join-Path $logDirectory ($label + '.proj')
    New-BatchProject $batchProject $entries
    $log = Join-Path $logDirectory ($label + '.log')
    $errorLog = Join-Path $logDirectory ($label + '_errores.log')
    foreach ($project in $Projects) { $project.Log = $log }
    Write-Status ("Lote {0}: {1} proyecto(s) en paralelo ({2} nucleos)." -f $script:batchNumber, $Projects.Count, $nodes)
    Add-GlobalLog (($Projects | ForEach-Object { "[$($_.Index)/$($script:total)] $($_.Relative)" }) -join [Environment]::NewLine)
    $batchTimer = [Diagnostics.Stopwatch]::StartNew()
    $reported = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    $tick = {
        foreach ($project in $Projects) {
            if ($reported.Contains($project.Path) -or -not (Test-Path -LiteralPath $markerOf[$project.Path] -PathType Leaf)) { continue }
            # Una advertencia tratada como error no detiene el proyecto: se confirma al cerrar el lote.
            $known = Get-ErrorsByProject $errorLog
            if ($known.ContainsKey((Get-CompilePath $project.Path).ToLowerInvariant())) { continue }
            [void]$reported.Add($project.Path)
            $project.Duration += $batchTimer.Elapsed.TotalSeconds
            Complete-Project $project 'Correcto' @()
        }
    }
    # Los dos registros van con verbosity=minimal, como la consola del compilador anterior: con mas
    # detalle MSBuild emite MSB3245 por referencias sin usar que hoy no impiden compilar.
    $arguments = $common + @((Get-CompilePath $batchProject), ('/m:' + $nodes), '/p:UseSharedCompilation=false', '/warnAsError:MSB3245,MSB3243',
        ('-flp:logfile=' + (Get-CompilePath $log) + ';verbosity=minimal;encoding=UTF-8'),
        ('-flp1:logfile=' + (Get-CompilePath $errorLog) + ';errorsonly;verbosity=minimal;encoding=UTF-8'))
    $code = Invoke-MSBuild $arguments (Join-Path $logDirectory ($label + '.rsp')) $workingDirectory $tick
    $batchTimer.Stop()
    $started = Test-Path -LiteralPath $log -PathType Leaf
    $console = @()
    if (-not $started) { $console = @(Get-StartFailure (Join-Path $logDirectory ($label + '.rsp'))) }
    Add-GlobalLogFile $log
    $errors = Get-ErrorsByProject $errorLog
    foreach ($project in $Projects) {
        if ($reported.Contains($project.Path)) { continue }
        $key = (Get-CompilePath $project.Path).ToLowerInvariant()
        $own = @()
        if ($errors.ContainsKey($key)) { $own = @($errors[$key]) }
        $project.Duration += $batchTimer.Elapsed.TotalSeconds
        if ($own.Count -eq 0 -and (Test-Path -LiteralPath $markerOf[$project.Path] -PathType Leaf)) {
            Complete-Project $project 'Correcto' @()
            continue
        }
        $details = New-Object 'System.Collections.Generic.List[string]'
        if ($started) { $details.Add("MSBuild no completo el proyecto (codigo $code). Log: $log") }
        else {
            $details.Add("MSBuild no pudo iniciar el lote (codigo $code). Argumentos: " + (Join-Path $logDirectory ($label + '.rsp')))
            foreach ($line in $console) { $details.Add($line) }
        }
        foreach ($line in ($own | Select-Object -First 12)) { $details.Add($line) }
        if ($own.Count -eq 0) {
            if ($restoreErrors.ContainsKey($key)) { foreach ($line in ($restoreErrors[$key] | Select-Object -First 6)) { $details.Add($line) } }
            if ($errors.ContainsKey('')) { foreach ($line in ($errors[''] | Select-Object -First 6)) { $details.Add($line) } }
        }
        $safeName = $project.Name -replace '[^a-zA-Z0-9_.-]', '_'
        [IO.File]::WriteAllLines((Join-Path $logDirectory ('error_{0:D3}_{1}.log' -f $project.Index, $safeName)), [string[]]$details, $utf8)
        Complete-Project $project 'Error' $details.ToArray()
    }
}

function Build-PivotSeed($Cycle, [int]$Round) {
    # El pivote y las vistas que abre se referencian entre si. Se compila una "semilla": el pivote
    # sin los formularios que dependen de DLL que aun no existen. Los archivos a excluir se
    # descubren leyendo los errores de cada intento y se recuerdan para la siguiente corrida.
    $project = $Cycle.Pivot
    $safe = $project.Name -replace '[^a-zA-Z0-9_.-]', '_'
    $memory = Join-Path $repo ('logs\build_seguridad_global\cache\semilla_' + $safe + '.txt')
    $baseTargets = [IO.File]::ReadAllText($legacyTargets)
    $projectPrefix = (Get-CompilePath $project.Directory).TrimEnd('\', '/') + '\'
    $excluded = New-Object 'System.Collections.Generic.List[string]'
    $hints = @('FrmSplash.cs', 'FrmSplash.Designer.cs')
    if (Test-Path -LiteralPath $memory -PathType Leaf) { $hints += @([IO.File]::ReadAllLines($memory)) }
    foreach ($hint in $hints) {
        if ($hint -and (Test-Path -LiteralPath (Join-Path $project.Directory $hint) -PathType Leaf) -and
            -not ($excluded | Where-Object { $_ -ieq $hint })) { $excluded.Add($hint) }
    }
    Remove-Item -LiteralPath (Get-StampPath $project) -Force -ErrorAction SilentlyContinue
    [void]$script:outputTimes.Remove($project.Output)
    $seedTimer = [Diagnostics.Stopwatch]::StartNew()
    $seedCode = 1
    $seedLog = $null
    for ($attempt = 1; $attempt -le 8; $attempt++) {
        $seedLog = Join-Path $logDirectory ('ronda_{0}_semilla_{1}_{2}.log' -f $Round, $safe, $attempt)
        $items = (@($excluded | ForEach-Object { '<Compile Remove="' + [Security.SecurityElement]::Escape((ConvertTo-MSBuildLiteral $_)) + '" />' }) -join '')
        $extra = '<Target Name="SeguridadSemillaExcluir" BeforeTargets="CoreCompile"><ItemGroup>' + $items + '</ItemGroup></Target>'
        $seedTargets = Join-Path $logDirectory ('semilla_{0}_{1}.targets' -f $Round, $attempt)
        [IO.File]::WriteAllText($seedTargets, $baseTargets.Substring(0, $baseTargets.LastIndexOf('</Project>')) + $extra + '</Project>', $utf8)
        $seedArguments = @((Get-CompilePath $project.Path), '/t:Build', '/nologo', '/noconlog', '/nr:false',
            '/p:Configuration=Debug', '/p:BuildProjectReferences=false', '/p:SeguridadSemilla=true', '/p:UseSharedCompilation=false',
            '/p:VentasDependenciasPreparadas=true', ('/p:' + (Get-SolutionProperty $project)),
            ('/p:CustomAfterMicrosoftCommonTargets=' + (ConvertTo-MSBuildLiteral (Get-CompilePath $seedTargets))),
            ('-flp:logfile=' + (Get-CompilePath $seedLog) + ';verbosity=minimal;encoding=UTF-8'))
        if (($project.NeedsRestore -or $script:restoreForImports) -and (Test-NeedsRestore $project $Round)) { $seedArguments += @('/restore', '/p:RestorePackagesConfig=true') }
        $seedCode = Invoke-MSBuild $seedArguments (Join-Path $logDirectory ('ronda_{0}_semilla_{1}_{2}.rsp' -f $Round, $safe, $attempt)) (Get-CompilePath $project.Directory) $null
        Add-GlobalLogFile $seedLog
        if ($seedCode -eq 0) { break }
        $added = 0
        foreach ($line in ((Read-SharedText $seedLog) -split "`r?`n")) {
            if ($line -match '^\s*(.+\.cs)\(\d+,\d+\): error CS\d+') {
                $file = $Matches[1]
                if (-not $file.StartsWith($projectPrefix, [StringComparison]::OrdinalIgnoreCase)) { continue }
                $relative = $file.Substring($projectPrefix.Length)
                $names = @($relative)
                if ($relative -notmatch '\.Designer\.cs$') { $names += ($relative -replace '\.cs$', '.Designer.cs') }
                foreach ($name in $names) {
                    if (-not ($excluded | Where-Object { $_ -ieq $name })) { $excluded.Add($name); $added++ }
                }
            }
        }
        if ($added -eq 0) { break }
    }
    $seedTimer.Stop()
    $project.Duration += $seedTimer.Elapsed.TotalSeconds
    [void]$script:outputTimes.Remove($project.Output)
    if ($seedCode -eq 0 -and (Test-Path -LiteralPath $project.Output -PathType Leaf)) {
        [IO.File]::WriteAllLines($memory, [string[]]$excluded.ToArray(), $utf8)
        Write-Status "[AJUSTE] $($project.Name): dependencia circular con las vistas que abre; se compilo una DLL semilla sin: $($excluded -join ', '). Se recompila completa al final."
        return $true
    }
    Remove-Item -LiteralPath $memory -Force -ErrorAction SilentlyContinue
    $details = @("No se pudo compilar la DLL semilla de $($project.Name) (codigo $seedCode). Log: $seedLog")
    $details += @(((Read-SharedText $seedLog) -split "`r?`n") | Where-Object { $_ -match '(?i)\berror\b' } | Select-Object -Unique | Select-Object -First 8 | ForEach-Object { $_.Trim() })
    $project.Log = $seedLog
    Complete-Project $project 'Error' $details
    return $false
}

function Sync-SeedCopies($Cycle) {
    # Las vistas del ciclo copiaron la semilla a su bin: se reemplaza por la DLL completa.
    $source = Split-Path -Parent $Cycle.Pivot.Output
    $names = @([IO.Path]::GetFileName($Cycle.Pivot.Output), ([IO.Path]::GetFileNameWithoutExtension($Cycle.Pivot.Output) + '.pdb'))
    foreach ($path in $Cycle.Members) {
        $member = $byPath[$path]
        if ($member -eq $Cycle.Pivot) { continue }
        foreach ($folder in @((Split-Path -Parent $member.Output), (Join-Path $member.Directory 'bin\Release'))) {
            foreach ($name in $names) {
                $from = Join-Path $source $name
                $to = Join-Path $folder $name
                if ((Test-Path -LiteralPath $from -PathType Leaf) -and (Test-Path -LiteralPath $to -PathType Leaf)) {
                    try { Copy-Item -LiteralPath $from -Destination $to -Force } catch { Write-Status "[AVISO] No se pudo actualizar $to con la DLL completa." }
                }
            }
        }
    }
}

function Start-Cycle($Cycle, $Todo, [int]$Round) {
    # Devuelve $true si hizo algun avance sobre el ciclo.
    if ($Cycle.Started -or -not $Cycle.Pivot) { return $false }
    if (@($Cycle.Members | Where-Object { $Todo.Contains($_) }).Count -eq 0) { return $false }
    foreach ($path in $Cycle.Members) {
        foreach ($dependency in $byPath[$path].Dependencies) {
            if (-not $Cycle.Members.Contains($dependency) -and $byPath[$dependency].State -ne 'Correcto') { return $false }
        }
    }
    $Cycle.Started = $true
    $members = @($Cycle.Members | ForEach-Object { $byPath[$_] } | Sort-Object Index)
    if (@($members | Where-Object { $_.ReferenceErrors.Count -gt 0 }).Count -eq 0 -and
        @($members | Where-Object { -not (Test-UpToDate $_) }).Count -eq 0) {
        foreach ($member in $members) {
            if (-not $Todo.Remove($member.Path)) { continue }
            $member.Mode = 'Sin cambios'
            Complete-Project $member 'Correcto' @()
        }
        return $true
    }
    $pivotFresh = (Test-UpToDate $Cycle.Pivot) -and $Cycle.Pivot.ReferenceErrors.Count -eq 0
    foreach ($member in $members) { $member.Force = $true }
    if ($pivotFresh) {
        # El pivote no cambio: su DLL completa anterior sirve de base para las vistas del ciclo.
        $Cycle.SeedReady = $true
        return $true
    }
    if ($Cycle.Pivot.ReferenceErrors.Count -gt 0) {
        [void]$Todo.Remove($Cycle.Pivot.Path)
        Complete-Project $Cycle.Pivot 'Error' @($Cycle.Pivot.ReferenceErrors -join ' ')
        return $true
    }
    if (Build-PivotSeed $Cycle $Round) { $Cycle.SeedReady = $true; $Cycle.SeedBuilt = $true }
    else { [void]$Todo.Remove($Cycle.Pivot.Path) }
    return $true
}

function Test-Ready($Project) {
    foreach ($dependency in $Project.Dependencies) {
        $producer = $byPath[$dependency]
        if ($producer.State -eq 'Correcto') { continue }
        if ($Project.Cycle -and $Project.Cycle.SeedReady -and $Project.Cycle.Pivot -eq $producer -and $Project -ne $producer) { continue }
        return $false
    }
    if ($Project.Cycle -and $Project.Cycle.Pivot -and -not $Project.Cycle.Started) { return $false }
    return $true
}



function Get-HelpInfo([IO.FileInfo]$File) {
    $encoding = [Text.Encoding]::Default
    $bytes = [IO.File]::ReadAllBytes($File.FullName)
    if ($bytes.Length -ge 3 -and $bytes[0] -eq 239 -and $bytes[1] -eq 187 -and $bytes[2] -eq 191) { $encoding = New-Object Text.UTF8Encoding($true) }
    elseif ($bytes.Length -ge 2 -and $bytes[0] -eq 255 -and $bytes[1] -eq 254) { $encoding = [Text.Encoding]::Unicode }
    elseif ($bytes.Length -ge 2 -and $bytes[0] -eq 254 -and $bytes[1] -eq 255) { $encoding = [Text.Encoding]::BigEndianUnicode }
    $content = [IO.File]::ReadAllText($File.FullName, $encoding)
    $match = [regex]::Match($content, '(?im)^\s*Compiled file\s*=([^\r\n]+)')
    if (-not $match.Success) { throw "El HHP no define Compiled file: $($File.FullName)" }
    $destination = [IO.Path]::GetFullPath([IO.Path]::Combine($File.DirectoryName, $match.Groups[1].Value.Trim().Trim('"')))
    if (-not $destination.StartsWith($repo + '\', [StringComparison]::OrdinalIgnoreCase) -or
        [IO.Path]::GetExtension($destination) -ine '.chm') { throw "Salida CHM fuera del repositorio o no valida: $destination" }
    [pscustomobject]@{ Path = $File.FullName; Directory = $File.DirectoryName; Destination = $destination; Content = $content; Encoding = $encoding }
}

function Assert-Chm([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "Falta el archivo de ayuda: $Path" }
    $stream = [IO.File]::OpenRead($Path)
    try {
        $header = New-Object byte[] 4
        if ($stream.Read($header, 0, 4) -ne 4 -or [Text.Encoding]::ASCII.GetString($header) -ne 'ITSF') {
            throw "El archivo no tiene un encabezado CHM valido (ITSF): $Path"
        }
    } finally { $stream.Dispose() }
}

function Build-Help($Help, [string]$Compiler, [int]$Index, [string]$LogDirectory) {
    $id = [guid]::NewGuid().ToString('N')
    $temporaryProject = Join-Path $Help.Directory ('.compilar-' + $id + '.hhp')
    $temporaryChm = Join-Path $LogDirectory ('temporal-' + $id + '.chm')
    $log = Join-Path $LogDirectory ('ayuda_{0:D3}.log' -f $Index)
    $content = [regex]::Replace($Help.Content, '(?im)^\s*Compiled file\s*=[^\r\n]*',
        [Text.RegularExpressions.MatchEvaluator]{ param($match) 'Compiled file=' + $temporaryChm })
    try {
        [IO.File]::WriteAllText($temporaryProject, $content, $Help.Encoding)
        Push-Location -LiteralPath $Help.Directory
        try { $code = Invoke-Logged $Compiler @($temporaryProject) $log }
        finally { Pop-Location }
        if ($code -notin @(0, 1) -or [IO.File]::ReadAllText($log) -match '(?im)\bHHC\d+\s*:\s*Error\b') {
            throw "Fallo al compilar la ayuda; se conserva su CHM anterior. Log: $log"
        }
        Assert-Chm $temporaryChm
        Copy-Item -LiteralPath $temporaryChm -Destination $Help.Destination -Force
        Write-Status "[OK] Ayuda: $($Help.Destination)"
    } finally {
        foreach ($path in @($temporaryProject, $temporaryChm)) {
            if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Force }
        }
    }
}

function Write-Reports($Plan, [string]$Directory) {
    $Plan | ForEach-Object {
        [pscustomobject]@{
            Area = $_.Group; Proyecto = $_.Relative; Ensamblado = $_.Assembly
            Estado = $_.State; Intentos = $_.Attempts; Registro = $_.Log
            Segundos = [Math]::Round($_.Duration, 2)
            Modo = $_.Mode
            Detalle = $_.LastErrors -join ' | '
        }
    } | Export-Csv -LiteralPath (Join-Path $Directory 'resultados.csv') -NoTypeInformation -Encoding UTF8
    $Plan | Group-Object Group | Sort-Object Name | ForEach-Object {
        [pscustomobject]@{
            Area = $_.Name; Total = $_.Count
            Correctos = @($_.Group | Where-Object State -eq 'Correcto').Count
            Errores = @($_.Group | Where-Object State -eq 'Error').Count
            Bloqueados = @($_.Group | Where-Object State -eq 'Bloqueado').Count
            Pendientes = @($_.Group | Where-Object State -eq 'Pendiente').Count
        }
    } | Export-Csv -LiteralPath (Join-Path $Directory 'resumen_modulos.csv') -NoTypeInformation -Encoding UTF8
    [IO.File]::WriteAllLines($script:failedFile, [string[]]@($Plan | Where-Object State -ne 'Correcto' |
        ForEach-Object { '[' + $_.State + '] ' + $_.Relative }), $utf8)
    $duplicates = @($Plan | Group-Object Assembly | Where-Object Count -gt 1 | ForEach-Object { $_.Group })
    if ($duplicates.Count -gt 0) {
        $duplicates | Select-Object @{Name='Ensamblado';Expression={$_.Assembly}},
            @{Name='Proyecto';Expression={$_.Relative}}, @{Name='SalidaCompartida';Expression={$_.SharedOutput}} |
            Export-Csv -LiteralPath (Join-Path $Directory 'duplicados.csv') -NoTypeInformation -Encoding UTF8
    }
}

try {
    $repo = Get-RepositoryRoot $RepoRoot
    $codeDirectory = Join-Path $repo 'codigo'
    $helpDirectory = Join-Path $repo 'ayuda'
    $projects = @(Get-SourceProjects $codeDirectory | Sort-Object FullName | ForEach-Object { Get-ProjectInfo $_ })
    if ($projects.Count -eq 0) { throw 'No se encontraron proyectos C# dentro de codigo.' }
    # Directory.Build.* tambien se hereda desde carpetas superiores al clon.
    $cursor = Get-Item -LiteralPath $repo
    while ($cursor) {
        foreach ($name in @('Directory.Build.props', 'Directory.Build.targets')) {
            if (Test-Path -LiteralPath (Join-Path $cursor.FullName $name)) { $script:restoreForImports = $true }
        }
        $cursor = $cursor.Parent
    }
    $plan = @(Get-BuildPlan $projects)
    $byPath = @{}
    foreach ($project in $plan) { $byPath[$project.Path] = $project }
    $script:total = $plan.Count
    $script:finished = 0
    $script:batchNumber = 0
    $script:maxNodes = [Math]::Max(1, [Math]::Min([Environment]::ProcessorCount, 16))
    $requested = 0
    if ([int]::TryParse([string]$env:COMPILACION_NUCLEOS, [ref]$requested) -and $requested -ge 1) { $script:maxNodes = [Math]::Min($requested, 64) }
    if ($Secuencial) { $script:maxNodes = 1 }
    Write-Header $plan.Count
    Write-Status "Repositorio: $repo"
    $help = @()
    if ($CompilarAyudas -and (Test-Path -LiteralPath $helpDirectory -PathType Container)) {
        $help = @(Get-ChildItem -LiteralPath $helpDirectory -Filter '*.hhp' -Recurse -File |
            Where-Object Name -notlike '.compilar-*' | Sort-Object FullName)
    }
    Write-Status "Plan: $($help.Count) proyectos de ayuda y $($plan.Count) proyectos C#."
    $duplicates = @($projects | Group-Object Assembly | Where-Object Count -gt 1)
    if ($duplicates.Count -gt 0) {
        $shared = @($projects | Where-Object SharedOutput).Count
        Write-Status "[AVISO] $($duplicates.Count) nombres de DLL compartidos; $shared proyectos comparten archivo de salida y se compilan uno tras otro."
    }
    foreach ($cycle in $script:cycles) {
        $names = @($cycle.Members | ForEach-Object { $byPath[$_].Name } | Sort-Object) -join ', '
        if ($cycle.Pivot) { Write-Status "[CICLO] $($cycle.Members.Count) proyectos se referencian entre si y se resuelven con una DLL semilla de $($cycle.Pivot.Name): $names" }
        else { Write-Status "[AVISO] Dependencia circular sin resolver entre $($cycle.Members.Count) proyectos: $names" }
    }
    foreach ($root in @('componentes', 'modulos')) {
        foreach ($area in Get-ChildItem -LiteralPath (Join-Path $codeDirectory $root) -Directory | Sort-Object Name) {
            $group = $root + '\' + $area.Name
            $count = @($projects | Where-Object Group -eq $group).Count
            if ($count -eq 0) { Write-Status "[SIN PROYECTOS] $group no contiene archivos .csproj." }
            else { Write-Status "[AREA] $($group): $count proyectos." }
        }
    }
    if ($SoloListar) {
        foreach ($item in $help) { Write-Status "AYUDA $($item.FullName)" }
        foreach ($project in $plan) { Write-Status ('{0:D3} {1}' -f $project.Index, $project.Relative) }
        foreach ($project in $plan) {
            foreach ($message in $project.ReferenceErrors) { Write-Status "[ERROR] $($project.Relative): $message" }
        }
        if (@($plan | Where-Object { $_.ReferenceErrors.Count -gt 0 }).Count -gt 0) { exit 1 }
        exit 0
    }
    $runId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0, 6)
    $logDirectory = Join-Path $repo ('logs\build_seguridad_global\' + $runId)
    [void][IO.Directory]::CreateDirectory($logDirectory)
    $script:globalLog = Join-Path $logDirectory 'build_log.txt'
    $script:failedFile = Join-Path $logDirectory 'fallidos.txt'
    $script:logWriter = New-Object IO.StreamWriter($script:globalLog, $false, $utf8)
    $script:logWriter.AutoFlush = $true
    $script:logWriter.WriteLine('Inicio: ' + (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))
    [IO.File]::WriteAllText($script:failedFile, '', $utf8)
    Write-Status "Repositorio: $repo"
    Write-Status "Proyectos: $($plan.Count). Registros: $logDirectory"
    Write-Status ('Modo: {0}' -f $(if ($RecompilarTodo) { 'Rebuild' } else { 'Build incremental' }))
    Write-Reports $plan $logDirectory
    $oldBats = @('Estos BAT no son invocados por el compilador global. Revisar sus opciones antes de retirarlos.', '')
    $oldBats += @(Get-ChildItem -LiteralPath $repo -Filter '*.bat' -File | ForEach-Object { $_.FullName })
    $oldBats += @($script:batFiles)
    if (Test-Path -LiteralPath $helpDirectory -PathType Container) {
        $oldBats += @(Get-ChildItem -LiteralPath $helpDirectory -Filter '*.bat' -Recurse -File | ForEach-Object { $_.FullName })
    }
    $oldBats = @($oldBats[0..1]) + @($oldBats | Select-Object -Skip 2 | Where-Object { $_ -ne $env:SEGURIDAD_SELF } |
        Sort-Object -Unique | ForEach-Object { $_.Substring($repo.Length + 1) })
    [IO.File]::WriteAllLines((Join-Path $logDirectory 'bat_anteriores.txt'), [string[]]$oldBats, $utf8)
    # vswhere tarda cerca de un segundo: se recuerda la ruta mientras siga existiendo.
    $msbuildMemory = Join-Path $repo 'logs\build_seguridad_global\cache\msbuild.txt'
    $msbuild = $null
    if (-not $env:MSBUILD_MANUAL -and [IO.File]::Exists($msbuildMemory)) {
        $remembered = [IO.File]::ReadAllText($msbuildMemory).Trim()
        if ($remembered -and [IO.File]::Exists($remembered)) { $msbuild = $remembered }
    }
    if (-not $msbuild) {
        $msbuild = Find-Tool 'MSBUILD'
        try {
            [void][IO.Directory]::CreateDirectory((Split-Path -Parent $msbuildMemory))
            [IO.File]::WriteAllText($msbuildMemory, $msbuild, $utf8)
        } catch {}
    }
    Write-Status "MSBuild: $msbuild"
    $script:resourceTimes = Get-ResourceTimes
    Initialize-CompileAlias $plan
    $legacyTargets = New-LegacyBuildTargets $logDirectory
    Write-Status "Referencias internas resueltas: $($script:referenceOverrides.Count)."
    $helpErrors = New-Object 'System.Collections.Generic.List[string]'
    if ($CompilarAyudas) {
        try {
            if ($help.Count -eq 0) { throw 'No se encontraron proyectos HHP de ayuda.' }
            $compiler = Find-Tool 'HHC'
            for ($index = 0; $index -lt $help.Count; $index++) {
                try {
                    Write-Status "Compilando ayuda: $($help[$index].FullName)"
                    Build-Help (Get-HelpInfo $help[$index]) $compiler ($index + 1) $logDirectory
                } catch {
                    $helpErrors.Add($_.Exception.Message)
                    Write-Status "[ERROR AYUDA] $($_.Exception.Message)"
                }
            }
        } catch {
            $helpErrors.Add($_.Exception.Message)
            Write-Status "[ERROR AYUDA] $($_.Exception.Message)"
        }
    } else { Write-Status 'Se conservan las ayudas existentes. Use /con-ayudas para recompilarlas.' }
    if ($script:legacyRelease) { Write-Status '[AVISO] Se compila Debug. Las copias a bin\Release para referencias heredadas siguen siendo Debug.' }
    else { Write-Status 'Se compila Debug. Ningun proyecto referencia bin\Release: no se hacen las copias heredadas a esa carpeta.' }
    $pending = $plan
    for ($round = 1; $round -le 3 -and $pending.Count -gt 0; $round++) {
        Write-Status "Ronda $round de 3: $($pending.Count) proyectos pendientes."
        $todo = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
        foreach ($project in $pending) { [void]$todo.Add($project.Path) }
        $script:finished = $plan.Count - $todo.Count
        while ($todo.Count -gt 0) {
            $advanced = $false
            foreach ($cycle in $script:cycles) { if (Start-Cycle $cycle $todo $round) { $advanced = $true } }
            $ready = @($plan | Where-Object { $todo.Contains($_.Path) -and (Test-Ready $_) })
            if ($ready.Count -eq 0) {
                if ($advanced) { continue }
                break
            }
            $build = New-Object 'System.Collections.Generic.List[object]'
            foreach ($project in $ready) {
                [void]$todo.Remove($project.Path)
                if ($project.ReferenceErrors.Count -gt 0) {
                    $project.Log = Join-Path $logDirectory ('error_{0:D3}_{1}.log' -f $project.Index, ($project.Name -replace '[^a-zA-Z0-9_.-]', '_'))
                    [IO.File]::WriteAllLines($project.Log, [string[]]$project.ReferenceErrors.ToArray(), $utf8)
                    Complete-Project $project 'Error' @($project.ReferenceErrors -join ' ')
                } elseif (Test-UpToDate $project) {
                    $project.Mode = 'Sin cambios'
                    Complete-Project $project 'Correcto' @()
                } else { $build.Add($project) }
            }
            if ($build.Count -gt 0) {
                Invoke-Batch $build.ToArray() $round
                foreach ($cycle in $script:cycles) {
                    if ($cycle.SeedBuilt -and $cycle.Pivot -and $cycle.Pivot.State -eq 'Correcto' -and $build.Contains($cycle.Pivot)) { Sync-SeedCopies $cycle }
                }
                Write-Reports $plan $logDirectory
            }
        }
        foreach ($project in @($plan | Where-Object { $todo.Contains($_.Path) })) {
            $unavailable = @($project.Dependencies | Where-Object { $byPath[$_].State -ne 'Correcto' })
            $project.Log = ''
            Complete-Project $project 'Bloqueado' @($unavailable | ForEach-Object { 'Dependencia sin compilar: ' + $byPath[$_].Relative })
        }
        if ($script:useColor) { Write-Progress -Activity 'Compilando componentes y modulos' -Completed }
        Write-Reports $plan $logDirectory
        $failed = @($plan | Where-Object { $_.State -ne 'Correcto' })
        if ($failed.Count -eq 0) { $pending = @(); break }
        if ($failed.Count -eq $pending.Count -or @($failed | Where-Object State -eq 'Error').Count -eq 0) {
            if ($round -lt 3) { Write-Status 'No hubo progreso en esta ronda; se detienen los reintentos.' }
            $pending = $failed
            break
        }
        $pending = $failed
        if ($round -lt 3) {
            # Un ciclo con algun miembro pendiente se vuelve a resolver completo.
            $again = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
            foreach ($project in $failed) { [void]$again.Add($project.Path) }
            foreach ($cycle in $script:cycles) {
                if (@($cycle.Members | Where-Object { $byPath[$_].State -ne 'Correcto' }).Count -eq 0) { continue }
                $cycle.Started = $false; $cycle.SeedReady = $false; $cycle.SeedBuilt = $false
                foreach ($member in $cycle.Members) { [void]$again.Add($member) }
            }
            $pending = @($plan | Where-Object { $again.Contains($_.Path) })
            foreach ($project in $pending) { $project.State = 'Pendiente'; $project.LastErrors = @() }
        }
    }
    Write-Reports $plan $logDirectory
    if ($helpErrors.Count -gt 0) {
        [IO.File]::AppendAllText($script:failedFile, '[AYUDAS] ' + ($helpErrors -join [Environment]::NewLine) + [Environment]::NewLine, $utf8)
    }
    Write-BuildSummary $plan $logDirectory
    Remove-UntrackedVentasPackages $repo
    if ($pending.Count -gt 0) {
        $rootErrors = @($pending | Where-Object State -eq 'Error')
        $blockedProjects = @($pending | Where-Object State -ne 'Error')
        Write-Status ''
        Write-Status ('[ERROR] CAUSA RAIZ: {0} proyecto(s) no compilan y dejan {1} proyecto(s) bloqueados por dependencia.' -f $rootErrors.Count, $blockedProjects.Count)
        foreach ($project in $rootErrors) {
            Write-Status "[ERROR] $($project.Relative)"
            @($project.LastErrors | Select-Object -Unique | Select-Object -First 7) | ForEach-Object { Write-Status ('    ' + ([string]$_).Trim()) }
        }
        foreach ($project in $blockedProjects) {
            Write-Status "[BLOQUEADO] $($project.Relative)"
            @($project.LastErrors | Select-Object -First 3) | ForEach-Object { Write-Status ('    ' + ([string]$_).Trim()) }
        }
        exit 1
    }
    if ($helpErrors.Count -gt 0) { exit 1 }
    exit 0
} catch {
    $message = '[ERROR] ' + $_.Exception.Message
    Write-Host $message
    try { Add-GlobalLog $message } catch {}
    if ($script:failedFile) { try { [IO.File]::AppendAllText($script:failedFile, $message + [Environment]::NewLine, $utf8) } catch {} }
    exit 1
} finally {
    if ($script:useColor) { Write-Progress -Activity 'Compilando componentes y modulos' -Completed }
    Remove-CompileAlias
    if ($script:logWriter) { try { $script:logWriter.Dispose() } catch {} }
}
