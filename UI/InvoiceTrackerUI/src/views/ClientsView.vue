<template>
  <div class="clients">
    <!-- Header -->
    <div class="page-header">
      <div>
        <h1>Client Directory</h1>
        <p class="page-sub">Manage relationships and billing history for your active clients.</p>
      </div>
      <div class="header-actions">
        <button class="btn-primary" @click="openAdd">+ Add Client</button>
      </div>
    </div>

    <div v-if="store.loading" class="empty-state">Loading…</div>

    <div v-else-if="store.error" class="empty-state" style="color:#ef4444">{{ store.error }}</div>

    <div v-else-if="filtered.length === 0" class="empty-state">
      No clients found. Add your first client to get started.
    </div>

    <div v-else class="client-grid">
      <div
        v-for="client in filtered"
        :key="client.id"
        class="client-card"
      >
        <div class="client-header">
          <div class="client-avatar">{{ initials(client.name) }}</div>
          <button class="card-menu-btn">⋮</button>
        </div>
        <div class="client-body">
          <p class="client-name">{{ client.name }}</p>
          <p class="client-email">{{ client.email }}</p>
          <p v-if="client.phone" class="client-phone">📞 {{ client.phone }}</p>
          <div class="client-statuses">
            <span v-if="client.invoiceCount > 0" class="badge badge-sent">
              {{ client.invoiceCount }} invoice{{ client.invoiceCount !== 1 ? 's' : '' }}
            </span>
          </div>
        </div>
        <div class="client-footer">
          <div class="client-stat">
            <p class="stat-label">INVOICES</p>
            <p class="stat-value">{{ client.invoiceCount }}</p>
          </div>
          <div class="client-stat">
            <p class="stat-label">TOTAL BILLED</p>
            <p class="stat-value">R {{ fmt(client.totalBilled) }}</p>
          </div>
          <div class="client-stat">
            <p class="stat-label">LAST INVOICE</p>
            <p class="stat-value">{{ fmtDate(client.lastInvoice) }}</p>
          </div>
        </div>
        <div class="client-actions">
          <button class="action-link" @click="router.push({ path: '/invoices', query: { search: client.name } })">View Invoices</button>
          <button class="action-link" @click="openEdit(client)">Edit</button>
        </div>
      </div>

      <!-- Add new client card -->
      <div class="client-card client-card--add" @click="openAdd">
        <div class="add-client-content">
          <span class="add-icon">+</span>
          <p class="add-title">Add New Client</p>
          <p class="add-sub">Grow your business by adding a new client profile to your directory.</p>
        </div>
      </div>
    </div>

    <!-- Add / Edit Client Modal -->
    <div v-if="showModal" class="modal-overlay" @click.self="closeModal">
      <div class="modal">
        <div class="modal-header">
          <h2>{{ editingClient ? 'Edit Client' : 'Add New Client' }}</h2>
          <button class="modal-close" @click="closeModal">×</button>
        </div>
        <p class="modal-sub">
          {{ editingClient
            ? 'Update the client\'s details. Changes apply to future invoices.'
            : 'Enter the client\'s details to add them to your directory.' }}
        </p>

        <div class="modal-field">
          <label class="modal-label">CLIENT NAME</label>
          <input v-model="modalForm.name" class="modal-input" placeholder="Acme Corp" />
        </div>
        <div class="modal-field">
          <label class="modal-label">EMAIL ADDRESS</label>
          <input v-model="modalForm.email" type="email" class="modal-input" placeholder="billing@acme.com" />
        </div>
        <div class="modal-field">
          <label class="modal-label">CELLPHONE NUMBER</label>
          <input v-model="modalForm.phone" type="tel" class="modal-input" placeholder="+27 82 000 0000" />
        </div>

        <div v-if="modalError" class="modal-error">{{ modalError }}</div>

        <div class="modal-actions">
          <button class="modal-btn-cancel" @click="closeModal">Cancel</button>
          <button class="modal-btn-primary" :disabled="saving" @click="saveClient">
            {{ saving ? 'Saving…' : editingClient ? 'Save Changes' : 'Add Client' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useClientStore } from '@/stores/clientStore'
import { extractApiError } from '@/api/http'
import type { Client } from '@/models/client'

const router = useRouter()
const store  = useClientStore()
const search = ref('')

const showModal     = ref(false)
const modalError    = ref('')
const saving        = ref(false)
const editingClient = ref<Client | null>(null)
const modalForm     = reactive({ name: '', email: '', phone: '' })

onMounted(() => store.fetchAll())

const filtered = computed(() => {
  if (!search.value) return store.clients
  const q = search.value.toLowerCase()
  return store.clients.filter(c =>
    c.name.toLowerCase().includes(q) || c.email.toLowerCase().includes(q)
  )
})

function openAdd() {
  editingClient.value = null
  modalForm.name  = ''
  modalForm.email = ''
  modalForm.phone = ''
  modalError.value = ''
  showModal.value = true
}

function openEdit(client: Client) {
  editingClient.value = client
  modalForm.name  = client.name
  modalForm.email = client.email
  modalForm.phone = client.phone ?? ''
  modalError.value = ''
  showModal.value = true
}

function closeModal() {
  showModal.value = false
}

async function saveClient() {
  modalError.value = ''
  if (!modalForm.name.trim()) { modalError.value = 'Please enter a client name.'; return }
  if (!modalForm.email.trim() || !/^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/.test(modalForm.email)) {
    modalError.value = 'Please enter a valid email address.'
    return
  }

  saving.value = true
  try {
    if (editingClient.value) {
      await store.update(editingClient.value.id, {
        name:  modalForm.name.trim(),
        email: modalForm.email.trim(),
        phone: modalForm.phone.trim() || undefined,
      })
    } else {
      await store.create({
        name:  modalForm.name.trim(),
        email: modalForm.email.trim(),
        phone: modalForm.phone.trim() || undefined,
      })
    }
    closeModal()
  } catch (e) {
    modalError.value = extractApiError(e, 'Failed to save client.')
  } finally {
    saving.value = false
  }
}

function initials(name: string) {
  return name.split(' ').map(w => w[0]).join('').toUpperCase().slice(0, 2)
}
function fmt(n: number) {
  return n.toLocaleString('en-ZA', { minimumFractionDigits: 0 })
}
function fmtDate(d: string | undefined) {
  if (!d) return '—'
  return new Date(d).toLocaleDateString('en-ZA', { day: '2-digit', month: 'short', year: 'numeric' })
}
</script>

<style scoped>
.clients { display: flex; flex-direction: column; gap: 20px; }

.page-header { display: flex; justify-content: space-between; align-items: flex-end; }
.page-header h1 { font-size: 28px; font-weight: 700; color: #1a1a1a; }
.page-sub { color: #999; font-size: 13px; margin-top: 4px; }
.header-actions { display: flex; gap: 10px; }

.btn-primary {
  padding: 8px 16px; background: #1e293b; border: none; border-radius: 6px;
  color: #fff; font-family: inherit; font-size: 13px; font-weight: 700;
  cursor: pointer; transition: background .15s;
}
.btn-primary:hover { background: #0f172a; }

.empty-state { color: #999; font-size: 14px; padding: 60px 0; text-align: center; }

.client-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 16px;
}

.client-card {
  background: #fff; border: 1px solid #e0e0e0; border-radius: 10px;
  padding: 20px; display: flex; flex-direction: column; gap: 16px; transition: all .15s;
}
.client-card:hover { border-color: #d0d0d0; box-shadow: 0 2px 8px rgba(0,0,0,.05); }

.client-card--add {
  border: 2px dashed #e0e0e0; align-items: center; justify-content: center;
  min-height: 280px; cursor: pointer;
}
.client-card--add:hover { border-color: #1a1a1a; background: #f9f9f9; }

.add-client-content { text-align: center; display: flex; flex-direction: column; align-items: center; gap: 10px; }
.add-icon {
  width: 40px; height: 40px; border-radius: 50%; background: #f0f0f0;
  display: flex; align-items: center; justify-content: center; font-size: 20px; color: #999;
}
.add-title { font-size: 14px; font-weight: 700; color: #1a1a1a; }
.add-sub { font-size: 12px; color: #999; }

.client-header { display: flex; justify-content: space-between; align-items: flex-start; }
.client-avatar {
  width: 48px; height: 48px; border-radius: 8px; background: #dbeafe;
  border: 1px solid #bfdbfe; display: flex; align-items: center; justify-content: center;
  font-size: 14px; font-weight: 700; color: #1e40af; flex-shrink: 0;
}
.card-menu-btn { background: none; border: none; color: #999; font-size: 16px; padding: 0; cursor: pointer; }
.card-menu-btn:hover { color: #1a1a1a; }

.client-body { display: flex; flex-direction: column; gap: 8px; }
.client-name { font-size: 15px; font-weight: 700; color: #1a1a1a; }
.client-email { font-size: 12px; color: #999; }
.client-phone { font-size: 12px; color: #666; }
.client-statuses { display: flex; gap: 6px; flex-wrap: wrap; }

.client-footer {
  display: grid; grid-template-columns: repeat(3, 1fr); gap: 12px;
  padding-top: 12px; border-top: 1px solid #f0f0f0;
}
.client-stat { display: flex; flex-direction: column; gap: 3px; }
.stat-label { font-size: 10px; letter-spacing: .5px; color: #999; font-weight: 600; }
.stat-value { font-size: 14px; font-weight: 700; color: #1a1a1a; }

.client-actions { display: flex; gap: 10px; padding-top: 12px; border-top: 1px solid #f0f0f0; }
.action-link {
  flex: 1; background: #f5f5f5; border: 1px solid #e0e0e0; border-radius: 6px;
  color: #666; font-family: inherit; font-size: 12px; padding: 8px; cursor: pointer; transition: all .15s;
}
.action-link:hover { background: #e8e8e8; color: #1a1a1a; }

/* Modal */
.modal-overlay {
  position: fixed; inset: 0; background: rgba(0,0,0,.4);
  display: flex; align-items: center; justify-content: center; z-index: 200;
}
.modal {
  background: #fff; border-radius: 12px; padding: 28px;
  width: 440px; box-shadow: 0 20px 60px rgba(0,0,0,.15);
}
.modal-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px; }
.modal-header h2 { font-size: 18px; font-weight: 700; color: #1a1a1a; }
.modal-close { background: none; border: none; font-size: 22px; color: #999; cursor: pointer; padding: 0; }
.modal-close:hover { color: #1a1a1a; }
.modal-sub { font-size: 13px; color: #666; margin-bottom: 20px; line-height: 1.5; }
.modal-field { display: flex; flex-direction: column; gap: 6px; margin-bottom: 14px; }
.modal-label { font-size: 11px; letter-spacing: .5px; color: #666; font-weight: 600; }
.modal-input {
  background: #f9f9f9; border: 1px solid #e0e0e0; border-radius: 8px;
  color: #1a1a1a; font-family: inherit; font-size: 14px; padding: 10px 14px;
  outline: none; transition: border-color .15s;
}
.modal-input:focus { border-color: #1e293b; }
.modal-error {
  font-size: 12px; color: #ef4444; margin-bottom: 12px; padding: 8px 12px;
  background: #fef2f2; border-radius: 6px; border: 1px solid #fecaca;
}
.modal-actions { display: flex; gap: 10px; justify-content: flex-end; }
.modal-btn-cancel {
  padding: 9px 18px; background: #fff; border: 1px solid #e0e0e0; border-radius: 8px;
  color: #666; font-family: inherit; font-size: 13px; cursor: pointer; transition: all .15s;
}
.modal-btn-cancel:hover { border-color: #1a1a1a; color: #1a1a1a; }
.modal-btn-primary {
  padding: 9px 20px; background: #1e293b; border: none; border-radius: 8px;
  color: #fff; font-family: inherit; font-size: 13px; font-weight: 700; cursor: pointer; transition: background .15s;
}
.modal-btn-primary:hover { background: #0f172a; }

.modal-invoice-hint {
  margin-top: 16px; padding-top: 16px; border-top: 1px solid #f0f0f0;
  font-size: 12px; color: #999;
}
.hint-link {
  background: none; border: none; color: #1e293b; font-family: inherit;
  font-size: 12px; font-weight: 700; cursor: pointer; text-decoration: underline; padding: 0;
}
</style>
