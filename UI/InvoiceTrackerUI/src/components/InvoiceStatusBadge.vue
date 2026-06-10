<template>
  <span :class="['badge', `badge--${status.toLowerCase()}`]">{{ status }}</span>
</template>

<script setup lang="ts">
import type { InvoiceStatus } from '../models/invoice'

defineProps<{ status: InvoiceStatus }>()
</script>

<style scoped>
.badge {
  padding: 2px 8px;
  border-radius: 12px;
  font-size: 0.75rem;
  font-weight: 600;
}
.badge--draft    { background: #e2e8f0; color: #475569; }
.badge--sent     { background: #dbeafe; color: #1d4ed8; }
.badge--paid     { background: #dcfce7; color: #15803d; }
.badge--overdue  { background: #fee2e2; color: #b91c1c; }
</style>
