<template>
  <DialogBase :ctrl-show="ctrlShow" @opened="opened" @closed="closed">
    <div id="dialog-container">
      <div class="dialog-contents">
        <div id="headline" class="ryo-typography-headline-small">
          {{ t('addItem') }}
        </div>
        <OutlinedTextField ref="nameInputRef" :label="t('itemName')" v-model="itemName" :placeholder="t('useGangAndMinor')"
                           :error="basicNameErrorText"/>
        <OutlinedTextField :label="t('searchTypeHere')" v-model="filterText"
                           :placeholder="t('ignoreCase')" :error="typeFilterErrorText"/>
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
      <!--TODO：包装器选项-->
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
      <div id="dialog-actions">
        <TextButton @click="handleCancel">{{ t('cancel') }}</TextButton>
        <TextButton :disabled="!available" @click="handleConfirm">{{ t('confirm') }}</TextButton>
      </div>
    </div>
  </DialogBase>
</template>

<script setup lang="ts">
import {onMounted, ref, computed, defineProps, watch} from 'vue'
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

const {t} = useI18n()

const TAG = "AddItemDialog"

const appState = useAppStateStore()
const dataTypeSchemas = appState.dataTypeSchemas

const ctrlShow = ref(false)
const selectedSchema = ref<any>(null)

const typeFilterErrorText = ref("")

const wrapperTypeKeys = ["noWrapper", "array", "array2D"]

const wrapperTypeLabels = computed(() => wrapperTypeKeys.map(key => t(key)))

const preferredWrapperType = ref(-1)
const effectiveWrapperType = computed(() => preferredWrapperType.value === -1 ? 0 : preferredWrapperType.value)
const wrapperSelectIndex = computed({
  get: () => preferredWrapperType.value,
  set: (value: number) => {
    preferredWrapperType.value = value === 0 ? -1 : value
  }
})

const props = defineProps<{
  showOverwrite?: boolean
  overwriteDefault?: boolean
  overwriteLabel?: string
  overwriteDisabled?: (name: string) => boolean
  overwriteDisabledReason?: (name: string) => string | undefined
  nameValidator?: (name: string, allowOverwrite: boolean) => string | undefined
  confirm: (str: string, ryoType: RyoType, allowOverwrite?: boolean) => void
}>()

const emit = defineEmits(['open', 'opened', 'close', 'closed'])

const itemName = ref("")
const filterText = ref("")
const allowOverwrite = ref(!!props.overwriteDefault)
const nameInputRef = ref<{focus?: () => void} | null>(null)
const hasEditedName = ref(false)

const viewport = ref<HTMLElement>()

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
const overwriteHintText = computed(() => overwriteDisabledReasonText.value || overwriteValidationErrorText.value)
const available = computed(() => !!selectedSchema.value && hasValidName.value && !overwriteHintText.value)

watch(overwriteDisabled, disabled => {
  if (disabled && allowOverwrite.value) allowOverwrite.value = false
}, {immediate: true})

watch(itemName, (next, prev) => {
  if (next !== prev) hasEditedName.value = true
})

// 添加计算属性获取实际高度
const viewportHeight = computed(() => viewport.value?.clientHeight || 240)

// 每个条目的高度
const ITEM_HEIGHT = 60

// 筛选后的数据
const filteredSchemas = computed(() => {
  if (!filterText.value) {
    typeFilterErrorText.value = ""
    return dataTypeSchemas
  }

  const res = dataTypeSchemas.filter(schema => schema.type.toLowerCase().includes(filterText.value.toLowerCase()))

  typeFilterErrorText.value = res.length == 0 ? t("noResultCheckUrInput") : ""

  return res
})

// 使用虚拟滚动 hook
const {
  visibleRange,
  topOffsetTransform,
  onScroll,
  resetScroll
} = useVirtualScroll({
  itemHeight: ITEM_HEIGHT,
  viewportHeight: viewportHeight,
  totalItems: computed(() => filteredSchemas.value.length)
})

// 可见的数据项
const visibleSchemas = computed(() => {
  return filteredSchemas.value.slice(visibleRange.value[0], visibleRange.value[1])
})

// 选择类型
function handleSelectType(schema: any) {
  console.log(TAG, "选择类型", schema)
  selectedSchema.value = schema
}

// 取消
function handleCancel() {
  console.log(TAG, "取消")
  closeDialog()
}

// 确认
function handleConfirm() {
  // 计算RyoType
  console.log(TAG, "啊玉桂狗", effectiveWrapperType.value)
  let ryoType = appState.typeSchemaToRyoType(selectedSchema.value, effectiveWrapperType.value !== 0)
  if (effectiveWrapperType.value === 2) ryoType = appState.getRyoTypeByDataTypeName(appState.getDataTypeNameByRyoType(ryoType) + "[]")

  props.confirm(normalizedName.value, ryoType, allowOverwrite.value)

  closeDialog()
}

onMounted(() => {
  emit('open')
  ctrlShow.value = true
})

function opened() {
  emit('opened')

  // 重置滚动位置和选中状态
  resetScroll()

  selectedSchema.value = null
  itemName.value = ""
  allowOverwrite.value = !!props.overwriteDefault
  hasEditedName.value = false
  preferredWrapperType.value = -1
  filterText.value = ""
  typeFilterErrorText.value = ""
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
</style>
