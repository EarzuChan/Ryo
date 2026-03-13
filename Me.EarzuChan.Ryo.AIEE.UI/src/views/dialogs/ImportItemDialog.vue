<template>
  <DialogBase :ctrl-show="ctrlShow" @opened="opened" @closed="closed">
    <div id="dialog-container">
      <div class="dialog-contents">
        <div id="headline" class="ryo-typography-headline-small">
          {{ t('importItemFromFile') }}
        </div>
        <OutlinedTextField
            ref="nameInputRef"
            :label="t('itemName')"
            v-model="itemName"
            :placeholder="t('useGangAndMinor')"
            :error="basicNameErrorText"/>
        <OutlinedTextField
            :label="t('searchTypeHere')"
            v-model="filterText"
            :placeholder="t('ignoreCase')"
            :error="typeFilterErrorText"/>
        <div class="file-picker">
          <div class="file-picker-row">
            <div class="selected-file ryo-typography-body-medium" :class="{'placeholder': !selectedFilePath}">
              {{ selectedFilePath || t('noFileSelected') }}
            </div>
            <TextButton class="choose-file-button" @click="handlePickFile">{{ t('chooseFile') }}</TextButton>
          </div>
          <div v-if="fileHintText" class="file-error-text ryo-typography-body-medium">
            {{ fileHintText }}
          </div>
        </div>
      </div>
      <Divider/>
      <div id="types-container" ref="viewport" @wheel.prevent="onScroll">
        <div class="types-list" :style="topOffsetTransform">
          <div class="type-item"
               v-for="schema in visibleSchemas"
               :key="schema.type"
               :class="{'selected': selectedSchema?.type === schema.type}"
               @click="handleSelectType(schema)">
            <div class="type-name">{{ schema.type }}</div>
            <div class="type-description">{{ t('nothing') }}</div>
          </div>
        </div>
      </div>
      <Divider/>
      <div class="dialog-contents">
        <Select :items="wrapperTypeLabels" v-model:selected="wrapperSelectIndex"
                :unselected-text="t('wrapperNoWrapperPrompt')"/>
      </div>
      <Divider v-if="props.showOverwrite"/>
        <div v-if="props.showOverwrite" class="dialog-contents overwrite-container">
        <div class="overwrite-row">
          <CheckBox v-model:checked="allowOverwrite" :disabled="overwriteDisabled"/>
          <div class="ryo-typography-body-medium" :class="{'overwrite-disabled': overwriteDisabled}">
            {{ overwriteLabelText }}
          </div>
        </div>
        <div v-if="overwriteHintText" class="overwrite-disabled-text ryo-typography-body-medium">
          {{ overwriteHintText }}
        </div>
      </div>
      <div v-if="submitErrorText" class="dialog-contents submit-error-container">
        <div class="submit-error-text ryo-typography-body-medium">
          {{ submitErrorText }}
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
import {computed, nextTick, onMounted, ref, watch} from "vue"
import DialogBase from "@/views/DialogBase.vue"
import Divider from "@/components/Divider.vue"
import TextButton from "@/components/TextButton.vue"
import {useAppStateStore} from "@/stores/AppState"
import {useVirtualScroll} from "@/composables/VirtualScroll"
import OutlinedTextField from "@/components/OutlinedTextField.vue"
import {useI18n} from "vue-i18n"
import type {RyoType} from "@/models/AppModels"
import Select from "@/components/Select.vue"
import CheckBox from "@/components/CheckBox.vue"

interface PickFileResult {
  filePath?: string
  suggestedItemName?: string
  suggestedDataTypeName?: string
  error?: string
}

interface ImportDialogSubmitResult {
  close?: boolean
  error?: string
}

const {t} = useI18n()
const appState = useAppStateStore()
const dataTypeSchemas = appState.dataTypeSchemas

const props = defineProps<{
  showOverwrite?: boolean
  overwriteDefault?: boolean
  overwriteLabel?: string
  overwriteDisabled?: (name: string) => boolean
  overwriteDisabledReason?: (name: string) => string | undefined
  nameValidator?: (name: string, allowOverwrite: boolean) => string | undefined
  requestFile: () => Promise<PickFileResult | undefined>
  confirm: (
    filePath: string,
    name: string,
    ryoType: RyoType,
    allowOverwrite?: boolean
  ) => ImportDialogSubmitResult | boolean | void | Promise<ImportDialogSubmitResult | boolean | void>
}>()

const emit = defineEmits(["open", "opened", "close", "closed"])

