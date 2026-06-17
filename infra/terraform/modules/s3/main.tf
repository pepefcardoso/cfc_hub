# Documents Bucket
resource "aws_s3_bucket" "documents" {
  bucket = "cfchub-${var.environment}-documents"
}

resource "aws_s3_bucket_server_side_encryption_configuration" "documents" {
  bucket = aws_s3_bucket.documents.id
  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

resource "aws_s3_bucket_public_access_block" "documents" {
  bucket                  = aws_s3_bucket.documents.id
  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_iam_policy" "documents_policy" {
  name        = "cfchub-${var.environment}-documents-policy"
  description = "Policy for documents bucket"
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = [
          "s3:PutObject",
          "s3:GetObject",
          "s3:DeleteObject",
          "s3:ListBucket"
        ]
        Effect   = "Allow"
        Resource = [
          aws_s3_bucket.documents.arn,
          "${aws_s3_bucket.documents.arn}/*"
        ]
      }
    ]
  })
}


# Medical Bucket
resource "aws_s3_bucket" "medical" {
  bucket = "cfchub-${var.environment}-medical"

  object_lock_enabled = true
}

resource "aws_s3_bucket_server_side_encryption_configuration" "medical" {
  bucket = aws_s3_bucket.medical.id
  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

resource "aws_s3_bucket_public_access_block" "medical" {
  bucket                  = aws_s3_bucket.medical.id
  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_versioning" "medical" {
  bucket = aws_s3_bucket.medical.id
  versioning_configuration {
    status = "Enabled"
  }
}

resource "aws_s3_bucket_object_lock_configuration" "medical" {
  bucket = aws_s3_bucket.medical.id
  depends_on = [aws_s3_bucket_versioning.medical]
  
  rule {
    default_retention {
      mode  = "COMPLIANCE"
      years = 5
    }
  }
}

resource "aws_iam_policy" "medical_policy" {
  name        = "cfchub-${var.environment}-medical-policy"
  description = "Policy for medical bucket"
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = [
          "s3:PutObject",
          "s3:GetObject",
          "s3:DeleteObject",
          "s3:ListBucket"
        ]
        Effect   = "Allow"
        Resource = [
          aws_s3_bucket.medical.arn,
          "${aws_s3_bucket.medical.arn}/*"
        ]
      }
    ]
  })
}

output "documents_bucket_name" {
  value = aws_s3_bucket.documents.bucket
}

output "medical_bucket_name" {
  value = aws_s3_bucket.medical.bucket
}
