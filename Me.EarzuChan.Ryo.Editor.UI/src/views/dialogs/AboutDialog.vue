<template>
  <DialogBase @overlay-click="clickOverlay" :ctrl-show="ctrlShow" @opened="opened" @closed="closed">
    <div id="dialog-container">
      <div id="row1">
        <div id="limited-box">
          <img src="/assets/images/illustration_colored_icon.svg">
        </div>
        <div class="column">
          <div class="ryo-typography-headline-small primary">{{ appInfo.name }}</div>
          <div class="ryo-typography-title-medium surface">by {{ appInfo.author }}<br>{{ appInfo.version }}</div>
        </div>
      </div>
      <div id="row2">
        <div class="ryo-typography-title-small surface-variant">{{ $t('哈哈，你想') }}
          <div class="primary inline" @click="openLink(appInfo.repoLink)"> {{ $t('support') }}</div>
          {{ $t('吗') }}<br>{{ $t('怎么，你不') }}
          <div class="primary inline" @click="openLink(appInfo.authorLink)"> {{ $t('follow') }}</div>
          {{ $t('吗') }}
        </div>
      </div>
    </div>
  </DialogBase>
</template>

<script setup lang="ts">
import {onMounted, ref} from 'vue'
import DialogBase from "@/views/DialogBase.vue"
import {inject} from "vue"
import {openLink} from "@/utils/UsefulUtils"

const appInfo: any = inject('app_info')

// TODO：把`怎么你不 关注 吗`，换成`速速加入 我们的Discord`：https://discord.gg/HP66Jg3Uz

const props = defineProps({
  closeOnOverlayClick: {
    type: Boolean,
    default: true
  }
})

const ctrlShow = ref(false)

const emit = defineEmits(['open', 'opened', 'close', 'closed'])

function clickOverlay() {
  if (props.closeOnOverlayClick) {
    closeDialog()
  }
}

onMounted(() => {
  emit('open')
  ctrlShow.value = true
})

function opened() {
  emit('opened')
}

function closed() {
  emit('closed')
}

function closeDialog() {
  emit('close')
  ctrlShow.value = false
}
</script>

<style scoped>
#dialog-container {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 24px;
  min-width: 232px;
}

#row1 {
  display: flex;
  flex-direction: row;
  gap: 16px;
  align-items: center;
}

#row2 {
  display: flex;
  flex-direction: column;
  padding-left: 64px;
}

.column {
  display: flex;
  flex-direction: column;
}

.primary {
  color: var(--ryo-color-primary);
}

.surface {
  color: var(--ryo-color-on-surface);
}

.surface-variant {
  color: var(--ryo-color-on-surface-variant);
}

#limited-box {
  width: 48px;
  height: 48px;
  align-items: center;
  justify-content: center;
  display: flex;
}

.inline {
  display: inline;
  cursor: pointer;
}
</style>