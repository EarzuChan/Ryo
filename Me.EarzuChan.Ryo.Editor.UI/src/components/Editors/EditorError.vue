<template>
  <div class="editor-error ryo-typography-body-medium">
    编辑器错误：{{ errorMsg }}
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

  color: white;

  align-items: center;
}
</style>