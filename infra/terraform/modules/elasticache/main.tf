resource "aws_elasticache_subnet_group" "this" {
  name       = "cfchub-${var.environment}-redis-subnet-group"
  subnet_ids = var.private_subnet_ids
}

resource "aws_security_group" "redis" {
  name        = "cfchub-${var.environment}-redis-sg"
  description = "Security group for Redis"
  vpc_id      = var.vpc_id

  ingress {
    from_port   = 6379
    to_port     = 6379
    protocol    = "tcp"
    cidr_blocks = ["10.0.0.0/8"]
  }
}

resource "aws_elasticache_replication_group" "this" {
  replication_group_id       = "cfchub-${var.environment}-redis"
  description                = "Redis cluster for CFCHub"
  node_type                  = "cache.t4g.medium"
  port                       = 6379
  engine                     = "redis"
  engine_version             = "7.1"
  parameter_group_name       = "default.redis7.cluster.on"
  automatic_failover_enabled = true
  multi_az_enabled           = true
  subnet_group_name          = aws_elasticache_subnet_group.this.name
  security_group_ids         = [aws_security_group.redis.id]
  at_rest_encryption_enabled = true
  kms_key_id                 = var.kms_key_arn
  transit_encryption_enabled = true

  num_node_groups         = 2
  replicas_per_node_group = 1
}

output "configuration_endpoint_address" {
  value = aws_elasticache_replication_group.this.configuration_endpoint_address
}
