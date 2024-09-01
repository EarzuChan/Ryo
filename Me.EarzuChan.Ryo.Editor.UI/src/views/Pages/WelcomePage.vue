<template>
  <div id="welcome-page">
    <div class="text-group">
      <div class="ryo-typography-display-large white">{{ appInfo.name }}</div>
      <div class="ryo-typography-headline-large on-surface">{{ $t('觉醒编集の力') }}</div>
      <div class="ryo-typography-headline-large on-surface">{{ $t('铸造次世代の伝说剧情') }}</div>
    </div>
    <div id="task-group">
      <div class="text-group">
        <div class="ryo-typography-title-large white">{{ $t('launch') }}</div>
        <div class="ryo-typography-body-large primary action">{{ $t('newFile') }}</div>
        <div class="ryo-typography-body-large primary action">{{ $t('openFile') }}</div>
      </div>
      <div class="text-group">
        <div class="ryo-typography-title-large white">{{ $t('recent') }}</div>
        <div v-for="file in recentFiles" class="horizontal-group">
          <div class="ryo-typography-body-large primary">{{ file.name }}</div>
          <div class="ryo-typography-body-large on-surface-variant">{{ file.path }}</div>
        </div>
      </div>
      <div class="text-group">
        <div class="ryo-typography-title-large white">{{ $t('resources') }}</div>
        <div v-for="resource in resources" class="horizontal-group" @click="openLink(resource.link)">
          <div class="ryo-typography-body-large primary">{{ resource.title }}</div>
          <div class="ryo-typography-body-large on-surface-variant">{{ resource.description }}</div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import {ref} from "vue"
import type {RecentFile} from "@/models/AppModels"
import type {ResLinkModel} from "@/models/AppModels"
import {openLink} from "@/utils/UsefulUtils"
import {inject} from "@vue/runtime-core";
import {useI18n} from "vue-i18n";

const TAG = 'WelcomePage'

const appInfo: any = inject('app_info')
const {t} = useI18n()

const recentFiles = ref<RecentFile[]>([{name: 'test', path: 'man'}])
const resources: ResLinkModel[] = [
  {title: t('quickStart'), description: 'RyoDocs', link: 'https://www.earzuchan.me/'},
  {title: t('deepGuidance'), description: 'RyoDocs', link: 'https://www.earzuchan.me/'},
  {title: t('useRyoLibrary'), description: 'RyoDocs', link: 'https://www.earzuchan.me/'},
  {title: t('ryoRepository'), description: 'Github', link: appInfo.repoLink},
  {title: t('advicesAndFeedback'), description: 'Github', link: appInfo.issueLink},
  {title: t('authorLink'), description: 'Github', link: appInfo.authorLink},
]
</script>

<style scoped>
#welcome-page {
  display: flex;
  flex-direction: column;
  gap: 24px;

  min-height: 100%;
}

.white {
  color: white;
}

.on-surface {
  color: var(--ryo-color-on-surface);
}

.primary {
  color: var(--ryo-color-primary);
}

.on-surface-variant {
  color: var(--ryo-color-on-surface-variant);
}

.text-group {
  display: flex;
  min-width: max(50% - 12px, 300px);
  flex-direction: column;
  gap: 8px;
}

.horizontal-group {
  display: flex;
  flex-direction: row;
  gap: 8px;
  cursor: pointer;
}

.action {
  cursor: pointer;
}

#task-group {
  display: flex;
  flex-direction: row;
  flex-wrap: wrap;
  gap: 24px;
}
</style>