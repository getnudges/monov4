# # IAM Role for API Gateway
# resource "aws_iam_role" "api_gateway_role" {
#   name = "api_gateway_role"

#   assume_role_policy = jsonencode({
#     "Version" : "2012-10-17",
#     "Statement" : [
#       {
#         "Effect" : "Allow",
#         "Principal" : {
#           "Service" : "apigateway.amazonaws.com"
#         },
#         "Action" : "sts:AssumeRole"
#       }
#     ]
#   })
# }

# # IAM Policy for API Gateway to access ECS
# resource "aws_iam_policy" "api_gateway_policy" {
#   name        = "api_gateway_policy"
#   description = "Policy for API Gateway to access ECS"

#   policy = jsonencode({
#     "Version" : "2012-10-17",
#     "Statement" : [
#       {
#         "Effect" : "Allow",
#         "Action" : "ecs:RunTask",
#         "Resource" : "*"
#       }
#     ]
#   })
# }

# # Attach API Gateway policy to the role
# resource "aws_iam_role_policy_attachment" "api_gateway_policy_attachment" {
#   role       = aws_iam_role.api_gateway_role.name
#   policy_arn = aws_iam_policy.api_gateway_policy.arn
# }

resource "aws_api_gateway_rest_api" "admin_api" {
  name        = "unad_admin_api"
  description = "API gateway for admin site"
}

resource "aws_api_gateway_resource" "graphql_resource" {
  rest_api_id = aws_api_gateway_rest_api.admin_api.id
  parent_id   = aws_api_gateway_rest_api.admin_api.root_resource_id
  path_part   = "graphql"
}

resource "aws_api_gateway_method" "graphql_method" {
  rest_api_id   = aws_api_gateway_rest_api.admin_api.id
  resource_id   = aws_api_gateway_resource.graphql_resource.id
  http_method   = "POST"
  authorization = "NONE" # TODO: look into cognito authorizer for this
}

resource "aws_api_gateway_resource" "auth_resource" {
  rest_api_id = aws_api_gateway_rest_api.admin_api.id
  parent_id   = aws_api_gateway_rest_api.admin_api.root_resource_id
  path_part   = "auth"
}

resource "aws_api_gateway_method" "auth_method" {
  rest_api_id   = aws_api_gateway_rest_api.admin_api.id
  resource_id   = aws_api_gateway_resource.auth_resource.id
  http_method   = "POST"
  authorization = "NONE" # TODO: look into cognito authorizer for this
}

resource "aws_api_gateway_domain_name" "admin_domain" {
  domain_name              = local.admin_site_domain_name
  regional_certificate_arn = local.admin_site_certificate_arn
  security_policy          = "TLS_1_2"
  endpoint_configuration {
    types = ["REGIONAL"]
  }
}

resource "aws_route53_record" "admin_site" {
  zone_id = data.aws_route53_zone.main.zone_id
  name    = local.admin_site_domain_name
  type    = "A"

  alias {
    name                   = aws_api_gateway_domain_name.admin_domain.regional_domain_name
    zone_id                = aws_api_gateway_domain_name.admin_domain.regional_zone_id
    evaluate_target_health = true
  }
}
