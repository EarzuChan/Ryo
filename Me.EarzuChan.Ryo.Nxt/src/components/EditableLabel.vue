<template>
  <div class="label-holder" :class="labelHolderStyle"
       @mouseleave="handleMouseLeave" ref="labelHolder" tabindex="0" @focus="handleFocus(true)">
    <div v-if="editable && holderFocusing" class="input-holder"
         @focusout="handleFocus(false)">
      <input class="label ryo-typography-label-large" ref="input"
             type="text" :value="editText"
             @input="inputText($event.target)"
             @keydown.enter.prevent="handlePressEnter"/>
      <IconButton icon="close" :size="iconButtonSize" @mousedown.left="clearText"/>
    </div>
    <div v-else class="label ryo-typography-label-large">
      <slot/>
    </div>
  </div>
</template>

<script lang="ts" setup>
import {ref, nextTick, computed} from 'vue'
import IconButton from "./IconButton.vue"

const props = defineProps({
  editable: {
    type: Boolean,
    default: false
  },
  editText: {
    type: String,
    default: '请绑定编辑文本'
  },
  elegant: {
    type: Boolean,
    default: false
  }
})
const emit = defineEmits(['update:editText'])

const iconButtonSize = computed(() => props.elegant ? 28 : 24)

const labelHolderStyle = computed(() => {
  let arr: string[] = []
  if (props.editable && !notHoverAble.value) arr.push('hover-able')
  if (holderFocusing.value) arr.push('focusing')
  if (props.elegant) arr.push("elegant")
  return arr
})

const notHoverAble = ref(false)
const holderFocusing = ref(false)

const input = ref<any>(null)
const labelHolder = ref<any>(null)

function handleMouseLeave() {
  if (!holderFocusing.value) notHoverAble.value = false
}

function clearText() {
  // console.log('Cleared')
  emit('update:editText', '')
}

function inputText(target: any) {
  emit('update:editText', (target as HTMLInputElement).value)
}

function handlePressEnter() {
  input.value.blur()
}

async function handleFocus(isFocusing: boolean) {
  if (!props.editable) return

  // console.log("专注：" + isFocusing)

  if (isFocusing) {
    holderFocusing.value = true

    // 切换焦点
    await nextTick()
    if (input.value) {
      notHoverAble.value = true
      input.value.focus()
    }
  } else {
    // 防止阻止事件
    holderFocusing.value = false
    notHoverAble.value = false
  }
}

// console.log("可编辑：" + props.editable)
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

.input-holder {
  display: flex;
  flex-direction: row;

  flex: 1;
}

.label-holder.elegant.hover-able:hover, .label-holder.elegant.focusing {
  border-radius: 18px;
}

.label-holder:not(.elegant).hover-able:hover, .label-holder:not(.elegant).focusing {
  border-radius: 4px;
}

.label-holder.hover-able:hover {
  background-color: rgba(var(--ryo-color-state-layers-on-primary-container), var(--ryo-opacity-state-layers-008));
}

.label-holder.focusing {
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