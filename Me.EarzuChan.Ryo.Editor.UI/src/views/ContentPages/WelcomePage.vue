<template>
  <div id="welcome-page">
    <div class="text-group">
      <div class="ryo-typography-display-large white">Ryo</div>
      <div class="ryo-typography-headline-large on-surface">觉醒编集の力</div>
      <div class="ryo-typography-headline-large on-surface">铸造次世代の伝说剧情</div>
    </div>
    <div id="task-group">
      <div class="text-group">
        <div class="ryo-typography-title-large white">启动</div>
        <div class="ryo-typography-body-large primary">新建文件</div>
        <div class="ryo-typography-body-large primary">打开文件</div>
      </div>
      <div class="text-group">
        <div class="ryo-typography-title-large white">最近</div>
        <div v-for="file in recentFiles" class="horizontal-group">
          <div class="ryo-typography-body-large primary">{{ file.name }}</div>
          <div class="ryo-typography-body-large on-surface-variant">{{ file.path }}</div>
        </div>
      </div>
      <div class="text-group">
        <div class="ryo-typography-title-large white">资源</div>
        <div v-for="resource in resources" class="horizontal-group" @click="goTo(resource.link)">
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
import type {WelcomePageResource} from "@/models/UIModels"
import {emitWebEvent, makeWebLetter} from "@/utils/KurisuUtils"

const TAG = 'WelcomePage'

const recentFiles = ref<RecentFile[]>([{name: 'test', path: 'man'}])
const resources = ref<WelcomePageResource[]>(
    [
      {title: '快速上手', description: 'RyoDocs', link: 'https://www.earzuchan.me/'},
      {title: '深度指南', description: 'RyoDocs', link: 'https://www.earzuchan.me/'},
      {title: 'Ryo存储库', description: 'Github', link: 'https://www.github.com/EarzuChan/Ryo'},
      {title: '使用Ryo库', description: 'RyoDocs', link: 'https://www.earzuchan.me/'},
    ])

function goTo(link: string) {
  emitWebEvent(makeWebLetter('OpenLink', link))
}
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

#task-group {
  display: flex;
  flex-direction: row;
  flex-wrap: wrap;
  gap: 24px;
}
</style>