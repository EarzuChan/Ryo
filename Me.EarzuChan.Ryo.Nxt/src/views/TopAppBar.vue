<template>
  <div id="top-app-bar">
    <div id="app-logo-container">
      <Icon icon="ryo" id="app-logo"/>
    </div>
    <div id="app-bar">
      <div id="app-bar-menu">
        <TextButton :padding-vertical="8" :padding-horizontal="8" @click="testDialog">文件
        </TextButton>
        <TextButton :padding-vertical="8" :padding-horizontal="8">编辑</TextButton>
        <TextButton :padding-vertical="8" :padding-horizontal="8">视图</TextButton>
        <TextButton :padding-vertical="8" :padding-horizontal="8" @click="toggleErr">帮助
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
import {WinWebAppWindowState} from "@/models/Models"
import Icon from "@/components/Icon.vue"
import TextButton from "@/components/TextButton.vue"
import {useDialogStateStore} from "@/stores/DialogState";


const appState = useAppStateStore()
const dialogState = useDialogStateStore()

function toggleErr() {
  throw new Error('You ordered an error')
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