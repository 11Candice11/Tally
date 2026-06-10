/**
 * Centralised validation & sanitization helpers.
 *
 * Sanitization (strip dangerous chars) happens at the point of submission,
 * not while the user is typing, so the UX stays smooth.
 */

// ── Sanitizers ────────────────────────────────────────────────────────────────

/** Remove HTML tags and trim whitespace */
export function sanitizeText(value: string): string {
  return value.replace(/<[^>]*>/g, '').trim()
}

/** Sanitize and collapse internal whitespace */
export function sanitizeName(value: string): string {
  return sanitizeText(value).replace(/\s{2,}/g, ' ')
}

/** Lowercase + trim email */
export function sanitizeEmail(value: string): string {
  return value.trim().toLowerCase()
}

/** Strip everything except digits, spaces, +, -, (, ), . */
export function sanitizePhone(value: string): string {
  return value.replace(/[^\d\s+\-().]/g, '').trim()
}

/** Strip HTML tags from multiline text */
export function sanitizeNotes(value: string): string {
  return value.replace(/<[^>]*>/g, '').trim()
}

// ── Validators ────────────────────────────────────────────────────────────────

export function isValidEmail(value: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/.test(value.trim())
}

export function isValidPhone(value: string): boolean {
  const v = value.trim()
  if (!v) return true // optional
  return /^\+?[\d\s\-().]{7,20}$/.test(v)
}

export function isValidName(value: string): boolean {
  const v = value.trim()
  return v.length >= 2 && v.length <= 200
}

export function isValidPassword(value: string): boolean {
  return value.length >= 8 && value.length <= 100
}

export function isValidDescription(value: string): boolean {
  const v = value.trim()
  return v.length >= 1 && v.length <= 500
}

export function isValidNotes(value: string): boolean {
  return value.trim().length <= 1000
}

export function isValidPositiveNumber(value: number): boolean {
  return Number.isFinite(value) && value > 0
}

export function isValidNonNegativeNumber(value: number): boolean {
  return Number.isFinite(value) && value >= 0
}

export function isValidDate(value: string): boolean {
  if (!value) return false
  const d = new Date(value)
  return !isNaN(d.getTime())
}

export function isDueDateAfterIssue(issueDate: string, dueDate: string): boolean {
  if (!issueDate || !dueDate) return true
  return new Date(dueDate) >= new Date(issueDate)
}

// ── Composite form validators ─────────────────────────────────────────────────

export interface ValidationError {
  field: string
  message: string
}

export function validateLoginForm(email: string, password: string): ValidationError[] {
  const errors: ValidationError[] = []
  if (!isValidEmail(email))    errors.push({ field: 'email',    message: 'Please enter a valid email address.' })
  if (!password.trim())        errors.push({ field: 'password', message: 'Password is required.' })
  return errors
}

export function validateRegisterForm(name: string, email: string, password: string): ValidationError[] {
  const errors: ValidationError[] = []
  if (!isValidName(name))      errors.push({ field: 'name',     message: 'Name must be between 2 and 200 characters.' })
  if (!isValidEmail(email))    errors.push({ field: 'email',    message: 'Please enter a valid email address.' })
  if (!isValidPassword(password)) errors.push({ field: 'password', message: 'Password must be at least 8 characters.' })
  return errors
}

export function validateClientForm(name: string, email: string, phone: string): ValidationError[] {
  const errors: ValidationError[] = []
  if (!isValidName(name))   errors.push({ field: 'name',  message: 'Client name must be between 2 and 200 characters.' })
  if (!isValidEmail(email)) errors.push({ field: 'email', message: 'Please enter a valid email address.' })
  if (!isValidPhone(phone)) errors.push({ field: 'phone', message: 'Please enter a valid phone number (7–20 digits).' })
  return errors
}

export function validateInvoiceForm(form: {
  clientName: string
  clientEmail: string
  issueDate: string
  dueDate: string
  notes: string
  lineItems: { description: string; qty: number; unitPrice: number }[]
}): ValidationError[] {
  const errors: ValidationError[] = []

  if (!isValidName(form.clientName))
    errors.push({ field: 'clientName', message: 'Client name must be between 2 and 200 characters.' })

  if (!isValidEmail(form.clientEmail))
    errors.push({ field: 'clientEmail', message: 'Please enter a valid client email address.' })

  if (!isValidDate(form.issueDate))
    errors.push({ field: 'issueDate', message: 'Please enter a valid issue date.' })

  if (!isValidDate(form.dueDate))
    errors.push({ field: 'dueDate', message: 'Please enter a valid due date.' })

  if (isValidDate(form.issueDate) && isValidDate(form.dueDate) && !isDueDateAfterIssue(form.issueDate, form.dueDate))
    errors.push({ field: 'dueDate', message: 'Due date must be on or after the issue date.' })

  if (!isValidNotes(form.notes))
    errors.push({ field: 'notes', message: 'Notes must not exceed 1000 characters.' })

  if (form.lineItems.length === 0)
    errors.push({ field: 'lineItems', message: 'Please add at least one line item.' })

  form.lineItems.forEach((item, i) => {
    if (!isValidDescription(item.description))
      errors.push({ field: `lineItems[${i}].description`, message: `Line item ${i + 1}: description is required (max 500 chars).` })
    if (!Number.isInteger(item.qty) || item.qty < 1)
      errors.push({ field: `lineItems[${i}].qty`, message: `Line item ${i + 1}: quantity must be a whole number ≥ 1.` })
    if (!isValidPositiveNumber(item.unitPrice))
      errors.push({ field: `lineItems[${i}].unitPrice`, message: `Line item ${i + 1}: unit price must be greater than 0.` })
  })

  return errors
}
