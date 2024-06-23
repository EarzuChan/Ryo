<template>
  <div class="label-holder" :class="labelHolderStyle"
       ref="labelHolder" tabindex="0" @click="toggleItemsMenu">
    <div class="label ryo-typography-label-large" id="mamba-out">
      {{ selected !== -1 ? items[selected] : "未选择" }}
    </div>
    <IconButton v-if="currentMenu!==null" icon="unfold_less"
                :size="iconButtonSize" @mousedown.left="toggleItemsMenu"/>
  </div>
</template>

<script lang="ts" setup>
import {ref, computed, type PropType} from 'vue'
import IconButton from "./IconButton.vue"
import {menu} from "@/utils/MenuUtils"
import {AttachMethod} from "@/models/Models";

const props = defineProps({
  items: {
    type: Array as PropType<any[]>,
    default: false
  },
  selected: {
    type: Number,
    default: -1
  },
  elegant: {
    type: Boolean,
    default: false
  }
})
const emit = defineEmits(['update:selected'])

const iconButtonSize = computed(() => props.elegant ? 28 : 24)
const labelHolderStyle = computed(() => {
  let arr: string[] = []
  if (currentMenu.value) arr.push('showingMenu')
  if (props.elegant) arr.push("elegant")
  return arr
})
const menuItems = computed(() => props.items.map((item, index) => {
  return {
    name: item,
    action: () => {
      emit('update:selected', index)
    }
  }
}))

const currentMenu = ref<any>(null)

function toggleItemsMenu() {
  if (currentMenu.value) {
    currentMenu.value.closeMenu()
  } else {
    const ind = props.selected === -1 ? 0 : props.selected
    const fix = props.elegant ? 0 : 0.5
    currentMenu.value = menu({
      items: menuItems.value,
      attachToId: 'mamba-out', locateToIndex: ind,
      left: -8, top: -12 - fix, attachMethod: AttachMethod.UpLeft, // 菜单超长时
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
  padding: 0 2px;

  min-height: 24px;
  max-height: 24px;
}

.label-holder.elegant {
  padding: 8px 16px;

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
  display: flex;
  white-space: nowrap;
  background: none;
  border: none;

  flex: 1;
}
</style>