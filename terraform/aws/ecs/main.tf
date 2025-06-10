terraform {
  required_providers {
    random = {
      source = "hashicorp/random"
      version = "~> 3"
    }
  }
}

data "aws_region" "current" {}

resource "aws_ecr_repository" "this" {
  name         = "nudges/${var.project_name}"
  force_delete = true
}

resource "aws_ecr_lifecycle_policy" "lasttwoimages" {
  repository = aws_ecr_repository.this.name
  policy = jsonencode({
    "rules" : [
      {
        "rulePriority" : 1,
        "description" : "Keep only the last 2 images",
        "selection" : {
          "tagStatus" : "any",
          "countType" : "imageCountMoreThan",
          "countNumber" : 2
        },
        "action" : {
          "type" : "expire"
        }
      }
    ]
  })
}

resource "aws_cloudwatch_log_group" "this_log_group" {
  name = "/ecs/${var.project_name}"

  retention_in_days = 7
}

resource "aws_ecs_task_definition" "this_task" {
  family                   = var.project_name
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = var.task_cpu
  memory                   = var.task_memory
  task_role_arn            = var.task_role_arn
  execution_role_arn       = var.execution_role_arn

  container_definitions = jsonencode([
    {
      name  = "${var.project_name}"
      image = "${aws_ecr_repository.this.repository_url}:latest"
      portMappings = [
        {
          containerPort = "${var.container_port}"
          protocol      = "tcp"
          name          = "${var.project_name}"
        }
      ]
      # healthCheck = {
      #   command     = ["CMD-SHELL", "wget http://localhost:${var.container_port}${var.health_check_path}"]
      #   interval    = 30
      #   timeout     = 5
      #   startPeriod = 5
      #   retries     = 3
      # }
      # TODO: Add depends_on for other services
      logConfiguration = {
        logDriver = "awslogs"
        options = {
          awslogs-region        = "${data.aws_region.current.name}",
          awslogs-group         = "${aws_cloudwatch_log_group.this_log_group.name}",
          awslogs-create-group  = "true",
          awslogs-stream-prefix = "ecs"
        }
      }
      environment = var.container_environment
      secrets     = var.container_secrets
    }
  ])
}

resource "aws_appautoscaling_target" "ecs_target" {
  max_capacity       = 5
  min_capacity       = 0
  resource_id        = "service/${var.cluster_name}/${aws_ecs_service.this.name}"
  scalable_dimension = "ecs:service:DesiredCount"
  service_namespace  = "ecs"
}

resource "aws_appautoscaling_policy" "cpu_scaling_up" {
  name               = "cpu_autoscale_up"
  service_namespace  = aws_appautoscaling_target.ecs_target.service_namespace
  resource_id        = aws_appautoscaling_target.ecs_target.resource_id
  scalable_dimension = aws_appautoscaling_target.ecs_target.scalable_dimension
  policy_type        = "TargetTrackingScaling"
  target_tracking_scaling_policy_configuration {
    predefined_metric_specification {
      predefined_metric_type = "ECSServiceAverageCPUUtilization"
    }
    target_value = 75.0 # Scale up when CPU usage exceeds 75%
  }
}

# TODO: figure out how to scale down
# resource "aws_appautoscaling_policy" "cpu_scaling_down" {
#   name               = "cpu_autoscale_down"
#   service_namespace  = aws_appautoscaling_target.ecs_target.service_namespace
#   resource_id        = aws_appautoscaling_target.ecs_target.resource_id
#   scalable_dimension = aws_appautoscaling_target.ecs_target.scalable_dimension
#   policy_type        = "TargetTrackingScaling"
#   target_tracking_scaling_policy_configuration {
#     predefined_metric_specification {
#       predefined_metric_type = "ECSServiceAverageCPUUtilization"
#     }
#     target_value     = 50.0 # Scale down when CPU usage falls below 75% (adjust threshold)
#     disable_scale_in = true # Ensures minimum task count of 1
#   }
# }

