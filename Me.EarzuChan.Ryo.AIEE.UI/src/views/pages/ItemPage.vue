<template>
  <div id="item-page">
    <div class="info-group">
      <div class="ryo-typography-label-large">{{ t('itemMetadata') }}</div>
      <div class="horizontal-layout">
        <div class="info ryo-typography-body-large">{{
            t('id', {id: itemData.id})
          }}<br>{{
            t('name', {name: itemData.name ? itemData.name : t('unnamedInlineItem')})
          }}<br>{{ t('type', {type: itemData.ryoType ? itemData.ryoType.typeName : t('unknownType')}) }}
        </div>
        <div class="info ryo-typography-body-large">{{
            t('parseStatus', {status: boolToText(itemData.parseSuccess)})
          }}<br>{{
            t('editors', {editors: arrayToText(supportedEditorTitles)})
          }}<br>{{ t('importExport', {methods: arrayToText(inOutMethods)}) }}
        </div>
      </div>
    </div>
    <EditorHolder ref="holder" card-surrounded :type="itemData.ryoType" v-model="itemData.tempData"
                  :prefer-editor-id="resolvedEditorId" :item-key="itemKey" editor-path="$"
                  :data-type-name="itemData.dataTypeName" is-root-editor>
      <div id="editor-holder-action-bar">
        <IconButton button-style="filled" id="rename-item-button" icon="edit" @click="renameItem"/>
        <IconButton button-style="filled" id="reload-editor-button" icon="reload" @click="reload(false)"/>
        <IconButton button-style="filled" id="discard-unsaved-changes-button" icon="discard" @click="discard"/>
        <Select id="action-bar-text" :items="supportedEditorTitles" v-model:selected="selectedEditorIndex"/>
        <TextButton button-style="filled" id="save-button" @click="save(null)">{{ t('save') }}</TextButton>
      </div>
    </EditorHolder>
  </div>
</template>

<script setup lang="ts">
import {computed, getCurrentInstance, onActivated, onDeactivated, ref, watch} from "vue"
import {arrayToText, boolToText, ensure} from "@/utils/UsefulUtils"
import EditorHolder from "@/components/EditorHolder.vue"
import IconButton from "@/components/IconButton.vue"
import TextButton from "@/components/TextButton.vue"
import {useAppStateStore} from "@/stores/AppState"
import Select from "@/components/Select.vue"
import {useDialogStateStore} from "@/stores/DialogState"
import {useWorkspaceStateStore} from "@/stores/WorkspaceState"
import {type EditorDescriptor, type FileModel} from "@/models/AppModels"
import {useI18n} from "vue-i18n"
import {applyEditorSelectionWithStrategy} from "@/utils/EditorPreferenceUtils"

const TAG = "ItemPage"

const {t} = useI18n()

const appState = useAppStateStore()
const dialogState = useDialogStateStore()
const workspaceState = useWorkspaceStateStore()

// TODO，CHECK：当关闭卷时，卷的items会被一并关闭，从外表看是没问题的，一干二净，也没有行为Bug。但控制台会有点报错，无伤大雅，但想修可以修修？
// TODO、FIXME：当EditorHolder有滚动条时，滚动条的宽度会导致ItemPage需要水平滚动。为了滚动条就让页面水平滚动，又不是页面太窄，没必要，应该修掉

const props = defineProps({
  data: String
})

const itemKey = computed(() => ensure(props.data) ? props.data! : "")

const itemData = computed<FileModel>(() => {
  console.debug(TAG, "获取项目数据", props.data)
  const item = workspaceState.openedItems.find(i => i.itemKey === itemKey.value)
  if (item) {
    workspaceState.ensureItemSession(itemKey.value)
    return item
  } else {
    console.error(TAG, "无效的项目Key")
    return {id: -1, parseSuccess: false}
  }
})
const rootEditorContext = computed(() => {
  if (!itemData.value.ryoType || !itemData.value.dataTypeName) return undefined
  return appState.createEditorContext(itemData.value.ryoType, {
    itemKey: itemKey.value,
    dataTypeName: itemData.value.dataTypeName,
    path: "$",
    isRoot: true,
  })
})
const supportedEditors = computed(() => {
  return rootEditorContext.value ? appState.getEditorsByContext(rootEditorContext.value) : []
})
const supportedEditorTitles = computed(() => {
  const editors = supportedEditors.value
  if (editors.length === 0) return [t("noAvailableEditorForUnknownType")]
  return editors.map((editor: EditorDescriptor) => t(editor.titleKey))
})
const inOutMethods = computed(() => {
  return [t("jsonFileShort")]
})

const holder = ref<any>(null)
const currentEditorOverrideVersion = computed(() =>
    `${appState.editorOverrideVersion}-${itemKey.value ? workspaceState.getItemSessionEditorOverrideVersion(itemKey.value) : 0}`
)
const resolvedEditorSelection = computed(() => {
  currentEditorOverrideVersion.value
  if (!rootEditorContext.value || !itemKey.value) return undefined
  return appState.resolveEditorForContext(
      rootEditorContext.value,
      workspaceState.getItemSessionOnceEditorOverrides(itemKey.value)
  )
})
const resolvedEditorId = computed(() => resolvedEditorSelection.value?.editor?.id ?? "")
const preferEditorId = computed({
  get() {
    return resolvedEditorId.value
  },
  set(value: string) {
    if (!value || value === resolvedEditorId.value) return
    const editor = supportedEditors.value.find((it: EditorDescriptor) => it.id === value)
    if (!editor) return
    chooseEditorApplyStrategy(editor)
  }
})

