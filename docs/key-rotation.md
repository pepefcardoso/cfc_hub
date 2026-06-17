# JWT Key Rotation Procedure

This document describes the procedure for rotating the JWT RSA signing keys for CFCHub. 

## Overview
CFCHub uses an RSA-4096 key pair to sign and validate JWT tokens. The keys are stored securely in AWS Secrets Manager.
To ensure uninterrupted service without requiring a redeployment, the application API uses `ISecretsManagerService` to load keys dynamically. AWS Secrets Manager automatically retains the previous version of a secret under the `AWSPREVIOUS` staging label, which allows the API to validate existing valid tokens (which have a 1-hour expiration) while signing new tokens with the new `AWSCURRENT` key.

## Executing the Key Rotation
To rotate the key, execute the `rotate-jwt-key.sh` script.

### Prerequisites
Ensure the following environment variables are set in your environment:
- `AWS_REGION`
- `CFCHUB_JWT_PRIVATE_KEY_ARN`
- `CFCHUB_JWT_PUBLIC_KEY_ARN`

You must also have AWS credentials configured (`aws configure` or exported AWS keys) with permissions to perform `secretsmanager:PutSecretValue` on those ARNs.

### Steps
1. Run the rotation script:
   ```bash
   ./scripts/rotate-jwt-key.sh
   ```
2. The script will:
   - Generate a new RSA-4096 key pair in `/tmp`.
   - Update the Secrets Manager secrets using `put-secret-value`. AWS will automatically move the current version to `AWSPREVIOUS`.
   - Shred and remove the temporary files from disk (`shred -u`).

### Verification
You can verify the keys have been rotated by checking AWS Secrets Manager:
```bash
aws secretsmanager list-secret-version-ids --secret-id "$CFCHUB_JWT_PRIVATE_KEY_ARN"
```
You should see one version labeled `AWSCURRENT` and another labeled `AWSPREVIOUS`.
The API will begin signing new JWTs immediately. Existing sessions will remain valid for up to 1 hour using the `AWSPREVIOUS` key.

## Rollback Procedure
If the rotation causes an issue (e.g., API cannot read the new keys, or signature validation fails), you can revert to the previous key.

1. Find the VersionId of the previous key (`AWSPREVIOUS`):
   ```bash
   aws secretsmanager list-secret-version-ids --secret-id "$CFCHUB_JWT_PRIVATE_KEY_ARN"
   aws secretsmanager list-secret-version-ids --secret-id "$CFCHUB_JWT_PUBLIC_KEY_ARN"
   ```
2. Use the AWS CLI to restore the previous versions to `AWSCURRENT`:
   ```bash
   aws secretsmanager update-secret-version-stage \
       --secret-id "$CFCHUB_JWT_PRIVATE_KEY_ARN" \
       --version-stage AWSCURRENT \
       --move-to-version-id <PREVIOUS_VERSION_ID> \
       --remove-from-version-id <CURRENT_VERSION_ID>
       
   aws secretsmanager update-secret-version-stage \
       --secret-id "$CFCHUB_JWT_PUBLIC_KEY_ARN" \
       --version-stage AWSCURRENT \
       --move-to-version-id <PREVIOUS_VERSION_ID> \
       --remove-from-version-id <CURRENT_VERSION_ID>
   ```
3. The API will pick up the restored keys dynamically.
