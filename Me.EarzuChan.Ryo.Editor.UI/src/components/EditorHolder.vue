<template>
  <div class="use-flex"
       :class="{'with-margin':isComplexEditor&&props.withMargin,'editor-holder-card':isComplexEditor && !notUseCard || cardSurrounded,'fulfill':!isComplexEditor}">
    <component class="fulfill" :is="editorType" v-model="model" :type="type"/>
    <slot/>
  </div>
</template>

<script lang="ts" setup>
import {ensure} from "@/utils/UsefulUtils"

const TAG = "EditorHolder"

import {computed, type PropType, ref} from "vue"
import type {RyoType} from "@/models/Models"
import {useAppStateStore} from "@/stores/AppState"
import EditorError from "@/components/Editors/EditorError.vue"
import FieldEditor from "@/components/Editors/FieldEditor.vue"

const appState = useAppStateStore()

const props = defineProps({
  withMargin: Boolean,
  cardSurrounded: Boolean,
  notUseCard: Boolean,
  type: Object as PropType<RyoType>,
  preferEditor: Number,
})

const model = defineModel<any>()

const isComplexEditor = ref(false)

// watch(model, v => console.log(TAG, "监测", v), {immediate: true})

const editorType = computed(() => {
  if (!ensure(model.value)) {
    isComplexEditor.value = false
    console.error(TAG, "绑定的数据为空")
    return EditorError
  } else if (props.type) {
    console.log(TAG, "给Ryo类型查找编辑器", props.type)

    const editors = appState.getEditorsByRyoType(props.type)

    if (editors.length === 0) {
      isComplexEditor.value = false
      console.error(TAG, "没有可用编辑器")
      return EditorError
    }

    const chosen = editors[ensure(props.preferEditor) && props.preferEditor! < editors.length ? props.preferEditor! : 0]

    isComplexEditor.value = chosen === FieldEditor
    return chosen
  } else {
    isComplexEditor.value = false
    console.error(TAG, "RyoType为空")
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