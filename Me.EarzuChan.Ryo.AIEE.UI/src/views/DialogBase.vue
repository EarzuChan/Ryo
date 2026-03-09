<template>
  <Teleport to="#ryo-viewport">
    <div id="dialog">
      <Transition name="overlay" @after-enter="afterEnter" @after-leave="afterLeave">
        <div id="dialog-overlay" @click="clickOverlay" v-if="showOverlay" v-show="ctrlShow"/>
      </Transition>
      <Transition name="dialog">
        <div id="dialog-base" v-show="ctrlShow">
          <slot/>
        </div>
      </Transition>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
defineProps({
  showOverlay: {
    type: Boolean,
    default: true
  },
  ctrlShow: {
    type: Boolean,
    default: false
  }
})

const emit = defineEmits(['overlayClick', 'opened', 'closed'])

function clickOverlay() {
  emit('overlayClick')
}

function afterEnter() {
  emit('opened')
}

function afterLeave() {
  emit('closed')
}
</script>

<style scoped>
#dialog {
  position: fixed;
  align-items: center;
  justify-content: center;
  display: flex;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
}

#dialog-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;

  /*根视图有圆角时，这个没被裁切，何也*/
  background-color: rgba(var(--ryo-color-scrim), var(--ryo-opacity-040));
}

#dialog-base {
  display: flex;
  overflow: hidden;

  border-radius: 28px;
  background-color: var(--ryo-color-surface-container-high);
  box-shadow: var(--ryo-elevation-3);

  position: relative;

  margin: 128px;
}

.overlay-enter-active {
  transition: opacity 250ms cubic-bezier(0.05, 0.7, 0.1, 1);
}

.overlay-leave-active {
  transition: opacity 200ms cubic-bezier(0.3, 0, 0.8, 0.15);
}

.dialog-enter-active {
  transition:
      transform 250ms cubic-bezier(0.05, 0.7, 0.1, 1),
      opacity 250ms cubic-bezier(0.05, 0.7, 0.1, 1);
}

.dialog-leave-active {
  transition:
      transform 200ms cubic-bezier(0.3, 0, 0.8, 0.15),
      opacity 200ms cubic-bezier(0.3, 0, 0.8, 0.15);
}

.overlay-enter-from,
.overlay-leave-to {
  opacity: 0;
}

.dialog-enter-from,
.dialog-leave-to {
  transform: translateY(8px) scale(0.98);
  opacity: 0;
}

.dialog-enter-to,
.dialog-leave-from {
  transform: translateY(0) scale(1);
  opacity: 1;
}
</style>