const ctrlShow = ref(false)
const selectedSchema = ref<any>(null)
const typeFilterErrorText = ref("")
const itemName = ref("")
const filterText = ref("")
const allowOverwrite = ref(props.overwriteDefault)
const selectedFilePath = ref("")
const selectedFileErrorText = ref("")
const viewport = ref<HTMLElement>()
const nameInputRef = ref<{focus?: () => void} | null>(null)
const hasEditedName = ref(false)
const hasSelectedTypeManually = ref(false)
const submitting = ref(false)
const suppressNameEditedTracking = ref(false)
const submitErrorText = ref("")

const wrapperTypeKeys = ["noWrapper", "array", "array2D"]
const wrapperTypeLabels = computed(() => wrapperTypeKeys.map(key => t(key)))
const preferredWrapperType = ref(-1)
const effectiveWrapperType = computed(() => preferredWrapperType.value === -1 ? 0 : preferredWrapperType.value)
const wrapperSelectIndex = computed({
  get: () => preferredWrapperType.value,
  set: (value: number) => {
    preferredWrapperType.value = value === 0 ? -1 : value
    hasSelectedTypeManually.value = true
  }
})

const normalizedName = computed(() => itemName.value.trim())
const overwriteLabelText = computed(() => props.overwriteLabel || t("allowOverwriteSameName"))
const overwriteDisabled = computed(() => props.showOverwrite
  ? (props.overwriteDisabled?.(normalizedName.value) ?? false)
  : false
)
const overwriteDisabledReasonText = computed(() => {
  if (!overwriteDisabled.value) return ""
  return props.overwriteDisabledReason?.(normalizedName.value) ?? t("itemOverwriteBlockedByOpenedTarget")
})
const hasValidName = computed(() => !!normalizedName.value)
const basicNameErrorText = computed(() => {
  if (!normalizedName.value) return hasEditedName.value ? t("nameCannotBeEmpty") : ""
  return ""
})
const overwriteValidationErrorText = computed(() => {
  if (!props.showOverwrite || overwriteDisabled.value || !normalizedName.value) return ""
  const customError = props.nameValidator?.(normalizedName.value, allowOverwrite.value)
  if (customError) return customError
  return ""
})
const fileHintText = computed(() => {
  if (selectedFileErrorText.value) return selectedFileErrorText.value
  if (!selectedFilePath.value) return ""
  return ""
})
const overwriteHintText = computed(() => overwriteDisabledReasonText.value || overwriteValidationErrorText.value)
const available = computed(() =>
  !!selectedSchema.value
  && !!selectedFilePath.value
  && hasValidName.value
  && !overwriteHintText.value
  && !selectedFileErrorText.value
)

watch(itemName, (next, prev) => {
  if (!suppressNameEditedTracking.value && next !== prev) hasEditedName.value = true
})

watch(overwriteDisabled, disabled => {
  if (disabled && allowOverwrite.value) allowOverwrite.value = false
}, {immediate: true})

watch([itemName, filterText, allowOverwrite, preferredWrapperType], () => {
  submitErrorText.value = ""
})

const viewportHeight = computed(() => viewport.value?.clientHeight || 240)
const ITEM_HEIGHT = 60

const filteredSchemas = computed(() => {
  if (!filterText.value) {
    typeFilterErrorText.value = ""
    return dataTypeSchemas
  }

  const result = dataTypeSchemas.filter(schema => schema.type.toLowerCase().includes(filterText.value.toLowerCase()))
  typeFilterErrorText.value = result.length === 0 ? t("noResultCheckUrInput") : ""
  return result
})

const {
  visibleRange,
  topOffsetTransform,
  onScroll,
  resetScroll,
  smoothScrollTo
} = useVirtualScroll({
  itemHeight: ITEM_HEIGHT,
  viewportHeight,
  totalItems: computed(() => filteredSchemas.value.length)
})

const visibleSchemas = computed(() => filteredSchemas.value.slice(visibleRange.value[0], visibleRange.value[1]))

async function scrollToSchema(typeName: string) {
  await nextTick()
  const index = filteredSchemas.value.findIndex(schema => schema.type === typeName)
  if (index !== -1) smoothScrollTo(index * ITEM_HEIGHT)
}

function applySuggestedType(dataTypeName?: string) {
  if (!dataTypeName) return

  let baseName = dataTypeName.trim()
  let wrapperMode = -1

  if (baseName.endsWith("[][]")) {
    baseName = baseName.slice(0, -4)
    wrapperMode = 2
  } else if (baseName.endsWith("[]")) {
    baseName = baseName.slice(0, -2)
    wrapperMode = 1
  }

  const schema = dataTypeSchemas.find(it => it.type === baseName)
  if (!schema) return

  selectedSchema.value = schema
  preferredWrapperType.value = wrapperMode
  void scrollToSchema(schema.type)
}

