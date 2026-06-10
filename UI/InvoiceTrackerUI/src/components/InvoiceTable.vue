<template>
  <table class="invoice-table">
    <thead>
      <tr>
        <th>#</th>
        <th>Client</th>
        <th>Amount</th>
        <th>Status</th>
        <th>Due Date</th>
        <th>Actions</th>
      </tr>
    </thead>
    <tbody>
      <tr v-for="invoice in invoices" :key="invoice.id">
        <td>{{ invoice.invoiceNumber }}</td>
        <td>{{ invoice.clientName }}</td>
        <td>R {{ invoiceTotal(invoice).toLocaleString() }}</td>
        <td><InvoiceStatusBadge :status="invoice.status" /></td>
        <td>{{ invoice.dueDate }}</td>
        <td>
          <button @click="$emit('edit', invoice)">Edit</button>
          <button @click="$emit('delete', invoice)">Delete</button>
        </td>
      </tr>
    </tbody>
  </table>
</template>

<script setup lang="ts">
import InvoiceStatusBadge from './InvoiceStatusBadge.vue'
import type { Invoice } from '../models/invoice'
import { invoiceTotal } from '../models/invoice'

defineProps<{ invoices: Invoice[] }>()
defineEmits<{ (e: 'edit', invoice: Invoice): void; (e: 'delete', invoice: Invoice): void }>()
</script>

<style scoped>
.invoice-table { width: 100%; border-collapse: collapse; }
th, td { padding: .6rem 1rem; text-align: left; border-bottom: 1px solid #e2e8f0; }
th { font-size: .75rem; color: #64748b; text-transform: uppercase; }
button { margin-right: .4rem; padding: .25rem .6rem; border-radius: 4px; border: 1px solid #cbd5e1; cursor: pointer; }
</style>
