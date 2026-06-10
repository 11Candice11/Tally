import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createRouter, createMemoryHistory } from 'vue-router'
import LoginView from '../../views/LoginView.vue'
import * as authApi from '../../api/authApi'

vi.mock('../../api/authApi')

const router = createRouter({
  history: createMemoryHistory(),
  routes: [
    { path: '/',      component: { template: '<div>Home</div>' } },
    { path: '/login', component: LoginView },
  ],
})

function mountLogin() {
  return mount(LoginView, {
    global: { plugins: [createPinia(), router] },
  })
}

describe('LoginView', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  // ── Login mode ─────────────────────────────────────────────────────────────

  it('renders sign in form by default', () => {
    const wrapper = mountLogin()
    expect(wrapper.text()).toContain('Sign in to your account')
    expect(wrapper.find('input[type="email"]').exists()).toBe(true)
    expect(wrapper.find('input[type="password"]').exists()).toBe(true)
  })

  it('calls auth.login on form submit', async () => {
    vi.mocked(authApi.login).mockResolvedValue({
      token: 'tok',
      refreshToken: 'ref',
      user: { id: 1, email: 'a@b.com', name: 'Alice' },
    })
    const wrapper = mountLogin()

    await wrapper.find('input[type="email"]').setValue('a@b.com')
    await wrapper.find('input[type="password"]').setValue('password123')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(authApi.login).toHaveBeenCalledWith('a@b.com', 'password123')
  })

  it('shows error message on failed login', async () => {
    vi.mocked(authApi.login).mockRejectedValue(new Error('Invalid email or password.'))
    const wrapper = mountLogin()

    await wrapper.find('input[type="email"]').setValue('bad@test.com')
    await wrapper.find('input[type="password"]').setValue('wrong')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(wrapper.text()).toContain('Invalid email or password.')
  })

  // ── Register mode ──────────────────────────────────────────────────────────

  it('switches to register mode on "Create account" click', async () => {
    const wrapper = mountLogin()

    await wrapper.findAll('button.link-btn')
      .find(b => b.text().includes('Create account'))!
      .trigger('click')

    expect(wrapper.text()).toContain('Create a new account')
  })

  it('calls auth.register on register form submit', async () => {
    vi.mocked(authApi.register).mockResolvedValue({
      token: 'tok',
      refreshToken: 'ref',
      user: { id: 2, email: 'new@test.com', name: 'New User' },
    })
    const wrapper = mountLogin()

    // Switch to register
    await wrapper.findAll('button.link-btn')
      .find(b => b.text().includes('Create account'))!
      .trigger('click')

    const inputs = wrapper.findAll('input')
    await inputs[0].setValue('New User')       // name
    await inputs[1].setValue('new@test.com')   // email
    await inputs[2].setValue('password123')    // password
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(authApi.register).toHaveBeenCalledWith('New User', 'new@test.com', 'password123')
  })

  // ── Forgot password mode ───────────────────────────────────────────────────

  it('switches to forgot password mode', async () => {
    const wrapper = mountLogin()

    await wrapper.findAll('button.link-btn')
      .find(b => b.text().includes('Forgot password'))!
      .trigger('click')

    expect(wrapper.text()).toContain('Reset your password')
  })

  it('shows success message after forgot password submit', async () => {
    vi.mocked(authApi.forgotPassword).mockResolvedValue(undefined)
    const wrapper = mountLogin()

    await wrapper.findAll('button.link-btn')
      .find(b => b.text().includes('Forgot password'))!
      .trigger('click')

    await wrapper.find('input[type="email"]').setValue('user@test.com')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(wrapper.text()).toContain('reset link has been sent')
  })
})
