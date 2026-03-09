<template>
  <div class="string-list-editor">
    <div v-for="(item, index) in safeModel" :key="index" class="string-item">
      <OutlinedTextField
          class="field"
          :label="t('editorStringListItemLabel', {index: index + 1})"
          :model-value="item"
          @update:model-value="updateItem(index, $event)"
      />
      <IconButton class="remove-button" icon="close" @click="removeItem(index)"/>
    </div>
    <TextButton class="add-button" @click="addItem">{{ t("editorStringListAdd") }}</TextButton>
  </div>
</template>

<script setup lang="ts">
import {computed} from "vue"
import {useI18n} from "vue-i18n"
import OutlinedTextField from "@/components/OutlinedTextField.vue"
import TextButton from "@/components/TextButton.vue"
import IconButton from "@/components/IconButton.vue"

const {t} = useI18n()
const model = defineModel<string[]>()

const safeModel = computed(() => model.value ?? [])

function updateItem(index: number, value: string) {
    if (!model.value) model.value = []
    model.value[index] = value
}

function removeItem(index: number) {
    if (!model.value) return
    model.value.splice(index, 1)
}

function addItem() {
    if (!model.value) model.value = []
    model.value.push("")
}
</script>

<style scoped>
.string-list-editor {
  display: flex;
  flex: 1;
  flex-direction: column;
  align-items: stretch;
  gap: 12px;
  padding: 12px;
}

.string-item {
  display: flex;
  align-items: center;
  gap: 8px;
}

.field {
  flex: 1;
}

.remove-button {
  flex-shrink: 0;
}

.add-button {
  align-self: flex-start;
}
</style>
