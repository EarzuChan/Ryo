<template>
  <div id="ryo-app" class="ryo-dark">
    <div id="error-box">
      <div id="contents">
        <div id="contents-scroll-wrapper">
          <div id="contents-container">
            <div class="ryo-typography-headline-large info-text">不是，哥们：应用崩溃了</div>
            <div id="details-container">
              <div class="ryo-typography-body-medium info-text">错误详情：</div>
              <CodeBlob>{{ getUpToNLines(errText, 3) }}</CodeBlob>
              <div class="ryo-typography-body-medium info-text">应用版本：</div>
              <CodeBlob>{{ appInfo.name + ' v' + appInfo.version }}</CodeBlob>
              <div class="ryo-typography-body-medium info-text">建议的操作：</div>
              <CodeBlob>重新加载试试看？</CodeBlob>
              <div class="ryo-typography-body-medium info-text">哥们可在控制台获得更多信息</div>
            </div>
          </div>
        </div>
      </div>
      <div id="actions">
        <TextButton @click="repoErr">报告错误</TextButton>
        <TextButton @click="copyErr">复制错误信息</TextButton>
        <TextButton button-style="filled" @click="reloadApp">重新加载应用程序</TextButton>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import TextButton from "@/components/TextButton.vue"
import CodeBlob from "@/components/CodeBlob.vue"
import {inject} from "@vue/runtime-core"
import {copyTextToClipboard, getUpToNLines} from "@/utils/UsefulUtils"
import {useDialogStateStore} from "@/stores/DialogState"

const dialogState = useDialogStateStore()
const err: any = inject('err')
const errText = `${err.stack}`
const appInfo: any = inject('app_info')

function repoErr() {
  dialogState.dialog({
    icon: "close",
    headline: "这这不能",
    description: "抱歉，此功能尚未实现。",
    actions: [
      {
        text: "唉"
      }
    ]
  })
}

function copyErr() {
  copyTextToClipboard(errText);
}

function reloadApp() {
  location.reload()
}
</script>

<style scoped>
#ryo-app {
  display: flex;
  flex-direction: column;
  height: 100vh;

  background-color: var(--ryo-color-surface);
}

#error-box {
  margin: 64px;
  flex-direction: column;
  flex: 1;
  display: flex;
  overflow: hidden;

  border-radius: 28px;

  background-color: var(--ryo-color-surface-container);
}

.info-text {
  color: var(--ryo-color-on-background);
}

#contents-scroll-wrapper {
  overflow: auto;
}

#contents-container {
  display: flex;
  flex-direction: column;

  padding: 0 24px;
  gap: 24px;
}

#details-container {
  display: flex;
  flex-direction: column;

  gap: 4px;
}

#contents {
  overflow: hidden;
  flex: 1;
  display: flex;

  flex-direction: column;
  justify-content: center;
}

#actions {
  border-top: 1px solid var(--ryo-color-outline-varient);

  background-color: var(--ryo-color-surface-container-high);

  display: flex;
  padding: 24px;
  justify-content: end;
  gap: 8px;
}
</style>