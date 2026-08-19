# Empacota as 3 Lambdas em artifacts/*.zip para o Terraform.
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $root "artifacts"

New-Item -ItemType Directory -Force -Path $artifacts | Out-Null

$functions = @(
    @{ Name = "validate-cpf"; Project = "functions\validate-cpf\GearFlow.Lambda.ValidateCpf.csproj" },
    @{ Name = "check-client"; Project = "functions\check-client\GearFlow.Lambda.CheckClient.csproj" },
    @{ Name = "generate-token"; Project = "functions\generate-token\GearFlow.Lambda.GenerateToken.csproj" }
)

foreach ($fn in $functions) {
    $publishDir = Join-Path $artifacts "$($fn.Name)-publish"
    $zipPath = Join-Path $artifacts "$($fn.Name).zip"

    Write-Host ">> Publicando $($fn.Name)..." -ForegroundColor Cyan

    if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

    dotnet publish (Join-Path $root $fn.Project) `
        -c Release `
        -r linux-x64 `
        --self-contained false `
        -o $publishDir

    Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -Force
    Write-Host "   OK -> $zipPath" -ForegroundColor Green
}

Write-Host "`nArtefatos prontos em $artifacts" -ForegroundColor Green
