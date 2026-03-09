<template>
  <div class="icon"
       :style="{height : iconSize + 'px' , width : iconSize + 'px','-webkit-mask-image': `url(/assets/icons/icon_${icon}${(filledIcon?'_filled':'')}.svg)`}"/>
</template>

<script setup lang="ts">
defineProps({
  icon: {
    type: String,
    default: 'default',
  },
  iconSize: {
    type: Number,
    default: 24,
  },
  filledIcon: {
    type: Boolean,
    default: false,
  },
})
</script>

<style scoped>
.icon {
  -webkit-mask-size: contain;
  -webkit-mask-position: center;
  background-color: var(--ryo-color-primary);
}
</style>