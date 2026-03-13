<template>
  <div class="use-flex fulfill" :class="{'with-margin':isComplexEditor&&props.withMargin,
  'editor-holder-card':shouldUseCard}"
       :data-array-drag-safe="isArrayDragSafe ? 'true' : undefined"
       @contextmenu.prevent.stop="handleContextMenu">
    <component v-if="ready" :even="realEven" @err="(e:Error)=>onError(e as any as string)" :errorMsg="errorMsg"
               class="fulfill" :is="editorType" v-model="model" :type="type" :item-key="itemKey"
               :editor-path="editorPath" :data-type-name="dataTypeName" :is-root-editor="isRootEditor"
               :context-menu-contributions="childContextMenuContributions"
               v-memo="[model]"/>
    <!-- 右上 v-memo="[model] 不会搞死原子编辑器 或因只是代办-->
    <slot/>
  </div>
</template>

<script lang="ts" setup>
// TODO：编辑器和容器要善用v-memo来提高性能

import {computed, nextTick, type PropType, ref, watch} from "vue"
import {ensure} from "@/utils/UsefulUtils"
import type {EditorContext, EditorDescriptor, RyoType} from "@/models/AppModels"
import {useAppStateStore} from "@/stores/AppState"
import ErrorEditor from "@/components/editors/ErrorEditor.vue"
import FieldEditor from "@/components/editors/FieldEditor.vue"
import {useWorkspaceStateStore} from "@/stores/WorkspaceState"
import {dataTypeNameFromRyoType} from "@/utils/EditorOverrideUtils"
import type {ContextMenuContribution, MenuItem} from "@/models/UIModels"
import {showMenu} from "@/utils/MenuUtils"
import {useDialogStateStore} from "@/stores/DialogState"
import {useI18n} from "vue-i18n"
import {applyEditorSelectionWithStrategy} from "@/utils/EditorPreferenceUtils"
import {composeContextMenuItems, createContextMenuGroup} from "@/utils/ContextMenuUtils"
import EditorOverrideManagerDialog from "@/views/dialogs/EditorOverrideManagerDialog.vue"

const TAG = "EditorHolder"

const appState = useAppStateStore()
const workspaceState = useWorkspaceStateStore()
const dialogState = useDialogStateStore()
const {t} = useI18n()
const props = defineProps({
  withMargin: Boolean,
  cardSurrounded: Boolean,
  notUseCard: Boolean,
  itemKey: String,
  editorPath: String,
  dataTypeName: String,
  isRootEditor: {
    type: Boolean,
    default: false,
  },
  type: Object as PropType<RyoType>,
  preferEditorId: String,
  preferEditor: Number,
  even: Boolean,
  contextMenuContributions: Array as PropType<ContextMenuContribution[]>,
})

const editorContext = computed<EditorContext | undefined>(() => {
  if (!props.type) return
  return appState.createEditorContext(props.type, {
    itemKey: props.itemKey,
    path: props.editorPath ?? "$",
    dataTypeName: props.dataTypeName ?? dataTypeNameFromRyoType(props.type),
    isRoot: props.isRootEditor,
  })
})

const currentEditorOverrideVersion = computed(() => {
  if (!props.itemKey) return "no-session"
  const appVersion = appState.editorOverrideVersion
  const sessionVersion = props.itemKey ? workspaceState.getItemSessionEditorOverrideVersion(props.itemKey) : 0
  return `${appVersion}-${sessionVersion}`
})

const editorType = computed(() => {
  currentEditorOverrideVersion.value
  if (isError.value) return getError("编辑器错误：\n" + errorMsg.value)
  else if (!ensure(model.value)) return getError("数据错误：绑定的数据为空")
  // TODO: 确保提供的类型和实际数据类型一致
  else if (props.type && editorContext.value) {
    console.log(TAG, "给Ryo类型查找编辑器", props.type, editorContext.value)

    if (!appState.ensureRyoType(props.type, model.value)) return getError(`数据错误：数据类型不匹配：\n应为${props.type.typeName}，实为${typeof model.value}，内容：\n${model.value}`)

    const editors = appState.getEditorsByContext(editorContext.value)
    if (editors.length === 0) return getError("编辑器错误：没有可用的编辑器")

    let chosen: EditorDescriptor | undefined
    const resolution = appState.resolveEditorForContext(
        editorContext.value,
        props.itemKey ? workspaceState.getItemSessionOnceEditorOverrides(props.itemKey) : undefined,
        props.preferEditorId
    )
    chosen = resolution.editor

    if (!chosen && ensure(props.preferEditor) && props.preferEditor! < editors.length) {
      chosen = editors[props.preferEditor!]
    }

    if (!chosen) chosen = editors[0]
    if (!chosen) return getError("编辑器错误：无法取得指定编辑器")
    if (!props.isRootEditor && chosen.surface !== "inline") {
      chosen = editors.find((editor: EditorDescriptor) => editor.surface === "inline") ?? chosen
    }

    const finalChosen = chosen
    isComplexEditor.value = finalChosen.component === FieldEditor
    return finalChosen.component
  } else return getError("更多错误：Ryo类型为空？")
})

