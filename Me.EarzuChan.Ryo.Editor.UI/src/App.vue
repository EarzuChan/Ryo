<template>
  <div id="ryo-viewport" class="ryo-dark">
    <Transition mode="out-in" name="shifter">
      <div v-if="allAvailable" id="ryo-app" class="flex">
        <TopAppBar/>
        <div id="ryo-app-contents" class="flex">
          <SidePanel/>
          <TabPanel/>
        </div>
      </div>
      <EmptyPage v-else style="flex: 1"/>
    </Transition>
  </div>
</template>

<script setup lang="ts">
import {useAppStateStore} from "@/stores/AppState"
import TopAppBar from "@/views/TopAppBar.vue"
import SidePanel from "@/views/SidePanel.vue"
import TabPanel from "@/views/TabPanel.vue"
import {computed} from "vue"
import {useKurisuStateStore} from "@/stores/KurisuState"
import {useWorkspaceStateStore} from "@/stores/WorkspaceState"
import EmptyPage from "@/views/Pages/EmptyPage.vue"

const appState = useAppStateStore()
const kurisuState = useKurisuStateStore()
const openedFilesState = useWorkspaceStateStore()

const allAvailable = computed(() => appState.available && kurisuState.available && openedFilesState.available)
</script>

<style scoped>
#ryo-viewport {
  display: flex;
  height: 100vh;

  background-color: var(--ryo-color-surface);
  overflow: hidden;
}

#ryo-app-contents {
  flex-direction: row;
}

#ryo-app {
  flex-direction: column;
}

.flex {
  flex: 1;
  display: flex;
  overflow: hidden;

}

.shifter-enter-active {
  transition: all var(--ryo-motion-emphasized-decelerate);
}

.shifter-leave-active {
  transition: all var(--ryo-motion-emphasized-accelerate);
}

.shifter-enter-from {
  opacity: 0;
  transform: translateY(10px);
}

.shifter-leave-to {
  opacity: 0;
  transform: translateY(-40px);
}
</style>