function handleSelectType(schema: any) {
  selectedSchema.value = schema
  hasSelectedTypeManually.value = true
  submitErrorText.value = ""
}

async function handlePickFile() {
  submitErrorText.value = ""
  const result = await props.requestFile()
  if (!result) return

  if (result.error) {
    selectedFilePath.value = ""
    selectedFileErrorText.value = result.error
    return
  }

  if (!result.filePath) return

  selectedFilePath.value = result.filePath
  selectedFileErrorText.value = ""

  if (!hasEditedName.value && result.suggestedItemName && !normalizedName.value) {
    suppressNameEditedTracking.value = true
    itemName.value = result.suggestedItemName
    suppressNameEditedTracking.value = false
  }

  if (!hasSelectedTypeManually.value && result.suggestedDataTypeName)
    applySuggestedType(result.suggestedDataTypeName)
}

function buildRyoType(): RyoType {
  let ryoType = appState.typeSchemaToRyoType(selectedSchema.value, effectiveWrapperType.value !== 0)
  if (effectiveWrapperType.value === 2)
    ryoType = appState.getRyoTypeByDataTypeName(`${appState.getDataTypeNameByRyoType(ryoType)}[]`)
  return ryoType
}

function handleCancel() {
  closeDialog()
}

async function handleConfirm() {
  if (!available.value || !selectedFilePath.value) return

  submitting.value = true
  try {
    submitErrorText.value = ""
    const result = await props.confirm(
      selectedFilePath.value,
      normalizedName.value,
      buildRyoType(),
      allowOverwrite.value
    )
    if (result === false) return
    if (typeof result === "object" && result?.error) {
      submitErrorText.value = result.error
      return
    }
    if (!(typeof result === "object" && result?.close === false)) closeDialog()
  } finally {
    submitting.value = false
  }
}

onMounted(() => {
  emit("open")
  ctrlShow.value = true
})

function opened() {
  emit("opened")
  resetScroll()
  selectedSchema.value = null
  selectedFilePath.value = ""
  selectedFileErrorText.value = ""
  itemName.value = ""
  allowOverwrite.value = !!props.overwriteDefault
  hasEditedName.value = false
  hasSelectedTypeManually.value = false
  preferredWrapperType.value = -1
  filterText.value = ""
  typeFilterErrorText.value = ""
  submitErrorText.value = ""
  nameInputRef.value?.focus?.()
}

function closed() {
  emit("closed")
}

function closeDialog() {
  emit("close")
  ctrlShow.value = false
}
</script>

<style scoped>
#dialog-container {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 520px;
  max-width: 520px;
}

.dialog-contents {
  gap: 16px;
  display: flex;
  flex-direction: column;
  padding: 24px;
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

#types-container {
  flex: 1;
  overflow: hidden;
  position: relative;
  background: var(--ryo-color-surface);
  min-height: 240px;
  max-height: 240px;
}

.type-item {
  display: flex;
  min-height: 60px;
  padding: 0 24px;
  justify-content: center;
  align-items: center;
  gap: 16px;
  align-self: stretch;
  transition: background-color var(--ryo-motion-standard);
  cursor: pointer;
}

.type-item:hover {
  background-color: rgba(var(--ryo-color-state-layers-on-primary-container), var(--ryo-opacity-state-layers-008));
}

.type-item.selected {
  background-color: var(--ryo-color-primary-container);
  color: var(--ryo-color-on-primary-container);
}

.type-name {
  color: var(--ryo-color-on-surface);
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
}

.type-description {
  color: var(--ryo-color-on-surface-variant);
}

.file-picker {
  border-radius: 16px;
  outline: 1px solid var(--ryo-color-outline-varient);
  outline-offset: -1px;
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.file-picker-row {
  align-items: center;
  display: flex;
  gap: 12px;
}

.selected-file {
  color: var(--ryo-color-on-surface);
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.selected-file.placeholder {
  color: var(--ryo-color-on-surface-variant);
}

.choose-file-button {
  flex-shrink: 0;
}

.file-error-text,
.overwrite-disabled-text,
.submit-error-text {
  color: var(--ryo-color-error);
}

.submit-error-text {
  white-space: pre-wrap;
}

.submit-error-container {
  padding-top: 0;
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
</style>
