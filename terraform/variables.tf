variable "aws_region" {
  description = "Região AWS"
  type        = string
  default     = "us-east-1"
}

variable "project_name" {
  description = "Prefixo dos recursos"
  type        = string
  default     = "gearflow"
}

variable "environment" {
  description = "Ambiente (dev, homolog, prod)"
  type        = string
  default     = "dev"
}

variable "jwt_signing_key" {
  description = "Chave HMAC para assinar JWT (generate-token)"
  type        = string
  sensitive   = true
}

variable "jwt_issuer" {
  type    = string
  default = "GearFlow.Api"
}

variable "jwt_audience" {
  type    = string
  default = "GearFlow.Client"
}

variable "jwt_access_token_minutes" {
  type    = number
  default = 30
}

variable "db_connection_string" {
  description = "Connection string readonly (Repo 3) para check-client"
  type        = string
  sensitive   = true
  default     = ""
}

# --- Conectividade privada com o RDS (gearflow-infra-database) ---
# Plumbados manualmente a partir dos outputs daquele repo, seguindo o mesmo
# padrão do db_connection_string acima (não há remote state cross-repo hoje).

variable "lambda_vpc_id" {
  description = "ID da VPC do banco (output database_vpc_id em gearflow-infra-database), onde a Lambda check-client roda para acessar o RDS pela rede privada."
  type        = string
  default     = ""
}

variable "lambda_vpc_subnet_ids" {
  description = "IDs das subnets privadas do RDS (output database_private_subnet_ids em gearflow-infra-database) usadas no vpc_config da Lambda check-client."
  type        = list(string)
  default     = []
}

variable "database_security_group_id" {
  description = "ID do security group do RDS (output database_security_group_id em gearflow-infra-database), usado para autorizar apenas a Lambda a alcançar a porta 1433."
  type        = string
  default     = ""
}

variable "validate_cpf_zip_path" {
  description = "Caminho do zip publicado da Lambda validate-cpf"
  type        = string
  default     = "../artifacts/validate-cpf.zip"
}

variable "check_client_zip_path" {
  description = "Caminho do zip publicado da Lambda check-client"
  type        = string
  default     = "../artifacts/check-client.zip"
}

variable "generate_token_zip_path" {
  description = "Caminho do zip publicado da Lambda generate-token"
  type        = string
  default     = "../artifacts/generate-token.zip"
}
