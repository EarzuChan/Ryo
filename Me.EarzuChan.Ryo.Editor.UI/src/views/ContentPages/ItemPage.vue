<template>
  <div id="item-page">
    <div class="info-group">
      <div class="ryo-typography-label-large">项目元数据</div>
      <div class="horizontal-layout">
        <div class="info ryo-typography-body-large">ID：{{
            data.id
          }}<br>名称：{{ data.name ? data.name : "（无名内联项目）" }}<br>类型：{{
            data.type ? data.type.typeName : "（未知类型）"
          }}
        </div>
        <div class="info ryo-typography-body-large">解析状态：{{
            boolToText(data.parseSuccess)
          }}<br>编辑器：{{ arrayToText(supportedEditors) }}<br>导入导出：{{ arrayToText(inOutMethods) }}
        </div>
      </div>
    </div>
    <EditorHolder ref="holder" card-surrounded :type="data.type" v-model="data.tempData" :prefer-editor="preferEditor">
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
import {computed, getCurrentInstance, onActivated, onDeactivated, type PropType, ref} from "vue"
import type {ItemModel} from "@/models/AppModels"
import {arrayToText, boolToText, deepCopy, getSfcName, TODO} from "@/utils/UsefulUtils"
import EditorHolder from "@/components/EditorHolder.vue"
import IconButton from "@/components/IconButton.vue"
import TextButton from "@/components/TextButton.vue"
import {useAppStateStore} from "@/stores/AppState"
import Select from "@/components/Select.vue"
import {useDialogStateStore} from "@/stores/DialogState"
import {useWorkspaceStateStore} from "@/stores/WorkspaceState"

const TAG = "ItemPage"

const appState = useAppStateStore()
const dialogState = useDialogStateStore()
const workspaceState = useWorkspaceStateStore()
// TODO: 暂存未保存了可以，watch data然后init，用户在暂存上修改，保存才写入data
// TODO: 历史记录，撤消重做
// TODO: 重做编辑器容器底部栏 弄成插槽
// TODO: 默认编辑器选择的提示该如何？

const props = defineProps({
  data: {
    type: Object as PropType<ItemModel>,
    default: {}
  }
})

const supportedEditors = computed(() => {
  const type = props.data.type
  if (type) {
    return appState.getEditorsByRyoType(type).map(et => getSfcName(et))
  }

  return ["TODO"]
})
const inOutMethods = computed(() => {
  const typeName = props.data.type

  return ["TODO"]
})

const holder = ref<any>(null)

// BUG: 不稳定啊，应该在Prop里面设置一个键值代表当前文件码，在WorkspaceState里面另外提取内容
const preferEditor = ref(0)

function save() {
  console.log(TAG, "保存", props.data.tempData, props.data.data)

  dialogState.order({
    headline: "保存",
    description: "您确定要保存吗？",
    actions: [
      {text: "取消"},
      {
        text: "确定", onClick() {
          props.data.data = deepCopy(props.data.tempData)
          console.log(TAG, "保存成功", props.data.tempData, props.data.data)
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
      {text: "确定", onClick: () => props.data.tempData = deepCopy(props.data.data)},
      {
        text: "确定并重载", onClick() { // TODO:重不重载弄个偏好设置
          props.data.tempData = deepCopy(props.data.data)
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