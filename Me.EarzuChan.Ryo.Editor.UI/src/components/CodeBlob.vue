<template>
  <div id="code-blob" class="ryo-typography-body-medium">
    <slot></slot>
  </div>
</template>

<script setup lang="ts">
</script>

<style scoped>
#code-blob {
  padding: 12px 16px;
  background-color: var(--ryo-color-surface-container-high);
  border-radius: 12px;

  color: white;
  white-space: pre-wrap;

  border: 1px solid var(--ryo-color-outline-varient);
}
</style>