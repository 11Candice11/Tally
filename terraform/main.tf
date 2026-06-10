terraform {
  required_providers {
    postgresql = {
      source  = "cyrilgdn/postgresql"
      version = "~> 1.22"
    }
  }
}

# ── Variables ─────────────────────────────────────────────────────────────────

variable "pg_host"     { default = "localhost" }
variable "pg_port"     { default = 5433 }
variable "pg_username" { default = "postgres" }
variable "pg_password" { default = "postgres" }
variable "pg_database" { default = "invoicetracker" }

provider "postgresql" {
  host     = var.pg_host
  port     = var.pg_port
  username = var.pg_username
  password = var.pg_password
  database = var.pg_database
  sslmode  = "disable"
  superuser = false
}

# ── Database ──────────────────────────────────────────────────────────────────

resource "postgresql_database" "invoicetracker" {
  name = var.pg_database
}

# ── Schema + seed (applied via psql) ─────────────────────────────────────────

resource "local_file" "seed_sql" {
  filename = "${path.module}/seed.sql"
  content  = <<-SQL
    -- ── Tables ────────────────────────────────────────────────────────────────

    CREATE TABLE IF NOT EXISTS "Users" (
      "Id"                       SERIAL       PRIMARY KEY,
      "Name"                     TEXT         NOT NULL,
      "Email"                    TEXT         NOT NULL UNIQUE,
      "PasswordHash"             TEXT         NOT NULL,
      "PasswordResetToken"       TEXT,
      "PasswordResetTokenExpiry" TIMESTAMPTZ,
      "CreatedAt"                TIMESTAMPTZ  NOT NULL DEFAULT NOW()
    );

    CREATE TABLE IF NOT EXISTS "Invoices" (
      "Id"            SERIAL       PRIMARY KEY,
      "InvoiceNumber" TEXT         NOT NULL UNIQUE,
      "ClientName"    TEXT         NOT NULL,
      "ClientEmail"   TEXT         NOT NULL,
      "Status"        TEXT         NOT NULL DEFAULT 'Draft'
                                   CHECK ("Status" IN ('Draft','Sent','Paid','Overdue','Cancelled')),
      "IssueDate"     TIMESTAMPTZ  NOT NULL,
      "DueDate"       TIMESTAMPTZ  NOT NULL,
      "VatRate"       NUMERIC      NOT NULL DEFAULT 0.15,
      "Notes"         TEXT,
      "UserId"        INTEGER      NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE
    );

    CREATE TABLE IF NOT EXISTS "LineItems" (
      "Id"          SERIAL   PRIMARY KEY,
      "Description" TEXT     NOT NULL,
      "Qty"         INTEGER  NOT NULL DEFAULT 1,
      "UnitPrice"   NUMERIC  NOT NULL,
      "InvoiceId"   INTEGER  NOT NULL REFERENCES "Invoices"("Id") ON DELETE CASCADE
    );

    -- ── Indexes ───────────────────────────────────────────────────────────────

    CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Email"         ON "Users"("Email");
    CREATE        INDEX IF NOT EXISTS "IX_Invoices_UserId"     ON "Invoices"("UserId");
    CREATE        INDEX IF NOT EXISTS "IX_LineItems_InvoiceId" ON "LineItems"("InvoiceId");

    -- ── Seed users ────────────────────────────────────────────────────────────
    -- Password for all seed users: Password123!

    INSERT INTO "Users" ("Name", "Email", "PasswordHash") VALUES
      ('Alice Demo',   'alice@tally.dev', '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi'),
      ('Bob Invoicer', 'bob@tally.dev',   '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi')
    ON CONFLICT ("Email") DO NOTHING;

    -- ── Seed invoices ─────────────────────────────────────────────────────────

    INSERT INTO "Invoices" ("InvoiceNumber", "ClientName", "ClientEmail", "Status", "IssueDate", "DueDate", "VatRate", "Notes", "UserId") VALUES
      ('INV-202504-001', 'Morebo Financial Solutions', 'josh@morebo.co.za',      'Paid',    '2025-04-01', '2025-04-15', 0.15, 'Payment received via EFT. Reference: MOR-042.', (SELECT "Id" FROM "Users" WHERE "Email" = 'alice@tally.dev')),
      ('INV-202504-002', 'Lulurai Body Corporate',     'accounts@lulurai.co.za', 'Sent',    '2025-03-28', '2025-04-12', 0.15, NULL,                                            (SELECT "Id" FROM "Users" WHERE "Email" = 'alice@tally.dev')),
      ('INV-202504-003', 'Third Dimension Prints',     'billing@3dp.co.za',      'Overdue', '2025-03-15', '2025-03-29', 0.15, NULL,                                            (SELECT "Id" FROM "Users" WHERE "Email" = 'alice@tally.dev')),
      ('INV-202504-004', 'Josh Raad',                  'josh@raad.dev',          'Draft',   '2025-03-10', '2025-03-24', 0.15, NULL,                                            (SELECT "Id" FROM "Users" WHERE "Email" = 'alice@tally.dev')),
      ('INV-202504-005', 'Claire Anne Zoghby',         'claire@zoghby.com',      'Paid',    '2025-03-01', '2025-03-15', 0.15, NULL,                                            (SELECT "Id" FROM "Users" WHERE "Email" = 'alice@tally.dev')),
      ('INV-202504-006', 'Acme Corp',                  'billing@acme.com',       'Sent',    '2025-04-05', '2025-04-20', 0.15, 'Net 15 terms agreed.',                          (SELECT "Id" FROM "Users" WHERE "Email" = 'bob@tally.dev')),
      ('INV-202504-007', 'Globex Industries',          'ap@globex.com',          'Draft',   '2025-04-10', '2025-04-25', 0.15, NULL,                                            (SELECT "Id" FROM "Users" WHERE "Email" = 'bob@tally.dev'))
    ON CONFLICT ("InvoiceNumber") DO NOTHING;

    -- ── Seed line items ───────────────────────────────────────────────────────

    INSERT INTO "LineItems" ("Description", "Qty", "UnitPrice", "InvoiceId") VALUES
      ('FinAdvisor – Sprint 7 Development',  1, 12000.00, (SELECT "Id" FROM "Invoices" WHERE "InvoiceNumber" = 'INV-202504-001')),
      ('Microsoft Graph API Integration',    1,  8000.00, (SELECT "Id" FROM "Invoices" WHERE "InvoiceNumber" = 'INV-202504-001')),
      ('UI/UX Review – 20 Screens',          1,  4000.00, (SELECT "Id" FROM "Invoices" WHERE "InvoiceNumber" = 'INV-202504-001')),
      ('Monthly Levy Management',            1,  8500.00, (SELECT "Id" FROM "Invoices" WHERE "InvoiceNumber" = 'INV-202504-002')),
      ('Brand Identity Package',             1, 14500.00, (SELECT "Id" FROM "Invoices" WHERE "InvoiceNumber" = 'INV-202504-003')),
      ('Consulting – April',                 1,  5750.00, (SELECT "Id" FROM "Invoices" WHERE "InvoiceNumber" = 'INV-202504-004')),
      ('Website Redesign',                   1, 18200.00, (SELECT "Id" FROM "Invoices" WHERE "InvoiceNumber" = 'INV-202504-005')),
      ('Backend API Development',            3,  4500.00, (SELECT "Id" FROM "Invoices" WHERE "InvoiceNumber" = 'INV-202504-006')),
      ('Code Review & Documentation',        1,  2000.00, (SELECT "Id" FROM "Invoices" WHERE "InvoiceNumber" = 'INV-202504-006')),
      ('System Architecture Consultation',   2,  3500.00, (SELECT "Id" FROM "Invoices" WHERE "InvoiceNumber" = 'INV-202504-007'));
  SQL
}

resource "null_resource" "apply_seed" {
  depends_on = [postgresql_database.invoicetracker, local_file.seed_sql]

  triggers = {
    sql_hash = local_file.seed_sql.content
  }

  provisioner "local-exec" {
    command = "PGPASSWORD=${var.pg_password} psql -h ${var.pg_host} -p ${var.pg_port} -U ${var.pg_username} -d ${var.pg_database} -f ${path.module}/seed.sql"
  }
}

# ── Outputs ───────────────────────────────────────────────────────────────────

output "connection_string" {
  value       = "Host=${var.pg_host};Port=${var.pg_port};Database=${var.pg_database};Username=${var.pg_username};Password=${var.pg_password}"
  description = "EF Core connection string"
  sensitive   = true
}
