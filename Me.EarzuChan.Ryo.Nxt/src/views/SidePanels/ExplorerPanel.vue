<template>
  <div id="panel">
    <EditableLabel elegant editable v-model:edit-text="filterText">资源管理器</EditableLabel>
    <TreeView :nodes="computedMassFiles" :filter-text="filterText"/>
  </div>
</template>

<script setup lang="ts">
import EditableLabel from "@/components/EditableLabel.vue"
import {computed, ref} from "vue"
import TreeView from "@/components/TreeView.vue"
import type {MassFile, TreeNodeModel} from "@/models/Models"
import {useOpenedFilesStateStore} from "@/stores/OpenedFilesState"

const openedFilesStateStore = useOpenedFilesStateStore()

const computedMassFiles = computed(() => {
  const ori = openedFilesStateStore.openedFiles as MassFile[]

  // 把MassFile[]弄成TreeNodeModels
  let result: TreeNodeModel[] = []
  ori?.forEach(mf => {
    let childrenLTreeNodeModel: TreeNodeModel[] = []
    mf.items?.forEach(child => {
      childrenLTreeNodeModel.push({name: child.name})
    })
    result.push({name: mf.name, children: childrenLTreeNodeModel})
  })
  return result
})

const filterText = ref("")
</script>

<style scoped>
#panel {
  display: flex;
  flex: 1;

  flex-direction: column;
}
</style>