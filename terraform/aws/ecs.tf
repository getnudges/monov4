# ####################
# # ECS
# # This file specifies all the required resources to run a containerized application on ECS.
# # It creates a cluster, a task definition, a service, and a load balancer.
# # It also creates a CloudWatch log group and a CloudWatch log stream for the service.
# # It optionally creates a service discovery namespace and a service discovery service.
# # It optionally creates a service connect namespace and a service connect service.
# ####################

resource "aws_service_discovery_private_dns_namespace" "this" {
  name = "nudges.local"
  vpc  = aws_vpc.vpc.id
}

resource "aws_ecs_cluster" "cluster" {
  name = "nudges-cluster"

  setting {
    name  = "containerInsights"
    value = "enabled"
  }
  service_connect_defaults {
    namespace = aws_service_discovery_private_dns_namespace.this.arn
  }
}

resource "aws_iam_role" "ecs_task_execution_role" {
  name = "ecsTaskExecutionRole"
  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "ecs-tasks.amazonaws.com"
        }
      }
    ]
  })
}

resource "aws_iam_policy" "ecr_policy" {
  name = "ecr_policy"
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "ecr:GetAuthorizationToken",
          "logs:CreateLogStream",
          "logs:PutLogEvents",
          "ecr:BatchCheckLayerAvailability",
          "ecr:GetDownloadUrlForLayer",
          "ecr:GetRepositoryPolicy",
          "ecr:DescribeRepositories",
          "ecr:ListImages",
          "ecr:DescribeImages",
          "ecr:BatchGetImage",
          "ssm:GetParameters",
          "secretsmanager:GetSecretValue",
          "kms:Decrypt",
          "cognito-idp:AdminGetUser" # for cognito user pool.  This needs to be more granular in the future
        ]
        Resource = "*"
      }
    ]
  })
}

resource "aws_iam_role_policy_attachment" "ecs_task_execution_role_ecr_policy" {
  policy_arn = aws_iam_policy.ecr_policy.arn
  role       = aws_iam_role.ecs_task_execution_role.name
}

resource "aws_security_group" "ecs_private" {
  name   = "ecs-private"
  vpc_id = aws_vpc.vpc.id

  tags = {
    Name = "ecs-private"
  }
}

resource "aws_security_group_rule" "ecs_egress_redis" {
  type              = "egress"
  from_port         = aws_elasticache_cluster.nudges.port
  to_port           = aws_elasticache_cluster.nudges.port
  protocol          = "tcp"
  cidr_blocks       = [var.vpc_cidr]
  security_group_id = aws_security_group.ecs_private.id
}

resource "aws_security_group_rule" "ecs_egress_rds" {
  type              = "egress"
  from_port         = aws_rds_cluster.aurora.port
  to_port           = aws_rds_cluster.aurora.port
  protocol          = "tcp"
  cidr_blocks       = [var.vpc_cidr]
  security_group_id = aws_security_group.ecs_private.id
}

resource "aws_security_group_rule" "ecs_egress_tls" {
  # we need this for the ECS service to be able to access SSM and Secrets Manager
  type              = "egress"
  from_port         = 443
  to_port           = 443
  protocol          = "tcp"
  cidr_blocks       = ["0.0.0.0/0"]
  security_group_id = aws_security_group.ecs_private.id
}

resource "aws_security_group_rule" "ecs_egress_http" {
  # we need this for the ECS service to be able to access other ECS services (Gateways to GraphQL APIs, etc.)
  type              = "egress"
  from_port         = 80
  to_port           = 80
  protocol          = "tcp"
  cidr_blocks       = [var.vpc_cidr]
  security_group_id = aws_security_group.ecs_private.id
}

# TODO: these ports being exposed are a bit of a hack.
# We should make the containers listen on 80 instead.
resource "aws_security_group_rule" "ecs_egress_containers" {
  # we need this for the ECS service to be able to access other ECS services (Gateways to GraphQL APIs, etc.)
  type              = "egress"
  from_port         = 3000
  to_port           = 3000
  protocol          = "tcp"
  cidr_blocks       = [var.vpc_cidr]
  security_group_id = aws_security_group.ecs_private.id
}

resource "aws_security_group_rule" "ecs_ingess_containers" {
  # we need this for the ECS service to be able to access other ECS services (Gateways to GraphQL APIs, etc.)
  type              = "ingress"
  from_port         = 3000
  to_port           = 3000
  protocol          = "tcp"
  cidr_blocks       = [var.vpc_cidr]
  security_group_id = aws_security_group.ecs_private.id
}

