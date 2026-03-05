<template>
  <div id="settings-page">
    <div class="ryo-typography-headline-small">{{ $t('preferences') }}</div>
    <div class="ryo-typography-body-medium desc">Settings page is under construction.</div>
  </div>
</template>

<script setup lang="ts">
</script>

<style scoped>
#settings-page {
  display: flex;
  flex-direction: column;
  gap: 12px;

  color: var(--ryo-color-on-surface);
}

.desc {
  color: var(--ryo-color-on-surface-variant);
}
</style>
