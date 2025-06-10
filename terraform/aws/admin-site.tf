
locals {
  admin_site_domain_name     = "portal.${data.aws_route53_zone.main.name}"
  admin_site_certificate_arn = aws_acm_certificate.main_wildcard.arn
}

resource "aws_s3_bucket" "admin_site" {
  bucket        = "nudges-admin-site${var.postfix}"
  force_destroy = var.environment == "production" ? false : true
}

# resource "aws_s3_bucket_website_configuration" "admin_site" {
#   bucket = aws_s3_bucket.admin_site.id

#   index_document {
#     suffix = "index.html"
#   }

#   error_document {
#     key = "error.html"
#   }
# }

# resource "aws_s3_bucket_acl" "admin_site_acl" {
#   bucket = aws_s3_bucket.admin_site.id

#   acl = "public-read"
# }

# resource "aws_s3_bucket_policy" "admin_site_bucket_policy" {
#   bucket = aws_s3_bucket.admin_site.id

#   policy = jsonencode({
#     Version = "2012-10-17"
#     Statement = [
#       {
#         Action    = ["s3:GetObject"]
#         Effect    = "Allow"
#         Resource  = "${aws_s3_bucket.admin_site.arn}/*"
#         Principal = "*"
#       },
#     ]
#   })
# }

# resource "aws_api_gateway_resource" "admin_site_resource" {
#   rest_api_id = aws_api_gateway_rest_api.admin_api.id
#   parent_id   = aws_api_gateway_rest_api.admin_api.root_resource_id
#   path_part   = "{proxy+}"
# }

# resource "aws_api_gateway_method" "admin_site_method" {
#   rest_api_id   = aws_api_gateway_rest_api.admin_api.id
#   resource_id   = aws_api_gateway_resource.admin_site_resource.id
#   http_method   = "GET"
#   authorization = "NONE" # TODO: look into cognito authorizer for this
# }

# resource "aws_api_gateway_integration" "admin_site_integeration" {
#   rest_api_id = aws_api_gateway_rest_api.admin_api.id
#   resource_id = aws_api_gateway_resource.admin_site_resource.id
#   http_method = aws_api_gateway_method.admin_site_method.http_method

#   type                    = "HTTP_PROXY"
#   uri                     = "http://${aws_s3_bucket_website_configuration.admin_site.website_endpoint}/"
#   integration_http_method = aws_api_gateway_method.admin_site_method.http_method
# }

# resource "aws_api_gateway_deployment" "admin_site_deployment" {
#   rest_api_id = aws_api_gateway_rest_api.admin_api.id
#   stage_name  = var.environment

#   depends_on = [
#     aws_api_gateway_integration.admin_site_integeration
#   ]
# }

output "admin_site_bucket_name" {
  value = aws_s3_bucket.admin_site.bucket
}

