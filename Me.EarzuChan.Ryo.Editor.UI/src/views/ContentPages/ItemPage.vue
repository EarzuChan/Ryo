<template>
  <div id="item-page">
    <div class="info-group">
      <div class="ryo-typography-label-large">项目元数据</div>
      <div class="horizontal-layout">
        <div class="info ryo-typography-body-large">ID：{{
            itemData.id
          }}<br>名称：{{ itemData.name ? itemData.name : "（无名内联项目）" }}<br>类型：{{
            itemData.type ? itemData.type.typeName : "（未知类型）"
          }}
        </div>
        <div class="info ryo-typography-body-large">解析状态：{{
            boolToText(itemData.parseSuccess)
          }}<br>编辑器：{{ arrayToText(supportedEditors) }}<br>导入导出：{{ arrayToText(inOutMethods) }}
        </div>
      </div>
    </div>
    <EditorHolder ref="holder" card-surrounded :type="itemData.type" v-model="itemData.tempData"
                  :prefer-editor="preferEditor">
      <div id="editor-holder-action-bar">
        <IconButton button-style="filled" id="reload-editor-button" icon="reload" @click="reload(false)"/>
        <IconButton button-style="filled" id="discard-unsaved-changes-button" icon="discard" @click="discard"/>
        <Select id="action-bar-text" :items="supportedEditors" v-model:selected="preferEditor"/>
        <TextButton button-style="filled" id="save-button" @click="save">保存</TextButton>
      </div>
    </EditorHolder>
  </div>
</template>

<script setup lang="ts">
import {computed, getCurrentInstance, onActivated, onDeactivated, ref, watch} from "vue"
import {arrayToText, boolToText, deepCopy, ensure, getSfcName, TODO} from "@/utils/UsefulUtils"
import EditorHolder from "@/components/EditorHolder.vue"
import IconButton from "@/components/IconButton.vue"
import TextButton from "@/components/TextButton.vue"
import {useAppStateStore} from "@/stores/AppState"
import Select from "@/components/Select.vue"
import {useDialogStateStore} from "@/stores/DialogState"
import {useWorkspaceStateStore} from "@/stores/WorkspaceState"
import {type FileModel} from "@/models/AppModels"

const TAG = "ItemPage"

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
  const type = itemData.value.type
  if (type) {
    return appState.getEditorsByRyoType(type).map(et => getSfcName(et))
  }

  return ["未知类型 无可用编辑器"]
})
const inOutMethods = computed(() => {
  const typeName = itemData.value.type

  return [TODO(TAG, "获取导入导出方法")]
})

const holder = ref<any>(null)
const preferEditor = ref(0)

function save() {
  console.log(TAG, "保存", itemData.value.tempData, itemData.value.data)

  dialogState.order({
    headline: "保存",
    description: "您确定要保存吗？",
    actions: [
      {text: "取消"},
      {
        text: "确定", onClick() {
          (async () => {
            console.log(TAG, "异步保存")

            itemData.value.data = deepCopy(itemData.value.tempData)

            const newItemId = await workspaceState.saveItem(itemData.value.fromFile!, itemData.value.name!, itemData.value.data)

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
    icon: "discard", headline: "放弃未保存更改",
    description: "您确定要放弃未保存的更改吗？\n这将恢复编辑器到上次保存的状态",
    actions: [
      {text: "取消"},
      {text: "确定", onClick: () => itemData.value.tempData = deepCopy(itemData.value.data)},
      {
        text: "确定并重载", onClick() { // TODO:重不重载弄个偏好设置
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
    icon: "reload", headline: "重载编辑器",
    description: "您确定要重载编辑器吗？\n这将放弃未写入暂存的编辑中不正确数据",
    actions: [
      {text: "取消"},
      {text: "确定", onClick: () => holder.value.reload()},
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
  workspaceState.setActiveTabPage(getCurrentInstance()!.exposed)
})

onDeactivated(() => {
  if (workspaceState.activeTabPage === ref(getCurrentInstance()!.exposed).value) workspaceState.setActiveTabPage(null)
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