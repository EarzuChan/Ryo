<template>
  <div class="text-editor">
    <textarea ref="textField" class="text-editor-field" v-model="model"/>
    <div class="button-container" v-if="showButton">
      <IconButton icon="close" :size="24" id="clear-button" @click="clearText"/>
    </div>
  </div>
</template>

<script lang="ts" setup>
import {nextTick, onMounted, ref, watch} from "vue"
import IconButton from "../IconButton.vue"

const textField = ref<any>(null)

const showButton = ref(false)

//FIXME:修复文字删除最后一个，Prop清空，但是文本框又有原文
const model = defineModel<string>()

async function clearText() {
  model.value = ""
}

const fitHeight = () => {
  // console.log("调教", textField.value.scrollHeight)
  textField.value.style.height = '14px'
  textField.value.style.height = (textField.value.scrollHeight + 2) + 'px'
}

onMounted(() => watch(model, async (newValue) => {
  // console.log("Text 接到新数据")

  await nextTick()
  fitHeight()
  showButton.value = newValue!.length != 0
}, {immediate: true}))

</script>

<style scoped>
.text-editor-field {
  color: white;
  font-family: inherit;
  font-size: 14px;
  resize: none;
  overflow-y: hidden;
  padding: 0;
  flex: 1;
  border: none;
  background-color: transparent;

  --ryo-color-on-surface-variant: white;
}

#clear-button {
  --ryo-color-on-surface-variant: white;
}

.button-container {
  height: 20px;
  display: flex;
  align-items: center;
}

.text-editor {
  margin: 6px;
  display: flex;
  flex-direction: row;

  align-items: center;
}
</style>