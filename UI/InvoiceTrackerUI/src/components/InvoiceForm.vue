<template>
  <form @submit.prevent="submit">
    <label>Client Name
      <input v-model="form.clientName" required />
    </label>
    <label>Client Email
      <input v-model="form.clientEmail" type="email" required />
    </label>
    <label>Status
      <select v-model="form.status">
        <option v-for="s in statuses" :key="s" :value="s">{{ s }}</option>
      </select>
    </label>
    <label>Issue Date
      <input v-model="form.issueDate" type="date" required />
    </label>
    <label>Due Date
      <input v-model="form.dueDate" type="date" required />
    </label>
    <button type="submit">{{ invoice ? 'Update' : 'Create' }}</button>
  </form>
</template>

<script setup lang="ts">
import { reactive } from 'vue'
import { InvoiceStatus, type Invoice, type CreateInvoiceDto } from '../models/invoice'

const props = defineProps<{ invoice?: Invoice }>()
const emit = defineEmits<{ (e: 'submit', data: CreateInvoiceDto): void }>()

const statuses = Object.values(InvoiceStatus)

const form = reactive<CreateInvoiceDto>({
  invoiceNumber: props.invoice?.invoiceNumber ?? '',
  clientName:    props.invoice?.clientName ?? '',
  clientEmail:   props.invoice?.clientEmail ?? '',
  status:        props.invoice?.status ?? InvoiceStatus.Draft,
  issueDate:     props.invoice?.issueDate ?? '',
  dueDate:       props.invoice?.dueDate ?? '',
  vatRate:       props.invoice?.vatRate ?? 0.15,
  discount:      props.invoice?.discount ?? 0,
  lateFee:       props.invoice?.lateFee ?? 0,
  paymentTerms:  props.invoice?.paymentTerms ?? 'Net 30',
  currency:      props.invoice?.currency ?? 'ZAR',
  lineItems:     props.invoice?.lineItems ?? [],
})

function submit() {
  emit('submit', { ...form })
}
</script>

<style scoped>
form { display: flex; flex-direction: column; gap: .75rem; max-width: 400px; }
label { display: flex; flex-direction: column; font-size: .875rem; gap: .25rem; }
input, select { padding: .4rem .6rem; border: 1px solid #cbd5e1; border-radius: 6px; }
button { padding: .5rem 1rem; background: #3b82f6; color: #fff; border: none; border-radius: 6px; cursor: pointer; }
</style>
