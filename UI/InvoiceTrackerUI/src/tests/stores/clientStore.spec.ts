import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useClientStore } from '../../stores/clientStore'
import clientService from '../../api/ClientService'
import type { Client } from '../../models/client'

vi.mock('../../api/ClientService', () => ({
  default: {
    getAll:  vi.fn(),
    getById: vi.fn(),
    create:  vi.fn(),
    update:  vi.fn(),
    remove:  vi.fn(),
  },
}))

const makeClient = (id: number, name = 'Acme Corp'): Client => ({
  id,
  name,
  email: `${name.toLowerCase().replace(/ /g, '')}@test.com`,
  phone: '+27 11 000 0000',
  invoiceCount: 2,
  totalBilled: 5000,
  lastInvoice: '2025-04-01',
})

describe('clientStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  // ── initial state ──────────────────────────────────────────────────────────

  it('starts with empty clients, not loading, no error', () => {
    const store = useClientStore()
    expect(store.clients).toHaveLength(0)
    expect(store.loading).toBe(false)
    expect(store.error).toBeNull()
  })

  // ── fetchAll ───────────────────────────────────────────────────────────────

  it('fetchAll populates clients list', async () => {
    vi.mocked(clientService.getAll).mockResolvedValue([makeClient(1), makeClient(2, 'Beta Ltd')])
    const store = useClientStore()

    await store.fetchAll()

    expect(store.clients).toHaveLength(2)
    expect(store.loading).toBe(false)
    expect(store.error).toBeNull()
  })

  it('fetchAll sets loading true during fetch then false after', async () => {
    let resolvePromise!: (v: Client[]) => void
    vi.mocked(clientService.getAll).mockReturnValue(new Promise(r => { resolvePromise = r }))
    const store = useClientStore()

    const fetchPromise = store.fetchAll()
    expect(store.loading).toBe(true)

    resolvePromise([makeClient(1)])
    await fetchPromise
    expect(store.loading).toBe(false)
  })

  it('fetchAll sets error on failure', async () => {
    vi.mocked(clientService.getAll).mockRejectedValue(new Error('Network error'))
    const store = useClientStore()

    await store.fetchAll()

    expect(store.error).toBe('Failed to load clients')
    expect(store.clients).toHaveLength(0)
    expect(store.loading).toBe(false)
  })

  // ── create ─────────────────────────────────────────────────────────────────

  it('create appends client and sorts alphabetically', async () => {
    vi.mocked(clientService.getAll).mockResolvedValue([makeClient(1, 'Zeta Corp')])
    vi.mocked(clientService.create).mockResolvedValue(makeClient(2, 'Alpha Ltd'))
    const store = useClientStore()
    await store.fetchAll()

    await store.create({ name: 'Alpha Ltd', email: 'alpha@test.com' })

    expect(store.clients).toHaveLength(2)
    expect(store.clients[0].name).toBe('Alpha Ltd') // sorted first
    expect(store.clients[1].name).toBe('Zeta Corp')
  })

  it('create returns the new client', async () => {
    const newClient = makeClient(5, 'New Client')
    vi.mocked(clientService.create).mockResolvedValue(newClient)
    const store = useClientStore()

    const result = await store.create({ name: 'New Client', email: 'new@test.com' })

    expect(result).toEqual(newClient)
  })

  // ── update ─────────────────────────────────────────────────────────────────

  it('update replaces client in list', async () => {
    const original = makeClient(1, 'Old Name')
    const updated  = { ...original, name: 'New Name' }
    vi.mocked(clientService.getAll).mockResolvedValue([original])
    vi.mocked(clientService.update).mockResolvedValue(updated)
    const store = useClientStore()
    await store.fetchAll()

    await store.update(1, { name: 'New Name' })

    expect(store.clients[0].name).toBe('New Name')
  })

  it('update returns the updated client', async () => {
    const original = makeClient(1)
    const updated  = { ...original, email: 'new@email.com' }
    vi.mocked(clientService.getAll).mockResolvedValue([original])
    vi.mocked(clientService.update).mockResolvedValue(updated)
    const store = useClientStore()
    await store.fetchAll()

    const result = await store.update(1, { email: 'new@email.com' })

    expect(result.email).toBe('new@email.com')
  })

  // ── remove ─────────────────────────────────────────────────────────────────

  it('remove deletes client from list', async () => {
    vi.mocked(clientService.getAll).mockResolvedValue([makeClient(1), makeClient(2, 'Beta Ltd')])
    vi.mocked(clientService.remove).mockResolvedValue()
    const store = useClientStore()
    await store.fetchAll()

    await store.remove(1)

    expect(store.clients).toHaveLength(1)
    expect(store.clients[0].id).toBe(2)
  })

  it('remove calls clientService.remove with the correct id', async () => {
    vi.mocked(clientService.getAll).mockResolvedValue([makeClient(42)])
    vi.mocked(clientService.remove).mockResolvedValue()
    const store = useClientStore()
    await store.fetchAll()

    await store.remove(42)

    expect(clientService.remove).toHaveBeenCalledWith(42)
  })
})
