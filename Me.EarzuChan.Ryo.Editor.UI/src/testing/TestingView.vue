<template>
  <div class="testing">
    <EditorHolder card-surrounded :type="testingState.testingType"
                  v-model="testingState.testingData" ref="testingEditor">
      <div class="testing-action-bar">
        <TextButton class="a-button" @click="appState.preferTesting = false">离开测试模式</TextButton>
        <TextButton class="a-button" @click="reloadEditor">重载编辑器</TextButton>
        <TextButton class="a-button" button-style="filled" @click="testingState.reinitData">重置数据</TextButton>
        <TextButton class="a-button" button-style="filled" @click="printData">打印数据</TextButton>
      </div>
    </EditorHolder>
  </div>
</template>

<script setup lang="ts">
import EditorHolder from "@/components/EditorHolder.vue"
import {useAppStateStore} from "@/stores/AppState"
import {ref} from "vue"
import TextButton from "@/components/TextButton.vue"
import {useTestingStateStore} from "@/testing/TestingState"

const TAG = "Testing"

const appState = useAppStateStore()
const testingState = useTestingStateStore()

const testingEditor = ref<any>()

function reloadEditor() {
  testingEditor.value.reload()
}

function printData() {
  console.log(TAG, "打印数据", testingState.testingData)
}
</script>

<style scoped>
.testing {
  padding: 24px;
  display: flex;
}

.testing-action-bar {
  padding: 16px;
  gap: 16px;
  display: flex;

  box-shadow: 0 -1px 0 var(--ryo-color-outline-varient);
}

.a-button {
  flex: 1;
}
</style>