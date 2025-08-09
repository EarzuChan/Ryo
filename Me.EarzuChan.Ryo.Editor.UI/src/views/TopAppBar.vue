<template>
  <div id="top-app-bar">
    <div id="app-logo-container">
      <Icon icon="ryo" id="app-logo"/>
    </div>
    <div id="app-bar">
      <div id="app-bar-menu">
        <TextButton v-for="item in menuBarItems" :padding-vertical="8" :padding-horizontal="8"
                    @mouseenter="hoverMenuButton(item)"
                    @click="clickMenuButton(item)" :id="item.id">{{ item.name }}
        </TextButton>
      </div>
      <div id="app-bar-window-controls">
        <IconButton :size="48" icon="minimize" @click="minimizeWindow"/>
        <IconButton :size="48" :icon="kurisuState.isAppWindowMaximized?'restore':'fullscreen'"
                    @click="switchWindowState"/>
        <IconButton :size="48" icon="close" @click="kurisuState.stopApp()"/>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import IconButton from "@/components/IconButton.vue"
import {useAppStateStore} from "@/stores/AppState"
import Icon from "@/components/Icon.vue"
import TextButton from "@/components/TextButton.vue"
import {useDialogStateStore} from "@/stores/DialogState"
import {showMenu} from "@/utils/MenuUtils"
import {ref} from "vue"
import type {MenuBarItem, MenuItem} from "@/models/UIModels"
import {useKurisuStateStore} from "@/stores/KurisuState"
import {KurisuWindowState} from "@/models/KurisuModels"
import {useWorkspaceStateStore} from "@/stores/WorkspaceState"
import {ensure, openLink, TODO} from "@/utils/UsefulUtils"
import {TabType} from "@/models/AppModels"
import AboutDialog from "@/views/dialogs/AboutDialog.vue"
import {inject} from "vue"
import {useI18n} from "vue-i18n"

const TAG = 'TopAppBar'

const currentMenu = ref<any>(null)
const lastMenu = ref<MenuBarItem | null>(null)

const appState = useAppStateStore()
const dialogState = useDialogStateStore()
const kurisuState = useKurisuStateStore()
const workspaceState = useWorkspaceStateStore()

const appInfo: any = inject('app_info')
const {t} = useI18n()

const menuBarItems: MenuBarItem[] = [
  {id: 'file', name: t('file')},
  {id: 'edit', name: t('edit')},
  {id: 'view', name: t('view')},
  {id: 'help', name: t('help')},
]

function toggleErr() {
  throw new Error('You ordered an error')
}

function clickMenuButton(menuType: MenuBarItem) {
  console.log(TAG, 'clickMenuButton', menuType, currentMenu.value)

  if (currentMenu.value === null) {
    showMenuOf(menuType)
  }
}

function hoverMenuButton(menuType: MenuBarItem) {
  console.log(TAG, 'hoverMenuButton', menuType, currentMenu.value)

  if (currentMenu.value !== null && lastMenu.value!.id !== menuType.id) {
    currentMenu.value.closeMenu(true)

    showMenuOf(menuType)
  }
}

