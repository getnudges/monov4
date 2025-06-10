resource "aws_elasticache_subnet_group" "redis" {
  name       = "nudges-elasticache-subnet-group${var.postfix}"
  subnet_ids = tolist(aws_subnet.private_subnet.*.id)
}

resource "aws_security_group" "redis" {
  name        = "nudges-elasticache-sg"
  description = "Allow traffic for Elasticache"
  vpc_id      = aws_vpc.vpc.id

  ingress {
    from_port   = 6379
    to_port     = 6379
    protocol    = "tcp"
    cidr_blocks = [var.vpc_cidr]
  }
}

resource "aws_elasticache_cluster" "nudges" {
  cluster_id                   = "nudges-redis"
  engine                       = "redis"
  node_type                    = "cache.t2.micro"
  num_cache_nodes              = 1
  # preferred_availability_zones = local.availability_zones
  # az_mode                      = "cross-az"
  parameter_group_name         = "default.redis7"
  subnet_group_name            = aws_elasticache_subnet_group.redis.name
  security_group_ids           = [aws_security_group.redis.id]
  engine_version               = "7.1"
}

resource "aws_ssm_parameter" "redis_host" {
  name  = "/redis/hosts"
  type  = "String"
  value = join(",", [for endpoint in aws_elasticache_cluster.nudges.cache_nodes : "redis://${endpoint.address}:${aws_elasticache_cluster.nudges.port}"])
}

# TODO: make sure we don't use .value directly if we add a password
resource "aws_ssm_parameter" "redis_connection_string" {
  name  = "/redis/connection_string"
  type  = "String"
  value = "${join(",", [for endpoint in aws_elasticache_cluster.nudges.cache_nodes : "${endpoint.address}:${aws_elasticache_cluster.nudges.port}"])},ssl=false,abortConnect=false"
}

output "redis_endpoints" {
  value = join(",", [for endpoint in aws_elasticache_cluster.nudges.cache_nodes : "${endpoint.address}:${aws_elasticache_cluster.nudges.port}"])
}

output "redis_host" {
  value = join(",", [for endpoint in aws_elasticache_cluster.nudges.cache_nodes : "${endpoint.address}"])
}

output "redis_port" {
  value = aws_elasticache_cluster.nudges.port
}

output "redis_connection_string" {
  sensitive = true
  value     = "${join(",", [for endpoint in aws_elasticache_cluster.nudges.cache_nodes : "${endpoint.address}:${aws_elasticache_cluster.nudges.port}"])},ssl=false,abortConnect=false"
}
