terraform {
  required_providers {
    github = {
      source  = "integrations/github"
      version = "~> 5.0"
    }
  }
}

# Configure the GitHub Provider
provider "github" {
  token = var.token
  owner = "The-UnAd"
}

resource "github_actions_environment_variable" "db_host" {
  repository    = "monov2"
  environment   = var.environment
  variable_name = "DB_HOST"
  value         = var.db_host
}

resource "github_actions_environment_variable" "redis_host" {
  repository    = "monov2"
  environment   = var.environment
  variable_name = "REDIS_HOST"
  value         = var.redis_host
}

resource "github_actions_environment_variable" "redis_port" {
  repository    = "monov2"
  environment   = var.environment
  variable_name = "REDIS_PORT"
  value         = var.redis_port
}

resource "github_actions_environment_variable" "jumpbox_host" {
  repository    = "monov2"
  environment   = var.environment
  variable_name = "JUMPBOX_HOST"
  value         = var.jumpbox_host
}

resource "github_actions_environment_variable" "db_port" {
  repository    = "monov2"
  environment   = var.environment
  variable_name = "DB_PORT"
  value         = var.db_port
}

resource "github_actions_environment_variable" "admin_site_bucket_name" {
  repository    = "monov2"
  environment   = var.environment
  variable_name = "ADMIN_SITE_S3_BUCKET"
  value         = var.admin_site_bucket_name
}

resource "github_actions_environment_variable" "graph_monitor_url" {
  repository    = "monov2"
  environment   = var.environment
  variable_name = "GRAPH_MONITOR_URL"
  value         = var.graph_monitor_url
}

resource "github_actions_environment_secret" "db_pass" {
  repository      = "monov2"
  environment     = var.environment
  secret_name     = "DB_PASS"
  plaintext_value = var.db_pass
}

resource "github_actions_environment_secret" "graph_monitor_headers" {
  repository      = "monov2"
  environment     = var.environment
  secret_name     = "GRAPH_MONITOR_HEADERS"
  plaintext_value = "X-Api-Key: ${var.graph_monitor_api_key}"
}

variable "token" {
  type      = string
  sensitive = true
  nullable  = false
}

variable "environment" {
  type     = string
  nullable = false
}

variable "db_host" {
  type     = string
  nullable = false
}

variable "redis_host" {
  type     = string
  nullable = false
}

variable "redis_port" {
  type     = string
  nullable = false
}

variable "db_pass" {
  type      = string
  sensitive = true
  nullable  = false
}

variable "db_port" {
  type     = number
  nullable = false
}

variable "jumpbox_host" {
  type     = string
  nullable = false
}

variable "graph_monitor_url" {
  type = string
}

variable "graph_monitor_api_key" {
  type      = string
  sensitive = true
}

variable "admin_site_bucket_name" {
  type     = string
  nullable = false
}

variable "postfix" {
  description = "Postfix to apply to names that have to be globally unique"
  type        = string
  nullable    = false
}
