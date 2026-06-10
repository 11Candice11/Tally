import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { Client, CreateClientDto, UpdateClientDto } from '../models/client'
import clientService from '../api/ClientService'

// client store, pretty simple just fetch and mutate
// after creating a new client it sorts alphabeticaly so the list stays ordered
export const useClientStore = defineStore('clients', () => {
  const clients = ref<Client[]>([])
  const loading = ref(false)
  const error   = ref<string | null>(null)

  async function fetchAll() {
    loading.value = true
    error.value   = null
    try {
      clients.value = await clientService.getAll()
    } catch {
      error.value = 'Failed to load clients'
    } finally {
      loading.value = false
    }
  }

  async function create(dto: CreateClientDto): Promise<Client> {
    const client = await clientService.create(dto)
    clients.value.push(client)
    clients.value.sort((a, b) => a.name.localeCompare(b.name))
    return client
  }

  async function update(id: number, dto: UpdateClientDto): Promise<Client> {
    const updated = await clientService.update(id, dto)
    const idx = clients.value.findIndex(c => c.id === id)
    if (idx !== -1) clients.value[idx] = updated
    return updated
  }

  async function remove(id: number) {
    await clientService.remove(id)
    clients.value = clients.value.filter(c => c.id !== id)
  }

  return { clients, loading, error, fetchAll, create, update, remove }
})
