<template>
  <DialogBase :ctrl-show="ctrlShow" @opened="opened" @closed="closed">
    <div id="dialog-container">
      <div class="dialog-contents">
        <div id="headline" class="ryo-typography-headline-small">
          {{ mode === "create" ? t("editorRulesAddRule") : t("editorRulesEditRule") }}
        </div>
        <div id="description" class="ryo-typography-body-medium">
          {{ scope === "path" ? t("editorRulesPathRuleDesc") : t("editorRulesTypeRuleDesc") }}
        </div>

        <OutlinedTextField
            v-if="scope === 'path'"
            v-model="pattern"
            :label="t('editorRulesPatternPath')"
            :placeholder="t('editorRulesPatternPathHelper')"
        />

        <!--不显式显示类名了-->

        <OutlinedTextField
            v-model="filterText"
            :label="t('searchTypeHere')"
            :placeholder="t('ignoreCase')"
            :error="typeFilterErrorText"
        />
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

        <div class="editor-select-group">
          <div class="ryo-typography-title-small section-title">{{ t("editorRulesTargetEditor") }}</div>
          <Select :items="editorTitles" v-model:selected="selectedEditorIndex" :unselected-text="editorSelectUnselectedText"/>
        </div>
      </div>

      <div id="dialog-actions">
        <TextButton v-if="mode === 'edit'" @click="confirmDelete">{{ t("delete") }}</TextButton>
        <TextButton @click="closeDialog">{{ t("cancel") }}</TextButton>
        <TextButton :class="{disabled: !canSave}" @click="confirmAndClose">{{ t("confirm") }}</TextButton>
      </div>
    </div>
  </DialogBase>

  <CommonDialog
      v-if="deleting"
      :headline="t('editorRulesDeleteConfirmTitle')"
      :description="t('editorRulesDeleteConfirmDesc', {pattern: deleteTargetText})"
      :actions="deleteDialogActions"
      @closed="deleting = false"
  />
</template>

<script setup lang="ts">
import {computed, onMounted, ref, watch} from "vue"
import {useI18n} from "vue-i18n"
import DialogBase from "@/views/DialogBase.vue"
import TextButton from "@/components/TextButton.vue"
import OutlinedTextField from "@/components/OutlinedTextField.vue"
import Select from "@/components/Select.vue"
import Divider from "@/components/Divider.vue"
import {useAppStateStore} from "@/stores/AppState"
import type {EditorDescriptor, EditorOverrideRule, EditorOverrideScope, RyoType, TypeSchema} from "@/models/AppModels"
import {useVirtualScroll} from "@/composables/VirtualScroll"
import CommonDialog from "@/views/dialogs/CommonDialog.vue"
import type {DialogActionButtonModel} from "@/models/UIModels"

type EditorOverrideRuleDraft = {
  id?: string
  scope: EditorOverrideScope
  pattern: string
  editorId: string
  typeConstraint?: string
}

const {t} = useI18n()
const appState = useAppStateStore()

const props = defineProps<{
  mode: "create" | "edit"
  initialScope: EditorOverrideScope
  initialRule?: EditorOverrideRule
  confirm: (rule: EditorOverrideRuleDraft) => boolean | Promise<boolean>
  deleteRule?: (ruleId: string, scope: EditorOverrideScope) => void
}>()

const emit = defineEmits(["open", "opened", "close", "closed"])

const ctrlShow = ref(false)
const deleting = ref(false)
const scope = ref<EditorOverrideScope>("path")
const pattern = ref("")
const editorId = ref("")
const filterText = ref("")
const typeFilterErrorText = ref("")
const viewport = ref<HTMLElement>()
const selectedSchema = ref<TypeSchema | null>(null)
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
const ITEM_HEIGHT = 60

const selectedTypeName = computed(() => buildSelectedTypeName() ?? "")
const editorSelectUnselectedText = computed(() =>
    selectedTypeName.value ? t("editorRulesSelectEditor") : t("editorRulesSelectEditorFirst")
)
const normalizedPattern = computed(() => pattern.value.trim())
const viewportHeight = computed(() => viewport.value?.clientHeight || 240)
const filteredSchemas = computed(() => {
  if (!filterText.value) {
    typeFilterErrorText.value = ""
    return appState.dataTypeSchemas
  }

  const result = appState.dataTypeSchemas.filter((schema: TypeSchema) =>
      schema.type.toLowerCase().includes(filterText.value.toLowerCase())
  )
  typeFilterErrorText.value = result.length === 0 ? t("noResultCheckUrInput") : ""
  return result
})
const {
  visibleRange,
  topOffsetTransform,
  onScroll,
  resetScroll
} = useVirtualScroll({
  itemHeight: ITEM_HEIGHT,
  viewportHeight,
  totalItems: computed(() => filteredSchemas.value.length)
})
const visibleSchemas = computed(() =>
    filteredSchemas.value.slice(visibleRange.value[0], visibleRange.value[1])
)
const candidateEditors = computed<EditorDescriptor[]>(() =>
    appState.getEditorsForOverrideRule(scope.value, effectivePattern.value, effectiveTypeConstraint.value)
)
const effectivePattern = computed(() =>
    scope.value === "path" ? normalizedPattern.value : selectedTypeName.value
)
const effectiveTypeConstraint = computed(() =>
    scope.value === "path" ? selectedTypeName.value || undefined : undefined
)
const editorTitles = computed(() => candidateEditors.value.map((editor: EditorDescriptor) => t(editor.titleKey)))
const canSave = computed(() =>
    (scope.value === "type" || effectivePattern.value.length > 0) &&
    !!selectedTypeName.value &&
    candidateEditors.value.length > 0 &&
    !!editorId.value
)
const deleteTargetText = computed(() =>
    scope.value === "path" ? effectivePattern.value : selectedTypeName.value
)
const deleteDialogActions = computed<DialogActionButtonModel[]>(() => [
  {text: t("cancel")},
  {
    text: t("confirm"),
    onClick: () => {
      deleting.value = false
      if (props.initialRule?.id && props.deleteRule) props.deleteRule(props.initialRule.id, scope.value)
      closeDialog()
    }
  }
])

