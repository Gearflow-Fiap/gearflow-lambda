#!/usr/bin/env bash
# Empacota as 3 Lambdas em artifacts/*.zip para o Terraform (CI / Linux).
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
artifacts="${root}/artifacts"
mkdir -p "${artifacts}"

publish() {
  local name="$1"
  local project="$2"
  local out="${artifacts}/${name}-publish"
  local zip="${artifacts}/${name}.zip"

  echo ">> Publicando ${name}..."
  rm -rf "${out}" "${zip}"
  dotnet publish "${root}/${project}" \
    -c Release \
    -r linux-x64 \
    --self-contained false \
    -o "${out}"
  (cd "${out}" && zip -r "${zip}" .)
  echo "   OK -> ${zip}"
}

publish validate-cpf   "functions/validate-cpf/GearFlow.Lambda.ValidateCpf.csproj"
publish check-client   "functions/check-client/GearFlow.Lambda.CheckClient.csproj"
publish generate-token "functions/generate-token/GearFlow.Lambda.GenerateToken.csproj"

echo ""
echo "Artefatos prontos em ${artifacts}"
