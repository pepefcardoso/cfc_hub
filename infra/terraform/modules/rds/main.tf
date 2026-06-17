resource "aws_db_subnet_group" "this" {
  name       = "cfchub-${var.environment}-rds-subnet-group"
  subnet_ids = var.private_subnet_ids

  tags = {
    Environment = var.environment
    Name        = "cfchub-${var.environment}-rds-subnet-group"
  }
}

resource "aws_security_group" "rds" {
  name        = "cfchub-${var.environment}-rds-sg"
  description = "Security group for RDS"
  vpc_id      = var.vpc_id

  ingress {
    from_port   = 5432
    to_port     = 5432
    protocol    = "tcp"
    cidr_blocks = ["10.0.0.0/8"]
  }
}

resource "aws_rds_cluster" "this" {
  cluster_identifier      = "cfchub-${var.environment}-aurora-cluster"
  engine                  = "aurora-postgresql"
  engine_version          = "16.1"
  database_name           = var.db_name
  master_username         = var.master_username
  master_password         = var.master_password
  db_subnet_group_name    = aws_db_subnet_group.this.name
  vpc_security_group_ids  = [aws_security_group.rds.id]
  storage_encrypted       = true
  kms_key_id              = var.kms_key_arn
  backup_retention_period = 7
  deletion_protection     = true
  skip_final_snapshot     = false
  final_snapshot_identifier = "cfchub-${var.environment}-aurora-final-snapshot"
}

resource "aws_rds_cluster_instance" "this" {
  count                = 2
  identifier           = "cfchub-${var.environment}-aurora-instance-${count.index}"
  cluster_identifier   = aws_rds_cluster.this.id
  instance_class       = "db.t4g.medium"
  engine               = aws_rds_cluster.this.engine
  engine_version       = aws_rds_cluster.this.engine_version
  db_subnet_group_name = aws_db_subnet_group.this.name
  publicly_accessible  = false
}
