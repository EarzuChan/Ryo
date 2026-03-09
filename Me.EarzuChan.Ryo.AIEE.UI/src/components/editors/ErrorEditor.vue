<template>
  <div class="editor-error ryo-typography-body-medium">
    {{ errorMsg }}
  </div>
</template>

<script setup lang="ts">
const TAG = "EditorError"

const props = defineProps({
  errorMsg: String,
})
</script>

<style scoped>
.editor-error {
  display: flex;

  padding: 6px;
  min-height: 20px;

  color: var(--ryo-color-error);

  align-items: center;
}
</style>