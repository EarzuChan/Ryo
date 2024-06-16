<template>
  <div id="tabs">
    <div id="tabs_top_bar" v-if="openedFilesState.openedTabs.length!==0">
      <div id="tabs_container">
        <div v-for="(tab,index) in openedFilesState.openedTabs" @dblclick="anchorTab(index)" @click="clickTab(index)"
             class="tab_container"
             :class="{'active':openedFilesState.activeTabIndex===index}">
          <div class="tab_top_padding"/>
          <div class="tab_content">
            <div class="tab_title ryo-typography-label-large"
                 :class="{'non-resident':tab.nonResident}">{{ tab.name }}
            </div>
            <IconButton @click.stop="closeTab(index)" :size="24"
                        :icon="tabIconHovering===index || tab.unsaved!==true ? 'close_tab' : 'unsaved_dot'"
                        class="tab_icon" @mouseenter="tabIconHovering=index" @mouseleave="tabIconHovering=-1"/>
          </div>
          <div class="tab_bottom_padding"/>
        </div>
      </div>
      <div id="separator"/>
    </div>
    <div id="content_container">
      <component class="content" :is="currentTabPageOrEmptyPage" :data="openedFilesState.activeTab?.data"/>
    </div>
  </div>
</template>

<script setup lang="ts">
import IconButton from "@/components/IconButton.vue"
import {computed, ref} from "vue"
import EmptyPage from "@/views/ContentPages/EmptyPage.vue"
import {useOpenedFilesStateStore} from "@/stores/OpenedFilesState"

const TAG = "Tabs"

const tabIconHovering = ref(-1)
const openedFilesState = useOpenedFilesStateStore()
const currentTabPageOrEmptyPage = computed(() => openedFilesState.activeTab?.page || EmptyPage)

function clickTab(index: number) {
  console.log(TAG, "点击了第" + (index + 1) + "个标签")
  openedFilesState.clickTab(index)
}

function anchorTab(index: number) {
  console.log(TAG, "双击了第" + (index + 1) + "个标签，使其固定")
  openedFilesState.anchorTab(index)
}

function closeTab(index: number) {
  console.log(TAG, "关闭了第" + (index + 1) + "个标签")
  openedFilesState.closeTab(index)
}
</script>

<style scoped>
#tabs {
  display: flex;
  flex-direction: column;
  flex: 1;
  overflow: hidden;
  background-color: var(--ryo-color-surface-container);

  border-radius: 16px 0 0 0;
}

#tabs_top_bar {
  display: flex;
  flex-direction: column;
  height: 36px;
}

#tabs_container {
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
  flex: 1;
  overflow: auto;

  padding: 24px;
}

.content {
  min-height: 100%;
}
</style>