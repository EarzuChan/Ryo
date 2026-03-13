<template>
  <div id="tab_panel">
    <div id="tab_panel_top_bar" v-if="workspaceState.openedTabs.length!==0">
      <div id="tab_panel_scroller" ref="tabBarRef" @wheel="onTabBarWheel">
        <VueDraggable id="tab_panel_container" v-model="tabModels"
                      :animation="180" filter=".tab_icon" :prevent-on-filter="false">
          <div v-for="(tab,index) in workspaceState.openedTabs" :key="tab.key ?? index"
               @dblclick="anchorTab(index)" @click="clickTab(index)"
               @contextmenu.prevent.stop="openTabContextMenu(index, $event)"
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
        </VueDraggable>
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
import {showMenu} from "@/utils/MenuUtils"
import {useI18n} from "vue-i18n"
import type {MenuItem} from "@/models/UIModels"
import {VueDraggable} from "vue-draggable-plus"
import type {TabModel} from "@/models/AppModels"

const TAG = "Tabs"

const tabIconHovering = ref(-1)
const workspaceState = useWorkspaceStateStore()
const {t} = useI18n()
const currentTabPageOrEmptyPage = computed(() => workspaceState.activeTab?.page || EmptyPage)
const shouldPadActivePage = computed(() => {
  const page = workspaceState.activeTab?.page
  return page === ItemPage || page === WelcomePage
})
const tabBarRef = ref<HTMLElement | null>(null)
const tabModels = computed<TabModel[]>({
  get: () => workspaceState.openedTabs,
  set: value => workspaceState.reorderTabs(value),
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

function onTabBarWheel(event: WheelEvent) {
  const tabBar = tabBarRef.value
  if (!tabBar) return
  if (tabBar.scrollWidth <= tabBar.clientWidth + 1) return

  const delta = Math.abs(event.deltaX) > Math.abs(event.deltaY) ? event.deltaX : event.deltaY
  if (delta === 0) return

  event.preventDefault()
  tabBar.scrollLeft += delta
}

function openTabContextMenu(index: number, event: MouseEvent) {
  const hasLeft = index > 0
  const hasRight = index < workspaceState.openedTabs.length - 1
  const hasOtherTabs = workspaceState.openedTabs.length > 1
  const hasSavedTabs = workspaceState.openedTabs.some((_, tabIndex) => !workspaceState.getIsTabUnsaved(tabIndex))
  const canRename = workspaceState.canRenameTabItem(index)
  const canCopy = workspaceState.canCopyTabItem(index)
  const canExport = workspaceState.canExportTabItem(index)
  const canImport = workspaceState.canImportTabItem(index)
  const canLocate = workspaceState.canLocateTabInExplorer(index)

  const items: MenuItem[] = [
    {name: workspaceState.openedTabs[index]?.name ?? t("unknown"), disabled: true},
    {name: t("renameItem"), disabled: !canRename, action: () => workspaceState.renameTabItem(index)},
    {name: t("makeItemCopy"), disabled: !canCopy, action: () => workspaceState.copyTabItem(index)},
    {name: t("exportItem"), disabled: !canExport, action: () => workspaceState.exportTabItem(index)},
    {name: t("importReplaceItem"), disabled: !canImport, action: () => workspaceState.importTabItem(index)},
    {name: t("closeCurrentTab"), action: () => workspaceState.closeTab(index)},
    {name: t("closeOtherTabs"), disabled: !hasOtherTabs, action: () => workspaceState.closeOtherTabs(index)},
    {name: t("closeSavedTabs"), disabled: !hasSavedTabs, action: () => workspaceState.closeSavedTabs()},
    {name: t("closeAllTabs"), action: () => workspaceState.closeAllTabs()},
    {name: t("closeTabsToRight"), disabled: !hasRight, action: () => workspaceState.closeTabsToRight(index)},
    {name: t("closeTabsToLeft"), disabled: !hasLeft, action: () => workspaceState.closeTabsToLeft(index)},
    {name: t("locateItemInExplorer"), disabled: !canLocate, action: () => workspaceState.locateTabInExplorer(index)},
  ]

  showMenu({top: event.clientY - 8, left: event.clientX, items})
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
  min-width: 0;
}

#tab_panel_scroller {
  display: flex;
  flex: 1;
  min-width: 0;
  overflow-x: auto;
  overflow-y: hidden;
  scrollbar-width: none;
  -ms-overflow-style: none;
}

#tab_panel_scroller::-webkit-scrollbar {
  display: none;
}

#tab_panel_container {
  display: flex;
  flex-direction: row;
  min-width: 100%;
  width: max-content;
}

#separator {
  height: 1px;
  background-color: var(--ryo-color-outline-varient);
}

.tab_container {
  display: flex;
  flex-direction: column;
  flex: 0 0 auto;

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
