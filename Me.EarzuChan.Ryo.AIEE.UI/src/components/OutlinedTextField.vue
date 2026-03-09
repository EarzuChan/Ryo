<template>
  <div class="text-field-wrapper">
    <div class="text-field-container"
         :class="containerClass"
         @click="handleContainerClick">
      <!-- Outline -->
      <fieldset class="outline" :class="outlineClass">
        <legend :class="{ 'has-label': hasFloatingLabel }">
          <span v-if="hasFloatingLabel">{{ label }}</span>
        </legend>
      </fieldset>

      <!-- Floating Label -->
      <label v-if="label" class="label" :class="labelClass">
        {{ label }}
      </label>

      <!-- Input -->
      <input ref="inputRef"
             class="input ryo-typography-body-large"
             :class="inputClass"
             type="text"
             :value="modelValue"
             :placeholder="isFocused ? placeholder : ''"
             @input="handleInput"
             @focus="handleFocus"
             @blur="handleBlur"/>

      <!-- Clear Icon Button -->
      <IconButton v-if="modelValue && !disabled"
                  icon="close"
                  :size="48"
                  @mousedown.prevent="clearText"/>
    </div>
    <!-- Helper/Error Text -->
    <div v-if="helperText || error" class="support-text ryo-typography-body-small">
      {{ error || helperText }}
    </div>
  </div>
</template>

<script lang="ts" setup>
import {ref, computed} from 'vue'
import IconButton from './IconButton.vue'

const props = defineProps({
  modelValue: {
    type: String,
    default: ''
  },
  label: {
    type: String,
    default: ''
  },
  placeholder: {
    type: String,
    default: ''
  },
  error: {
    type: String,
    default: ''
  },
  helperText: {
    type: String,
    default: ''
  },
  disabled: {
    type: Boolean,
    default: false
  }
})

const emit = defineEmits(['update:modelValue'])

const inputRef = ref<HTMLInputElement | null>(null)
const isFocused = ref(false)

const hasFloatingLabel = computed(() => {
  return isFocused.value || props.modelValue.length > 0
})

const containerClass = computed(() => {
  const classes = []
  if (isFocused.value) classes.push('focused')
  if (props.error) classes.push('error')
  if (props.disabled) classes.push('disabled')
  return classes
})

const outlineClass = computed(() => {
  const classes = []
  if (isFocused.value) classes.push('focused')
  if (props.error) classes.push('error')
  return classes
})

const labelClass = computed(() => {
  const classes = []
  
  if (hasFloatingLabel.value) classes.push('floating', 'ryo-typography-body-small')
  else classes.push('ryo-typography-body-large')
  
  if (isFocused.value) classes.push('focused')
  if (props.error) classes.push('error')
  
  return classes
})

const inputClass = computed(() => {
  const classes = []
  if (isFocused.value) classes.push('focused')
  if (props.error) classes.push('error')
  return classes
})

function handleInput(event: Event) {
  const target = event.target as HTMLInputElement
  emit('update:modelValue', target.value)
}

function handleFocus() {
  isFocused.value = true
}

function handleBlur() {
  isFocused.value = false
}

function clearText() {
  emit('update:modelValue', '')
  inputRef.value?.focus()
}

function handleContainerClick() {
  if (!props.disabled) {
    inputRef.value?.focus()
  }
}
</script>

<style scoped>
.text-field-wrapper {
  display: flex;
  flex-direction: column;
  width: 100%;
}

.text-field-container {
  position: relative;
  display: flex;
  align-items: center;
  min-height: 56px;
  cursor: text;
}

.text-field-container.disabled {
  cursor: not-allowed;
  opacity: 0.38;
}

/* Outline */
.outline {
  position: absolute;
  inset: 0;
  margin: 0;
  padding: 0;
  border: 1px solid var(--ryo-color-outline);
  border-radius: 4px;
  pointer-events: none;
  transition: all var(--ryo-motion-standard);
}

.outline.focused {
  border-width: 3px;
  border-color: var(--ryo-color-primary);
}

.outline.error {
  border-color: var(--ryo-color-error);
}

.outline.focused.error {
  border-width: 3px;
  border-color: var(--ryo-color-error);
}

/* Legend for label cutout */
.outline legend {
  margin-left: 12px;
  padding: 0;
  height: 0;
  visibility: hidden;
  font-size: 12px;
  transition: all var(--ryo-motion-standard);
}

.outline.focused legend {
  margin-left: 10px;
}

.outline legend.has-label {
  padding: 0 4px;
  visibility: visible;
}

.outline legend span {
  opacity: 0;
}

/* Label */
.label {
  position: absolute;
  left: 16px;
  color: var(--ryo-color-on-surface-variant);
  pointer-events: none;
  transition: all var(--ryo-motion-standard);
  transform-origin: left top;
}

.label:not(.floating) {
  line-height: 24px;
  letter-spacing: 0.5px;
}

.label.floating {
  transform: translateY(-28px);
  color: var(--ryo-color-primary);
  line-height: 16px;
}

.label.floating.error {
  color: var(--ryo-color-error);
}

/* Input */
.input {
  flex: 1;
  padding: 16px 16px 16px 16px;
  border: none;
  outline: none;
  background: transparent;
  color: var(--ryo-color-on-surface-variant);
  font-family: inherit;
}

.input.focused {
  color: var(--ryo-color-on-surface);
}

.input.error {
  color: var(--ryo-color-error);
}

.input::placeholder {
  color: var(--ryo-color-on-surface-variant);
}

.input:disabled {
  cursor: not-allowed;
}

/* Support Text */
.support-text {
  margin-top: 4px;
  margin-left: 16px;
  color: var(--ryo-color-on-surface-variant);
}

.text-field-container.error + .support-text {
  color: var(--ryo-color-error);
}
</style>