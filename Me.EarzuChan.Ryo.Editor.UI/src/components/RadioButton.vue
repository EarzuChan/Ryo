<template>
  <div class="radio-button-holder">
    <div class="radio-button" :class="{'checked': checked, 'disabled': disabled}" @click="onClick">
      <div class="dot"/>
    </div>
  </div>
</template>

<script setup lang="ts">
const props = defineProps({
  checked: Boolean,
  disabled: Boolean
})

const emit = defineEmits(["update:checked"])

function onClick() {
  if (props.disabled) return
  emit("update:checked", true)
}
</script>

<style scoped>
.radio-button-holder {
  width: 24px;
  height: 24px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.radio-button {
  width: 20px;
  height: 20px;
  border-radius: 50%;
  box-sizing: border-box;
  border: 2px solid var(--ryo-color-on-surface-variant);

  display: flex;
  align-items: center;
  justify-content: center;

  transition: all var(--ryo-motion-standard);
}

.radio-button.checked {
  border-color: var(--ryo-color-primary);
}

.dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background-color: var(--ryo-color-primary);
  opacity: 0;
  transform: scale(0.6);
  transition: all var(--ryo-motion-standard);
}

.radio-button.checked .dot {
  opacity: 1;
  transform: scale(1);
}

.radio-button.disabled {
  opacity: 0.38;
}
</style>
