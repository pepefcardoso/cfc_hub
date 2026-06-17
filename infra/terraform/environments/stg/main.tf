terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
    tls = {
      source  = "hashicorp/tls"
      version = "~> 4.0"
    }
  }
}

provider "aws" {
  region = "sa-east-1"
}

resource "aws_vpc" "main" {
  cidr_block = "10.1.0.0/16"
}

resource "aws_subnet" "private1" {
  vpc_id            = aws_vpc.main.id
  cidr_block        = "10.1.1.0/24"
  availability_zone = "sa-east-1a"
}

resource "aws_subnet" "private2" {
  vpc_id            = aws_vpc.main.id
  cidr_block        = "10.1.2.0/24"
  availability_zone = "sa-east-1b"
}

resource "aws_kms_key" "main" {
  description             = "KMS key for CFCHub Stg"
  deletion_window_in_days = 7
}

resource "random_password" "db_master" {
  length  = 16
  special = false
}

module "rds" {
  source             = "../../modules/rds"
  environment        = "stg"
  vpc_id             = aws_vpc.main.id
  private_subnet_ids = [aws_subnet.private1.id, aws_subnet.private2.id]
  master_password    = random_password.db_master.result
  kms_key_arn        = aws_kms_key.main.arn
}

module "elasticache" {
  source             = "../../modules/elasticache"
  environment        = "stg"
  vpc_id             = aws_vpc.main.id
  private_subnet_ids = [aws_subnet.private1.id, aws_subnet.private2.id]
  kms_key_arn        = aws_kms_key.main.arn
}

module "s3" {
  source      = "../../modules/s3"
  environment = "stg"
}

module "secrets" {
  source                  = "../../modules/secrets"
  environment             = "stg"
  db_connection_string    = "Host=${module.rds.cluster_endpoint};Port=${module.rds.cluster_port};Database=cfchub;Username=postgres;Password=${random_password.db_master.result}"
  redis_connection_string = "${module.elasticache.configuration_endpoint_address}:6379,ssl=True,abortConnect=False"
}

resource "aws_ecs_cluster" "main" {
  name = "cfchub-cluster-stg"
}

module "ecr" {
  source      = "../../modules/ecr"
  environment = "stg"
}

module "alb" {
  source         = "../../modules/alb"
  environment    = "stg"
  vpc_id         = aws_vpc.main.id
  public_subnets = [aws_subnet.private1.id, aws_subnet.private2.id] # Reusing existing subnets for this example
}

module "ecs_api" {
  source                = "../../modules/ecs-api"
  environment           = "stg"
  cluster_id            = aws_ecs_cluster.main.id
  cluster_name          = aws_ecs_cluster.main.name
  vpc_id                = aws_vpc.main.id
  private_subnets       = [aws_subnet.private1.id, aws_subnet.private2.id]
  alb_security_group_id = module.alb.alb_security_group_id
  target_group_arn      = module.alb.target_group_arn
  image_url             = "${module.ecr.api_repository_url}:latest"
}

module "ecs_workers" {
  source          = "../../modules/ecs-workers"
  environment     = "stg"
  cluster_id      = aws_ecs_cluster.main.id
  cluster_name    = aws_ecs_cluster.main.name
  vpc_id          = aws_vpc.main.id
  private_subnets = [aws_subnet.private1.id, aws_subnet.private2.id]
  image_url       = "${module.ecr.workers_repository_url}:latest"
}
