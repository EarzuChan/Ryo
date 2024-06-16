<template>
  <div id="ryo-app" class="ryo-dark">
    <TopAppBar/>
    <div id="contents">
      <SidePanel/>
      <Tabs/>
    </div>
  </div>
</template>

<script setup lang="ts">
import {useAppStateStore} from "@/stores/AppState"
import TopAppBar from "@/views/TopAppBar.vue"
import type {TreeNodeModel} from "@/models/Models"
import {ref} from "vue"
import SidePanel from "@/views/SidePanel.vue"
import Tabs from "@/views/Tabs.vue"

const appState = useAppStateStore()
</script>

<style scoped>
#ryo-app {
  display: flex;
  flex-direction: column;
  height: 100vh;

  background-color: var(--ryo-color-surface);
  overflow: hidden;
}

#contents {
  display: flex;
  flex-direction: row;
  flex: 1;
  overflow: hidden;
}

#main {
  background-color: white;
  flex: 1;
}
</style>
