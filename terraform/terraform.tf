terraform {
  backend "s3" {
  }
}

module "aws" {
  source             = "./aws"
  region             = var.AWS_REGION
  signup_dns_zone    = var.SIGNUP_DNS_ZONE
  subscribe_dns_zone = var.SUBSCRIBE_DNS_ZONE
  vpc_cidr           = var.VPC_CIDR
  environment        = var.ENVIRONMENT
  postfix            = var.ENVIRONMENT == "production" ? "" : "-${var.ENVIRONMENT}"
}

module "github" {
  source                 = "./github"
  token                  = var.GITHUB_TOKEN
  db_host                = module.aws.rds_cluster_endpoint
  db_port                = module.aws.rds_cluster_port
  db_pass                = module.aws.rds_cluster_password
  jumpbox_host           = module.aws.jumpbox_host
  graph_monitor_url      = module.aws.graph_monitor_api_url
  graph_monitor_api_key  = module.aws.graph_monitor_api_key
  redis_host             = module.aws.redis_host
  redis_port             = module.aws.redis_port
  environment            = var.ENVIRONMENT
  postfix                = var.ENVIRONMENT == "production" ? "" : "-${var.ENVIRONMENT}"
  admin_site_bucket_name = module.aws.admin_site_bucket_name
}