function showMenuOf(menuType: MenuBarItem) {
  lastMenu.value = menuType

  const pageNotOk = workspaceState.activeTabExposed === null

  switch (menuType.id) {
    case 'file':
      const items: MenuItem[] = [
        {name: t('new'), action: () => workspaceState.newVolume()},
        {name: t('open'), action: () => workspaceState.openVolume()}]
      if (ensure(workspaceState.activeVolume)) {
        items.push({
              name: t('saveFile', {file: workspaceState.activeVolume!}),
              action: () => workspaceState.saveVolume(workspaceState.activeVolume!)
            }, {
              name: t('closeFile', {file: workspaceState.activeVolume!}),
              action: () => workspaceState.closeVolume(workspaceState.activeVolume!)
            },
            {
              name: t('saveFileAs', {file: workspaceState.activeVolume!}),
              action: () => workspaceState.saveVolumeAs(workspaceState.activeVolume!)
            })
      }
      items.push({name: t('saveAll'), disabled: true, action: () => console.log(TAG,'全部保存')},
          {name: t('closeAll'), disabled: true, action: () => console.log(TAG,'全部关闭')},
          {name: t('addItem'), disabled: true, action: () => console.log(TAG,'添加资源')},
          {name: t('exportCurrentItem'), disabled: true, action: () => console.log(TAG,'导出当前资源')},
          {name: t('importCurrentItem'), disabled: true, action: () => console.log(TAG,'导入当前资源')}, {
            name: t('recentFiles'), disabled: true, children:
                [
                  {name: '文件1', action: () => console.log(TAG,'文件1')},
                  {name: '文件2', action: () => console.log(TAG,'文件2')},
                  {name: '文件3', action: () => console.log(TAG,'文件3')},
                ]
          },
          {name: t('restartApp'), disabled: true, action: () => console.log(TAG,'重启软件')},
          {
            name: t('exit'), action: () => {
              dialogState.order({
                icon: 'ryo',
                headline: '退出Ryo',
                description: '您确定要退出Ryo吗？',
                actions: [
                  {
                    text: t('cancel')
                  },
                  {
                    text: t('exit'),
                    onClick: () => kurisuState.stopApp()
                  }
                ]
              })
            }
          })
      currentMenu.value = showMenu({
        items, attachToId: menuType.id, onClose() {
          currentMenu.value = null
        },
      })
      break
    case 'edit':
      currentMenu.value = showMenu({
        items: [
          {name: t('undo'), disabled: pageNotOk, action: () => workspaceState.pageUndo()},
          {name: t('redo'), disabled: pageNotOk, action: () => workspaceState.pageRedo()},
          {name: t('reloadEditor'), disabled: pageNotOk, action: () => workspaceState.pageReload()},
          {name: t('discardUnsavedChanges'), disabled: pageNotOk, action: () => workspaceState.pageDiscard()},
          {name: t('saveCurrentTab'), disabled: pageNotOk, action: () => workspaceState.pageSave()},
          {
            name: t('closeCurrentTab'),
            disabled: workspaceState.activeTabIndex === -1,
            action: () => workspaceState.closeTab(workspaceState.activeTabIndex)
          },
          {name: t('searchInCurrentTab'), disabled: true, action: () => TODO(TAG, '在标签页中查找')},
          {name: t('searchInAllFiles'), disabled: true, action: () => TODO(TAG, '在所有文件中查找')},
          {name: t('searchInExplorer'), disabled: true, action: () => TODO(TAG, '在资源管理器中查找')},
        ], attachToId: menuType.id, onClose() {
          currentMenu.value = null
        },
      })
      break
    case 'view':
      currentMenu.value = showMenu({
        items: [
          {
            name: (appState.sidePanelExpanded ? t('narrow') : t('expand')) + t('sidePanel'),
            action: () => appState.sidePanelExpanded = !appState.sidePanelExpanded
          }, {
            name: t('toolWindow'), disabled: true, children:
                [{name: 'TexturePacker', action: () => console.log(TAG,'TexturePacker')},]
          },
          {name: t('saveAllTabs'), disabled: true, action: () => console.log(TAG,'保存全部标签页')},
          {name: t('closeAllTabs'), disabled: true, action: () => console.log(TAG,'关闭全部标签页')},
          {name: t('preferences'), disabled: true, action: () => console.log(TAG,'偏好设置')},
        ], attachToId: menuType.id, onClose() {
          currentMenu.value = null
        },
      })
      break
    case 'help':
      currentMenu.value = showMenu({
        items: [
          {
            name: "进入测试模式",
            action: () => appState.preferTesting = true
          },
          {name: t('showWelcomePage'), action: () => workspaceState.openTab(TabType.Welcome)}, {
            name: t('resources'), children:
                [
                  {name: t('quickStart'), action: () => console.log(TAG,'快速上手')}, // TODO
                  {name: t('deepGuidance'), action: () => console.log(TAG,'深度指南')},
                  {name: t('useRyoLibrary'), action: () => console.log(TAG,'使用Ryo库')},
                  {name: t('ryoRepository'), action: () => openLink(appInfo.repoLink)},
                  {name: t('authorLink'), action: () => openLink(appInfo.authorLink)}
                ]
          },
          {name: t('advicesAndFeedback'), action: () => openLink(appInfo.issue)},
          {name: t('aboutRyo'), action: () => dialogState.orderSpecial(AboutDialog)},
        ], attachToId: menuType.id, onClose() {
          currentMenu.value = null
        },
      })
      break
  }
}

