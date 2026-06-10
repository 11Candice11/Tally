-- ── Seed users ────────────────────────────────────────────────────────────────
-- Password for all seed users: password

INSERT INTO "Users" ("Name", "Email", "PasswordHash", "CreatedAt") VALUES
  ('Alice Demo',   'alice@ledgr.dev', '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', NOW()),
  ('Bob Invoicer', 'bob@ledgr.dev',   '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', NOW())
ON CONFLICT ("Email") DO NOTHING;

-- ── Seed invoices ─────────────────────────────────────────────────────────────

INSERT INTO "Invoices" ("InvoiceNumber","ClientName","ClientEmail","Status","IssueDate","DueDate","VatRate","Notes","UserId") VALUES
  ('INV-202504-001','Morebo Financial Solutions','josh@morebo.co.za',      'Paid',   '2025-04-01','2025-04-15',0.15,'Payment received via EFT.',(SELECT "Id" FROM "Users" WHERE "Email"='alice@ledgr.dev')),
  ('INV-202504-002','Lulurai Body Corporate',    'accounts@lulurai.co.za', 'Sent',   '2025-03-28','2025-04-12',0.15,NULL,                      (SELECT "Id" FROM "Users" WHERE "Email"='alice@ledgr.dev')),
  ('INV-202504-003','Third Dimension Prints',    'billing@3dp.co.za',      'Overdue','2025-03-15','2025-03-29',0.15,NULL,                      (SELECT "Id" FROM "Users" WHERE "Email"='alice@ledgr.dev')),
  ('INV-202504-004','Josh Raad',                 'josh@raad.dev',          'Draft',  '2025-03-10','2025-03-24',0.15,NULL,                      (SELECT "Id" FROM "Users" WHERE "Email"='alice@ledgr.dev')),
  ('INV-202504-005','Claire Anne Zoghby',        'claire@zoghby.com',      'Paid',   '2025-03-01','2025-03-15',0.15,NULL,                      (SELECT "Id" FROM "Users" WHERE "Email"='alice@ledgr.dev')),
  ('INV-202504-006','Acme Corp',                 'billing@acme.com',       'Sent',   '2025-04-05','2025-04-20',0.15,'Net 15 terms agreed.',    (SELECT "Id" FROM "Users" WHERE "Email"='bob@ledgr.dev')),
  ('INV-202504-007','Globex Industries',         'ap@globex.com',          'Draft',  '2025-04-10','2025-04-25',0.15,NULL,                      (SELECT "Id" FROM "Users" WHERE "Email"='bob@ledgr.dev'))
ON CONFLICT ("InvoiceNumber") DO NOTHING;

-- ── Seed line items ───────────────────────────────────────────────────────────

INSERT INTO "LineItems" ("Description","Qty","UnitPrice","InvoiceId") VALUES
  ('FinAdvisor – Sprint 7 Development', 1,12000,(SELECT "Id" FROM "Invoices" WHERE "InvoiceNumber"='INV-202504-001')),
  ('Microsoft Graph API Integration',   1, 8000,(SELECT "Id" FROM "Invoices" WHERE "InvoiceNumber"='INV-202504-001')),
  ('UI/UX Review – 20 Screens',         1, 4000,(SELECT "Id" FROM "Invoices" WHERE "InvoiceNumber"='INV-202504-001')),
  ('Monthly Levy Management',           1, 8500,(SELECT "Id" FROM "Invoices" WHERE "InvoiceNumber"='INV-202504-002')),
  ('Brand Identity Package',            1,14500,(SELECT "Id" FROM "Invoices" WHERE "InvoiceNumber"='INV-202504-003')),
  ('Consulting – April',                1, 5750,(SELECT "Id" FROM "Invoices" WHERE "InvoiceNumber"='INV-202504-004')),
  ('Website Redesign',                  1,18200,(SELECT "Id" FROM "Invoices" WHERE "InvoiceNumber"='INV-202504-005')),
  ('Backend API Development',           3, 4500,(SELECT "Id" FROM "Invoices" WHERE "InvoiceNumber"='INV-202504-006')),
  ('Code Review & Documentation',       1, 2000,(SELECT "Id" FROM "Invoices" WHERE "InvoiceNumber"='INV-202504-006')),
  ('System Architecture Consultation',  2, 3500,(SELECT "Id" FROM "Invoices" WHERE "InvoiceNumber"='INV-202504-007'));
