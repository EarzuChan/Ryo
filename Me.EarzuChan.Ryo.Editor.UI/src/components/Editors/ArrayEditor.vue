<template>
  <!--TODO:添加、文本框宽度最窄、文本框清空按钮状态修复、右键或脱出删除、文本框文字选取-->
  <div class="array-editor"
       :class="{'even':even}">
    <VueDraggable class="draggable-place"
                  v-model="model!"
                  @start="notice(true)"
                  :animation="200"
                  handle=".array-item"
                  @end="notice(false)">
      <div class="array-item" v-for="(ke,index) in model" :key="ke">
        <EditorHolder :even="!even" not-use-card v-model="model![index]" :type="itemType"/>
      </div>
    </VueDraggable>
    <div id="add-item-button" @click="addItem">
      <IconButton :size="32" id="add-item-icon" icon="add"/>
    </div>
  </div>
</template>

<script lang="ts" setup>
const TAG = "ArrayEditor"

import {useAppStateStore} from "@/stores/AppState"
import {VueDraggable} from 'vue-draggable-plus'
import {computed, type PropType, ref} from "vue"
import IconButton from "../IconButton.vue"
import EditorHolder from "../EditorHolder.vue"
import type {RyoType} from "@/models/AppModels"
import {useDialogStateStore} from "@/stores/DialogState"

// TODO：再加上文本编辑器的宽度自适应（作为atom时最小），数字编辑器的父级传递错误，右键删除，添加
// BUG: 输入一个数字（数据一但变化）就重载组件，或者拖动项目时也重载了组件，导致暂存数据丢失或者输入框失焦
const appState = useAppStateStore()
const dialogState = useDialogStateStore()
const props = defineProps({
  type: Object as PropType<RyoType>,
  even: Boolean,
})
const model = defineModel<any[]>()
const drag = ref(false)
const itemType = computed(() => {
  if (props.type && props.type.typeName) {
    console.log(TAG, "基础Ryo类型", props.type.typeName)

    return appState.getRyoTypeByName(props.type.typeName)
  }
})

function notice(state: boolean) {
  drag.value = state

  console.log(TAG, "拖拽状态", state, model.value)
}

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
.even > .draggable-place > .array-item, .even > #add-item-button {
  background-color: var(--ryo-color-surface-container-highest);
}

.array-editor {
  padding: 6px;
  display: flex;
  flex-wrap: wrap;
  flex-direction: row;
  gap: 6px;
}

.draggable-place {
  display: contents;
}

.array-item {
  background-color: var(--ryo-color-surface-container-high);
  overflow: hidden;
  border-radius: 12px;
  border: 1px solid var(--ryo-color-outline-varient);
  align-items: center;
  display: flex;
}

#add-item-button {
  background-color: var(--ryo-color-surface-container-high);
  overflow: hidden;
  border-radius: 12px;
  border: 1px solid var(--ryo-color-outline-varient);
  min-width: 32px;
  min-height: 32px;
}

#add-item-icon {
  --ryo-color-on-surface-variant: white;
  border-radius: 0;
  height: 100% !important;
}
</style>