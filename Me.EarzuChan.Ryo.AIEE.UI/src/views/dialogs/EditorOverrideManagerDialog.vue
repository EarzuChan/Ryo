<template>
  <DialogBase :ctrl-show="ctrlShow" @opened="opened" @closed="closed">
    <div id="dialog-container">
      <div class="dialog-header">
        <div class="ryo-typography-headline-small headline">{{ headlineText }}</div>
        <div class="ryo-typography-body-medium description">{{ descriptionText }}</div>
      </div>

      <div class="dialog-body">
        <section v-if="showOnceRuleSection" class="rule-section">
          <div class="section-header">
            <div class="ryo-typography-title-medium">{{ t("editorRulesOnceRules") }}</div>
          </div>
          <div class="rule-list">
            <div class="rule-card rule-card-single">
              <PreferenceItem
                  :title="onceRuleEditorTitle"
                  :desc="t('editorRulesPathLabel', {path: props.onceOverride?.path ?? '$'})"
              >
                <template #tail>
                  <div @click.stop>
                    <Switch :checked="onceRuleEnabled" @update:checked="toggleOnceRule"/>
                  </div>
                </template>
              </PreferenceItem>
            </div>
          </div>
        </section>

        <section class="rule-section">
          <div class="section-header">
            <div class="ryo-typography-title-medium">{{ t("editorRulesPathRules") }}</div>
            <TextButton v-if="!props.matchedRuleIds?.length" @click="openCreate('path')">{{ t("add") }}</TextButton>
          </div>
          <div class="rule-list" v-if="pathRules.length > 0">
            <div
                v-for="(rule,index) in pathRules"
                :key="rule.id"
                class="rule-card"
                :class="getRuleCardClass(index, pathRules.length)"
            >
              <PreferenceItem
                  :title="getEditorTitle(rule)"
                  :desc="t('editorRulesPathLabel', {path: rule.pattern})"
                  clickable
                  @click="openEdit(rule)"
              >
                <template #tail>
                  <div @click.stop>
                    <Switch :checked="rule.enabled" @update:checked="toggleRule(rule, $event)"/>
                  </div>
                </template>
              </PreferenceItem>
            </div>
          </div>
          <div v-else class="empty-state ryo-typography-body-medium">{{ t("editorRulesEmptyPath") }}</div>
        </section>

        <section class="rule-section">
          <div class="section-header">
            <div class="ryo-typography-title-medium">{{ t("editorRulesTypeRules") }}</div>
            <TextButton v-if="!props.matchedRuleIds?.length" @click="openCreate('type')">{{ t("add") }}</TextButton>
          </div>
          <div class="rule-list" v-if="typeRules.length > 0">
            <div
                v-for="(rule,index) in typeRules"
                :key="rule.id"
                class="rule-card"
                :class="getRuleCardClass(index, typeRules.length)"
            >
              <PreferenceItem
                  :title="getEditorTitle(rule)"
                  :desc="t('editorRulesTypeLabel', {type: rule.pattern})"
                  clickable
                  @click="openEdit(rule)"
              >
                <template #tail>
                  <div @click.stop>
                    <Switch :checked="rule.enabled" @update:checked="toggleRule(rule, $event)"/>
                  </div>
                </template>
              </PreferenceItem>
            </div>
          </div>
          <div v-else class="empty-state ryo-typography-body-medium">{{ t("editorRulesEmptyType") }}</div>
        </section>
      </div>

      <div id="dialog-actions">
        <TextButton @click="closeDialog">{{ t("close") }}</TextButton>
      </div>
    </div>
  </DialogBase>

  <EditorOverrideRuleFormDialog
      v-if="editingScope"
      :mode="editingRule ? 'edit' : 'create'"
      :initial-scope="editingScope"
      :initial-rule="editingRule"
      :confirm="saveRule"
      :delete-rule="deleteRule"
      @closed="closeEditorDialog"
  />
</template>

<script setup lang="ts">
import {computed, ref, onMounted} from "vue"
import {useI18n} from "vue-i18n"
import DialogBase from "@/views/DialogBase.vue"
import TextButton from "@/components/TextButton.vue"
import PreferenceItem from "@/components/PreferenceItem.vue"
import Switch from "@/components/Switch.vue"
import {useAppStateStore} from "@/stores/AppState"
import {useWorkspaceStateStore} from "@/stores/WorkspaceState"
import {useDialogStateStore} from "@/stores/DialogState"
import type {EditorOverrideRule, EditorOverrideScope} from "@/models/AppModels"
import EditorOverrideRuleFormDialog from "@/views/dialogs/EditorOverrideRuleFormDialog.vue"

// TODO：样式全面优化！！

type EditorOverrideRuleDraft = {
  id?: string
  scope: EditorOverrideScope
  pattern: string
  editorId: string
  typeConstraint?: string
}

const {t} = useI18n()
const appState = useAppStateStore()
const workspaceState = useWorkspaceStateStore()
const dialogState = useDialogStateStore()

const props = defineProps<{
  matchedRuleIds?: string[]
  headline?: string
  description?: string
  onceOverride?: {
    itemKey: string
    path: string
    editorId: string
  }
}>()

const emit = defineEmits(["open", "opened", "close", "closed"])

const ctrlShow = ref(false)
const editingScope = ref<EditorOverrideScope | undefined>(undefined)
const editingRule = ref<EditorOverrideRule | undefined>(undefined)
const onceRuleEnabled = ref(!!props.onceOverride)

