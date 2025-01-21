<template>
  <div class="boolean-editor" @click="switchState">
    <div id="current-state" class="ryo-typography-body-medium">{{ currentState }}</div>
    <div id="another-state" class="ryo-typography-body-medium">/{{ anotherState }}</div>
  </div>
</template>

<script lang="ts" setup>
import {computed, onMounted, ref, watch} from "vue"

//TODO:向上级转达当前非法状态？轻报错

const model = defineModel<boolean>()

const currentState = computed(() => model.value ? "真" : "假")
const anotherState = computed(() => model.value ? "假" : "真")

function switchState() {
  model.value = !model.value
}
</script>

<style scoped>
#current-state {
  color: white;
  flex: 1;
}

#another-state {
  color: var(--ryo-color-on-surface);
}

.boolean-editor {
  display: flex;

  padding: 6px;
  min-height: 20px;

  align-items: center;
}
</style>