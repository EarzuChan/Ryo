<template>
  <div id="test-viewport" class="ryo-dark">
    <Component :is="component" v-bind="bindProps" v-model="model"/>
  </div>
</template>

<script setup lang="ts">
const props = defineProps({
  component: Object,
  bindProps: Object,
})

const model = defineModel<any>()
</script>

<style scoped>
#test-viewport {
  display: flex;
  height: 100vh;
  
  padding: 24px;

  background-color: var(--ryo-color-surface);
  overflow: hidden;
}
</style>