const matchedRuleIdSet = computed(() => new Set(props.matchedRuleIds ?? []))
const headlineText = computed(() => props.headline ?? t("settingsPreferredEditors"))
const descriptionText = computed(() => props.description ?? t("editorRulesManagerDesc"))
const showOnceRuleSection = computed(() => !!props.onceOverride && onceRuleEnabled.value)
const onceRuleEditorTitle = computed(() => {
  const editorId = props.onceOverride?.editorId
  if (!editorId) return t("unknown")
  const descriptor = appState.getEditorDescriptorById(editorId)
  return descriptor ? t(descriptor.titleKey) : editorId
})

const pathRules = computed(() => {
  const rules = [...appState.editorOverrideRules.pathRules]
  return props.matchedRuleIds?.length
      ? rules.filter(rule => matchedRuleIdSet.value.has(rule.id))
      : rules
})
const typeRules = computed(() => {
  const rules = [...appState.editorOverrideRules.typeRules]
  return props.matchedRuleIds?.length
      ? rules.filter(rule => matchedRuleIdSet.value.has(rule.id))
      : rules
})

function getEditorTitle(rule: EditorOverrideRule) {
  const editor = appState.getEditorDescriptorById(rule.editorId)
  return editor ? t(editor.titleKey) : rule.editorId
}

function openCreate(scope: EditorOverrideScope) {
  editingScope.value = scope
  editingRule.value = undefined
}

function openEdit(rule: EditorOverrideRule) {
  editingScope.value = rule.scope
  editingRule.value = rule
}

function closeEditorDialog() {
  editingScope.value = undefined
  editingRule.value = undefined
}

function askReplaceExistingRule(rule: EditorOverrideRuleDraft, duplicate: EditorOverrideRule): Promise<boolean> {
  return new Promise(resolve => {
    let confirmed = false
    const currentEditor = appState.getEditorDescriptorById(duplicate.editorId)
    const nextEditor = appState.getEditorDescriptorById(rule.editorId)

    dialogState.order({
      headline: t("editorRulesReplaceConfirmTitle"),
      description: t("editorRulesReplaceConfirmDesc", {
        scope: rule.scope === "path" ? t("editorRulesPathRules") : t("editorRulesTypeRules"),
        pattern: rule.pattern,
        currentEditor: currentEditor ? t(currentEditor.titleKey) : duplicate.editorId,
        nextEditor: nextEditor ? t(nextEditor.titleKey) : rule.editorId,
      }),
      actions: [
        {text: t("cancel"), onClick() { confirmed = false }},
        {text: t("confirm"), onClick() { confirmed = true }},
      ],
      onClosed: () => resolve(confirmed),
    })
  })
}

async function saveRule(rule: EditorOverrideRuleDraft): Promise<boolean> {
  const duplicate = appState.findDuplicateEditorOverrideRule(
      rule.scope,
      rule.pattern,
      rule.typeConstraint,
      rule.id
  )

  if (duplicate) {
    const confirmed = await askReplaceExistingRule(rule, duplicate)
    if (!confirmed) return false

    // Editing one rule into another target should keep only one final rule.
    if (rule.id && rule.id !== duplicate.id) {
      appState.removeEditorOverrideRule(rule.scope, rule.id)
    }
  }

  appState.upsertEditorOverrideRule({
    ...rule,
    id: duplicate?.id ?? rule.id,
    enabled: editingRule.value?.enabled ?? true,
  })
  return true
}

function deleteRule(ruleId: string, scope: EditorOverrideScope) {
  appState.removeEditorOverrideRule(scope, ruleId)
  closeEditorDialog()
}

function toggleRule(rule: EditorOverrideRule, enabled: boolean) {
  appState.upsertEditorOverrideRule({
    ...rule,
    enabled,
  })
}

function toggleOnceRule(enabled: boolean) {
  if (enabled || !props.onceOverride) return
  workspaceState.removeItemSessionOnceEditorOverride(props.onceOverride.itemKey, props.onceOverride.path)
  onceRuleEnabled.value = false
}

function getRuleCardClass(index: number, total: number) {
  if (total <= 1) return "rule-card-single"
  if (index === 0) return "rule-card-first"
  if (index === total - 1) return "rule-card-last"
  return "rule-card-middle"
}

function closeDialog() {
  emit("close")
  ctrlShow.value = false
}

onMounted(() => {
  emit("open")
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
  min-width: 760px;
  max-width: 760px;
}

.dialog-header {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 24px 24px 0;
}

.headline {
  color: var(--ryo-color-on-surface);
}

.description {
  color: var(--ryo-color-on-surface-variant);
}

.dialog-body {
  display: flex;
  flex-direction: column;
  gap: 24px;
  padding: 24px;
  max-height: 60vh;
  overflow: auto;
}

.rule-section {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 12px;
  color: var(--ryo-color-on-surface);
}

.rule-list {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.rule-card {
  background-color: var(--ryo-color-surface-container);
  overflow: hidden;
}

.rule-card-single {
  border-radius: 16px;
}

.rule-card-first {
  border-radius: 16px 16px 8px 8px;
}

.rule-card-middle {
  border-radius: 8px;
}

.rule-card-last {
  border-radius: 8px 8px 16px 16px;
}

:deep(.rule-card .preference-item.clickable:hover) {
  background-color: var(--ryo-color-surface-container-low);
}

.empty-state {
  color: var(--ryo-color-on-surface-variant);
  padding: 12px 16px;
  border-radius: 16px;
  background-color: var(--ryo-color-surface-container);
}

#dialog-actions {
  display: flex;
  flex-direction: row;
  justify-content: flex-end;
  align-items: center;
  padding: 0 24px 24px 0;
  gap: 8px;
}
</style>
