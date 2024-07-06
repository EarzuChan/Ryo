<template>
  <div class="field-editor">
    <div class="field-holder">
      <div v-for="(item,index) in keys" class="field-list-item" :class="{ 'even': isEven(index) }">
        <div class="item-name">{{ item.name }}</div>
        <div class="item-value-holder" :class="{ 'even': isEven(index) }">
          <EditorHolder with-margin v-model="modelValue[item.name]" :type="appState.getRyoTypeByName(item.type)"/>
        </div>
      </div>
    </div>
  </div>
</template>

<script lang="ts" setup>
import EditorHolder from "../EditorHolder.vue"
import {computed, type PropType} from "vue"
import type {RyoType} from "@/models/Models"
import {useAppStateStore} from "@/stores/AppState"

const appState = useAppStateStore()

const props = defineProps({
  modelValue: Object as PropType<any>,
  type: Object as PropType<RyoType>,
})

defineEmits(['update:modelValue'])

console.log("编辑器：组件加载")
const keys = computed(() => {
  if (props.type && props.type.baseType) {
    return props.type.baseType.members
  }
})

const isEven = (index: number) => index % 2 != 0
</script>

<style scoped>
.field-editor {
  display: flex;
  flex-direction: column;

  overflow-x: auto;
}

.field-holder {
  flex: 1;
  display: flex;
  flex-direction: column;

  min-width: 100%;
  width: fit-content;

  overflow-x: visible;
}

.field-list-item {
  min-height: 32px;
  color: white;
  font-size: 14px;
  display: flex;

  background-color: var(--ryo-color-surface-container-high);

  border-bottom: 1px solid var(--ryo-color-outline-varient);
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

  border-right: 1px solid var(--ryo-color-outline-varient);
}

.item-value-holder {
  flex: 1;
  background-color: var(--ryo-color-surface-container-highest);
  /*padding-left: 12px;*/

  display: flex;
  /*align-items: center;*/
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