resource "aws_ecs_service" "this" {
  name            = var.project_name
  launch_type     = "FARGATE"
  cluster         = var.cluster_arn
  task_definition = aws_ecs_task_definition.this_task.arn

  force_new_deployment = false # set to true for debugging
  desired_count        = 1

  network_configuration {
    subnets          = var.private_subnet_ids
    security_groups  = concat(var.service_security_group_ids, aws_security_group.tasks.*.id)
    assign_public_ip = false
  }

  dynamic "load_balancer" {
    for_each = (length(var.public_subnet_ids) > 0) || var.enable_vpc_link ? [1] : []
    content {
      target_group_arn = var.enable_vpc_link ?  aws_lb_target_group.this_internal_target_group[0].arn : aws_lb_target_group.this_target_group[0].arn
      container_name   = var.project_name
      container_port   = var.container_port
    }
  }

  deployment_controller {
    type = "ECS"
  }

  dynamic "service_connect_configuration" {
    for_each = var.enable_service_connect ? [1] : []
    content {
      enabled   = var.enable_service_connect
      namespace = var.service_connect_namespace
      log_configuration {
        log_driver = "awslogs"
        options = {
          awslogs-region        = "${data.aws_region.current.name}"
          awslogs-group         = "${aws_cloudwatch_log_group.this_log_group.name}"
          awslogs-create-group  = "true"
          awslogs-stream-prefix = "service_connect"
        }
      }
    }
  }

  dynamic "service_registries" {
    for_each = var.enable_service_connect ? [1] : []
    content {
      registry_arn   = aws_service_discovery_service.this[0].arn
      container_name = var.project_name
    }
  }

  lifecycle {
    ignore_changes = [
      task_definition,
      desired_count,
    ]
  }
}

resource "aws_service_discovery_service" "this" {
  count        = var.enable_service_connect ? 1 : 0
  name         = var.project_name
  namespace_id = var.service_connect_namespace_id

  dynamic "health_check_custom_config" {
    for_each = length(var.health_check_path) > 0 ? [1] : []
    content {
      failure_threshold = 1
    }
  }
  dns_config {
    namespace_id = var.service_connect_namespace_id
    dns_records {
      ttl  = 10
      type = "A"
    }
    routing_policy = "MULTIVALUE"
  }
}

output "service_discovery_service_arn" {
  value = length(aws_service_discovery_service.this) > 0 ? aws_service_discovery_service.this[0].arn : null
}

resource "aws_lb" "internal_lb" {
  count              = var.enable_vpc_link ? 1 : 0
  name               = "${var.project_name}-private-lb"
  internal           = true
  load_balancer_type = "network"
  security_groups    = [aws_security_group.lb[0].id]
  subnets            = var.private_subnet_ids
  tags = {
    Name = "${var.project_name}-private-lb"
  }
  enable_deletion_protection = false # TODO: turn on for production
  dynamic "access_logs" {
    for_each = length(var.alb_logs_bucket_name) > 0 ? [1] : []
    content {
      bucket  = var.alb_logs_bucket_name
      prefix  = "${var.project_name}-private-lb"
      enabled = true
    }

  }
}

resource "random_pet" "target_group" {
  keepers = {
    project_name = var.project_name
  }
  length = 1
}

resource "aws_lb_target_group" "this_internal_target_group" {
  count       = var.enable_vpc_link ? 1 : 0
  name        = "${var.project_name}-${random_pet.target_group.id}"
  port        = var.container_port
  protocol    = "TCP"
  vpc_id      = var.vpc_id
  target_type = "ip"

  dynamic "health_check" {
    for_each = var.health_check_path != null ? [1] : []
    content {
      path = var.health_check_path
      port = var.container_port
    }
  }
}

resource "aws_lb_listener" "this_internal_listener" {
  count             = var.enable_vpc_link ? 1 : 0
  load_balancer_arn = aws_lb.internal_lb[count.index].arn
  port              = 80
  protocol          = "TCP"

  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.this_internal_target_group[count.index].arn
  }

  lifecycle {
    replace_triggered_by = [aws_lb_target_group.this_internal_target_group[count.index].id]
  }
}

