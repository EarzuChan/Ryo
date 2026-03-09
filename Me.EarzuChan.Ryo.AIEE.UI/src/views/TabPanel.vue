<template>
  <div id="tab_panel">
    <div id="tab_panel_top_bar" v-if="workspaceState.openedTabs.length!==0">
      <div id="tab_panel_container">
        <div v-for="(tab,index) in workspaceState.openedTabs" :key="tab.key ?? index"
             @dblclick="anchorTab(index)" @click="clickTab(index)"
             class="tab_container"
             :class="{'active':workspaceState.activeTabIndex===index}">
          <div class="tab_top_padding"/>
          <div class="tab_content">
            <div class="tab_title ryo-typography-label-large"
                 :class="{'non-resident':tab.nonResident}">{{ tab.name }}
            </div>
            <IconButton @click.stop="closeTab(index)" :size="24"
                        :icon="tabIconHovering===index || !workspaceState.getIsTabUnsaved(index) ? 'close_tab' : 'unsaved_dot'"
                        class="tab_icon" @mouseenter="tabIconHovering=index" @mouseleave="tabIconHovering=-1"/>
          </div>
          <div class="tab_bottom_padding"/>
        </div>
      </div>
      <div id="separator"/>
    </div>
    <div id="content_container">
      <div class="content_shell" :class="{'page-padding': shouldPadActivePage}">
        <KeepAlive include="ItemPage">
          <Component class="content" :key="workspaceState.activeTab?.key ?? workspaceState.activeTabIndex"
                     :is="currentTabPageOrEmptyPage"
                     :data="workspaceState.activeTab?.data"/>
        </KeepAlive>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import IconButton from "@/components/IconButton.vue"
import {computed, ref} from "vue"
import EmptyPage from "@/views/pages/EmptyPage.vue"
import {useWorkspaceStateStore} from "@/stores/WorkspaceState"
import ItemPage from "@/views/pages/ItemPage.vue"
import WelcomePage from "@/views/pages/WelcomePage.vue"

const TAG = "Tabs"

const tabIconHovering = ref(-1)
const workspaceState = useWorkspaceStateStore()
const currentTabPageOrEmptyPage = computed(() => workspaceState.activeTab?.page || EmptyPage)
const shouldPadActivePage = computed(() => {
  const page = workspaceState.activeTab?.page
  return page === ItemPage || page === WelcomePage
})

function clickTab(index: number) {
  console.log(TAG, "点击了第" + (index + 1) + "个标签")
  workspaceState.clickTab(index)
}

function anchorTab(index: number) {
  console.log(TAG, "双击了第" + (index + 1) + "个标签，使其固定")
  workspaceState.anchorTab(index)
}

function closeTab(index: number) {
  console.log(TAG, "关闭了第" + (index + 1) + "个标签")
  workspaceState.closeTab(index)
}
</script>

<style scoped>
#tab_panel {
  display: flex;
  flex-direction: column;
  flex: 1;
  overflow: hidden;
  background-color: var(--ryo-color-surface-container);

  border-radius: 16px 0 0 0;
}

#tab_panel_top_bar {
  display: flex;
  flex-direction: column;
  height: 36px;
}

#tab_panel_container {
  display: flex;
  flex: 1;
  flex-direction: row;
}

#separator {
  height: 1px;
  background-color: var(--ryo-color-outline-varient);
}

.tab_container {
  display: flex;
  flex-direction: column;

  overflow: hidden;
  position: relative;
}

.tab_container * {
  transition: all 0ms;
}

.tab_container::after {
  pointer-events: none;
  content: "";
  position: absolute;

  transition: all var(--ryo-motion-standard);

  top: 0;
  bottom: 0;
  right: 0;
  left: 0;
}

.tab_container:hover::after {
  background-color: rgba(var(--ryo-color-state-layers-on-surface-variant), var(--ryo-opacity-state-layers-008));
}

.tab_container:active::after {
  background-color: rgba(var(--ryo-color-state-layers-primary), var(--ryo-opacity-state-layers-012));
}

.tab_top_padding, .tab_bottom_padding {
  height: 2px;
}

.tab_content {
  display: flex;
  flex: 1;
  align-items: center;
  flex-direction: row;
  gap: 8px;
  padding: 0 16px;
}

.tab_title {
  color: var(--ryo-color-on-surface-variant);
}

.tab_title.non-resident {
  font-style: italic;
}

.active .tab_title {
  color: var(--ryo-color-primary);
}

.active .tab_icon {
  --ryo-color-on-surface-variant: var(--ryo-color-primary);
}

.active .tab_bottom_padding {
  background-color: var(--ryo-color-primary);
}

#content_container {
  display: flex;
  flex: 1;
  min-height: 0;
  overflow: hidden;
}

.content_shell {
  box-sizing: border-box;
  display: flex;
  flex: 1;
  min-height: 0;
  overflow: auto;
}

.content_shell.page-padding {
  padding: 24px;
}

.content {
  flex: 1;
  min-height: 0;
}
</style>