resource "aws_security_group_rule" "ecs_ingress_http" {
  # we need this for the ECS service to be able to access SSM and Secrets Manager
  type              = "ingress"
  from_port         = 80
  to_port           = 80
  protocol          = "tcp"
  cidr_blocks       = [var.vpc_cidr]
  security_group_id = aws_security_group.ecs_private.id
}

resource "aws_s3_bucket" "alb_logs" {
  bucket = "nudges-alb-logs-bucket${var.postfix}"

  force_destroy = true
}

data "aws_elb_service_account" "main" {}

data "aws_iam_policy_document" "s3_bucket_lb_write" {
  policy_id = "s3_bucket_lb_logs"

  statement {
    actions = [
      "s3:PutObject",
    ]
    effect = "Allow"
    resources = [
      "${aws_s3_bucket.alb_logs.arn}/*",
    ]

    principals {
      identifiers = ["${data.aws_elb_service_account.main.arn}"]
      type        = "AWS"
    }
  }

  statement {
    actions = [
      "s3:PutObject"
    ]
    effect    = "Allow"
    resources = ["${aws_s3_bucket.alb_logs.arn}/*"]
    principals {
      identifiers = ["delivery.logs.amazonaws.com"]
      type        = "Service"
    }
  }


  statement {
    actions = [
      "s3:GetBucketAcl"
    ]
    effect    = "Allow"
    resources = ["${aws_s3_bucket.alb_logs.arn}"]
    principals {
      identifiers = ["delivery.logs.amazonaws.com"]
      type        = "Service"
    }
  }
}

resource "aws_s3_bucket_policy" "logs_policy" {
  bucket = aws_s3_bucket.alb_logs.id
  policy = data.aws_iam_policy_document.s3_bucket_lb_write.json
}

module "signup-site" {
  source                     = "./ecs"
  region                     = data.aws_region.current.name
  project_name               = "signup-site"
  container_port             = 3000
  task_role_arn              = aws_iam_role.ecs_task_execution_role.arn
  execution_role_arn         = aws_iam_role.ecs_task_execution_role.arn
  vpc_id                     = aws_vpc.vpc.id
  vpc_cidr                   = var.vpc_cidr
  private_subnet_ids         = aws_subnet.private_subnet.*.id
  public_subnet_ids          = aws_subnet.public_subnet.*.id
  service_security_group_ids = [aws_security_group.ecs_private.id]
  desired_count              = 1
  cluster_arn                = aws_ecs_cluster.cluster.arn
  cluster_name               = aws_ecs_cluster.cluster.name
  health_check_path          = "/health"
  task_cpu                   = 256
  task_memory                = 512
  ssl_certificate_arns = [
    aws_acm_certificate_validation.main_wildcard.certificate_arn,
    aws_acm_certificate_validation.subscribe.certificate_arn
  ]
  container_secrets = [{
    name      = "TWILIO_ACCOUNT_SID"
    valueFrom = "${data.aws_ssm_parameter.twilio_account_sid.arn}"
    }, {
    name      = "TWILIO_AUTH_TOKEN"
    valueFrom = "${data.aws_ssm_parameter.twilio_auth_token.arn}"
    }, {
    name      = "STRIPE_API_KEY"
    valueFrom = "${data.aws_ssm_parameter.stripe_api_key.arn}"
    }, {
    name      = "NEXT_PUBLIC_STRIPE_PUBLIC_KEY"
    valueFrom = "${data.aws_ssm_parameter.stripe_publishable_key.arn}"
    }, {
    name      = "JWT_PRIVATE_KEY"
    valueFrom = "${aws_ssm_parameter.jwt_private_key.arn}"
    }, {
    name      = "REDIS_URL"
    valueFrom = "${aws_ssm_parameter.redis_host.arn}"
    }, {
    name      = "DATABASE_URL"
    valueFrom = "${aws_ssm_parameter.rds_cluster_userdb_url.arn}"
    }, {
    name      = "MIXPANEL_TOKEN"
    valueFrom = "${data.aws_ssm_parameter.mixpanel_token.arn}"
    }, {
    name      = "STRIPE_PORTAL_HOST"
    valueFrom = "${data.aws_ssm_parameter.stripe_portal_url.arn}"
    }, {
    name      = "STRIPE_PRODUCT_BASIC_PRICING_TABLE"
    valueFrom = "${data.aws_ssm_parameter.stripe_pricing_table.arn}"
    }, {
    name      = "TWILIO_MESSAGE_SERVICE_SID"
    valueFrom = "${data.aws_ssm_parameter.twilio_message_service_sid.arn}"
  }]
  container_environment = [{
    name  = "NEXT_PUBLIC_JWT_PUBLIC_KEY"
    value = "${tls_private_key.jwt_key.public_key_openssh}"
    }, {
    name  = "NEXT_PUBLIC_SESSION_LENGTH"
    value = "86400"
    }, {
    name  = "SESSION_LENGTH"
    value = "86400"
    }, {
    name  = "OTP_WINDOW"
    value = "10"
    }, {
    name  = "OTP_STEP"
    value = "1000"
    }, {
    name  = "SHARE_HOST"
    value = "https://${aws_route53_record.signup-site.name}"
    }, {
    name  = "SUBSCRIBE_HOST"
    value = "https://${aws_route53_record.subscribe-link.name}"
    }, {
    name  = "SITE_HOST"
    value = "https://${aws_route53_record.signup-site.name}"
    }, {
    name  = "PORT"
    value = "3000"
    }, {
    name  = "DB_NAME"
    value = "userdb"
  }]
}

