<template>
  <div class="auth-page">
    <div class="auth-card">
      <div class="auth-logo"><span class="dot">●</span> TALLY </div>

      <!-- LOGIN -->
      <template v-if="mode === 'login'">
        <p class="auth-subtitle">Sign in to your account</p>
        <form @submit.prevent="handleLogin">
          <div class="field">
            <label>Email</label>
            <input v-model="email" type="email" placeholder="you@example.com" required autocomplete="email" />
          </div>
          <div class="field">
            <label>Password</label>
            <input v-model="password" type="password" placeholder="••••••••" required autocomplete="current-password" />
          </div>
          <p v-if="error" class="auth-error">{{ error }}</p>
          <button type="submit" class="btn-primary" :disabled="loading">
            {{ loading ? 'Signing in…' : 'Sign in' }}
          </button>
        </form>
        <div class="auth-links">
          <button class="link-btn" @click="mode = 'forgot'">Forgot password?</button>
          <button class="link-btn" @click="mode = 'register'">Create account</button>
        </div>
      </template>

      <!-- REGISTER -->
      <template v-else-if="mode === 'register'">
        <p class="auth-subtitle">Create a new account</p>
        <form @submit.prevent="handleRegister">
          <div class="field">
            <label>Name</label>
            <input v-model="name" type="text" placeholder="Your name" required autocomplete="name" />
          </div>
          <div class="field">
            <label>Email</label>
            <input v-model="email" type="email" placeholder="you@example.com" required autocomplete="email" />
          </div>
          <div class="field">
            <label>Password</label>
            <input v-model="password" type="password" placeholder="Min. 8 characters" required minlength="8" autocomplete="new-password" />
          </div>
          <p v-if="error" class="auth-error">{{ error }}</p>
          <button type="submit" class="btn-primary" :disabled="loading">
            {{ loading ? 'Creating account…' : 'Create account' }}
          </button>
        </form>
        <div class="auth-links">
          <button class="link-btn" @click="mode = 'login'">← Back to sign in</button>
        </div>
      </template>

      <!-- FORGOT PASSWORD -->
      <template v-else-if="mode === 'forgot'">
        <p class="auth-subtitle">Reset your password</p>
        <template v-if="!forgotSent">
          <form @submit.prevent="handleForgot">
            <div class="field">
              <label>Email</label>
              <input v-model="email" type="email" placeholder="you@example.com" required autocomplete="email" />
            </div>
            <p v-if="error" class="auth-error">{{ error }}</p>
            <button type="submit" class="btn-primary" :disabled="loading">
              {{ loading ? 'Sending…' : 'Send reset link' }}
            </button>
          </form>
        </template>
        <template v-else>
          <p class="auth-success">If that email exists, a reset link has been sent.</p>
        </template>
        <div class="auth-links">
          <button class="link-btn" @click="mode = 'login'">← Back to sign in</button>
        </div>
      </template>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/authStore'
import {
  sanitizeEmail, sanitizeName,
  validateLoginForm, validateRegisterForm, isValidEmail,
} from '../composables/useValidation'

type Mode = 'login' | 'register' | 'forgot'

const router = useRouter()
const auth   = useAuthStore()

const mode       = ref<Mode>('login')
const name       = ref('')
const email      = ref('')
const password   = ref('')
const error      = ref('')
const loading    = ref(false)
const forgotSent = ref(false)

async function handleLogin() {
  error.value = ''
  const cleanEmail = sanitizeEmail(email.value)
  const errs = validateLoginForm(cleanEmail, password.value)
  if (errs.length) { error.value = errs[0].message; return }
  loading.value = true
  try {
    await auth.login(cleanEmail, password.value)
    router.push('/')
  } catch (e: any) {
    error.value = e.message
  } finally {
    loading.value = false
  }
}

async function handleRegister() {
  error.value = ''
  const cleanName  = sanitizeName(name.value)
  const cleanEmail = sanitizeEmail(email.value)
  const errs = validateRegisterForm(cleanName, cleanEmail, password.value)
  if (errs.length) { error.value = errs[0].message; return }
  loading.value = true
  try {
    await auth.register(cleanName, cleanEmail, password.value)
    router.push('/')
  } catch (e: any) {
    error.value = e.message
  } finally {
    loading.value = false
  }
}

async function handleForgot() {
  error.value = ''
  const cleanEmail = sanitizeEmail(email.value)
  if (!isValidEmail(cleanEmail)) { error.value = 'Please enter a valid email address.'; return }
  loading.value = true
  try {
    await auth.forgotPassword(cleanEmail)
    forgotSent.value = true
  } catch (e: any) {
    error.value = e.message
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.auth-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #f5f5f5;
  padding: 24px;
}

.auth-card {
  width: 100%;
  max-width: 380px;
  background: #fff;
  border: 1px solid #e0e0e0;
  border-radius: 8px;
  padding: 36px 32px;
  box-shadow: 0 18px 45px rgba(15, 23, 42, .08);
}

.auth-logo {
  font-weight: 700;
  font-size: 16px;
  letter-spacing: .5px;
  color: #1a1a1a;
  margin-bottom: 24px;
}
.auth-logo .dot { color: #1e293b; }

.auth-subtitle {
  color: #666;
  font-size: 13px;
  letter-spacing: .3px;
  margin-bottom: 24px;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 6px;
  margin-bottom: 16px;
}

.field label {
  font-size: 11px;
  letter-spacing: .8px;
  color: #666;
  text-transform: uppercase;
}

.btn-primary {
  width: 100%;
  background: #1e293b;
  color: #fff;
  font-weight: 700;
  font-size: 13px;
  padding: 11px;
  border: none;
  border-radius: 6px;
  margin-top: 4px;
  transition: background .15s, transform .15s;
}
.btn-primary:hover:not(:disabled) {
  background: #334155;
  transform: translateY(-1px);
}
.btn-primary:disabled { opacity: .5; cursor: not-allowed; }

.auth-links {
  display: flex;
  justify-content: space-between;
  margin-top: 20px;
}

.link-btn {
  background: none;
  border: none;
  color: #666;
  font-size: 12px;
  font-family: inherit;
  letter-spacing: .3px;
  padding: 0;
  cursor: pointer;
  transition: color .15s;
}
.link-btn:hover { color: #1a1a1a; }

.auth-error {
  color: #b91c1c;
  background: #fee2e2;
  border: 1px solid #fecaca;
  border-radius: 6px;
  font-size: 12px;
  padding: 9px 10px;
  margin-bottom: 12px;
}

.auth-success {
  color: #065f46;
  background: #d1fae5;
  border: 1px solid #a7f3d0;
  border-radius: 6px;
  font-size: 12px;
  padding: 10px 12px;
  margin-bottom: 16px;
  line-height: 1.6;
}

@media (max-width: 480px) {
  .auth-page {
    align-items: flex-start;
    padding: 28px 16px;
  }

  .auth-card {
    padding: 28px 22px;
  }
}
</style>
