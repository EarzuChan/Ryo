<template>
  <EditableLabel elegant editable v-model:edit-text="filterText">{{ $t('explorer') }}</EditableLabel>
  <TreeView :nodes="computedMassFiles" @node-click="treeNodeClicked"
            @node-right-click="treeNodeRightClicked" :filter-text="filterText" ref="treeViewRef"/>
</template>

<script setup lang="ts">
import EditableLabel from "@/components/EditableLabel.vue"
import {computed, nextTick, ref, watch} from "vue"
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
const treeViewRef = ref<any>(null)

const computedMassFiles = computed(() => {
  const ori = workspaceState.openedVolumes as VolumeModel[]
  const sameNameCount = new Map<string, number>()
  ori.forEach(vol => sameNameCount.set(vol.name, (sameNameCount.get(vol.name) ?? 0) + 1))

  // 把MassFile[]弄成TreeNodeModels
  let result: TreeNodeModel[] = []

  ori?.forEach(mf => {
    let childrenTreeNodeModel: TreeNodeModel[] = []
    mf.items?.forEach(child => {
      const itemName = child.dirtyInVolume ? `${child.name} *` : child.name
      childrenTreeNodeModel.push({name: itemName})
    })

    let volumeName = mf.name
    const hasBoundPath = !!(mf.localPath && mf.localPath.trim().length > 0)
    if ((sameNameCount.get(mf.name) ?? 0) > 1) {
      const suffix = mf.localPath ?? t("volumePathUnsaved")
      volumeName = t("volumeNameWithPath", {name: mf.name, path: suffix})
    }
    if (mf.unsaved || !hasBoundPath) volumeName = `${volumeName} *`
    result.push({name: volumeName, children: childrenTreeNodeModel})
  })

  return result
})

const filterText = ref("")

watch(() => workspaceState.explorerLocateTarget, target => {
  if (!target) return
  filterText.value = ""

  const volumeIndex = workspaceState.openedVolumes.findIndex(vol => vol.id === target.volumeId)
  if (volumeIndex === -1) return

  nextTick(() => treeViewRef.value?.locatePath?.([volumeIndex]))
}, {deep: true})

function treeNodeClicked(nodePath: number[]) {
  const [item, _, dad] = parsePath(nodePath)
  if (!item || !dad) return
  dialogState.order({
    headline: t('itemClicked'),
    description: t('nodeDescription', {path: nodePath.join('/'), name: item.name, id: item.id}),
    closeOnOverlayClick: true,
    actions: [
      {
        text: t('open'), onClick() {
          workspaceState.mentionItem(dad.id, item.id)
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
          action: () => workspaceState.mentionItem(dad.id, stuff.id)
        },
        {
          name: t('renameItem'),
          action: () => workspaceState.renameItem(dad.id, stuff.name)
        },
        {
          name: t('deleteItem'),
          action: () => workspaceState.deleteItem(dad.id, stuff.name)
        }
    )
    else items.push(
        {
          name: t('renameVolume'),
          action: () => workspaceState.renameVolume(stuff.id, stuff.name)
        },
        {
          name: t('cloneVolume'),
          action: () => workspaceState.cloneVolume(stuff.id)
        },
        {
          name: t('addItem'),
          action: () => workspaceState.addItemInVolume(stuff.id)
        },
        {
          name: t('garbageCollect'),
          action: () => workspaceState.gcVolume(stuff.id)
        },
        {
          name: t('saveMass'),
          action: () => workspaceState.saveVolume(stuff.id)
        },
        {
          name: t('closeMass'),
          action: () => workspaceState.closeVolume(stuff.id)
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
