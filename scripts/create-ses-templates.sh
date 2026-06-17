#!/bin/bash
# Script to create all SES templates for CFCHub
# Note: All templates must be sent using From = "CFCHub <noreply@cfchub.com.br>" in the application code.

set -e

echo "Creating SES Templates..."

# Use absolute path or relative to project root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" &> /dev/null && pwd)"
TEMPLATE_DIR="${SCRIPT_DIR}/../src/CFCHub.Infrastructure/Email/Templates"

# Array of template files
TEMPLATES=(
  "cfchub-welcome.json"
  "cfchub-slot-reminder.json"
  "cfchub-contract-ready.json"
  "cfchub-payment-receipt.json"
  "cfchub-doc-expiry-d30.json"
  "cfchub-doc-expiry-d7.json"
  "cfchub-erasure-complete.json"
)

for file in "${TEMPLATES[@]}"; do
  filepath="${TEMPLATE_DIR}/${file}"
  if [ -f "$filepath" ]; then
    echo "Creating template from ${file}..."
    aws ses create-template --cli-input-json "file://${filepath}"
  else
    echo "Error: ${filepath} not found!"
    exit 1
  fi
done

echo "All templates created successfully."