function testDialog() {
  dialogState.order({
    icon: 'ryo',
    headline: '测试对话框',
    description: '这是一个测试对话框。\n人类有三大欲望，食欲，性欲，睡眠欲，而在这三大欲望当中，因为食欲是满足人类生存需求的欲望，所以，满足食欲的行为，在这三者中，优先性是第一位的。如果能在进食的过程中，吃下了美味的食物，也能使人类无比愉快，而在现实生活中，存在着对于这种快感执着追求的人，我们通常把这种人称之为美食家，而本餐厅，则专门为那些厌倦世间常见美食的人，量体裁衣，提供符合他们身份的美食。',
    actions: [
      {
        text: '取消',
        onClick: () => {
          console.log(TAG,'点击了取消')
        }
      },
      {
        text: '确定',
        onClick: () => {
          console.log(TAG,'点击了确定')
          return false
        }
      },
      {
        text: '再来一个',
        onClick: () => {
          dialogState.order({
            icon: 'ryo',
            headline: '测试对话框3',
            description: '这是另一个测试对话框。\n鸭蛋么鸭蛋',
            actions: [
              {
                text: '取消',
                onClick: () => {
                  console.log(TAG,'点击了取消')
                }
              },
              {
                text: '确定',
                onClick: () => {
                  console.log(TAG,'点击了确定')
                }
              }
            ]
          })
          return false
        }
      }
    ]
  })
  dialogState.order({
    icon: 'ryo',
    headline: '测试对话框2',
    description: '这是另一个测试对话框。\n非常的新鲜，非常的美味',
    actions: [
      {
        text: '取消',
        onClick: () => {
          console.log(TAG,'点击了取消')
        }
      },
      {
        text: '确定',
        onClick: () => {
          console.log(TAG,'点击了确定')
        }
      }
    ]
  })
  dialogState.order({
    icon: 'ryo',
    headline: '最后的吻别',
    description: '最后の警告Desu',
    actions: [
      {
        text: '取消',
        onClick: () => {
          console.log(TAG,'点击了取消')
        }
      },
      {
        text: '确定',
        onClick: () => {
          console.log(TAG,'点击了确定')
        }
      }
    ]
  })
}

function minimizeWindow() {
  kurisuState.setAppWindowState(KurisuWindowState.Minimized)
}

function switchWindowState() {
  kurisuState.setAppWindowState(kurisuState.isAppWindowMaximized ? KurisuWindowState.Normal : KurisuWindowState.Maximized)
}
</script>

<style scoped>
#top-app-bar {
  display: flex;
  height: 56px;
  align-items: center;

  app-region: drag;
}

#app-logo {
  background-color: var(--ryo-color-on-surface-variant);
}

#app-logo-container {
  width: 64px;
  display: flex;
  flex-direction: column;
  align-items: center;
}

#app-bar {
  flex: 1;
  display: flex;
  padding: 0 4px;

  align-items: center;
}

.text-button.disabled > .text {
  color: var(--ryo-color-primary);
}

#app-bar-menu {
  flex: 1;
  display: flex;

  gap: 4px;
  height: 36px;
}

#app-bar-window-controls {
  display: flex;
}
</style>