resource "aws_route53_record" "signup-site" {
  allow_overwrite = true
  name            = "signup.${data.aws_route53_zone.main.name}"
  records         = [module.signup-site.load_balancer_dns_name]
  ttl             = 60
  type            = "CNAME"
  zone_id         = data.aws_route53_zone.main.zone_id
}

resource "aws_route53_record" "subscribe-link" {
  name    = data.aws_route53_zone.subscribe.name
  type    = "A"
  zone_id = data.aws_route53_zone.subscribe.zone_id

  alias {
    name                   = module.signup-site.load_balancer_dns_name
    zone_id                = module.signup-site.load_balancer_zone_id
    evaluate_target_health = false
  }
}

resource "aws_lb_listener_rule" "subscribe_redirect_rule" {
  listener_arn = module.signup-site.listener_arn
  priority     = 100
  action {
    type = "redirect"
    redirect {
      port        = "443"
      protocol    = "HTTPS"
      host        = "signup.${data.aws_route53_zone.main.name}"
      path        = "/subscribe/#{path}"
      query       = "#{query}"
      status_code = "HTTP_301"
    }
  }

  condition {
    host_header {
      values = [data.aws_route53_zone.subscribe.name]
    }
  }

  condition {
    path_pattern {
      values = ["/*"]
    }
  }
}

module "graphql-gateway" {
  source                       = "./ecs"
  region                       = data.aws_region.current.name
  project_name                 = "graphql-gateway"
  container_port               = 3000
  task_role_arn                = aws_iam_role.ecs_task_execution_role.arn
  execution_role_arn           = aws_iam_role.ecs_task_execution_role.arn
  vpc_id                       = aws_vpc.vpc.id
  vpc_cidr                     = var.vpc_cidr
  private_subnet_ids           = aws_subnet.private_subnet.*.id
  service_security_group_ids   = [aws_security_group.ecs_private.id]
  desired_count                = 1
  cluster_arn                  = aws_ecs_cluster.cluster.arn
  cluster_name                 = aws_ecs_cluster.cluster.name
  task_cpu                     = 256
  task_memory                  = 512
  health_check_path            = "/health"
  enable_service_connect       = true
  service_connect_namespace    = aws_service_discovery_private_dns_namespace.this.arn
  service_connect_namespace_id = aws_service_discovery_private_dns_namespace.this.id
  enable_vpc_link              = true
  container_secrets = [{
    name      = "REDIS_URL"
    valueFrom = "${aws_ssm_parameter.redis_connection_string.arn}"
    }, {
    name      = "COGNITO_AUTHORITY"
    valueFrom = "${aws_ssm_parameter.cognito_pool_endpoint.arn}"
  }]
  container_environment = [{
    name  = "ASPNETCORE_ENVIRONMENT"
    value = "Production"
    }, {
    name  = "ASPNETCORE_URLS"
    value = "http://+:3000"
  }]
}