const selectedEditorIndex = computed({
  get() {
    return candidateEditors.value.findIndex((editor: EditorDescriptor) => editor.id === editorId.value)
  },
  set(value: number) {
    const selected = candidateEditors.value[value]
    if (selected) editorId.value = selected.id
  }
})

watch(candidateEditors, nextEditors => {
  if (nextEditors.some((editor: EditorDescriptor) => editor.id === editorId.value)) return
  editorId.value = nextEditors[0]?.id ?? ""
}, {immediate: true})

watch(preferredWrapperType, () => {
  if (selectedSchema.value) refreshPatternForSelectedType()
})

function parseTypeSelection(typeName: string | undefined): { schema?: TypeSchema; wrapperIndex: number } {
  if (!typeName) return {wrapperIndex: 0}

  let wrapperIndex = 0
  let baseType = typeName
  if (typeName.endsWith("[][]")) {
    wrapperIndex = 2
    baseType = typeName.slice(0, -4)
  } else if (typeName.endsWith("[]")) {
    wrapperIndex = 1
    baseType = typeName.slice(0, -2)
  }

  return {
    wrapperIndex,
    schema: appState.dataTypeSchemas.find((schema: TypeSchema) => schema.type === baseType),
  }
}

function buildSelectedTypeName(): string | undefined {
  if (!selectedSchema.value) return undefined

  let ryoType: RyoType = appState.typeSchemaToRyoType(selectedSchema.value, effectiveWrapperType.value !== 0)
  if (effectiveWrapperType.value === 2) {
    ryoType = appState.getRyoTypeByDataTypeName(appState.getDataTypeNameByRyoType(ryoType) + "[]")
  }
  return appState.getDataTypeNameByRyoType(ryoType)
}

function refreshPatternForSelectedType() {
  if (scope.value === "type") pattern.value = selectedTypeName.value
}

function handleSelectType(schema: TypeSchema) {
  selectedSchema.value = schema
  refreshPatternForSelectedType()
}

function confirmDelete() {
  deleting.value = true
}

async function confirmAndClose() {
  if (!canSave.value) return

  const confirmed = await props.confirm({
    id: props.initialRule?.id,
    scope: scope.value,
    pattern: effectivePattern.value,
    editorId: editorId.value,
    typeConstraint: effectiveTypeConstraint.value,
  })
  if (confirmed !== false) closeDialog()
}

function closeDialog() {
  emit("close")
  ctrlShow.value = false
}

onMounted(() => {
  emit("open")
  scope.value = props.initialRule?.scope ?? props.initialScope
  pattern.value = props.initialRule?.pattern ?? ""
  editorId.value = props.initialRule?.editorId ?? ""
  const initialTypeName = scope.value === "path" ? props.initialRule?.typeConstraint : props.initialRule?.pattern
  const parsed = parseTypeSelection(initialTypeName)
  selectedSchema.value = parsed.schema ?? null
  preferredWrapperType.value = parsed.wrapperIndex === 0 ? -1 : parsed.wrapperIndex
  filterText.value = parsed.schema?.type ?? ""
  resetScroll()
  ctrlShow.value = true
})

function opened() {
  emit("opened")
}

function closed() {
  emit("closed")
}
</script>

<style scoped>
#dialog-container {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 560px;
  max-width: 560px;
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

#description {
  color: var(--ryo-color-on-surface-variant);
}

.selection-group,
.editor-select-group {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.section-title {
  color: var(--ryo-color-on-surface);
}

.helper {
  color: var(--ryo-color-on-surface-variant);
}

.type-preview {
  min-height: 24px;
  color: var(--ryo-color-on-surface);
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

#dialog-actions {
  display: flex;
  flex-direction: row;
  justify-content: flex-end;
  align-items: center;
  padding: 24px 24px 24px 0;
  gap: 8px;
}
</style>
