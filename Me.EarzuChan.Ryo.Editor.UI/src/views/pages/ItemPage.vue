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
                  :prefer-editor-id="preferEditorId">
      <div id="editor-holder-action-bar">
        <IconButton button-style="filled" id="reload-editor-button" icon="reload" @click="reload(false)"/>
        <IconButton button-style="filled" id="discard-unsaved-changes-button" icon="discard" @click="discard"/>
        <Select id="action-bar-text" :items="supportedEditorTitles" v-model:selected="selectedEditorIndex"/>
        <TextButton button-style="filled" id="save-button" @click="save">{{ t('save') }}</TextButton>
      </div>
    </EditorHolder>
  </div>
</template>

<script setup lang="ts">
import {computed, getCurrentInstance, onActivated, onDeactivated, ref, watch} from "vue"
import {arrayToText, boolToText, ensure, TODO} from "@/utils/UsefulUtils"
import EditorHolder from "@/components/EditorHolder.vue"
import IconButton from "@/components/IconButton.vue"
import TextButton from "@/components/TextButton.vue"
import {useAppStateStore} from "@/stores/AppState"
import Select from "@/components/Select.vue"
import {useDialogStateStore} from "@/stores/DialogState"
import {useWorkspaceStateStore} from "@/stores/WorkspaceState"
import {type FileModel} from "@/models/AppModels"
import {useI18n} from "vue-i18n"

const TAG = "ItemPage"

const {t} = useI18n()

const appState = useAppStateStore()
const dialogState = useDialogStateStore()
const workspaceState = useWorkspaceStateStore()
/* TODO: 默认编辑器选择的提示该如何？
重做编辑器容器底部栏 弄成插槽？*/

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
const supportedEditors = computed(() => {
  const type = itemData.value.ryoType
  if (type) {
    return appState.getEditorsByRyoType(type)
  }

  return []
})
const supportedEditorTitles = computed(() => {
  const editors = supportedEditors.value
  if (editors.length === 0) return [t("noAvailableEditorForUnknownType")]
  return editors.map(editor => t(editor.titleKey))
})
const inOutMethods = computed(() => {
  const typeName = itemData.value.ryoType

  return [TODO(TAG, "Get Import/Export Methods")]
})

const holder = ref<any>(null)
const preferEditorId = computed({
  get() {
    if (!itemData.value.preferredRootEditorId) itemData.value.preferredRootEditorId = supportedEditors.value[0]?.id
    return itemData.value.preferredRootEditorId ?? ""
  },
  set(value: string) {
    itemData.value.preferredRootEditorId = value
  }
})

const selectedEditorIndex = computed({
  get() {
    if (supportedEditors.value.length === 0) return -1
    const index = supportedEditors.value.findIndex(editor => editor.id === preferEditorId.value)
    return index === -1 ? 0 : index
  },
  set(value: number) {
    const selected = supportedEditors.value[value]
    if (selected) preferEditorId.value = selected.id
  }
})

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

function save(onSaved?: () => void) {
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

#save-button {
  --ryo-color-primary: var(--ryo-color-tertiary-container);
  --ryo-color-on-primary: var(--ryo-color-on-tertiary-container);
}
</style>
