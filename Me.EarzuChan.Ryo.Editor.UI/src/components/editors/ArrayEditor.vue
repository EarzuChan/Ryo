<template>
  <!--TODO:文本框宽度最窄、右键或脱出删除、文本框文字选取-->
  <div class="array-editor"
       :class="{'even':!even}">
    <VueDraggable class="draggable-place" v-model="modelWithIds" @start="notice(true)"
                  :animation="200" @end="notice(false)">
      <div class="array-item base" v-for="(item,index) in modelWithIds" :key="item.second"
           @contextmenu.prevent.stop="e=>showContextMenu(e,index)">
        <EditorHolder :even="even" not-use-card v-model="model![index]" :type="itemType"/>
      </div>
    </VueDraggable>
    <div id="add-item-button" class="base" @click="addItem">
      <IconButton :size="32" id="add-item-icon" icon="add"/>
    </div>
  </div>
</template>

<script lang="ts" setup>
import {generateId} from "@/utils/UsefulUtils"
import {useAppStateStore} from "@/stores/AppState"
import {VueDraggable} from 'vue-draggable-plus'
import {computed, type PropType, type Ref, ref, watch, watchEffect, watchSyncEffect} from "vue"
import IconButton from "../IconButton.vue"
import EditorHolder from "../EditorHolder.vue"
import type {Pair, RyoType} from "@/models/AppModels"
import {useDialogStateStore} from "@/stores/DialogState"
import {showMenu} from "@/utils/MenuUtils";

const TAG = "ArrayEditor"

// TODO：再加上文本编辑器的宽度自适应（作为atom时最小），子编辑器的父级传递错误，右键删除
const appState = useAppStateStore()
const dialogState = useDialogStateStore()

const props = defineProps({
  type: Object as PropType<RyoType>,
  even: Boolean,
})
const model = defineModel<any[]>()

const itemType = computed(() => {
  if (props.type && props.type.typeName) {
    const subRyo = appState.getRyoTypeByName(props.type.typeName)
    console.debug(TAG, "获取元素类型", props.type.typeName, subRyo)

    return subRyo
  }
})
const modelWithIds = computed<Pair<any, number>[]>({
  get() {
    const mo = model.value!.map((v, i) => ({first: v, second: ids.value[i]}))
    console.debug(TAG, "获取模型", mo)

    return mo
  }, set(val) {
    console.debug(TAG, "设置模型", val)

    model.value = val.map(v => v.first)
    ids.value = val.map(v => v.second)
  }
})
const ids = ref<number[]>([])

watchSyncEffect(() => {
  console.debug(TAG, "数量监测", model.value!.length, ids.value.length)
  if (model.value!.length !== ids.value.length) {
    console.debug(TAG, "模型与ID数量不匹配")
    if (model.value!.length > ids.value.length) {
      console.debug(TAG, `模型多于ID，补充${model.value!.length - ids.value.length}个ID`)
      model.value!.slice(ids.value.length).forEach((_, i) => ids.value.push(generateId(i)))
    } else {
      console.debug(TAG, `ID多于模型，截断${ids.value.length - model.value!.length}个ID`)
      ids.value = ids.value.slice(0, model.value!.length)
    }
  }
})

// TODO: Obj在Array里的实验
// TODO: 统一的右键菜单接口

const isDragging = ref(false)

function showContextMenu(e: MouseEvent, index: number) {
  console.debug(TAG, "右键菜单", e, index)

  showMenu({
    top: e.clientY - 8, left: e.clientX, items: [
      {name: "删除", action: () => model.value!.splice(index, 1)},
    ],
  })
}

function notice(state: boolean) {
  isDragging.value = state

  console.debug(TAG, "拖拽状态", state, model.value)
}

function addItem() {
  try {
    if (itemType.value) model.value!.push(appState.getInitValue(itemType.value))
    else throw new Error("未找到元素类型")
  } catch (e) {
    const errText = `无法添加新项目：${e}`
    dialogState.order({
      icon: 'close',
      headline: TAG,
      description: errText,
      actions: [{text: "好的"}]
    })
    console.error(TAG, errText, model.value, itemType.value)
  }
}
</script>

<style scoped>
.even > .draggable-place > .array-item, .even > #add-item-button {
  background-color: var(--ryo-color-surface-container-highest);
}

.array-editor {
  padding: 8px;
  display: flex;
  flex-wrap: wrap;
  flex-direction: row;
  gap: 8px;
}

.draggable-place {
  display: contents;
}

.base {
  background-color: var(--ryo-color-surface-container-high);
  overflow: hidden;
  border-radius: 12px;
  outline: 1px solid var(--ryo-color-outline-varient);
  outline-offset: -1px;
}

.array-item {
  align-items: center;
  display: flex;
}

#add-item-button {
  min-width: 32px;
  min-height: 32px;
}

#add-item-icon {
  --ryo-color-on-surface-variant: white;
  border-radius: 0;
  height: 100% !important;
}
</style>