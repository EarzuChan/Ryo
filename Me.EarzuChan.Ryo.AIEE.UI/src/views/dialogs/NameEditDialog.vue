<template>
  <DialogBase :ctrl-show="ctrlShow" @opened="opened" @closed="closed">
    <div id="dialog-container">
      <div class="dialog-contents">
        <div id="headline" class="ryo-typography-headline-small">
          {{ headlineText }}
        </div>
        <div v-if="descriptionText" id="description" class="ryo-typography-body-medium">
          {{ descriptionText }}
        </div>
        <OutlinedTextField
            ref="nameInputRef"
            :label="nameLabelText"
            v-model="itemName"
            :placeholder="placeholderText"
            :error="basicNameErrorText"/>

        <div v-if="props.showOverwrite" class="overwrite-row">
          <CheckBox v-model:checked="allowOverwrite" :disabled="overwriteDisabled"/>
          <div class="ryo-typography-body-medium" :class="{'overwrite-disabled': overwriteDisabled}">
            {{ overwriteLabelText }}
          </div>
        </div>
        <div v-if="overwriteHintText" class="overwrite-disabled-text ryo-typography-body-medium">
          {{ overwriteHintText }}
        </div>
      </div>
      <div id="dialog-actions">
        <TextButton @click="handleCancel">{{ t('cancel') }}</TextButton>
        <TextButton :disabled="!available || submitting" @click="handleConfirm">{{ t('confirm') }}</TextButton>
      </div>
    </div>
  </DialogBase>
</template>

<script setup lang="ts">
import {computed, onMounted, ref, watch, withDefaults} from 'vue'
import DialogBase from "@/views/DialogBase.vue"
import TextButton from "@/components/TextButton.vue"
import OutlinedTextField from "@/components/OutlinedTextField.vue"
import CheckBox from "@/components/CheckBox.vue"
import {useI18n} from "vue-i18n"

const {t} = useI18n()

const TAG = "NameEditDialog"

const props = withDefaults(defineProps<{
  oldName?: string
  headline?: string
  description?: string
  nameLabel?: string
  placeholder?: string
  allowPlaceholderSubmit?: boolean
  showOverwrite?: boolean
  overwriteDefault?: boolean
  overwriteLabel?: string
  overwriteDisabled?: (name: string) => boolean
  overwriteDisabledReason?: (name: string) => string | undefined
  nameValidator?: (name: string, allowOverwrite: boolean) => string | undefined
  confirm: (name: string, allowOverwrite?: boolean) => boolean | void | Promise<boolean | void>
}>(), {
  oldName: "",
  headline: "",
  description: "",
  nameLabel: "",
  placeholder: "",
  allowPlaceholderSubmit: false,
  showOverwrite: false,
  overwriteDefault: false,
  overwriteLabel: "",
  overwriteDisabled: undefined,
  overwriteDisabledReason: undefined,
  nameValidator: undefined
})

const emit = defineEmits(['open', 'opened', 'close', 'closed'])

const ctrlShow = ref(false)
const itemName = ref("")
const nameInputRef = ref<{focus?: () => void} | null>(null)
const hasEditedName = ref(false)
const allowOverwrite = ref(props.overwriteDefault)
const submitting = ref(false)

const normalizedOldName = computed(() => (props.oldName ?? "").trim())
const normalizedName = computed(() => itemName.value.trim())

const headlineText = computed(() => props.headline || t("renameItem"))
const descriptionText = computed(() => props.description || "")
const nameLabelText = computed(() => props.nameLabel || t("itemName"))
const placeholderText = computed(() => props.placeholder || props.oldName || "")
const placeholderSubmitName = computed(() => props.allowPlaceholderSubmit ? placeholderText.value.trim() : "")
const effectiveName = computed(() => normalizedName.value || placeholderSubmitName.value)
const overwriteLabelText = computed(() => props.overwriteLabel || t("allowOverwriteSameName"))
const overwriteDisabled = computed(() => props.showOverwrite ? (props.overwriteDisabled?.(effectiveName.value) ?? false) : false)
const overwriteDisabledReasonText = computed(() => {
  if (!overwriteDisabled.value) return ""
  return props.overwriteDisabledReason?.(effectiveName.value) ?? t("itemOverwriteBlockedByOpenedTarget")
})

watch(itemName, (next, prev) => {
  if (next !== prev) hasEditedName.value = true
})

const basicNameIssue = computed<"empty" | "same" | "">(() => {
  if (!effectiveName.value) return "empty"
  if (normalizedOldName.value && effectiveName.value === normalizedOldName.value) return "same"
  return ""
})

watch(overwriteDisabled, disabled => {
  if (disabled && allowOverwrite.value) allowOverwrite.value = false
}, {immediate: true})

const basicNameErrorText = computed(() => {
  if (submitting.value) return ""
  if (basicNameIssue.value === "empty") return hasEditedName.value ? t("nameCannotBeEmpty") : ""
  if (basicNameIssue.value === "same") return t('cantNameSame')
  return ""
})

const overwriteValidationErrorText = computed(() => {
  if (submitting.value) return ""
  if (!props.showOverwrite || overwriteDisabled.value || !effectiveName.value) return ""
  const customError = props.nameValidator?.(effectiveName.value, allowOverwrite.value)
  if (customError) return customError
  return ""
})

const overwriteHintText = computed(() => overwriteDisabledReasonText.value || overwriteValidationErrorText.value)

const available = computed(() => !basicNameIssue.value && !overwriteHintText.value)

function handleCancel() {
  console.log(TAG, "取消")
  closeDialog()
}

async function handleConfirm() {
  if (!available.value || submitting.value) return

  submitting.value = true
  try {
    const result = await props.confirm(effectiveName.value, allowOverwrite.value)
    if (result === false) {
      submitting.value = false
      return
    }
    closeDialog()
  } catch (err) {
    submitting.value = false
    throw err
  }
}

onMounted(() => {
  emit('open')
  ctrlShow.value = true
})

function opened() {
  emit('opened')
  submitting.value = false
  nameInputRef.value?.focus?.()
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

#description {
  color: var(--ryo-color-on-surface-variant);
  white-space: pre-wrap;
}

.overwrite-row {
  align-items: center;
  color: var(--ryo-color-on-surface-variant);
  display: flex;
  gap: 8px;
}

.overwrite-disabled {
  opacity: 0.6;
}

.overwrite-disabled-text {
  color: var(--ryo-color-error);
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
