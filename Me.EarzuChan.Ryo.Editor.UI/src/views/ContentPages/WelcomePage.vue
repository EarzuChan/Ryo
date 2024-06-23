<template>
  <div id="welcome-page">
    <ComboBox :items="man" v-model:selected="sel2" elegant/>
  </div>
</template>

<script setup lang="ts">
import ComboBox from "@/components/ComboBox.vue"
import {ref, watch} from "vue"
import {testArray} from "@/utils/UsefulUtils"

const sel2 = ref(-1)
const man = testArray('太美丽', 100)
</script>

<style scoped>
#welcome-page {
  display: flex;
  flex-direction: column;
  gap: 24px;

  min-height: 100%;
}
</style>