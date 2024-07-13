<template>
  <div class="use-flex" :class="{'with-margin':isComplexEditor&&props.withMargin,
  'editor-holder-card':isComplexEditor && !notUseCard || cardSurrounded,'fulfill':!isComplexEditor}">
    <Component :even="even" @err="e=>onError(e as string)" :errorMsg="errorMsg" class="fulfill" :is="editorType"
               v-model="model"
               :type="type"/>
    <slot/>
  </div>
</template>

<script lang="ts" setup>
import {computed, type PropType, ref} from "vue"
import {ensure} from "@/utils/UsefulUtils"
import type {RyoType} from "@/models/AppModels"
import {useAppStateStore} from "@/stores/AppState"
import EditorError from "@/components/Editors/EditorError.vue"
import FieldEditor from "@/components/Editors/FieldEditor.vue"

const TAG = "EditorHolder"

const appState = useAppStateStore()
const props = defineProps({
  withMargin: Boolean,
  cardSurrounded: Boolean,
  notUseCard: Boolean,
  type: Object as PropType<RyoType>,
  preferEditor: Number,
  even: Boolean,
})

const model = defineModel<any>()
const errorMsg = ref("良好")
const isError = ref(false)
const isComplexEditor = ref(false)

function getError(msg: string) {
  isComplexEditor.value = false
  console.error(TAG, msg)
  errorMsg.value = msg
  return EditorError
}

const editorType = computed(() => {
  if (isError.value) return getError("编辑器错误：" + errorMsg.value)
  else if (!ensure(model.value)) return getError("绑定的数据为空")
  else if (props.type) {
    console.log(TAG, "给Ryo类型查找编辑器", props.type)

    const editors = appState.getEditorsByRyoType(props.type)

    if (editors.length === 0) return getError("没有可用的编辑器")

    const chosen = editors[ensure(props.preferEditor) && props.preferEditor! < editors.length ? props.preferEditor! : 0]
    isComplexEditor.value = chosen === FieldEditor
    return chosen
  } else return getError("Ryo类型为空")
})

function onError(err: string) {
  console.error(TAG, "编辑器错误", err)
  errorMsg.value = err
  isError.value = true
}
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