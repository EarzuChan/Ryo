<template>
  <Teleport to="#ryo-app">
    <Transition name="menu" @after-enter="afterEnter" @after-leave="afterLeave">
      <div id="menu-base" :style="menuItemStyle" v-show="ctrlShow">
        <div id="menu-contents">
          <div v-for="(item,index) in items" :class="{hover: currentHover === index}"
               :id="`${item.name}-${index}`"
               class="menu-item ryo-typography-body-medium"
               @click="invoke(item)" @mouseenter="hover(item,index)">
            {{ item.name }}
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import {computed, onBeforeUnmount, onMounted, onUnmounted, type PropType, reactive, ref} from "vue"
import {AttachMethod, type MenuItem} from "@/models/Models"
import {menu} from "@/utils/MenuUtils"
import {delayExecution} from "@/utils/UsefulUtils";

const TAG = 'Menu'

const emit = defineEmits(['open', 'opened', 'close', 'closed', 'close-on-menu-item'])

const props = defineProps({
  items: {
    type: Array as PropType<MenuItem[]>,
    default: []
  },
  top: {
    type: Number,
    default: 0
  },
  left: {
    type: Number,
    default: 0
  },
  closeOnClickOverlay: {
    type: Boolean,
    default: true
  }
})

const currentHover = ref(-1)

const ctrlShow = ref(true)

const menuItemStyle = computed(() => {
  return {
    top: props.top + 'px',
    left: props.left + 'px'
  }
})

const nowMenu = ref<any>(null)

function invoke(item: MenuItem) {
  if (item.action) {
    item.action()
  }

  closeMenu(true)
}

const delay = ref<any>(null)

function hover(item: MenuItem, index: number) {
  if (index === currentHover.value) return

  console.log(TAG, 'hover', index)
  currentHover.value = index

  if (nowMenu.value) {
    nowMenu.value.closeMenu()
  } else if (delay.value) {
    delay.value.cancel()
  }

  if (item.children) {
    let babe = item.children
    delay.value = delayExecution(100, () => {
      nowMenu.value = menu({
        items: babe,
        attachToId: `${item.name}-${index}`, attachMethod: AttachMethod.UpRight,
        onClose() {
          nowMenu.value = null
        },
        onCloseOnMenuItem() {
          closeMenu()
        },
        left: -8,
        top: -8
      })

      delay.value = null
    })
  }
}

function clickDocument(event: MouseEvent) {
  if (event.target && event.target instanceof HTMLElement) {
    if (!event.target.id.startsWith('menu-') && !event.target.classList.contains('menu-item')) {
      console.log(TAG, 'clickOverlay')
      closeMenu()
    }
  }
}

function closeMenu(closeOnMenuItem = false) {
  console.trace(TAG, 'closeMenu', closeOnMenuItem)

  emit('close')
  if (closeOnMenuItem) {
    emit('close-on-menu-item')
  }
  ctrlShow.value = false
}

function afterEnter() {
  emit('opened')
}

function afterLeave() {
  emit('closed')
}

defineExpose({closeMenu})

onMounted(() => setTimeout(() => document.addEventListener('click', clickDocument), 0))

onBeforeUnmount(() => document.removeEventListener('click', clickDocument))
</script>

<style scoped>
#menu-base {
  pointer-events: auto;

  position: absolute;
  padding: 8px 0;
  overflow: auto;
  max-height: 80vh;

  min-width: 112px;
  max-width: 280px;

  border-radius: 4px;
  background-color: var(--ryo-color-surface-container);
  box-shadow: var(--ryo-elevation-2);
}

#menu-contents {
  display: flex;
  flex-direction: column;
}

.menu-item {
  padding: 4px 8px;
  cursor: pointer;
  color: var(--ryo-color-on-surface);
}

.menu-item.hover {
  background-color: rgba(var(--ryo-color-state-layers-on-surface), var(--ryo-opacity-state-layers-008));
}

.menu-item:active {
  background-color: var(--ryo-color-surface-container-highest);
}

.menu-enter-active {
  transition: var(--ryo-motion-emphasized-decelerate);
}

.menu-leave-active {
  transition: var(--ryo-motion-emphasized-accelerate);
}

.menu-enter-from,
.menu-leave-to {
  opacity: 0;
}
</style>