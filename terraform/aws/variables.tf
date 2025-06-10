variable "region" {
  type     = string
  nullable = false
}

variable "vpc_cidr" {
  nullable = false
  type = string
}

variable "signup_dns_zone" {
  type     = string
}

variable "subscribe_dns_zone" {
  type     = string
  nullable = false
}

variable "postfix" {
  description = "Postfix to apply to names that have to be globally unique"
  type     = string
  nullable = false
}

variable "environment" {
  type     = string
  nullable = false
}
