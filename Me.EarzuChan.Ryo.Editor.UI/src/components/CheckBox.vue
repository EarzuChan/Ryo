<template>
  <div class="checkbox-container"
       :style="{ width: containerSize + 'px', height: containerSize + 'px' }"
       @click="handleClick">
    <div class="checkbox-box" :class="checkboxClass">
      <svg v-if="checked"
           class="checkmark"
           viewBox="0 0 12 9.4"
           width="12"
           height="12">
        <path d="M4 9.4L0 5.4L1.4 4L4 6.6L10.6 0L12 1.4L4 9.4Z" fill="var(--ryo-color-on-primary)"/>
      </svg>
    </div>
  </div>
</template>

<script lang="ts" setup>
import {computed} from 'vue'

const props = defineProps({
  checked: {
    type: Boolean,
    default: false
  },
  containerSize: {
    type: Number,
    default: 40
  },
  disabled: {
    type: Boolean,
    default: false
  }
})

const emit = defineEmits(['update:checked'])

const checkboxClass = computed(() => {
  const classes = []
  if (props.checked) classes.push('checked')
  if (props.disabled) classes.push('disabled')
  return classes
})

function handleClick() {
  if (!props.disabled) emit('update:checked', !props.checked)
}
</script>

<style scoped>
.checkbox-container {
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  user-select: none;
  position: relative;
}

.checkbox-container.disabled {
  cursor: not-allowed;
  opacity: 0.38;
}

.checkbox-box {
  width: 18px;
  height: 18px;
  border-radius: 2px;
  border: 2px solid var(--ryo-color-on-surface-variant);
  box-sizing: border-box;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all var(--ryo-motion-standard);
  position: relative;
}

.checkbox-box.checked {
  background-color: var(--ryo-color-primary);
  border-color: var(--ryo-color-primary);
}

.checkbox-box.disabled {
  border-color: var(--ryo-color-on-surface);
  opacity: 0.38;
}

.checkbox-box.checked.disabled {
  background-color: var(--ryo-color-on-surface);
  border-color: var(--ryo-color-on-surface);
}

.checkmark {
  position: absolute;
}

/* Hover effect (optional) */
.checkbox-container:not(.disabled):hover .checkbox-box:not(.checked) {
  border-color: var(--ryo-color-on-surface);
}

.checkbox-container:not(.disabled):active::before {
  background-color: rgba(var(--ryo-color-state-layers-on-surface), 0.12);
}
</style>