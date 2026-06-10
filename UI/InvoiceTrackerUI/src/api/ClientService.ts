import { ServiceBase } from './ServiceBase'
import type { Client, CreateClientDto, UpdateClientDto } from '../models/client'

// client service, extends ServiceBase so errors are always a plain Error with a message string
// nothing fancy here just the 5 CRUD methods
class ClientService extends ServiceBase {
  getAll(): Promise<Client[]> {
    return this.get<Client[]>('/api/clients')
  }

  getById(id: number): Promise<Client> {
    return this.get<Client>(`/api/clients/${id}`)
  }

  create(dto: CreateClientDto): Promise<Client> {
    return this.post<Client>('/api/clients', dto)
  }

  update(id: number, dto: UpdateClientDto): Promise<Client> {
    return this.put<Client>(`/api/clients/${id}`, dto)
  }

  remove(id: number): Promise<void> {
    return this.delete(`/api/clients/${id}`)
  }
}

export default new ClientService()