const selectedEditorIndex = computed({
  get() {
    if (supportedEditors.value.length === 0) return -1
    const index = supportedEditors.value.findIndex((editor: EditorDescriptor) => editor.id === preferEditorId.value)
    return index === -1 ? 0 : index
  },
  set(value: number) {
    const selected = supportedEditors.value[value]
    if (selected) preferEditorId.value = selected.id
  }
})

async function chooseEditorApplyStrategy(editor: EditorDescriptor) {
  if (!itemKey.value || !rootEditorContext.value) return
  const action = await applyEditorSelectionWithStrategy({
    appState,
    workspaceState,
    dialogState,
    t,
    itemKey: itemKey.value,
    context: rootEditorContext.value,
    editor
  })
  if (action !== "cancel") holder.value?.reload?.()
}

watch(() => {
  return itemData.value.tempData
}, (newValue) => {
  if (!ensure(newValue) || !itemKey.value) return

  try {
    workspaceState.recordItemSessionChange(itemKey.value, newValue)
  } catch (err) {
    console.error(TAG, "记录编辑会话变更失败", err)
  }
}, {deep: true})

function save(onSaved: (() => void) | null) {
  console.log(TAG, "保存", itemData.value.tempData, itemData.value.data)

  dialogState.order({
    headline: t('save'),
    description: t('areYouSureToSave'),
    actions: [
      {text: t('cancel')},
      {
        text: t('confirm'), onClick() {
          (async () => {
            if (!itemKey.value) return
            const saved = await workspaceState.saveItemByKey(itemKey.value, true)
            if (saved) onSaved?.()
          })()
        }
      },
    ]
  })
}

function discard() {
  console.log(TAG, "放弃未保存更改")

  dialogState.order({
    icon: "discard", headline: t('discardUnsavedChanges'),
    description: t('areYouSureToDiscard'),
    actions: [
      {text: t('cancel')},
      {
        text: t('confirm'), onClick: () => {
          if (!itemKey.value) return
          workspaceState.discardItemSessionChanges(itemKey.value)
        }
      },
      {
        text: t('confirmAndReload'), onClick() { // TODO:重不重载弄个偏好设置
          if (!itemKey.value) return
          workspaceState.discardItemSessionChanges(itemKey.value)
          reload(true)
        }
      }
    ]
  })
}

function reload(fromSystem: boolean = false) {
  console.log(TAG, "重载编辑器")

  if (fromSystem) holder.value.reload()
  else dialogState.order({
    icon: "reload", headline: t('reloadEditor'),
    description: t('areYouSureToReload'),
    actions: [
      {text: t('cancel')},
      {text: t('confirm'), onClick: () => holder.value.reload()},
    ]
  })
}

function undo() {
  if (!itemKey.value) return
  workspaceState.undoItemSession(itemKey.value)
}

function redo() {
  if (!itemKey.value) return
  workspaceState.redoItemSession(itemKey.value)
}

function renameItem() {
  if (!itemKey.value) return
  workspaceState.renameItemByKey(itemKey.value)
}

function canUndo() {
  if (!itemKey.value) return false
  return workspaceState.canUndoItemSession(itemKey.value)
}

function canRedo() {
  if (!itemKey.value) return false
  return workspaceState.canRedoItemSession(itemKey.value)
}

defineExpose({
  reload,
  save,
  discard,
  undo,
  redo,
  canUndo,
  canRedo
})

onActivated(() => {
  workspaceState.setActiveTabExposed(getCurrentInstance()!.exposed)
})

onDeactivated(() => {
  if (workspaceState.activeTabExposed === ref(getCurrentInstance()!.exposed).value) workspaceState.setActiveTabExposed(null)
})
</script>

<style scoped>
#item-page {
  display: flex;
  flex-direction: column;
  gap: 24px;

  min-height: 100%;
}

.info-group {
  display: flex;
  flex-direction: column;
  gap: 8px;

  color: white;
}

.horizontal-layout {
  display: flex;
}

.info {
  flex: 1;
}

#editor-holder-action-bar {
  padding: 16px;
  gap: 16px;
  display: flex;

  background-color: var(--ryo-color-surface-container);

  box-shadow: 0 -1px 0 var(--ryo-color-outline-varient);
}

#action-bar-text {
  flex: 1;
  max-height: unset;
}

#reload-editor-button {
  --ryo-color-primary: var(--ryo-color-secondary-container);
  --ryo-color-on-primary: var(--ryo-color-on-secondary-container);
}

#discard-unsaved-changes-button {
  --ryo-color-primary: var(--ryo-color-primary-container);
  --ryo-color-on-primary: var(--ryo-color-on-primary-container);
}

#rename-item-button {
  --ryo-color-primary: var(--ryo-color-tertiary-container);
  --ryo-color-on-primary: var(--ryo-color-on-tertiary-container);
}

#save-button {
  --ryo-color-primary: var(--ryo-color-tertiary-container);
  --ryo-color-on-primary: var(--ryo-color-on-tertiary-container);
}
</style>
