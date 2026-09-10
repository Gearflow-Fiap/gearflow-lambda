terraform {
  required_version = ">= 1.5.0"

  cloud {
    organization = "gearflowfiapmurilo"

    workspaces {
      name = "gearflow-lambda"
    }
  }

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
    archive = {
      source  = "hashicorp/archive"
      version = "~> 2.4"
    }
  }
}
