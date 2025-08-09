<template>
  <div id="ryo-viewport" class="ryo-dark">
    <div id="error-box">
      <div id="contents">
        <div id="contents-scroll-wrapper">
          <div id="contents-container">
            <div class="ryo-typography-headline-large info-text">{{ t('appCrashed') }}</div>
            <div id="details-container">
              <div class="ryo-typography-body-medium info-text">{{ t('errorDetails') }}</div>
              <CodeBlock>{{ getUpToNLines(errText, 3) }}</CodeBlock>
              <div class="ryo-typography-body-medium info-text"> {{ t('appVersion') }}</div>
              <CodeBlock>{{ appInfo.name + ' v' + appInfo.version }}</CodeBlock>
              <div class="ryo-typography-body-medium info-text">{{ t('suggestedAction') }}</div>
              <CodeBlock>{{ t('reloadSuggestion') }}</CodeBlock>
              <div class="ryo-typography-body-medium info-text">{{ t('moreInfoConsole') }}</div>
            </div>
          </div>
        </div>
      </div>
      <div id="actions">
        <TextButton @click="repoErr">{{ t('reportError') }}</TextButton>
        <TextButton @click="copyErr">{{ t('copyError') }}</TextButton>
        <TextButton button-style="filled" @click="reloadApp">{{ t('reloadApp') }}</TextButton>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import TextButton from "@/components/TextButton.vue"
import {inject} from "vue"
import {copyTextToClipboard, getUpToNLines} from "@/utils/UsefulUtils"
import {useDialogStateStore} from "@/stores/DialogState"
import CodeBlock from "@/components/CodeBlock.vue"
import {useI18n} from "vue-i18n"

const dialogState = useDialogStateStore()
const err: any = inject('err')
const errText = `${err.stack}`
const appInfo: any = inject('app_info')
const {t} = useI18n()

function repoErr() {
  dialogState.order({
    icon: "close",
    headline: t('featureNotImplemented'),
    description: t('featureNotImplementedDescription'),
    actions: [
      {
        text: t('sigh')
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
#ryo-viewport {
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