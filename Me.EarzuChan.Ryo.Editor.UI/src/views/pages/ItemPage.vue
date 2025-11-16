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
            t('editors', {editors: arrayToText(supportedEditors)})
          }}<br>{{ t('importExport', {methods: arrayToText(inOutMethods)}) }}
        </div>
      </div>
    </div>
    <EditorHolder ref="holder" card-surrounded :type="itemData.ryoType" v-model="itemData.tempData"
                  :prefer-editor="preferEditor">
      <div id="editor-holder-action-bar">
        <IconButton button-style="filled" id="reload-editor-button" icon="reload" @click="reload(false)"/>
        <IconButton button-style="filled" id="discard-unsaved-changes-button" icon="discard" @click="discard"/>
        <Select id="action-bar-text" :items="supportedEditors" v-model:selected="preferEditor"/>
        <TextButton button-style="filled" id="save-button" @click="save">{{ t('save') }}</TextButton>
      </div>
    </EditorHolder>
  </div>
</template>

<script setup lang="ts">
import {computed, getCurrentInstance, onActivated, onDeactivated, ref} from "vue"
import {arrayToText, boolToText, deepCopy, ensure, getSfcName, TODO} from "@/utils/UsefulUtils"
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
// TODO: 历史记录，撤消重做
/* TODO: 默认编辑器选择的提示该如何？
重做编辑器容器底部栏 弄成插槽？*/

const props = defineProps({
  data: Number
})

const itemData = computed<FileModel>(() => {
  console.debug(TAG, "获取项目数据", props.data, workspaceState.openedItems.length)
  if (ensure(props.data) && props.data! > -1 && props.data! < workspaceState.openedItems.length) {
    const item = workspaceState.openedItems[props.data!]
    if (!ensure(item.tempData)) {
      console.debug(TAG, "初始化项目数据暂存", item.data)
      item.unsaved = false // 怎么追踪更改
      item.tempData = deepCopy(item.data)
    }
    return item
  } else {
    console.error(TAG, "无效的项目数据索引")
    return {id: -1, parseSuccess: false}
  }
})
const supportedEditors = computed(() => {
  const type = itemData.value.ryoType
  if (type) {
    return appState.getEditorsByRyoType(type).map(et => getSfcName(et))
  }

  return ["未知类型 无可用编辑器"]
})
const inOutMethods = computed(() => {
  const typeName = itemData.value.ryoType

  return [TODO(TAG, "Get Import/Export Methods")]
})

const holder = ref<any>(null)
const preferEditor = ref(0)

function save() {
  console.log(TAG, "保存", itemData.value.tempData, itemData.value.data)

  dialogState.order({
    headline: t('save'),
    description: t('areYouSureToSave'),
    actions: [
      {text: t('cancel')},
      {
        text: t('confirm'), onClick() {
          (async () => {
            // HACK: 可能不稳定
            console.log(TAG, "异步保存")

            itemData.value.data = deepCopy(itemData.value.tempData)

            const newItemId = await workspaceState.saveItem(itemData.value.fromFile!, itemData.value.name!, itemData.value.data, itemData.value.dataTypeName!!)

            console.log(TAG, "保存成功", newItemId)

            itemData.value.id = newItemId
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
      {text: t('confirm'), onClick: () => itemData.value.tempData = deepCopy(itemData.value.data)},
      {
        text: t('confirmAndReload'), onClick() { // TODO:重不重载弄个偏好设置
          itemData.value.tempData = deepCopy(itemData.value.data)
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
  TODO(TAG, "撤销")
}

function redo() {
  TODO(TAG, "重做")
}

defineExpose({
  reload,
  save,
  discard,
  undo,
  redo
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
  margin: -2px;
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