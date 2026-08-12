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