resource "aws_api_gateway_vpc_link" "vpc_link" {
  count       = var.enable_vpc_link ? 1 : 0
  name        = var.project_name
  description = "VPC link for API Gateway to access ${var.project_name} ECS service"
  target_arns = [aws_lb.internal_lb[0].arn]
}

output "vpc_link_id" {
  value = length(aws_api_gateway_vpc_link.vpc_link) > 0 ? aws_api_gateway_vpc_link.vpc_link[0].id : null
}

resource "aws_lb" "this_lb" {
  count              = length(var.public_subnet_ids) > 0 ? 1 : 0
  name               = "${var.project_name}-lb"
  internal           = false
  load_balancer_type = "application"
  security_groups    = [aws_security_group.lb[0].id]
  subnets            = var.public_subnet_ids
  tags = {
    Name = "${var.project_name}-lb"
  }
  enable_deletion_protection = false # TODO: turn on for production
  dynamic "access_logs" {
    for_each = length(var.alb_logs_bucket_name) > 0 ? [1] : []
    content {
      bucket  = var.alb_logs_bucket_name
      prefix  = "${var.project_name}-lb"
      enabled = true
    }

  }
}

resource "aws_lb_listener" "this_listener" {
  count             = length(var.public_subnet_ids) > 0 ? 1 : 0
  load_balancer_arn = aws_lb.this_lb[count.index].arn
  port              = 443
  protocol          = "HTTPS"
  ssl_policy        = "ELBSecurityPolicy-2016-08"
  certificate_arn = var.ssl_certificate_arns[0]
  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.this_target_group[count.index].arn
  }

  lifecycle {
    replace_triggered_by = [aws_lb_target_group.this_target_group[count.index].id]
  }
}

resource "aws_lb_listener_certificate" "ssl_certs" {
  count        = length(var.ssl_certificate_arns)
  listener_arn = aws_lb_listener.this_listener[0].arn

  certificate_arn = var.ssl_certificate_arns[count.index]
}


output "listener_arn" {
  value = length(aws_lb_listener.this_listener) > 0 ? aws_lb_listener.this_listener[0].arn : null
}

output "alb_target_group_arn" {
  value = length(aws_lb_target_group.this_target_group) > 0 ? aws_lb_target_group.this_target_group[0].arn : null
}

resource "aws_lb_target_group" "this_target_group" {
  count       = length(var.public_subnet_ids) > 0 ? 1 : 0
  name        = var.project_name
  port        = var.container_port
  protocol    = "HTTP"
  vpc_id      = var.vpc_id
  target_type = "ip"

  dynamic "health_check" {
    for_each = var.health_check_path != null ? [1] : []
    content {
      path = var.health_check_path
      port = var.container_port
    }
  }
}

resource "aws_security_group" "lb" {
  count  = (length(var.public_subnet_ids) > 0) || var.enable_vpc_link ? 1 : 0
  name   = "${var.project_name}-lb-sg"
  vpc_id = var.vpc_id

  ingress {
    from_port   = var.enable_vpc_link ? 80 : 443
    to_port     = var.enable_vpc_link ? 80 : 443
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = [var.vpc_cidr]
  }

  tags = {
    Name = "${var.project_name}-lb"
  }
}

resource "aws_security_group" "tasks" {
  count  = (length(var.public_subnet_ids) > 0) || var.enable_vpc_link ? 1 : 0
  name   = "${var.project_name}-ecs-sg"
  vpc_id = var.vpc_id

  ingress {
    from_port       = var.container_port
    to_port         = var.container_port
    protocol        = "tcp"
    security_groups = [aws_security_group.lb[0].id]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = [var.vpc_cidr]
  }

  tags = {
    Name = "${var.project_name}-lb"
  }
}

output "load_balancer_dns_name" {
  value = var.enable_vpc_link ? join("", aws_lb.internal_lb.*.dns_name) : join("", aws_lb.this_lb.*.dns_name)
}

output "load_balancer_zone_id" {
  value = var.enable_vpc_link ? join("", aws_lb.internal_lb.*.zone_id) : join("", aws_lb.this_lb.*.zone_id)
}
