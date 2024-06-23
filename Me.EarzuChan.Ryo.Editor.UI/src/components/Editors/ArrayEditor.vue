<template>
  <!--TODO:文本框宽度最窄、右键或脱出删除、文本框文字选取-->
  <div class="array-editor"
       :class="{'even':even}">
    <VueDraggable class="draggable-place" v-model="modelWithIds" @start="notice(true)"
                  :animation="200" @end="notice(false)">
      <div class="array-item" v-for="(item,index) in modelWithIds" :key="item.second">
        <EditorHolder :even="!even" not-use-card v-model="model![index]" :type="itemType"/>
      </div>
    </VueDraggable>
    <div id="add-item-button" @click="addItem">
      <IconButton :size="32" id="add-item-icon" icon="add"/>
    </div>
  </div>
</template>

<script lang="ts" setup>
import {generateId} from "@/utils/UsefulUtils";

const TAG = "ArrayEditor"

import {useAppStateStore} from "@/stores/AppState"
import {VueDraggable} from 'vue-draggable-plus'
import {computed, type PropType, ref} from "vue"
import IconButton from "../IconButton.vue"
import EditorHolder from "../EditorHolder.vue"
import type {Pair, RyoType} from "@/models/AppModels"
import {useDialogStateStore} from "@/stores/DialogState"

// TODO：再加上文本编辑器的宽度自适应（作为atom时最小），数字编辑器的父级传递错误，右键删除
// BUG: 鲁棒性！
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
const modelWithIds = computed<Pair<any, number>[]>({
  get() {
    const mo = model.value!.map((v, i) => ({first: v, second: ids.value[i]}))
    console.log(TAG, "获取模型", mo)
    return mo
  }, set(val) {
    console.log(TAG, "设置模型", val)
    model.value = val.map(v => v.first)
    ids.value = val.map(v => v.second)
  }
})
const ids = ref<number[]>(model.value!.map((_, i) => generateId(i)))
// 观察到：当给array直接换model，每个都是绑model index，故误中的自然不显示、没问题的自然更新，
// 少于原数的话，ids就用不到这么多（也就是ids个数会大于model），这样子追踪没有问题。但再添加ids，
// 新的项目反而绑定到之前不可见的id，这无法解决，除非通过watchModel初始化，但还没实验。
// 不知道内部排序、元素更改会不会导致watch。上述无法解决，最简单解决就是直接图了重载——在哪重载？

// TODO: 以上的实验、Obj在Array里的实验

function notice(state: boolean) {
  drag.value = state

  console.log(TAG, "拖拽状态", state, model.value)
}

function addItem() {
  if (model.value && itemType.value) {
    ids.value.push(generateId(model.value.length))
    model.value.push(appState.getInitValue(itemType.value))
  } else {
    const errText = `无法添加新项目：${model.value} ${itemType.value}`
    dialogState.order({
      icon: 'close',
      headline: TAG,
      description: errText,
      actions: [{text: "行吧"}]
    })
    console.error(TAG)

    // 但是嘛，如果model undefined，Holder会出手罢
    // 而且init时也会难绷
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