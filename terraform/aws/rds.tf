
resource "random_password" "rds_password" {
  length  = 32
  special = false
}

resource "aws_rds_cluster" "aurora" {
  cluster_identifier      = "nudges-aurora"
  availability_zones      = local.availability_zones
  master_username         = local.rds_user
  master_password         = random_password.rds_password.result
  backup_retention_period = 7
  deletion_protection     = false # TODO: set to false for all but prod
  engine                  = "aurora-postgresql"
  engine_mode             = "provisioned"
  engine_version          = "16.1"
  skip_final_snapshot     = true
  apply_immediately       = true
  storage_encrypted       = true

  db_subnet_group_name   = aws_db_subnet_group.db_private_subnet_group.name
  vpc_security_group_ids = [aws_security_group.rds.id]

  serverlessv2_scaling_configuration  {
    min_capacity             = 0.5
    max_capacity             = 16
  }

  tags = {
    Name = "nudges-postgres"
  }
  lifecycle {
    prevent_destroy = false # TODO: set to false for all but prod
  }
}

resource "aws_rds_cluster_instance" "aurora" {
  cluster_identifier         = aws_rds_cluster.aurora.id
  instance_class             = "db.serverless"
  engine                     = aws_rds_cluster.aurora.engine
  engine_version             = aws_rds_cluster.aurora.engine_version
  publicly_accessible        = false
  auto_minor_version_upgrade = true
  db_subnet_group_name       = aws_db_subnet_group.db_private_subnet_group.name
}

resource "aws_security_group" "rds" {
  name   = "rds-public"
  vpc_id = aws_vpc.vpc.id

  tags = {
    Name = "rds-public"
  }
}

resource "aws_security_group_rule" "rds_ingress" {
  type              = "ingress"
  from_port         = aws_rds_cluster.aurora.port
  to_port           = aws_rds_cluster.aurora.port
  protocol          = "tcp"
  cidr_blocks       = [var.vpc_cidr]
  security_group_id = aws_security_group.rds.id
}

resource "aws_db_subnet_group" "db_private_subnet_group" {
  subnet_ids = aws_subnet.private_subnet.*.id
  tags = {
    Name = "db-private-subnet-group"
  }
}

output "rds_cluster_endpoint" {
  value = aws_rds_cluster.aurora.endpoint
}

output "rds_cluster_port" {
  value = aws_rds_cluster.aurora.port
}

output "rds_cluster_password" {
  value = random_password.rds_password.result
}

resource "aws_ssm_parameter" "rds_cluster_password" {
  name  = "/rds/password"
  type  = "SecureString"
  value = random_password.rds_password.result
}

resource "aws_ssm_parameter" "rds_cluster_endpoint" {
  name  = "/rds/endpoint"
  type  = "String"
  value = aws_rds_cluster.aurora.endpoint
}

resource "aws_ssm_parameter" "rds_cluster_user" {
  name  = "/rds/user"
  type  = "String"
  value = local.rds_user
}

resource "aws_ssm_parameter" "rds_cluster_db_port" {
  name  = "/rds/db_port"
  type  = "String"
  value = aws_rds_cluster.aurora.port
}

resource "aws_ssm_parameter" "rds_cluster_userdb_connection_string" {
  name  = "/rds/rds_cluster_userdb_connection_string"
  type  = "SecureString"
  value = "Username=${local.rds_user};Password=${random_password.rds_password.result};Host=${aws_rds_cluster.aurora.endpoint};Port=${aws_rds_cluster.aurora.port};Database=userdb;"
}

resource "aws_ssm_parameter" "rds_cluster_userdb_url" {
  name  = "/rds/rds_cluster_userdb_url"
  type  = "SecureString"
  value = "postgresql://${local.rds_user}:${random_password.rds_password.result}@${aws_rds_cluster.aurora.endpoint}:${aws_rds_cluster.aurora.port}/userdb?schema=public"
}



