<template>
  <div class="number-editor">
    <input ref="textField" id="number-editor-field"
           @input="e=>updateNumber(e.target)" class="ryo-typography-body-medium"/>
    <div id="number-state-error-line" v-if="numberStateError"/>
  </div>
</template>

<script lang="ts" setup>
import {onMounted, ref, watch} from "vue"

//TODO:向上级转达当前非法状态
// 有关问题：现在只能一报错就把当前编辑器换成EditorError，得想办法拨弄一个弱报错的机制

const model = defineModel<number>()
const textField = ref<any>(null)

const numberStateError = ref(false)

const oldValue = ref<any>(null)

function updateNumber(target: any) {
  try {
    const txt = target.value
    const num = Number.parseFloat(txt)
    // console.log(target.value, num)
    if (num !== num || num.toString() !== txt) throw "NaN "

    // console.log("upt " + num)
    oldValue.value = num
    model.value = num
    numberStateError.value = false
  } catch (e: any) {
    numberStateError.value = true
    // console.log("err fmt " + e.toString())
  }
}

onMounted(() => watch(model, (newValue) => {
  if (numberStateError.value && newValue !== oldValue.value || newValue === oldValue.value) {
    // console.log("nal")
    return
  }
  // console.log("apl " + newValue)
  textField.value.value = newValue
}, {immediate: true}))

</script>

<style scoped>
#number-editor-field {
  color: white;
  font-family: inherit;
  padding: 0;
  flex: 1;
  border: none;
  background-color: transparent;

  --ryo-color-on-surface-variant: --ryo-color-on-surface;

  margin: 6px;
  min-height: 20px;
}

#number-state-error-line {
  content: '';
  height: 3px;
  bottom: 0;
  left: 0;
  right: 0;
  background-color: var(--ryo-color-error);
  position: absolute;
}

.number-editor {
  display: flex;
  position: relative;
}
</style>