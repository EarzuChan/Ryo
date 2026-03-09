<template>
  <div :style="{padding:`12px ${horiPadding}px`}" class="preference-item" :class="{'clickable': clickable,'rounded': rounded}" @click="onClick">
    <Icon v-if="iconKey" :icon="iconKey"/>
    <div class="content">
      <div class="ryo-typography-body-large title">{{ title }}</div>
      <div v-if="desc" class="ryo-typography-body-medium desc">{{ desc }}</div>
    </div>
    <div v-if="$slots.tail" class="tail ryo-typography-body-large">
      <slot name="tail"/>
    </div>
  </div>
</template>

<script setup lang="ts">
import Icon from "@/components/Icon.vue"

const props = defineProps({
  iconKey: String,
  title: {
    type: String,
    required: true
  },
  desc: String,
  clickable: Boolean,
  rounded: {
    type: Boolean,
    default: true
  },
  horiPadding: {
    type: Number,
    default: 16
  }
})

const emit = defineEmits(["click"])

function onClick() {
  if (props.clickable) emit("click")
}
</script>

<style scoped>
.preference-item {
  display: flex;
  align-items: center;
  gap: 16px;
}

.rounded{
  border-radius: 16px;
}

.preference-item :deep(.icon) {
  background-color: var(--ryo-color-on-surface-variant);
}

.content {
  display: flex;
  flex-direction: column;
  justify-content: center;
  min-width: 0;
  flex: 1;
}

.title {
  color: var(--ryo-color-on-surface);
}

.desc {
  color: var(--ryo-color-on-surface-variant);
}

.tail {
  color: var(--ryo-color-on-surface-variant);
  white-space: nowrap;
  text-align: right;
}

.clickable {
  cursor: pointer;
}

.clickable:hover {
  background-color: rgba(var(--ryo-color-state-layers-on-surface-variant), var(--ryo-opacity-state-layers-008));
}

.clickable:active {
  background-color: rgba(var(--ryo-color-state-layers-on-surface-variant), var(--ryo-opacity-state-layers-012));
}
</style>
