#!/bin/bash
# Usage: ./deploy.sh <EC2_PUBLIC_IP> [ssh_key_path]
# Example: ./deploy.sh 56.228.45.59
#          ./deploy.sh 56.228.45.59 ~/.ssh/my_key

set -e

EC2_IP="${1:?Usage: ./deploy.sh <EC2_PUBLIC_IP> [ssh_key_path]}"
SSH_KEY="${2:-~/.ssh/tally_ec2}"
REMOTE="ubuntu@${EC2_IP}"
SSH="ssh -i ${SSH_KEY} -o StrictHostKeyChecking=no ${REMOTE}"

echo "▶ Deploying Tally to ${EC2_IP}..."

# ── 1. Wait for SSH ───────────────────────────────────────────────────────────
echo "▶ Waiting for SSH..."
for i in $(seq 1 20); do
  ssh -i "${SSH_KEY}" -o StrictHostKeyChecking=no -o ConnectTimeout=5 \
    "${REMOTE}" "echo ok" 2>/dev/null && break
  echo "  Attempt ${i}/20..."
  sleep 10
done

# ── 2. Fix permissions ────────────────────────────────────────────────────────
echo "▶ Fixing permissions..."
$SSH "sudo chown -R ubuntu:ubuntu /opt/tally"

# ── 3. Sync code (no secrets) ─────────────────────────────────────────────────
echo "▶ Copying files..."
rsync -az --progress \
  --exclude '.git' \
  --exclude '.env' \
  --exclude 'node_modules' \
  --exclude '**/bin' \
  --exclude '**/obj' \
  --exclude '**/.vite' \
  --exclude 'terraform/.terraform' \
  --exclude 'terraform/terraform.tfstate*' \
  -e "ssh -i ${SSH_KEY} -o StrictHostKeyChecking=no" \
  . "${REMOTE}:/opt/tally/"

# ── 4. Copy .env ──────────────────────────────────────────────────────────────
echo "▶ Copying .env..."
scp -i "${SSH_KEY}" -o StrictHostKeyChecking=no .env "${REMOTE}:/opt/tally/.env"

# ── 5. Build and start containers ─────────────────────────────────────────────
echo "▶ Building and starting containers..."
$SSH bash -s <<'REMOTE_SCRIPT'
  set -e
  cd /opt/tally
  docker compose build --no-cache
  docker compose up -d --remove-orphans

  echo "▶ Waiting for health check..."
  sleep 15
  docker compose ps
REMOTE_SCRIPT

echo ""
echo "✓ Deployed! App is live at: http://${EC2_IP}"
echo "  API health: http://${EC2_IP}/healthz"
echo "  SSH access: ssh -i ${SSH_KEY} ubuntu@${EC2_IP}"
