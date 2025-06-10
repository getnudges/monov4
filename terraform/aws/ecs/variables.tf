variable "project_name" {
  type = string
  validation {
    condition = length(var.project_name) < 20
      error_message = "The project name must be less than 20 characters"
  }
}
variable "container_port" {
  type = number
}
variable "execution_role_arn" {
  type = string
}
variable "task_role_arn" {
  type = string
}
variable "container_secrets" {
  type = list(object({
    name      = string
    valueFrom = string
  }))
  default = []
}
variable "container_environment" {
  type = list(object({
    name  = string
    value = string
  }))
  default = []
}
variable "health_check_path" {
  type    = string
  default = null
}
variable "vpc_id" {
  type = string
}
variable "vpc_cidr" {
  type = string
}
variable "public_subnet_ids" {
  type = list(string)
  default = [ ]
}
variable "private_subnet_ids" {
  type = list(string)
}
variable "task_cpu" {
  type = number
}
variable "task_memory" {
  type = number
}
variable "service_security_group_ids" {
  type = list(string)
}
variable "cluster_arn" {
  type = string
}
variable "cluster_name" {
  type = string
}
variable "desired_count" {
  type = number
}
variable "region" {
  type = string
}
variable "enable_service_connect" {
  type = bool
  default = false
}
variable "service_connect_namespace" {
  type = string
  default = null
}
variable "service_connect_namespace_id" {
  type = string
  default = null
}
variable "ssl_certificate_arns" {
  type = list(string)
  default = []
}
variable "enable_cognito" {
  type = bool
  default = false
}
variable "cognito_pool_arn" {
  type = string
  default = null
}
variable "cognito_pool_client_id" {
  type = string
  default = null
}
variable "cognito_pool_domain" {
  type = string
  default = null
}
variable "alb_logs_bucket_name" {
  type = string
  default = ""
}
variable "enable_vpc_link" {
  type = string
  default = false
}
