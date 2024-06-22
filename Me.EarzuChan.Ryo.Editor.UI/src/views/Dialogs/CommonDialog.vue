<!-- Dialog.vue -->
<template>
  <DialogBase @overlay-click="clickOverlay" :ctrl-show="ctrlShow" @opened="opened" @closed="closed">
    <div id="dialog-container">
      <div id="dialog-contents">
        <Icon class="ryo-align-center" id="icon" v-if="icon" :icon="icon"/>
        <div :class="{'ryo-align-center':icon}" id="headline" class="ryo-typography-headline-small" v-if="headline">
          {{ headline }}
        </div>
        <div id="description" class="ryo-typography-body-medium" v-if="description">{{ description }}</div>
      </div>
      <slot/>
      <div v-if="hasActions" id="dialog-actions">
        <TextButton v-for="(buttonModel, index) in actions" :key="index"
                    @click="handleActionsClick(buttonModel.onClick)">
          {{ buttonModel.text }}
        </TextButton>
      </div>
      <div v-else id="dialog-padding-bottom"/>
    </div>
  </DialogBase>
</template>

<script setup lang="ts">
import {computed, defineProps, onMounted, type PropType, ref} from 'vue'
import type {DialogActionButtonModel} from '@/models/Models'
import DialogBase from "@/views/DialogBase.vue"
import Icon from "@/components/Icon.vue"
import TextButton from "@/components/TextButton.vue"

const hasActions = computed(() => props.actions && props.actions.length > 0)

const ctrlShow = ref(false)

const props = defineProps({
  icon: String,
  headline: String,
  description: String,
  closeOnOverlayClick: {
    type: Boolean,
    default: false
  },
  actions: Array as PropType<DialogActionButtonModel[]>,
  afterClosed: Function
})

const emit = defineEmits(['open', 'opened', 'close', 'closed'])

function handleActionsClick(onClick?: Function) {
  if (!onClick || onClick() !== false) {
    closeDialog()
  }
}

function clickOverlay() {
  if (!hasActions.value || props.closeOnOverlayClick) {
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
  /*if (props.afterClosed) {
    props.afterClosed()
  }*/
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
  min-width: 280px;
  max-width: 560px;
}

#icon {
  --ryo-color-primary: var(--ryo-color-secondary);
}

#description {
  color: var(--ryo-color-on-surface-variant);
  white-space: pre-wrap;
}

#dialog-contents {
  gap: 16px;
  display: flex;
  flex-direction: column;
  padding: 24px 24px 0;
}

#headline {
  color: var(--ryo-color-on-surface);
}

#dialog-actions {
  display: flex;
  flex-direction: row;
  justify-content: flex-end;
  align-items: center;
  padding: 24px 24px 24px 0px;
  gap: 8px;
}

#dialog-padding-bottom {
  height: 24px;
}
</style>