<template>
  <div class="use-flex"
       :class="{'with-margin':isComplexEditor&&prop.withMargin,'editor-holder-card':isComplexEditor && !notUseCard || cardSurrounded,'fulfill':!isComplexEditor}">
    <!--    <component class="fulfill" :is="editorType" v-if="canShow" :model-value="prop.modelValue"
                   @update:model-value="(a:any)=>updateData(a)"/>-->
    <slot/>
  </div>
</template>

<script lang="ts" setup>
import {nextTick, onMounted, type PropType, ref, shallowRef, watch} from "vue"
import type {RyoType} from "@/models/Models"

const prop = defineProps({
  modelValue: {},
  withMargin: Boolean,
  cardSurrounded: Boolean,
  notUseCard: Boolean,
  type: Object as PropType<RyoType>
})
const emit = defineEmits(['update:modelValue'])

function updateData(data: any) {
  // console.log(data)
  emit('update:modelValue', data)
}

const isComplexEditor = ref(false)
const canShow = ref(false)

function getEditorType(item: any) {
  // console.log(typeof item)

  isComplexEditor.value = false

  /*switch (typeof item) {
    case "string":
      return StringEditor
    case "number":
      return NumberEditor
    case "boolean":
      return BooleanEditor
    default: // Or Array
      if (Array.isArray(item)) return ArrayEditor
      isComplexEditor.value = true
      return FieldEditor
  }*/
}

const editorType = shallowRef<any>() // 临时解决堆栈爆的权宜之计，太丑了

/*onMounted(() => watch(() => prop.modelValue, async () => {
  // console.log("Holder 接到新数据")

  let nowType = getEditorType(prop.modelValue)
  // console.log("新：", nowType.__name, "老：", editorType.value?.__name, "相等：", nowType.__name === editorType.value?.__name)
  if (editorType.value?.__name !== nowType.__name) {
    canShow.value = false
    editorType.value = nowType
    await nextTick()
    canShow.value = true
  }
}, {immediate: true}))*/
</script>

<style scoped>
.editor-holder-card {
  border-radius: 12px;

  border: 1px solid var(--ryo-color-outline-varient);
  background-color: var(--ryo-color-surface);

  flex-direction: column;
  overflow: hidden;

  flex: 1;
}

.use-flex {
  display: flex;
}

.fulfill {
  flex: 1;
}

.with-margin {
  margin: 8px;
}
</style>