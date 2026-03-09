<script setup lang="ts">
defineProps({
  vertical: {
    type: Boolean,
    default: false
  }
})
</script>

<template>
  <div
      class="ryo-divider"
      :class="{ 'vertical': vertical }"
  />
</template>

<style scoped>
.ryo-divider {
  background-color: var(--ryo-color-outline-varient);
  border: none;
}

.ryo-divider:not(.vertical) {
  width: 100%;
  height: 1px;
}

.vertical {
  width: 1px;
  height: 100%;
}
</style>