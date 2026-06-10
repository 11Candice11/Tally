terraform {
  required_version = ">= 1.6"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.50"
    }
  }
}

# ── Variables ─────────────────────────────────────────────────────────────────

variable "region"         { default = "eu-north-1" }
variable "instance_type"  { default = "t3.small" }

variable "key_name" {
  description = "Name of the EC2 key pair"
  type        = string
}

variable "public_key_path" {
  description = "Path to your local SSH public key"
  type        = string
  default     = "/Users/candiceyeatman/.ssh/tally_ec2.pub"
}

# ── Provider ──────────────────────────────────────────────────────────────────

provider "aws" {
  region = var.region
}

# ── Secrets Manager — one secret, all app config ──────────────────────────────
# Values are set here as placeholders. Update them in the AWS Console or via:
#   aws secretsmanager put-secret-value --secret-id tally/app \
#     --secret-string '{"POSTGRES_PASSWORD":"...","JWT_KEY":"...","EMAIL_PASSWORD":"...",...}'

resource "aws_secretsmanager_secret" "tally" {
  name                    = "tally/app"
  description             = "Tally Invoice Tracker runtime secrets"
  recovery_window_in_days = 0 # allow immediate deletion if you terraform destroy
  tags                    = { App = "tally" }
}

resource "aws_secretsmanager_secret_version" "tally" {
  secret_id = aws_secretsmanager_secret.tally.id
  secret_string = jsonencode({
    POSTGRES_PASSWORD = "changeme"
    JWT_KEY           = "super_secret_key_for_tally_minimum_32_chars!!"
    EMAIL_SMTP        = "smtp.gmail.com"
    EMAIL_PORT        = "587"
    EMAIL_USERNAME    = ""
    EMAIL_PASSWORD    = ""
    EMAIL_FROM        = ""
    EMAIL_FROM_NAME   = "Tally"
  })

  # Prevent Terraform from overwriting secrets you've updated manually
  lifecycle {
    ignore_changes = [secret_string]
  }
}

# ── IAM role — lets the EC2 read the secret, nothing else ────────────────────

resource "aws_iam_role" "tally_ec2" {
  name = "tally-ec2-role"
  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect    = "Allow"
      Principal = { Service = "ec2.amazonaws.com" }
      Action    = "sts:AssumeRole"
    }]
  })
  tags = { App = "tally" }
}

resource "aws_iam_role_policy" "tally_secrets" {
  name = "tally-read-secrets"
  role = aws_iam_role.tally_ec2.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect   = "Allow"
      Action   = ["secretsmanager:GetSecretValue"]
      Resource = aws_secretsmanager_secret.tally.arn
    }]
  })
}

resource "aws_iam_instance_profile" "tally" {
  name = "tally-ec2-profile"
  role = aws_iam_role.tally_ec2.name
}

# ── Data: latest Ubuntu 24.04 LTS AMI ────────────────────────────────────────

data "aws_ami" "ubuntu" {
  most_recent = true
  owners      = ["099720109477"]

  filter {
    name   = "name"
    values = ["ubuntu/images/hvm-ssd-gp3/ubuntu-noble-24.04-amd64-server-*"]
  }
  filter {
    name   = "virtualization-type"
    values = ["hvm"]
  }
}

# ── Key pair ──────────────────────────────────────────────────────────────────

resource "aws_key_pair" "tally" {
  key_name   = var.key_name
  public_key = file(var.public_key_path)
}

# ── Security group ────────────────────────────────────────────────────────────

resource "aws_security_group" "tally" {
  name        = "tally-sg"
  description = "Tally Invoice Tracker"

  ingress {
    from_port   = 22
    to_port     = 22
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }
  ingress {
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }
  ingress {
    from_port   = 443
    to_port     = 443
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }
  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = { Name = "tally-sg" }
}

# ── EC2 instance ──────────────────────────────────────────────────────────────

resource "aws_instance" "tally" {
  ami                    = data.aws_ami.ubuntu.id
  instance_type          = var.instance_type
  key_name               = aws_key_pair.tally.key_name
  vpc_security_group_ids = [aws_security_group.tally.id]
  iam_instance_profile   = aws_iam_instance_profile.tally.name

  root_block_device {
    volume_size = 20
    volume_type = "gp3"
  }

  user_data = <<-EOF
    #!/bin/bash
    set -e

    # ── Install Docker ────────────────────────────────────────────────────────
    apt-get update -y
    apt-get install -y ca-certificates curl gnupg awscli jq
    install -m 0755 -d /etc/apt/keyrings
    curl -fsSL https://download.docker.com/linux/ubuntu/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
    chmod a+r /etc/apt/keyrings/docker.gpg
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] \
      https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "$VERSION_CODENAME") stable" \
      > /etc/apt/sources.list.d/docker.list
    apt-get update -y
    apt-get install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin
    systemctl enable docker
    systemctl start docker
    usermod -aG docker ubuntu

    # ── App directory ─────────────────────────────────────────────────────────
    mkdir -p /opt/tally
    chown ubuntu:ubuntu /opt/tally

    # ── Write fetch-secrets script (runs on every deploy) ────────────────────
    cat > /opt/tally/fetch-secrets.sh <<'SCRIPT'
    #!/bin/bash
    set -e
    SECRET=$(aws secretsmanager get-secret-value \
      --secret-id tally/app \
      --region ${var.region} \
      --query SecretString \
      --output text)

    # Write .env from the JSON secret
    echo "$SECRET" | jq -r 'to_entries[] | "\(.key)=\(.value)"' > /opt/tally/.env

    # Append CORS origin
    PUBLIC_IP=$(curl -sf http://169.254.169.254/latest/meta-data/public-ipv4 || echo "")
    if [ -n "$PUBLIC_IP" ]; then
      echo "CORS_ORIGINS=http://$PUBLIC_IP" >> /opt/tally/.env
    fi
    SCRIPT
    chmod +x /opt/tally/fetch-secrets.sh

    echo "Bootstrap complete."
  EOF

  tags = { Name = "tally-invoice-tracker" }
}

# ── Elastic IP ────────────────────────────────────────────────────────────────

resource "aws_eip" "tally" {
  instance = aws_instance.tally.id
  domain   = "vpc"
  tags     = { Name = "tally-eip" }
}

# ── Outputs ───────────────────────────────────────────────────────────────────

output "public_ip" {
  value       = aws_eip.tally.public_ip
  description = "EC2 public IP — use this for your domain A record"
}

output "ssh_command" {
  value = "ssh -i ~/.ssh/tally_ec2 ubuntu@${aws_eip.tally.public_ip}"
}

output "secret_arn" {
  value       = aws_secretsmanager_secret.tally.arn
  description = "ARN of the Secrets Manager secret — update values here"
}
