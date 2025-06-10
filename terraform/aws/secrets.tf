
resource "tls_private_key" "jwt_key" {
  algorithm = "RSA"
}

resource "aws_ssm_parameter" "jwt_private_key" {
  name  = "/jwt/private_key"
  type  = "SecureString"
  value = tls_private_key.jwt_key.private_key_pem
}

data "aws_ssm_parameter" "twilio_account_sid" {
  name  = "/twilio/account_sid"
}

data "aws_ssm_parameter" "twilio_message_service_sid" {
  name  = "/twilio/message_service_sid"
}

data "aws_ssm_parameter" "mixpanel_token" {
  name  = "/mixpanel/token"
}

data "aws_ssm_parameter" "twilio_auth_token" {
  name  = "/twilio/auth_token"
}

data "aws_ssm_parameter" "stripe_api_key" {
  name  = "/stripe/api_key"
}

data "aws_ssm_parameter" "stripe_publishable_key" {
  name  = "/stripe/stripe_publishable_key"
}

data "aws_ssm_parameter" "stripe_subscription_endpoint_secret" {
  name  = "/stripe/subscription_endpoint_secret"
}

data "aws_ssm_parameter" "stripe_payment_endpoint_secret" {
  name  = "/stripe/payment_endpoint_secret"
}

data "aws_ssm_parameter" "stripe_product_endpoint_secret" {
  name  = "/stripe/product_endpoint_secret"
}

data "aws_ssm_parameter" "stripe_customer_endpoint_secret" {
  name  = "/stripe/customer_endpoint_secret"
}

data "aws_ssm_parameter" "stripe_portal_url" {
  name  = "/stripe/portal_url"
}

data "aws_ssm_parameter" "stripe_pricing_table" {
  name  = "/stripe/pricing_table"
}

output "jwt_public_key" {
  value = tls_private_key.jwt_key.public_key_openssh
}

output "jwt_private_key" {
  value = tls_private_key.jwt_key.private_key_pem
}
