<template>
  <div class="use-flex fulfill" :class="{'with-margin':isComplexEditor&&props.withMargin,
  'editor-holder-card':shouldUseCard}">
    <component v-if="ready" :even="realEven" @err="(e:Error)=>onError(e as any as string)" :errorMsg="errorMsg"
               class="fulfill" :is="editorType" v-model="model" :type="type" v-memo="[model]"/>
    <!-- 右上 v-memo="[model] 不会搞死原子编辑器 或因只是代办-->
    <slot/>
  </div>
</template>

<script lang="ts" setup>
// TODO：编辑器和容器要善用v-memo来提高性能

import {computed, nextTick, type PropType, ref} from "vue"
import {ensure} from "@/utils/UsefulUtils"
import type {EditorDescriptor, RyoType} from "@/models/AppModels"
import {useAppStateStore} from "@/stores/AppState"
import ErrorEditor from "@/components/editors/ErrorEditor.vue"
import FieldEditor from "@/components/editors/FieldEditor.vue"

const TAG = "EditorHolder"

const appState = useAppStateStore()
const props = defineProps({
  withMargin: Boolean,
  cardSurrounded: Boolean,
  notUseCard: Boolean,
  type: Object as PropType<RyoType>,
  preferEditorId: String,
  preferEditor: Number,
  even: Boolean,
})

const editorType = computed(() => {
  if (isError.value) return getError("编辑器错误：\n" + errorMsg.value)
  else if (!ensure(model.value)) return getError("数据错误：绑定的数据为空")
  // TODO: 确保提供的类型和实际数据类型一致
  else if (props.type) {
    console.log(TAG, "给Ryo类型查找编辑器", props.type)

    if (!appState.ensureRyoType(props.type, model.value)) return getError(`数据错误：数据类型不匹配：\n应为${props.type.typeName}，实为${typeof model.value}，内容：\n${model.value}`)

    const editors = appState.getEditorsByRyoType(props.type)
    if (editors.length === 0) return getError("编辑器错误：没有可用的编辑器")

    let chosen: EditorDescriptor | undefined

    if (ensure(props.preferEditorId)) {
      chosen = editors.find(editor => editor.id === props.preferEditorId)
    }

    if (!chosen && ensure(props.preferEditor) && props.preferEditor! < editors.length) {
      chosen = editors[props.preferEditor!]
    }

    if (!chosen) chosen = editors[0]
    if (!chosen) return getError("编辑器错误：无法取得指定编辑器")

    isComplexEditor.value = chosen.component === FieldEditor
    return chosen.component
  } else return getError("更多错误：Ryo类型为空？")
})

const realEven = computed(() => {
  let val = props.even
  if (!shouldUseCard.value) val = !val

  return val
})

const shouldUseCard = computed(() => isComplexEditor.value && !props.notUseCard || props.cardSurrounded)

const model = defineModel<any>()
const errorMsg = ref("良好")
const isError = ref(false)
const isComplexEditor = ref(false)
const ready = ref(true)

function getError(msg: string) {
  isComplexEditor.value = false
  console.error(TAG, msg)
  errorMsg.value = msg
  return ErrorEditor
}

function onError(err: string) {
  console.error(TAG, "检查到错误", err)
  errorMsg.value = err
  isError.value = true
}

async function reload() {
  isError.value = false

  ready.value = false
  await nextTick()
  ready.value = true
}

defineExpose({reload})
</script>

<style scoped>
.editor-holder-card {
  border-radius: 12px;

  outline: 1px solid var(--ryo-color-outline-varient);
  outline-offset: -1px;
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
