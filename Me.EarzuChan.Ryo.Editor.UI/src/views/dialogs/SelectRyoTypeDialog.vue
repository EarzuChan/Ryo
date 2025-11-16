<template>
  <DialogBase :ctrl-show="ctrlShow" @opened="opened" @closed="closed">
    <div id="dialog-container">
      <div class="dialog-contents">
        <div id="headline" class="ryo-typography-headline-small">
          {{ t('addItem') }}
        </div>
        <OutlinedTextField :label="t('itemName')" v-model="itemName" :placeholder="t('useGangAndMinor')"/>
        <OutlinedTextField :label="t('searchTypeHere')" v-model="filterText"
                           :placeholder="t('ignoreCase')" :error="errorText"/>
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
      <!--差一个Make Array-->
      <div class="dialog-contents">
        <div class="hori">
          <div class="desc ryo-typography-body-medium">{{ t('makeArray') }}</div>
          <CheckBox v-model:checked="makeArray" :container-size="20"/>
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
import {onMounted, ref, computed, defineProps, type PropType} from 'vue'
import DialogBase from "@/views/DialogBase.vue"
import Divider from "@/components/Divider.vue"
import TextButton from "@/components/TextButton.vue"
import {useAppStateStore} from "@/stores/AppState"
import {useVirtualScroll} from "@/composables/VirtualScroll"
import OutlinedTextField from "@/components/OutlinedTextField.vue"
import {useI18n} from "vue-i18n";
import CheckBox from "@/components/CheckBox.vue";
import type {DialogActionButtonModel} from "@/models/UIModels";
import type {RyoType} from "@/models/AppModels";

const {t} = useI18n()

const TAG = "SelectRyoTypeDialog"

const ctrlShow = ref(false)
const selectedSchema = ref<any>(null)

const errorText = ref("")

const props = defineProps<{
  confirm: (str: string, ryoType: RyoType) => void
}>()

const emit = defineEmits(['open', 'opened', 'close', 'closed'])

const itemName = ref("")
const filterText = ref("")

const appState = useAppStateStore()
const dataTypeSchemas = appState.dataTypeSchemas

const makeArray = ref(false)

const viewport = ref<HTMLElement>()

const available = computed(() => selectedSchema.value && itemName.value)

// 添加计算属性获取实际高度
const viewportHeight = computed(() => viewport.value?.clientHeight || 240)

// 每个条目的高度
const ITEM_HEIGHT = 60

// 筛选后的数据
const filteredSchemas = computed(() => {
  if (!filterText.value) {
    errorText.value = ""
    return dataTypeSchemas
  }

  let res = dataTypeSchemas.filter(schema =>
      schema.type.toLowerCase().includes(filterText.value.toLowerCase())
  )

  errorText.value = res.length == 0 ? t("noResultCheckUrInput") : ""

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
  props.confirm(itemName.value, appState.typeSchemaToRyoType(selectedSchema.value, makeArray.value))

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
  filterText.value = ""
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

.hori {
  display: flex;
  align-items: center;
}

.desc {
  color: var(--ryo-color-on-surface-variant);
  flex: 1;
}
</style>