resource "aws_api_gateway_integration" "graphql_integeration" {
  rest_api_id = aws_api_gateway_rest_api.admin_api.id
  resource_id = aws_api_gateway_resource.graphql_resource.id
  http_method = aws_api_gateway_method.graphql_method.http_method

  type                    = "HTTP_PROXY"
  uri                     = "http://${module.graphql-gateway.load_balancer_dns_name}/graphql"
  integration_http_method = aws_api_gateway_method.graphql_method.http_method
  connection_type         = "VPC_LINK"
  connection_id           = module.graphql-gateway.vpc_link_id
}

resource "aws_api_gateway_deployment" "graphql_deployment" {
  depends_on = [
    aws_api_gateway_integration.graphql_integeration
  ]

  rest_api_id = aws_api_gateway_rest_api.admin_api.id
  stage_name  = var.environment
}

resource "aws_api_gateway_base_path_mapping" "graphql_base_path_mapping" {
  api_id      = aws_api_gateway_rest_api.admin_api.id
  stage_name  = aws_api_gateway_deployment.graphql_deployment.stage_name
  domain_name = aws_api_gateway_domain_name.admin_domain.domain_name
}

module "user-api" {
  source                       = "./ecs"
  region                       = data.aws_region.current.name
  project_name                 = "user-api"
  container_port               = 3000
  task_role_arn                = aws_iam_role.ecs_task_execution_role.arn
  execution_role_arn           = aws_iam_role.ecs_task_execution_role.arn
  vpc_id                       = aws_vpc.vpc.id
  vpc_cidr                     = var.vpc_cidr
  private_subnet_ids           = aws_subnet.private_subnet.*.id
  service_security_group_ids   = [aws_security_group.ecs_private.id]
  desired_count                = 1
  cluster_arn                  = aws_ecs_cluster.cluster.arn
  cluster_name                 = aws_ecs_cluster.cluster.name
  task_cpu                     = 256
  task_memory                  = 512
  health_check_path            = "/health"
  enable_service_connect       = true
  service_connect_namespace    = aws_service_discovery_private_dns_namespace.this.arn
  service_connect_namespace_id = aws_service_discovery_private_dns_namespace.this.id
  container_secrets = [{
    name      = "ConnectionStrings__UserDb"
    valueFrom = "${aws_ssm_parameter.rds_cluster_userdb_connection_string.arn}"
    }, {
    name      = "REDIS_URL"
    valueFrom = "${aws_ssm_parameter.redis_connection_string.arn}"
    }, {
    name      = "STRIPE_API_KEY"
    valueFrom = "${data.aws_ssm_parameter.stripe_api_key.arn}"
    }, {
    name      = "TWILIO_ACCOUNT_SID"
    valueFrom = "${data.aws_ssm_parameter.twilio_account_sid.arn}"
    }, {
    name      = "TWILIO_AUTH_TOKEN"
    valueFrom = "${data.aws_ssm_parameter.twilio_auth_token.arn}"
    }, {
    name      = "TWILIO_MESSAGE_SERVICE_SID"
    valueFrom = "${data.aws_ssm_parameter.twilio_message_service_sid.arn}"
  }]
  container_environment = [{
    name  = "ASPNETCORE_ENVIRONMENT"
    value = "Production"
    }, {
    name  = "ASPNETCORE_URLS"
    value = "http://+:3000"
    }, {
    name  = "Cognito__Authority"
    value = "https://${aws_cognito_user_pool.cognito_pool.endpoint}"
    }, {
    name  = "SUBSCRIBE_HOST"
    value = "https://${aws_route53_record.subscribe-link.name}"
  }]
}

module "auth-api" {
  source                       = "./ecs"
  region                       = data.aws_region.current.name
  project_name                 = "auth-api"
  container_port               = 3000
  task_role_arn                = aws_iam_role.ecs_task_execution_role.arn
  execution_role_arn           = aws_iam_role.ecs_task_execution_role.arn
  vpc_id                       = aws_vpc.vpc.id
  vpc_cidr                     = var.vpc_cidr
  private_subnet_ids           = aws_subnet.private_subnet.*.id
  service_security_group_ids   = [aws_security_group.ecs_private.id]
  desired_count                = 1
  cluster_arn                  = aws_ecs_cluster.cluster.arn
  cluster_name                 = aws_ecs_cluster.cluster.name
  task_cpu                     = 256
  task_memory                  = 512
  health_check_path            = "/health"
  enable_service_connect       = true
  service_connect_namespace    = aws_service_discovery_private_dns_namespace.this.arn
  service_connect_namespace_id = aws_service_discovery_private_dns_namespace.this.id
  enable_vpc_link              = true
  container_secrets = [{
    name      = "REDIS_URL"
    valueFrom = "${aws_ssm_parameter.redis_connection_string.arn}"
  }]
  container_environment = [{
    name  = "ASPNETCORE_ENVIRONMENT"
    value = "Production"
    }, {
    name  = "ASPNETCORE_URLS"
    value = "http://+:3000"
    }, {
    name  = "COGNITO_USER_POOL_ID"
    value = "${aws_cognito_user_pool.cognito_pool.id}"
    }, {
    name  = "COGNITO_CLIENT_ID"
    value = "${aws_cognito_user_pool_client.cognito_client.id}"
  }]
}

