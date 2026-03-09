<template>
  <div class="label-holder" :class="labelHolderStyle"
       ref="labelHolder" tabindex="0" @click="toggleItemsMenu">
    <div class="label ryo-typography-label-large" :id="uniqueId">
      {{ selected !== -1 ? items[selected] : unselectedText ?? $t('unselected') }}
    </div>
  </div>
</template>

<script lang="ts" setup>
import {ref, computed, type PropType} from 'vue'
import {showMenu} from "@/utils/MenuUtils"
import {AttachMethod} from "@/models/UIModels"


const uniqueId = ref(`select-${Math.random().toString(36).slice(9)}`)

const props = defineProps({
  items: {
    type: Array as PropType<any[]>,
    default: false
  },
  elegant: {
    type: Boolean,
    default: false
  },
  unselectedText: {
    type: String || null,
    default: null
  }
})
const selected = defineModel<number>('selected', {default: -1})

const labelHolderStyle = computed(() => {
  let arr: string[] = []
  if (currentMenu.value) arr.push('showingMenu')
  if (props.elegant) arr.push("elegant")
  return arr
})

const menuItems = computed(() => props.items.map((item, index) => {
  return {
    name: item,
    action: () => selected.value = index,
  }
}))

const currentMenu = ref<any>(null)

function toggleItemsMenu() {
  if (currentMenu.value) currentMenu.value.closeMenu()
  else {
    const ind = selected.value
    currentMenu.value = showMenu({
      items: menuItems.value,
      attachToId: uniqueId.value, locateToIndex: ind,
      left: -8, top: -12, attachMethod: AttachMethod.UpLeft, // 菜单超长时
      onClose() {
        currentMenu.value = null
      },
    })
  }
}
</script>

<style scoped>
.label-holder {
  display: flex;
  transition: all var(--ryo-motion-standard);
  justify-content: left;
  align-items: center;
}

.label-holder:not(.elegant) {
  margin: 0 -2px;

  padding: 0 2px;

  min-height: 24px;
  max-height: 24px;
}

.label-holder.elegant {
  padding: 8px 8px;

  min-height: 20px;
  max-height: 20px;
}

.label-holder.elegant:not(.showingMenu):hover, .label-holder.elegant.showingMenu {
  border-radius: 18px;
}

.label-holder:not(.elegant):not(.showingMenu):hover, .label-holder:not(.elegant).showingMenu {
  border-radius: 4px;
}

.label-holder:not(.showingMenu):hover {
  background-color: rgba(var(--ryo-color-state-layers-on-primary-container), var(--ryo-opacity-state-layers-008));
}

.label-holder.showingMenu {
  background-color: rgba(var(--ryo-color-state-layers-on-primary-container), var(--ryo-opacity-state-layers-012));
}

.label {
  color: var(--ryo-color-on-surface-variant);

  flex: 1;
}
</style>