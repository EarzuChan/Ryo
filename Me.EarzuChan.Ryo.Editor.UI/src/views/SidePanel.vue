<template>
  <div id="side-panel">
    <div id="control-bar">
      <div id="side-panel-tab-container">
        <IconButton :size="48" :class="{'current-panel':currentPanel===index}" :filled-icon="currentPanel===index"
                    v-for="(panelItem,index) in sidePanelItems"
                    :icon="panelItem.icon" @click="clickPanelTab(panelItem,index)"/>
      </div>
      <div id="side-panel-control-buttons">
        <IconButton :size="48" :icon="appState.sidePanelExpanded?'panel_narrow':'panel'"
                    @click="appState.sidePanelExpanded=!appState.sidePanelExpanded"/>
        <IconButton :size="48" disabled icon="settings" @click="openSettings"/>
      </div>
    </div>
    <div id="side-panel-content" v-show="appState.sidePanelExpanded">
      <keep-alive>
        <Component :is="sidePanelItems[currentPanel].panel"/>
      </keep-alive>
    </div>
  </div>
</template>

<script setup lang="ts">
import IconButton from "@/components/IconButton.vue"
import {ref} from "vue"
import ExplorerPanel from "@/views/sidePanels/ExplorerPanel.vue"
import {useAppStateStore} from "@/stores/AppState"

interface SidePanelItem {
  name: string
  icon: string
  panel: any
}

const sidePanelItems = [
  {name: '资源管理器', icon: 'list', panel: ExplorerPanel},
]
const currentPanel = ref<number>(0)

const appState = useAppStateStore()

function openSettings() {
}

function clickPanelTab(item: SidePanelItem, index: number) {
  currentPanel.value = index
}
</script>

<style scoped>
#side-panel {
  display: flex;
  flex-direction: row;
  overflow: hidden;
}

#control-bar {
  display: flex;
  flex-direction: column;
  width: 64px;
  padding: 8px 0;
  align-items: center;
}

#side-panel-tab-container {
  display: flex;
  flex-direction: column;
  flex: 1;
  gap: 4px;
}

#side-panel-control-buttons {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.current-panel {
  background-color: var(--ryo-color-secondary-container);
  --ryo-color-on-surface-variant: var(--ryo-color-on-secondary-container);
}

#side-panel-content {
  border-radius: 16px 16px 0 0;
  width: 288px;
  margin-right: 8px;
  padding: 12px 12px 0;
  display: flex;
  flex-direction: column;
  flex: 1;
  background-color: var(--ryo-color-surface-container-high);
}
</style>