resource "aws_api_gateway_integration" "auth_api_integeration" {
  rest_api_id = aws_api_gateway_rest_api.admin_api.id
  resource_id = aws_api_gateway_resource.auth_resource.id
  http_method = aws_api_gateway_method.auth_method.http_method

  type                    = "HTTP_PROXY"
  uri                     = "http://${module.auth-api.load_balancer_dns_name}/login"
  integration_http_method = aws_api_gateway_method.auth_method.http_method
  connection_type         = "VPC_LINK"
  connection_id           = module.auth-api.vpc_link_id
}

resource "aws_api_gateway_deployment" "auth_api_deployment" {
  depends_on = [
    aws_api_gateway_integration.auth_api_integeration
  ]

  rest_api_id = aws_api_gateway_rest_api.admin_api.id
  stage_name  = var.environment
}

resource "aws_api_gateway_base_path_mapping" "auth_api_base_path_mapping" {
  api_id      = aws_api_gateway_rest_api.admin_api.id
  stage_name  = aws_api_gateway_deployment.auth_api_deployment.stage_name
  domain_name = aws_api_gateway_domain_name.admin_domain.domain_name
}


resource "random_password" "graph_monitor_api_key" {
  length  = 32
  special = false
}

resource "aws_ssm_parameter" "graph_monitor_api_key" {
  name  = "/ecs/graph-monitor-api-key"
  type  = "SecureString"
  value = random_password.graph_monitor_api_key.result
}

output "graph_monitor_api_key" {
  value     = random_password.graph_monitor_api_key.result
  sensitive = true
}

module "graph-monitor" {
  source                     = "./ecs"
  region                     = data.aws_region.current.name
  project_name               = "graph-monitor"
  container_port             = 3000
  task_role_arn              = aws_iam_role.ecs_task_execution_role.arn
  execution_role_arn         = aws_iam_role.ecs_task_execution_role.arn
  vpc_id                     = aws_vpc.vpc.id
  vpc_cidr                   = var.vpc_cidr
  private_subnet_ids         = aws_subnet.private_subnet.*.id
  public_subnet_ids          = aws_subnet.public_subnet.*.id
  service_security_group_ids = [aws_security_group.ecs_private.id]
  desired_count              = 1
  cluster_arn                = aws_ecs_cluster.cluster.arn
  cluster_name               = aws_ecs_cluster.cluster.name
  task_cpu                   = 256
  task_memory                = 512
  health_check_path          = "/health"
  ssl_certificate_arns       = [aws_acm_certificate_validation.main_wildcard.certificate_arn]
  container_secrets = [{
    name      = "REDIS_URL"
    valueFrom = "${aws_ssm_parameter.redis_connection_string.arn}"
    }, {
    name      = "API_KEY"
    valueFrom = "${aws_ssm_parameter.graph_monitor_api_key.arn}"
  }]
  container_environment = [{
    name  = "ASPNETCORE_ENVIRONMENT"
    value = "Production"
    }, {
    name  = "HTTP_PORTS"
    value = "3000"
  }]
}

resource "aws_route53_record" "graph-monitor" {
  allow_overwrite = true
  name            = "monitor.${data.aws_route53_zone.main.name}"
  records         = [module.graph-monitor.load_balancer_dns_name]
  ttl             = 60
  type            = "CNAME"
  zone_id         = data.aws_route53_zone.main.zone_id
}

output "graph_monitor_api_url" {
  value = "https://monitor.${data.aws_route53_zone.main.name}"
}

resource "random_password" "nudges_functions_api_key" {
  length  = 32
  special = false
}

