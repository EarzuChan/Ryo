<template>
  <div class="slider-container" ref="daddy">
    <div class="has-been" ref="hasBeen"/>
    <div class="thumb" ref="thumb" @mousedown.left="startDrag"/>
  </div>
</template>

<script setup lang="ts">
import {onMounted, ref, watch} from "vue";

const props = defineProps({
  min: {type: Number, default: 0},
  max: {type: Number, default: 100},
  step: {type: Number, default: 1},
  modelValue: Number
})

const hasBeen = ref<HTMLElement|null>(null)
const thumb = ref<HTMLElement|null>(null)
const daddy = ref<HTMLElement|null>(null)
const dragging = ref(false)

const emit = defineEmits(['update:modelValue', 'valueChanged'])

onMounted(() => watch(() => props.modelValue, (value) => {
  const percent = (((value as number) - props.min) / (props.max - props.min))
  hasBeen.value!.style.width = `${percent * daddy.value!.clientWidth}px`
  thumb.value!.style.left = `${(percent * (daddy.value!.clientWidth - 4)) - 8}px`
}, {immediate: true}))

function startDrag(event: MouseEvent) {
  dragging.value = true
  updatePosition(event)
  window.addEventListener('mousemove', updatePosition)
  window.addEventListener('mouseup', stopDrag)
}

const updatePosition = (event: MouseEvent) => {
  if (!dragging.value) return
  const rect = daddy.value!.getBoundingClientRect()
  const percent = Math.min(Math.max(0, event.clientX - rect.left), rect.width) / rect.width
  const value = props.min + percent * (props.max - props.min)
  emit('update:modelValue', value)
  emit('valueChanged', value)
}

const stopDrag = () => {
  dragging.value = false;
  window.removeEventListener('mousemove', updatePosition)
  window.removeEventListener('mouseup', stopDrag)
}
</script>

<style scoped>
.slider-container {
  margin: 8px;
  border-radius: 10px;
  flex: 1;
  min-width: 80px;
  height: 4px;
  background-color: var(--ryo-color-surface-container-highest);
}

.has-been {
  border-radius: 10px;
  background-color: var(--ryo-color-primary);
  width: 0;
  height: 4px;
}

.thumb {
  position: relative;
  width: 20px;
  height: 20px;
  top: -12px;
  left: -8px;
  border-radius: 10px;
  background-color: var(--ryo-color-primary);
  box-shadow: var(--ryo-elevation-2);
}
</style>