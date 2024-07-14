<template>
  <div class="field-editor">
    <div class="field-holder">
      <div v-for="(item,index) in keys" class="field-list-item" :class="{ 'even': isEven(index) }">
        <div class="item-name">{{ item.name }}</div>
        <div class="item-value-holder" :class="{ 'even': isEven(index) }">
          <EditorHolder with-margin :model-value="tryGetMember(item.name)"
                        @update:model-value="a=>trySetMember(item.name,a)"
                        :type="appState.getRyoTypeByName(item.type)" :even="isEven(index)"/>
        </div>
      </div>
    </div>
  </div>
</template>

<script lang="ts" setup>
import EditorHolder from "../EditorHolder.vue"
import {computed, type PropType, watch} from "vue"
import type {RyoType} from "@/models/AppModels"
import {useAppStateStore} from "@/stores/AppState"
import {ensure, ensureObject} from "@/utils/UsefulUtils"

const TAG = "FieldEditor"

const appState = useAppStateStore()

const props = defineProps({
  type: Object as PropType<RyoType>,
  even: Boolean,
})
const emit = defineEmits(["err"])
const model = defineModel<any>()

// watch(model, v => console.log(TAG, "监测", v), {immediate: true})

// console.log("编辑器：组件加载")
const keys = computed(() => {
  if (props.type && props.type.baseType) {
    return props.type.baseType.members
  }
})

function tryGetMember(name: string) {
  // console.log(TAG, "尝试获取成员", name)

  if (ensureObject(model.value)) {
    if (name in model.value) return model.value[name]
    else {
      console.error("绑定的数据中没有这个成员")
      return undefined
    }
  } else {
    const errMsg = "绑定的数据为空或传入值不是对象"
    console.error(errMsg, model.value)
    emit('err', `${errMsg}，请看：${model.value}`)
    return undefined
  }
}

function trySetMember(name: string, value: any) {
  console.log(TAG, "尝试设置成员", name, value)

  if (ensure(model.value)) {
    if (!(name in model.value)) console.warn("绑定的数据中没有这个成员，但是我们仍然赋值")
    model.value[name] = value
  } else {
    console.error("绑定的数据为空，不能赋值")
  }
}

function isEven(index: number) {
  let res = (index % 2) !== 0
  if (props.even) res = !res
  return res
}
</script>

<style scoped>
.field-editor {
  display: flex;
  flex-direction: column;

  overflow-x: auto;
}

.field-holder {
  display: flex;
  flex-direction: column;

  min-width: 100%;
  width: fit-content;

  overflow-x: visible;

  box-shadow: 0 1px var(--ryo-color-outline-varient);
}

.field-list-item {
  min-height: 32px;
  color: white;
  font-size: 14px;
  display: flex;

  background-color: var(--ryo-color-surface-container-high);
}

/*上面是否需要再考虑？*/

.field-list-item.even {
  background-color: var(--ryo-color-surface-container-highest);
}

.item-name {
  min-width: 188px;
  padding-left: 12px;
  padding-top: 6px;
  padding-bottom: 6px;

  font-size: 14px;
  color: white;

  box-shadow: inset -1px 0 0 0 var(--ryo-color-outline-varient), inset 0 1px 0 0 var(--ryo-color-outline-varient);
}

.item-value-holder {
  flex: 1;
  background-color: var(--ryo-color-surface-container-highest);
  display: flex;

  box-shadow: inset 0 1px 0 0 var(--ryo-color-outline-varient);
}

/*.sub-editor-card {
  border-radius: 12px;
  box-shadow: var(--ryo-elevation-2);

  margin: 6px;

  overflow: hidden;
}*/

.item-value-holder.even {
  background-color: var(--ryo-color-surface-container-high);
}

.full-flex {
  flex: 1;
}
</style>