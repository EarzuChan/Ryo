<template>
  <!--TODO:添加、文本框宽度最窄、文本框清空按钮状态修复、右键或脱出删除、文本框文字选取-->
  <div class="array-holder array-editor">
    <draggable class="array-holder"
               v-model="model"
               @start="drag=true"
               :animation="200"
               @end="drag=false">
      <template #item="{ element,index }">
        <div class="array-item">
          <EditorHolder not-use-card v-model="model![index]" :type="itemType"/>
        </div>
      </template>
      <template #footer>
        <div id="add-item-button" @click="addItem">
          <IconButton :size="32" id="add-item-icon" icon="add"/>
        </div>
      </template>
    </draggable>
  </div>
</template>

<script lang="ts" setup>
const TAG = "ArrayEditor"

import draggable from "vuedraggable"
import {useAppStateStore} from "@/stores/AppState"
import {computed, type PropType, ref} from "vue"
import IconButton from "../IconButton.vue"
import EditorHolder from "../EditorHolder.vue"
import type {RyoType} from "@/models/Models"
import {useDialogStateStore} from "@/stores/DialogState";

// TODO：再加上文本编辑器的宽度自适应（作为atom时最小），数字编辑器的父级传递错误，右键删除，添加

// BUG:数组有元素为undefined/null时，爆了

const appState = useAppStateStore()
const dialogState = useDialogStateStore()
const props = defineProps({
  type: Object as PropType<RyoType>,
})
const model = defineModel<any[]>() // 为空该如何是好
const drag = ref(false)
const itemType = computed(() => {
  if (props.type && props.type.typeName) {
    console.log(TAG, "基础Ryo类型", props.type.typeName)

    return appState.getRyoTypeByName(props.type.typeName)
  }
})

function addItem() {
  if (model.value && itemType.value) {
    model.value.push(appState.getInitValue(itemType.value))
  } else {
    const errText = `无法添加新项目：${model.value} ${itemType.value}`
    dialogState.dialog({
      icon: 'close',
      headline: TAG,
      description: errText,
      actions: [{text: "行吧"}]
    })
    console.error(TAG)
  }
}
</script>

<style scoped>
.array-holder {
  display: flex;
  flex-wrap: wrap;
  flex-direction: row;
  gap: 6px;
}

.array-editor {
  padding: 6px;
  flex-direction: column;
  gap: 0;
}

.array-item {
  background-color: var(--ryo-color-surface-container-high);
  overflow: hidden;
  border-radius: 12px;
  /*padding: 6px;
  font-size: 14px;
  color: white;*/
  box-shadow: var(--ryo-elevation-2);
  align-items: center;
  display: flex;
}

#add-item-button {
  box-shadow: var(--ryo-elevation-2);
  background-color: var(--ryo-color-surface-container-high);
  overflow: hidden;
  border-radius: 12px;
  min-width: 32px;
}

#add-item-icon {
  --ryo-color-on-surface-variant: white;
  border-radius: 0;
  height: 100% !important;
}
</style>