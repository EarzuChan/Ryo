<template>
  <Teleport to="#ryo-app">
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

.overlay-enter-active, .dialog-enter-active {
  transition: var(--ryo-motion-emphasized-decelerate);
}

.overlay-leave-active, .dialog-leave-active {
  transition: var(--ryo-motion-emphasized-accelerate);
}

.overlay-enter-from,
.overlay-leave-to {
  opacity: 0;
}

.dialog-enter-from,
.dialog-leave-to {
  transform: scale(5);
  opacity: 0;
}
</style>