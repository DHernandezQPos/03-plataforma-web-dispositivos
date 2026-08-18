$ErrorActionPreference = "Stop"

Write-Host "== P3 Prevalidacion automatica =="
Write-Host "1) Ejecutando pruebas API/Application/Domain/Infrastructure..."
dotnet test "src/C2C.DevicePlatform.slnx"

Write-Host "2) Verificando build Web..."
dotnet build "src/C2C.DevicePlatform.Web/C2C.DevicePlatform.Web.csproj"

Write-Host "3) Verificando vulnerabilidades..."
dotnet list "src/C2C.DevicePlatform.slnx" package --vulnerable --include-transitive

Write-Host "Prevalidacion automatica P3 completada."
