<template>
  <div v-if="modelValue" class="overlay">
    <div class="dialog">
      <p>{{ message }}</p>
      <div class="actions">
        <button @click="$emit('update:modelValue', false)">Cancel</button>
        <button class="danger" @click="confirm">Delete</button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
defineProps<{ modelValue: boolean; message: string }>()
const emit = defineEmits<{ (e: 'update:modelValue', v: boolean): void; (e: 'confirm'): void }>()

function confirm() {
  emit('confirm')
  emit('update:modelValue', false)
}
</script>

<style scoped>
.overlay { position: fixed; inset: 0; background: rgba(0,0,0,.4); display: flex; align-items: center; justify-content: center; }
.dialog { background: #fff; border-radius: 8px; padding: 1.5rem; min-width: 300px; }
.actions { display: flex; justify-content: flex-end; gap: .5rem; margin-top: 1rem; }
button { padding: .4rem .9rem; border-radius: 6px; border: 1px solid #cbd5e1; cursor: pointer; }
.danger { background: #ef4444; color: #fff; border-color: #ef4444; }
</style>
