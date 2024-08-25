<template>
  <div id="panel">
    <EditableLabel elegant editable v-model:edit-text="filterText">资源管理器</EditableLabel>
    <TreeView :nodes="computedMassFiles" @node-click="treeNodeClicked"
              @node-right-click="treeNodeRightClicked" :filter-text="filterText"/>
  </div>
</template>

<script setup lang="ts">
import EditableLabel from "@/components/EditableLabel.vue"
import {computed, ref} from "vue"
import TreeView from "@/components/TreeView.vue"
import type {MenuItem, TreeNodeModel} from "@/models/UIModels"
import {useWorkspaceStateStore} from "@/stores/WorkspaceState"
import type {VolumeModel} from "@/models/AppModels"
import {useDialogStateStore} from "@/stores/DialogState"
import {showMenu} from "@/utils/MenuUtils"

const TAG = "ExplorerPanel"

const workspaceState = useWorkspaceStateStore()
const dialogState = useDialogStateStore()

const computedMassFiles = computed(() => {
  const ori = workspaceState.openedVolumes as VolumeModel[]

  // 把MassFile[]弄成TreeNodeModels
  let result: TreeNodeModel[] = []

  ori?.forEach(mf => {
    let childrenTreeNodeModel: TreeNodeModel[] = []
    mf.items?.forEach(child => {
      childrenTreeNodeModel.push({name: child.name!})
    })

    result.push({name: mf.name, children: childrenTreeNodeModel})
  })

  return result
})

const filterText = ref("")

function treeNodeClicked(nodePath: number[]) {
  const [item, _, dad] = parsePath(nodePath)
  dialogState.order({
    headline: '点击了项目',
    description: `节点路径：${nodePath.join('/')}\n项目名称：${item.name}\n项目ID：${item.id}`,
    closeOnOverlayClick: true,
    actions: [
      {
        text: "打开", onClick() {
          workspaceState.mentionItem(dad.name, item.id)
        },
      },
      {text: '了解'}],
  })
}

function treeNodeRightClicked(nodePath: number[], e: MouseEvent) {
  const [stuff, isItem, dad] = parsePath(nodePath)

  const items: MenuItem[] = [
    {name: `右击了${stuff === undefined ? '未知' : isItem ? '项目' : 'Mass'}节点`, disabled: true},
    {name: '节点路径：' + nodePath.join('/'), disabled: true},
    {
      name: '节点名称：' + (stuff === undefined ? '未知' : isItem ? dad.name + '/' + stuff.name : stuff.name),
      disabled: true
    },
  ]

  if (stuff !== undefined) {
    if (isItem) items.push({name: `项目ID${stuff.id}`, disabled: true}, {
      name: '打开项目',
      action: () => workspaceState.mentionItem(dad.name, stuff.id)
    })
    else items.push({
      name: '保存Mass',
      action: () => workspaceState.saveVolume(stuff.name)
    }, {
      name: '关闭Mass',
      action: () => workspaceState.closeVolume(stuff.name)
    })
  }

  showMenu({top: e.clientY - 8, left: e.clientX, items})
}

function parsePath(path: number[]): any[] {
  const file = workspaceState.openedVolumes[path[0]]
  if (!file) {
    console.error(TAG, `No file found at index ${path[0]}`)
    return [undefined, false, undefined]
  }

  if (path.length === 2) {
    const item = file.items?.[path[1]];
    if (!item) {
      console.error(TAG, `No item found at index ${path[1]} in file ${file.name}`)
      return [undefined, true, file]
    }
    return [item, true, file]
  }

  return [file, false, undefined]
}
</script>

<style scoped>
#panel {
  display: flex;
  min-height: 100%;

  flex-direction: column;
}
</style>