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
        <IconButton :size="48" :icon="appState.isAppWindowMaximized?'restore':'fullscreen'" @click="switchWindowState"/>
        <IconButton :size="48" icon="close" @click="appState.stopApp()"/>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import IconButton from "@/components/IconButton.vue"
import {useAppStateStore} from "@/stores/AppState"
import {AttachMethod, type MenuBarItem, WinWebAppWindowState} from "@/models/Models"
import Icon from "@/components/Icon.vue"
import TextButton from "@/components/TextButton.vue"
import {useDialogStateStore} from "@/stores/DialogState"
import {menu} from "@/utils/MenuUtils"
import {reactive, ref} from "vue";
import {delayExecution} from "@/utils/UsefulUtils"

const TAG = 'TopAppBar'

const currentMenu = ref<any>(null)
const lastMenu = ref<MenuBarItem | null>(null)

const appState = useAppStateStore()
const dialogState = useDialogStateStore()

const menuBarItems: MenuBarItem[] = [
  {id: 'file', name: '文件'},
  {id: 'edit', name: '编辑'},
  {id: 'view', name: '视图'},
  {id: 'help', name: '帮助'}
]

function toggleErr() {
  throw new Error('You ordered an error')
}

function clickMenuButton(menuType: MenuBarItem) {
  console.log(TAG, 'clickMenuButton', menuType)

  if (currentMenu.value === null) {
    showMenu(menuType)
  }
}

function hoverMenuButton(menuType: MenuBarItem) {
  console.log(TAG, 'hoverMenuButton', menuType, currentMenu.value)
  
  if (currentMenu.value !== null && lastMenu.value!.id !== menuType.id) {
    currentMenu.value.closeMenu()

    showMenu(menuType)
  }
}

function showMenu(menuType: MenuBarItem) {
  lastMenu.value = menuType

  switch (menuType.id) {
    case 'file':
      currentMenu.value = menu({
        items: [
          {name: '新建', action: () => console.log('新建')},
          {name: '打开', action: () => console.log('打开')},
          {name: '保存', action: () => console.log('保存')},
          {name: '另存为', action: () => console.log('另存为')},
          {name: '关闭', action: () => console.log('关闭')},
          {name: '全部保存', action: () => console.log('全部保存')},
          {name: '全部关闭', action: () => console.log('全部关闭')},
          {name: '添加资源', action: () => console.log('添加资源')},
          {name: '导出当前资源', action: () => console.log('导出当前资源')},
          {name: '导入当前资源', action: () => console.log('导入当前资源')},
          {
            name: '最近打开', children:
                [
                  {name: '文件1', action: () => console.log('文件1')},
                  {name: '文件2', action: () => console.log('文件2')},
                  {name: '文件3', action: () => console.log('文件3')},
                ]
          },
          {name: '重启软件', action: () => console.log('重启软件')},
          {name: '退出', action: () => appState.stopApp()}
        ], attachToId: menuType.id, onClose() {
          currentMenu.value = null
        },
      })
      break
    case 'edit':
      currentMenu.value = menu({
        items: [
          {name: '撤销', action: () => console.log('撤销')}
        ], attachToId: menuType.id, onClose() {
          currentMenu.value = null
        },
      })
      break
    case 'view':
      currentMenu.value = menu({
        items: [
          {name: '侧边栏收起', action: () => console.log('侧边栏收起')}
        ], attachToId: menuType.id, onClose() {
          currentMenu.value = null
        },
      })
      break
    case 'help':
      currentMenu.value = menu({
        items: [
          {name: '显示软件文档', action: () => console.log('显示软件文档')}
        ], attachToId: menuType.id, onClose() {
          currentMenu.value = null
        },
      })
      break
  }
}

function testDialog() {
  dialogState.dialog({
    icon: 'ryo',
    headline: '测试对话框',
    description: '这是一个测试对话框。\n人类有三大欲望，食欲，性欲，睡眠欲，而在这三大欲望当中，因为食欲是满足人类生存需求的欲望，所以，满足食欲的行为，在这三者中，优先性是第一位的。如果能在进食的过程中，吃下了美味的食物，也能使人类无比愉快，而在现实生活中，存在着对于这种快感执着追求的人，我们通常把这种人称之为美食家，而本餐厅，则专门为那些厌倦世间常见美食的人，量体裁衣，提供符合他们身份的美食。',
    actions: [
      {
        text: '取消',
        onClick: () => {
          console.log('点击了取消')
        }
      },
      {
        text: '确定',
        onClick: () => {
          console.log('点击了确定')
          return false
        }
      },
      {
        text: '再来一个',
        onClick: () => {
          dialogState.dialog({
            icon: 'ryo',
            headline: '测试对话框3',
            description: '这是另一个测试对话框。\n鸭蛋么鸭蛋',
            actions: [
              {
                text: '取消',
                onClick: () => {
                  console.log('点击了取消')
                }
              },
              {
                text: '确定',
                onClick: () => {
                  console.log('点击了确定')
                }
              }
            ]
          })
          return false
        }
      }
    ]
  })
  dialogState.dialog({
    icon: 'ryo',
    headline: '测试对话框2',
    description: '这是另一个测试对话框。\n非常的新鲜，非常的美味',
    actions: [
      {
        text: '取消',
        onClick: () => {
          console.log('点击了取消')
        }
      },
      {
        text: '确定',
        onClick: () => {
          console.log('点击了确定')
        }
      }
    ]
  })
  dialogState.dialog({
    icon: 'ryo',
    headline: '最后的吻别',
    description: '最后の警告Desu',
    actions: [
      {
        text: '取消',
        onClick: () => {
          console.log('点击了取消')
        }
      },
      {
        text: '确定',
        onClick: () => {
          console.log('点击了确定')
        }
      }
    ]
  })
}

function minimizeWindow() {
  appState.setAppWindowState(WinWebAppWindowState.Minimized)
}

function switchWindowState() {
  appState.setAppWindowState(appState.isAppWindowMaximized ? WinWebAppWindowState.Normal : WinWebAppWindowState.Maximized)
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