const resolvedEditorSelection = computed(() => {
  if (!editorContext.value) return undefined
  return appState.resolveEditorForContext(
      editorContext.value,
      props.itemKey ? workspaceState.getItemSessionOnceEditorOverrides(props.itemKey) : undefined,
      props.preferEditorId
  )
})
const resolvedEditorId = computed(() => resolvedEditorSelection.value?.editor?.id ?? "")
const isArrayDragSafe = computed(() => [
  "generic.text",
  "generic.number",
  "generic.boolean",
  "special.string-list",
].includes(resolvedEditorId.value))

const selfContextMenuContribution = computed<ContextMenuContribution | undefined>(() => {
  if (!editorContext.value) return undefined

  return createContextMenuGroup(
      getContainerContributionTitle(editorContext.value.path),
      editorContext.value.path,
      buildSelfContextMenuItems()
  )
})

const childContextMenuContributions = computed<ContextMenuContribution[]>(() => {
  const contributions: ContextMenuContribution[] = []
  if (selfContextMenuContribution.value) contributions.push(selfContextMenuContribution.value)
  if (props.contextMenuContributions?.length) contributions.push(...props.contextMenuContributions)
  return contributions
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

async function chooseEditor(editor: EditorDescriptor) {
  if (!props.itemKey || !editorContext.value) return
  const action = await applyEditorSelectionWithStrategy({
    appState,
    workspaceState,
    dialogState,
    t,
    itemKey: props.itemKey,
    context: editorContext.value,
    editor,
  })
  if (action !== "cancel") await reload()
}

function openMatchedRulesDialog() {
  if (!editorContext.value) return
  const matchedRules = appState.getMatchedEditorOverrideRulesForContext(editorContext.value)
  const onceEditorId = props.itemKey
      ? workspaceState.getItemSessionOnceEditorOverrides(props.itemKey)[editorContext.value.path]
      : undefined
  if (matchedRules.length === 0 && !onceEditorId) return
  dialogState.orderSpecial(EditorOverrideManagerDialog, {
    matchedRuleIds: matchedRules.map(rule => rule.id),
    headline: t("editorAppliedRules"),
    description: t("editorAppliedRulesDesc", {path: editorContext.value?.path ?? "$"}),
    onceOverride: props.itemKey && onceEditorId
        ? {itemKey: props.itemKey, path: editorContext.value.path, editorId: onceEditorId}
        : undefined,
  })
}

function getContainerContributionTitle(path: string) {
  if (props.type?.baseType) return t("editorMenuParentObjectGroup", {path})
  if (props.type?.isArray) return t("editorMenuParentArrayGroup", {path})
  return t("editorMenuParentContainerGroup", {path})
}

function buildSelfContextMenuItems(): MenuItem[] {
  if (!editorContext.value) return []
  const editors = appState.getEditorsByContext(editorContext.value)
  const currentEditorId = resolvedEditorSelection.value?.editor?.id
  const items: MenuItem[] = [
    {
      name: t("editorCurrentPathLabel", {path: editorContext.value.path}),
      disabled: true,
    },
  ]

  if (props.itemKey && editors.length > 1) {
    items.push({
      name: t("switchEditor"),
      children: editors.map((editor: EditorDescriptor) => ({
        name: editor.id === currentEditorId
            ? `${t(editor.titleKey)} (${t("current")})`
            : t(editor.titleKey),
        disabled: editor.id === currentEditorId,
        action: () => chooseEditor(editor),
      })),
    })
  }

  const onceEditorId = props.itemKey
      ? workspaceState.getItemSessionOnceEditorOverrides(props.itemKey)[editorContext.value.path]
      : undefined
  if (editorContext.value && (appState.getMatchedEditorOverrideRulesForContext(editorContext.value).length > 0 || !!onceEditorId)) {
    items.push({
      name: t("editorAppliedRules"),
      action: () => openMatchedRulesDialog(),
    })
  }

  return items
}

function handleContextMenu(event: MouseEvent) {
  if (!editorContext.value || !selfContextMenuContribution.value) return

  event.preventDefault()
  event.stopPropagation()

  showMenu({
    top: event.clientY - 8,
    left: event.clientX,
    items: composeContextMenuItems(
        buildSelfContextMenuItems(),
        props.contextMenuContributions ?? []
    ),
  })
}

defineExpose({reload})

watch(
    () => [resolvedEditorSelection.value?.editor?.id, currentEditorOverrideVersion.value],
    async (next, prev) => {
      if (!prev) return
      if (next[0] === prev[0] && next[1] === prev[1]) return
      await reload()
    }
)
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
