<template>
  <div class="use-flex"
       :class="{'with-margin':isComplexEditor&&props.withMargin,'editor-holder-card':isComplexEditor && !notUseCard || cardSurrounded,'fulfill':!isComplexEditor}">
    <component class="fulfill" :is="editorType" :model-value="props.modelValue"
               @update:model-value="(a:any)=>updateData(a)" :type="type"/>
    <slot/>
  </div>
</template>

<script lang="ts" setup>
import {computed, nextTick, onMounted, type PropType, ref, shallowRef, watch} from "vue"
import type {RyoType} from "@/models/Models"
import {useAppStateStore} from "@/stores/AppState"
import EditorError from "@/components/Editors/EditorError.vue"
import FieldEditor from "@/components/Editors/FieldEditor.vue"

const appState = useAppStateStore()

const props = defineProps({
  modelValue: {},
  withMargin: Boolean,
  cardSurrounded: Boolean,
  notUseCard: Boolean,
  type: Object as PropType<RyoType>,
  preferEditor: Number,
})
const emit = defineEmits(['update:modelValue'])

function updateData(data: any) {
  // console.log(data)
  emit('update:modelValue', data)
}

const isComplexEditor = ref(false)

const editorType = computed(() => {
  if (props.type) {
    const editors = appState.getEditorsByRyoType(props.type)

    if (editors.length === 0) {
      isComplexEditor.value = false
      return EditorError
    }

    let chosen = editors[0]

    if (props.preferEditor && props.preferEditor < editors.length) chosen = editors[props.preferEditor]

    isComplexEditor.value = chosen === FieldEditor

    return chosen
  } else {
    isComplexEditor.value = false
    return EditorError
  }
})
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