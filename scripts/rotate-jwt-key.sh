#!/usr/bin/env bash
# TASK-078: Rotate JWT Key
set -euo pipefail

# Required environment variables
# CFCHUB_JWT_PRIVATE_KEY_ARN
# CFCHUB_JWT_PUBLIC_KEY_ARN
# AWS_REGION

TMP_PRIV="/tmp/jwt-private-new.pem"
TMP_PUB="/tmp/jwt-public-new.pem"

cleanup() {
    echo "Running cleanup..."
    # Ensure keys are shredded and removed from disk even if the script fails midway
    [ -f "$TMP_PRIV" ] && shred -u "$TMP_PRIV"
    [ -f "$TMP_PUB" ] && shred -u "$TMP_PUB"
}

trap cleanup EXIT ERR

echo "Starting JWT Key Rotation..."

if [ -z "${AWS_REGION:-}" ]; then
  echo "Error: AWS_REGION is not set."
  exit 1
fi
if [ -z "${CFCHUB_JWT_PRIVATE_KEY_ARN:-}" ] || [ -z "${CFCHUB_JWT_PUBLIC_KEY_ARN:-}" ]; then
  echo "Error: CFCHUB_JWT_PRIVATE_KEY_ARN and CFCHUB_JWT_PUBLIC_KEY_ARN must be set."
  exit 1
fi

echo "1. Generating new RSA-4096 private key..."
openssl genrsa -out "$TMP_PRIV" 4096

echo "2. Extracting new public key..."
openssl rsa -pubout -in "$TMP_PRIV" -out "$TMP_PUB"

echo "3. Updating AWS Secrets Manager (Private Key)..."
# put-secret-value automatically moves the current AWSCURRENT to AWSPREVIOUS
aws secretsmanager put-secret-value \
    --secret-id "$CFCHUB_JWT_PRIVATE_KEY_ARN" \
    --secret-string file://"$TMP_PRIV" \
    --region "$AWS_REGION"

echo "4. Updating AWS Secrets Manager (Public Key)..."
aws secretsmanager put-secret-value \
    --secret-id "$CFCHUB_JWT_PUBLIC_KEY_ARN" \
    --secret-string file://"$TMP_PUB" \
    --region "$AWS_REGION"

echo "Rotation complete!"
echo "Trap will execute cleanup to shred new key files from local disk."
echo "API will reload keys at runtime. Old tokens remain valid via AWSPREVIOUS until they expire (1h)."
