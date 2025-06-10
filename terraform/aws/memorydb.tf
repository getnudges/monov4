# resource "aws_memorydb_subnet_group" "redis_subnet_group" {
#   name        = "memorydb-subnet-group"
#   subnet_ids  = aws_subnet.private_subnet.*.id
#   description = "Subnet group for MemoryDB cluster"
# }

# resource "random_password" "redis_password" {
#   length  = 24
#   special = false
# }

# resource "aws_memorydb_user" "nudges_redis_user" {
#   user_name     = "nudges-redis-user"
#   access_string = "on ~* &* +@all"

#   authentication_mode {
#     type      = "password"
#     passwords = [random_password.redis_password.result]
#   }
# }

# resource "aws_memorydb_acl" "redis_acl" {
#   name       = "nudges-redis-acl"
#   user_names = [aws_memorydb_user.nudges_redis_user.user_name]
# }

# resource "aws_memorydb_cluster" "nudges" {
#   acl_name                   = aws_memorydb_acl.redis_acl.name
#   name                       = "nudges-redis"
#   node_type                  = "db.t4g.small"
#   auto_minor_version_upgrade = true
#   num_shards                 = 1
#   subnet_group_name          = aws_memorydb_subnet_group.redis_subnet_group.name
#   security_group_ids         = [aws_security_group.redis.id]
#   tls_enabled                = true
# }

# resource "aws_security_group" "redis" {
#   name   = "redis-public"
#   vpc_id = aws_vpc.vpc.id
#   tags = {
#     Name = "redis-public"
#   }
# }

# # Public because https://docs.aws.amazon.com/memorydb/latest/devguide/accessing-memorydb.html
# resource "aws_security_group_rule" "redis_ingress" {
#   type              = "ingress"
#   from_port         = aws_memorydb_cluster.nudges.port
#   to_port           = aws_memorydb_cluster.nudges.port
#   protocol          = "tcp"
#   cidr_blocks       = ["0.0.0.0/0"]
#   security_group_id = aws_security_group.redis.id
# }

# locals {
#   redis_nodes = flatten([
#     for shard in aws_memorydb_cluster.nudges.shards : [
#       for node in shard.nodes : [
#         for endpoint in node.endpoint : {
#           address = endpoint.address
#           port = endpoint.port
#         }
#       ]
#     ]
#   ])
# }

# output "redis_nodes" {
#   value = join(",", [for node in local.redis_nodes : "${node.address}:${node.port}"])
# }

# output "redis_endpoints" {
#   value = join(",", [for endpoint in aws_memorydb_cluster.nudges.cluster_endpoint : "${endpoint.address}:${aws_memorydb_cluster.nudges.port}"])
# }

# output "redis_username" {
#   value = aws_memorydb_user.nudges_redis_user.user_name
# }

# output "redis_port" {
#   value = aws_memorydb_cluster.nudges.port
# }

# output "redis_password" {
#   sensitive = true
#   value = random_password.redis_password.result
# }

# output "redis_connection_string" {
#   sensitive = true
#   value ="${join(",", [for node in local.redis_nodes : "${node.address}:${node.port}"])},ssl=true,abortConnect=false,user=${aws_memorydb_user.nudges_redis_user.user_name},password=${random_password.redis_password.result}"
# }
