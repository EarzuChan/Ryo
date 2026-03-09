<template>
  <DialogBase :ctrl-show="ctrlShow" @opened="opened" @closed="closed">
    <div id="dialog-container">
      <div class="dialog-contents">
        <div id="headline" class="ryo-typography-headline-small">
          {{ t('renameItem') }}
        </div>
        <OutlinedTextField :label="t('itemName')" v-model="itemName" :placeholder="oldName" :error="itemNameErrorText"/>
      </div>
      <div id="dialog-actions">
        <TextButton @click="handleCancel">{{ t('cancel') }}</TextButton>
        <TextButton :disabled="!available" @click="handleConfirm">{{ t('confirm') }}</TextButton>
      </div>
    </div>
  </DialogBase>
</template>

<script setup lang="ts">
import {onMounted, ref, computed, defineProps} from 'vue'
import DialogBase from "@/views/DialogBase.vue"
import TextButton from "@/components/TextButton.vue"
import OutlinedTextField from "@/components/OutlinedTextField.vue"
import {useI18n} from "vue-i18n"

const {t} = useI18n()

const TAG = "RenameItemDialog"

const ctrlShow = ref(false)

const itemName = ref("")

// 其它检查交由后端吧
const available = computed(() => itemName.value != props.oldName)

const itemNameErrorText = computed(() => available.value ? "" : t('cantNameSame'))

const props = defineProps<{
  oldName: string,
  confirm: (str: string) => void
}>()

const emit = defineEmits(['open', 'opened', 'close', 'closed'])

function handleCancel() {
  console.log(TAG, "取消")
  closeDialog()
}

// 确认
function handleConfirm() {
  console.log(TAG, "啊王桂", itemName.value)
  
  props.confirm(itemName.value)

  closeDialog()
}

onMounted(() => {
  emit('open')
  ctrlShow.value = true
})

function opened() {
  emit('opened')
}

function closed() {
  emit('closed')
}

function closeDialog() {
  emit('close')
  ctrlShow.value = false
}
</script>

<style scoped>
#dialog-container {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 480px;
  max-width: 480px;
}

.dialog-contents {
  gap: 16px;
  display: flex;
  flex-direction: column;
  padding: 24px 24px 0;
}

#headline {
  color: var(--ryo-color-on-surface);
}

#dialog-actions {
  display: flex;
  flex-direction: row;
  justify-content: flex-end;
  align-items: center;
  padding: 24px 24px 24px 0;
  gap: 8px;
}
</style>