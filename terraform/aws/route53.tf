
# Main domain
# This is the domain used for the admin site and the main application

data "aws_route53_zone" "main" {
  name = var.signup_dns_zone
}
resource "aws_acm_certificate" "main_wildcard" {
  domain_name       = "*.${data.aws_route53_zone.main.name}"
  validation_method = "DNS"
}

resource "aws_route53_record" "main_wildcard" {
  for_each = {
    for dvo in aws_acm_certificate.main_wildcard.domain_validation_options : dvo.domain_name => {
      name   = dvo.resource_record_name
      record = dvo.resource_record_value
      type   = dvo.resource_record_type
    }
  }

  allow_overwrite = true
  name            = each.value.name
  records         = [each.value.record]
  ttl             = 60
  type            = each.value.type
  zone_id         = data.aws_route53_zone.main.zone_id
}

resource "aws_acm_certificate_validation" "main_wildcard" {
  certificate_arn         = local.admin_site_certificate_arn
  validation_record_fqdns = [for record in aws_route53_record.main_wildcard : record.fqdn]
}

# Subscribe domain
# This is the domain used for the subscription service

data "aws_route53_zone" "subscribe" {
  name = var.subscribe_dns_zone
}

resource "aws_acm_certificate" "subscribe" {
  domain_name       = data.aws_route53_zone.subscribe.name
  validation_method = "DNS"
}

resource "aws_route53_record" "subscribe" {
  for_each = {
    for dvo in aws_acm_certificate.subscribe.domain_validation_options : dvo.domain_name => {
      name   = dvo.resource_record_name
      record = dvo.resource_record_value
      type   = dvo.resource_record_type
    }
  }

  allow_overwrite = true
  name            = each.value.name
  records         = [each.value.record]
  ttl             = 60
  type            = each.value.type
  zone_id         = data.aws_route53_zone.subscribe.zone_id
}

resource "aws_acm_certificate_validation" "subscribe" {
  certificate_arn         = aws_acm_certificate.subscribe.arn
  validation_record_fqdns = [for record in aws_route53_record.subscribe : record.fqdn]
}
