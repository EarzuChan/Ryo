<template>
  <div class="preference-label">
    <div class="ryo-typography-label-large text">
      <slot/>
    </div>
  </div>
</template>

<script setup lang="ts">
</script>

<style scoped>
.preference-label {
  min-height: 40px;
  display: flex;
  align-items: flex-end;

  padding: 4px 16px;
}

.text {
  color: var(--ryo-color-primary);
}
</style>
