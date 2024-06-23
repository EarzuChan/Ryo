<template>
  <Teleport to="#ryo-app">
    <Transition name="menu" @after-enter="afterEnter" @after-leave="afterLeave">
      <div id="menu-base" :style="menuItemStyle" v-show="ctrlShow" ref="menuBase">
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
import {computed, nextTick, onBeforeUnmount, onMounted, type PropType, ref} from "vue"
import {AttachMethod, type MenuItem} from "@/models/Models"
import {menu} from "@/utils/MenuUtils"
import {delayExecution, isScrollbarVisible} from "@/utils/UsefulUtils";

const TAG = 'Menu'
const menuBase = ref<HTMLElement | null>(null)
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
  },
  locateToIndex: {
    type: Number,
    default: -1
  }
})

const currentHover = ref(-1)
const fix = ref(0)
const ctrlShow = ref(true)

const menuItemStyle = computed(() => {
  return {
    top: fix.value + 'px',
    left: props.left + 'px'
  }
})

const currentMenu = ref<any>(null)
const menuItemClicked = ref(false)

function invoke(item: MenuItem) {
  if (item.action) {
    item.action()
  }

  menuItemClicked.value = true
  closeMenu()
}

const delay = ref<any>(null)

function hover(item: MenuItem, index: number) {
  if (index === currentHover.value) return

  // console.log(TAG, 'hover', index)
  currentHover.value = index

  if (currentMenu.value) {
    currentMenu.value.closeMenu()
  } else if (delay.value) {
    delay.value.cancel()
  }

  if (item.children) {
    let babe = item.children
    delay.value = delayExecution(100, () => {
      currentMenu.value = menu({
        items: babe,
        attachToId: `${item.name}-${index}`, attachMethod: AttachMethod.UpRight,
        onClose() {
          currentMenu.value = null
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

function closeMenu() {
  console.trace(TAG, 'closeMenu', menuItemClicked)

  emit('close')
  if (menuItemClicked.value) {
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

onMounted(() => {
  emit('open')
  fix.value = props.top
  if (props.locateToIndex !== -1 && props.locateToIndex < props.items.length) {
    console.log(TAG, 'locateToIndex', props.locateToIndex)
    if (isScrollbarVisible(menuBase.value!)) {
      const item = document.getElementById(`${props.items[props.locateToIndex].name}-${props.locateToIndex}`)
      if (item) {
        item.scrollIntoView(false)
        fix.value += 8 - item.getBoundingClientRect().top
        console.log(TAG, 'fix', item.getBoundingClientRect().top)
      }
    } else {
      fix.value -= props.locateToIndex * 28
    }
  }
  setTimeout(() => document.addEventListener('click', clickDocument), 0)
})
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
  padding: 0 8px;
  max-height: 28px;
  min-height: 28px;
  display: flex;
  align-items: center;
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