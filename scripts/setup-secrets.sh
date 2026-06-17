#!/usr/bin/env bash
# TASK-078: Initial Secrets Setup (JWT and Data Protection)
set -euo pipefail

# Required environment variables
# CFCHUB_JWT_PRIVATE_KEY_ARN
# CFCHUB_JWT_PUBLIC_KEY_ARN
# CFCHUB_DATA_PROTECTION_KEY_ARN
# AWS_REGION

TMP_PRIV="/tmp/jwt-private.pem"
TMP_PUB="/tmp/jwt-public.pem"
TMP_DP="/tmp/dp-key.bin"
TMP_DP_B64="/tmp/dp-key.b64"

cleanup() {
    echo "Running cleanup..."
    # Ensure keys are shredded and removed from disk even if the script fails midway
    [ -f "$TMP_PRIV" ] && shred -u "$TMP_PRIV"
    [ -f "$TMP_PUB" ] && shred -u "$TMP_PUB"
    [ -f "$TMP_DP" ] && shred -u "$TMP_DP"
    [ -f "$TMP_DP_B64" ] && shred -u "$TMP_DP_B64"
}

trap cleanup EXIT ERR

echo "Starting Secrets Manager setup for CFCHub..."

if [ -z "${AWS_REGION:-}" ]; then
  echo "Error: AWS_REGION is not set."
  exit 1
fi

echo "1. Generating RSA-4096 private key for JWT signing..."
openssl genrsa -out "$TMP_PRIV" 4096

echo "2. Extracting public key..."
openssl rsa -pubout -in "$TMP_PRIV" -out "$TMP_PUB"

echo "3. Storing private key in AWS Secrets Manager..."
if [ -n "${CFCHUB_JWT_PRIVATE_KEY_ARN:-}" ]; then
    aws secretsmanager put-secret-value \
        --secret-id "$CFCHUB_JWT_PRIVATE_KEY_ARN" \
        --secret-string file://"$TMP_PRIV" \
        --region "$AWS_REGION"
    echo "Private key stored."
else
    echo "Warning: CFCHUB_JWT_PRIVATE_KEY_ARN not set, skipping AWS Secrets Manager upload."
fi

echo "4. Storing public key in AWS Secrets Manager..."
if [ -n "${CFCHUB_JWT_PUBLIC_KEY_ARN:-}" ]; then
    aws secretsmanager put-secret-value \
        --secret-id "$CFCHUB_JWT_PUBLIC_KEY_ARN" \
        --secret-string file://"$TMP_PUB" \
        --region "$AWS_REGION"
    echo "Public key stored."
else
    echo "Warning: CFCHUB_JWT_PUBLIC_KEY_ARN not set, skipping AWS Secrets Manager upload."
fi

echo "5. Initializing Data Protection Key (Global KMS-wrapped)..."
# Generate a 256-bit (32 byte) key for Data Protection
openssl rand -out "$TMP_DP" 32
if [ -n "${CFCHUB_DATA_PROTECTION_KEY_ARN:-}" ]; then
    # Base64 encode into a file to avoid passing the secret in command-line arguments
    base64 < "$TMP_DP" > "$TMP_DP_B64"
    aws secretsmanager put-secret-value \
        --secret-id "$CFCHUB_DATA_PROTECTION_KEY_ARN" \
        --secret-string file://"$TMP_DP_B64" \
        --region "$AWS_REGION"
    echo "Data Protection key stored."
else
    echo "Warning: CFCHUB_DATA_PROTECTION_KEY_ARN not set, skipping AWS Secrets Manager upload."
fi

echo "Setup complete. Trap will execute cleanup to shred keys from local disk."
