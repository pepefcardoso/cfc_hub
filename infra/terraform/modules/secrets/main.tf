variable "environment" {
  type = string
}

variable "db_connection_string" {
  type = string
}

variable "redis_connection_string" {
  type = string
}

# DB Connection String
resource "aws_secretsmanager_secret" "db_conn" {
  name = "cfchub/${var.environment}/db_connection_string"
}

resource "aws_secretsmanager_secret_version" "db_conn" {
  secret_id     = aws_secretsmanager_secret.db_conn.id
  secret_string = var.db_connection_string
}

# Redis Connection String
resource "aws_secretsmanager_secret" "redis_conn" {
  name = "cfchub/${var.environment}/redis_connection_string"
}

resource "aws_secretsmanager_secret_version" "redis_conn" {
  secret_id     = aws_secretsmanager_secret.redis_conn.id
  secret_string = var.redis_connection_string
}

# JWT Private Key (RSA-4096)
resource "tls_private_key" "jwt" {
  algorithm = "RSA"
  rsa_bits  = 4096
}

resource "aws_secretsmanager_secret" "jwt_private_key" {
  name = "cfchub/${var.environment}/jwt_private_key"
}

resource "aws_secretsmanager_secret_version" "jwt_private_key" {
  secret_id     = aws_secretsmanager_secret.jwt_private_key.id
  secret_string = tls_private_key.jwt.private_key_pem
}

# Data Protection Keys
resource "aws_secretsmanager_secret" "data_protection_keys" {
  name = "cfchub/${var.environment}/data_protection_keys"
}

resource "aws_secretsmanager_secret_version" "data_protection_keys" {
  secret_id     = aws_secretsmanager_secret.data_protection_keys.id
  secret_string = "INITIAL_DUMMY_VALUE_REPLACE_IN_APP"
}
