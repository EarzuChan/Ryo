<template>
  <div class="switch-container" :class="{disabled}" @click="toggle">
    <div class="switch-track" :class="{checked, disabled}">
      <div class="switch-thumb" :class="{checked, disabled}"/>
    </div>
  </div>
</template>

<script setup lang="ts">
const props = defineProps({
  checked: {
    type: Boolean,
    default: false
  },
  disabled: {
    type: Boolean,
    default: false
  }
})

const emit = defineEmits(["update:checked"])

function toggle() {
  if (props.disabled) return
  emit("update:checked", !props.checked)
}
</script>

<style scoped>
.switch-container {
  width: 52px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
}

.switch-container.disabled {
  cursor: not-allowed;
  opacity: 0.38;
}

.switch-track {
  width: 52px;
  height: 32px;
  border-radius: 16px;
  box-sizing: border-box;
  display: flex;
  align-items: center;
  transition: all var(--ryo-motion-standard);
  background-color: var(--ryo-color-surface-container-highest);
  border: 2px solid var(--ryo-color-outline);
}

.switch-track.checked {
  background-color: var(--ryo-color-primary);
  border-color: var(--ryo-color-primary);
}

.switch-thumb {
  width: 16px;
  height: 16px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all var(--ryo-motion-standard);
  background-color: var(--ryo-color-outline);
  transform: translateX(6px);
}

.switch-thumb.checked {
  width: 24px;
  height: 24px;
  transform: translateX(22px);
  background-color: var(--ryo-color-on-primary);
}

.switch-container:not(.disabled):hover .switch-track {
  box-shadow: inset 0 0 0 999px rgba(var(--ryo-color-state-layers-on-surface), var(--ryo-opacity-state-layers-008));
}

.switch-container:not(.disabled):hover .switch-track.checked {
  box-shadow: inset 0 0 0 999px rgba(var(--ryo-color-state-layers-on-primary), var(--ryo-opacity-state-layers-008));
}
</style>
