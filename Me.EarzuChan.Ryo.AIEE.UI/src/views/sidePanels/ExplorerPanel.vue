<template>
  <EditableLabel elegant editable v-model:edit-text="filterText">{{ $t('explorer') }}</EditableLabel>
  <TreeView :nodes="computedMassFiles" @node-click="treeNodeClicked"
            @node-right-click="treeNodeRightClicked" :filter-text="filterText"/>
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
import {useI18n} from "vue-i18n"

const TAG = "ExplorerPanel"

const workspaceState = useWorkspaceStateStore()
const dialogState = useDialogStateStore()
const {t} = useI18n()

const computedMassFiles = computed(() => {
  const ori = workspaceState.openedVolumes as VolumeModel[]

  // 把MassFile[]弄成TreeNodeModels
  let result: TreeNodeModel[] = []

  ori?.forEach(mf => {
    let childrenTreeNodeModel: TreeNodeModel[] = []
    mf.items?.forEach(child => {
      const itemName = child.dirtyInVolume ? `${child.name} *` : child.name
      childrenTreeNodeModel.push({name: itemName})
    })

    const volumeName = mf.unsaved ? `${mf.name} *` : mf.name
    result.push({name: volumeName, children: childrenTreeNodeModel})
  })

  return result
})

const filterText = ref("")

function treeNodeClicked(nodePath: number[]) {
  const [item, _, dad] = parsePath(nodePath)
  dialogState.order({
    headline: t('itemClicked'),
    description: t('nodeDescription', {path: nodePath.join('/'), name: item.name, id: item.id}),
    closeOnOverlayClick: true,
    actions: [
      {
        text: t('open'), onClick() {
          workspaceState.mentionItem(dad.name, item.id)
        },
      },
      {text: t('cancel')}],
  })
}

function treeNodeRightClicked(nodePath: number[], e: MouseEvent) {
  const [stuff, isItem, dad] = parsePath(nodePath)

  const items: MenuItem[] = [
    {
      name: t('rightClickNode', {nodeType: stuff === undefined ? t('unknown') : isItem ? t('item') : t('mass')}),
      disabled: true
    },
    {name: t('nodePath', {path: nodePath.join('/')}), disabled: true},
    {
      name: t('nodeName', {name: stuff === undefined ? t('unknown') : isItem ? dad.name + '/' + stuff.name : stuff.name}),
      disabled: true
    },
  ]

  if (stuff !== undefined) {
    if (isItem) items.push(
        {name: t('itemId', {itemId: stuff.id}), disabled: true},
        {
          name: t('openItem'),
          action: () => workspaceState.mentionItem(dad.name, stuff.id)
        },
        {
          name: t('renameItem'),
          action: () => workspaceState.renameItem(dad.name, stuff.name)
        },
        {
          name: t('deleteItem'),
          action: () => workspaceState.deleteItem(dad.name, stuff.name)
        }
    )
    else items.push(
        {
          name: t('addItem'),
          action: () => workspaceState.addItemInVolume(stuff.name)
        },
        {
          name: t('garbageCollect'),
          action: () => workspaceState.gcVolume(stuff.name)
        },
        {
          name: t('saveMass'),
          action: () => workspaceState.saveVolume(stuff.name)
        },
        {
          name: t('closeMass'),
          action: () => workspaceState.closeVolume(stuff.name)
        },
    )
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
    const item = file.items?.[path[1]]
    if (!item) {
      console.error(TAG, `No item found at index ${path[1]} in file ${file.name}`)
      return [undefined, true, file]
    }
    return [item, true, file]
  }

  return [file, false, undefined]
}
</script>