resource "aws_ssm_parameter" "nudges_functions_api_key" {
  name  = "/ecs/webhooks-api-key"
  type  = "SecureString"
  value = random_password.nudges_functions_api_key.result
}

output "nudges_functions_api_key" {
  value     = random_password.nudges_functions_api_key.result
  sensitive = true
}

module "webhooks" {
  source                     = "./ecs"
  region                     = data.aws_region.current.name
  project_name               = "webhooks"
  container_port             = 3000
  task_role_arn              = aws_iam_role.ecs_task_execution_role.arn
  execution_role_arn         = aws_iam_role.ecs_task_execution_role.arn
  vpc_id                     = aws_vpc.vpc.id
  vpc_cidr                   = var.vpc_cidr
  private_subnet_ids         = aws_subnet.private_subnet.*.id
  public_subnet_ids          = aws_subnet.public_subnet.*.id
  service_security_group_ids = [aws_security_group.ecs_private.id]
  desired_count              = 1
  cluster_arn                = aws_ecs_cluster.cluster.arn
  cluster_name               = aws_ecs_cluster.cluster.name
  task_cpu                   = 256
  task_memory                = 512
  health_check_path          = "/health"
  ssl_certificate_arns       = [aws_acm_certificate_validation.main_wildcard.certificate_arn]
  container_secrets = [{
    name      = "REDIS_URL"
    valueFrom = "${aws_ssm_parameter.redis_connection_string.arn}"
    }, {
    name      = "API_KEY"
    valueFrom = "${aws_ssm_parameter.nudges_functions_api_key.arn}"
    }, {
    name      = "TWILIO_ACCOUNT_SID"
    valueFrom = "${data.aws_ssm_parameter.twilio_account_sid.arn}"
    }, {
    name      = "TWILIO_AUTH_TOKEN"
    valueFrom = "${data.aws_ssm_parameter.twilio_auth_token.arn}"
    }, {
    name      = "STRIPE_API_KEY"
    valueFrom = "${data.aws_ssm_parameter.stripe_api_key.arn}"
    }, {
    name      = "STRIPE_SUBSCRIPTION_ENDPOINT_SECRET"
    valueFrom = "${data.aws_ssm_parameter.stripe_subscription_endpoint_secret.arn}"
    }, {
    name      = "STRIPE_PRODUCT_ENDPOINT_SECRET"
    valueFrom = "${data.aws_ssm_parameter.stripe_product_endpoint_secret.arn}"
    }, {
    name      = "STRIPE_PAYMENT_ENDPOINT_SECRET"
    valueFrom = "${data.aws_ssm_parameter.stripe_payment_endpoint_secret.arn}"
    }, {
    name      = "STRIPE_CUSTOMER_ENDPOINT_SECRET"
    valueFrom = "${data.aws_ssm_parameter.stripe_customer_endpoint_secret.arn}"
    }, {
    name      = "MixpanelOptions__Token"
    valueFrom = "${data.aws_ssm_parameter.mixpanel_token.arn}"
    }, {
    name      = "ConnectionStrings__UserDb"
    valueFrom = "${aws_ssm_parameter.rds_cluster_userdb_connection_string.arn}"
    }, {
    name      = "TWILIO_MESSAGE_SERVICE_SID"
    valueFrom = "${data.aws_ssm_parameter.twilio_message_service_sid.arn}"
    }, {
    name      = "StripePortalUrl"
    valueFrom = "${data.aws_ssm_parameter.stripe_portal_url.arn}"
  }]
  container_environment = [{
    name  = "ASPNETCORE_ENVIRONMENT"
    value = "Production"
    }, {
    name  = "HTTP_PORTS"
    value = "3000"
    }, {
    name  = "ClientLinkBaseUri"
    value = "https://${data.aws_route53_zone.subscribe.name}"
    }, {
    name  = "SMS_LINK_BASE_URL"
    value = "https://signup.${data.aws_route53_zone.main.name}/announcement"
    }, {
    name  = "AccountUrl"
    value = "https://signup.${data.aws_route53_zone.main.name}/account"
  }]
}

resource "aws_route53_record" "functions" {
  allow_overwrite = true
  name            = "funcs.${data.aws_route53_zone.main.name}"
  records         = [module.webhooks.load_balancer_dns_name]
  ttl             = 60
  type            = "CNAME"
  zone_id         = data.aws_route53_zone.main.zone_id
}

output "nudges_functions_api_url" {
  value = "https://${aws_route53_record.functions.name}"
}
