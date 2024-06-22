<template>
  <div class="text-button"
       :class="[buttonStyle, {'disabled' : disabled}]"
       ref="button"
       :style="'padding: '+paddingVertical+'px '+paddingHorizontal+'px'"
       tabindex="0">
    <div class="text ryo-typography-label-large" :style="'font-size: '+textSize+'px'">
      <slot/>
    </div>
  </div>
</template>

<script lang="ts" setup>
import {computed} from "vue";

const props = defineProps({
  disabled: {
    type: Boolean,
    default: false,
  },
  // 其他样式
  buttonStyle: {
    type: String,
    default: 'standard',
  },
  textSize: {
    type: Number,
    default: 14,
  }
})

const paddingVertical = computed(() => {
  switch (props.buttonStyle) {
    default:
      return 10
  }
})

const paddingHorizontal = computed(() => {
  switch (props.buttonStyle) {
    case 'filled':
      return 24
    default:
      return 12
  }
})
</script>

<style scoped>
.text-button {
  border-radius: 100px;
  display: flex;
  position: relative;

  justify-content: center;
  align-items: center;
  cursor: pointer;

  overflow: hidden;

  app-region: no-drag;
}

.text-button.disabled {
  cursor: not-allowed;
}

.text-button::after {
  content: "";
  position: absolute;

  transition: all var(--ryo-motion-standard);

  top: 0;
  bottom: 0;
  right: 0;
  left: 0;
}

.text-button.disabled > .text {
  opacity: 0.38;
  color: var(--ryo-color-on-surface);
}

/*基本样式*/

.text-button.standard:not(.disabled):hover::after {
  background-color: rgba(var(--ryo-color-state-layers-primary), var(--ryo-opacity-state-layers-008));
}

.text-button.standard:not(.disabled):active::after {
  background-color: rgba(var(--ryo-color-state-layers-primary), var(--ryo-opacity-state-layers-012));
}

.text-button.standard:not(.disabled) > .text {
  color: var(--ryo-color-primary)
}

/*填充样式*/

.text-button.filled:not(.disabled) {
  background-color: var(--ryo-color-primary);
}

.text-button.filled.disabled::after {
  background-color: rgba(var(--ryo-color-state-layers-on-surface), var(--ryo-opacity-state-layers-012));
}

.text-button.filled:not(.disabled):hover::after {
  background-color: rgba(var(--ryo-color-state-layers-on-primary), var(--ryo-opacity-state-layers-008));
}

.text-button.filled:not(.disabled):active::after {
  background-color: rgba(var(--ryo-color-state-layers-on-primary), var(--ryo-opacity-state-layers-012));
}

.text-button.filled:not(.disabled) > .text {
  color: var(--ryo-color-on-primary)
}
</style>
