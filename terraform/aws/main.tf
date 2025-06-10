
terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3"
    }
    tls = {
      source = "hashicorp/tls"
      version = "~> 4"
    }
  }
}

provider "aws" {
  region = var.region
}

data "aws_region" "current" {}

data "aws_availability_zones" "available" {}

locals {
  availability_zones = data.aws_availability_zones.available.names
  rds_user = "unad"
}
