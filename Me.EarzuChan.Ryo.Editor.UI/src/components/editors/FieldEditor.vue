<template>
  <div class="field-editor">
    <div class="field-holder">
      <div v-for="(item,index) in keys" class="field-list-item" :key="item.name"
           :class="{ 'even': isEven(index) }"
           @contextmenu.prevent.stop="showFieldContextMenu($event, item.name, item.type)">
        <div class="item-name">{{ item.name }}</div>
        <div class="item-value-holder" :class="{ 'even': isEven(index) }"> <!-- 左下 v-memo="item" 会搞死原子编辑器-->
          <EditorHolder with-margin :model-value="tryGetMember(item.name)"
                        @update:model-value="a=>trySetMember(item.name,a)"
                        :type="appState.getRyoTypeByDataTypeName(item.type)" :data-type-name="getMemberTypeName(item.type)"
                        :item-key="itemKey" :editor-path="getMemberPath(item.name)" :even="isEven(index)"
                        :context-menu-contributions="contextMenuContributions"/>
        </div>
      </div>
    </div>
  </div>
</template>

<script lang="ts" setup>
import EditorHolder from "../EditorHolder.vue"
import {computed, type PropType} from "vue"
import type {EditorDescriptor, RyoType} from "@/models/AppModels"
import {useAppStateStore} from "@/stores/AppState"
import {ensure, ensureObject} from "@/utils/UsefulUtils"
import {appendEditorPath} from "@/utils/EditorOverrideUtils"
import {useWorkspaceStateStore} from "@/stores/WorkspaceState"
import {useDialogStateStore} from "@/stores/DialogState"
import {useI18n} from "vue-i18n"
import {applyEditorSelectionWithStrategy} from "@/utils/EditorPreferenceUtils"
import {showMenu} from "@/utils/MenuUtils"
import type {ContextMenuContribution, MenuItem} from "@/models/UIModels"
import {composeContextMenuItems} from "@/utils/ContextMenuUtils"
import EditorOverrideManagerDialog from "@/views/dialogs/EditorOverrideManagerDialog.vue"

const TAG = "FieldEditor"

const appState = useAppStateStore()
const workspaceState = useWorkspaceStateStore()
const dialogState = useDialogStateStore()
const {t} = useI18n()

const props = defineProps({
  type: Object as PropType<RyoType>,
  even: Boolean,
  itemKey: String,
  editorPath: String,
  contextMenuContributions: Array as PropType<ContextMenuContribution[]>,
})
const emit = defineEmits(["err"])
const model = defineModel<any>()

// watch(model, v => console.log(TAG, "监测", v), {immediate: true})

const keys = computed(() => {
  if (props.type && props.type.baseType) {
    return props.type.baseType.members
  }
})

function tryGetMember(name: string) {
  // console.log(TAG, "尝试获取成员", name)

  if (ensureObject(model.value)) {
    if (name in model.value) return model.value[name]
    else {
      console.error("绑定的数据中没有这个成员")
      return undefined
    }
  } else {
    const errMsg = "绑定的数据为空或传入值不是对象"
    console.error(errMsg, model.value)
    emit('err', `${errMsg}，请看：${model.value}`)
    return undefined
  }
}

function trySetMember(name: string, value: any) {
  console.log(TAG, "尝试设置成员", name, value)

  if (ensure(model.value)) {
    if (!(name in model.value)) console.warn("绑定的数据中没有这个成员，但是我们仍然赋值")
    model.value[name] = value
  } else {
    console.error("绑定的数据为空，不能赋值")
  }
}

function isEven(index: number) {
  let res = (index % 2) !== 0
  if (props.even) res = !res
  return res
}

function getMemberPath(name: string) {
  return appendEditorPath(props.editorPath ?? "$", name)
}

function getMemberTypeName(typeName: string) {
  return typeName
}

function showFieldContextMenu(event: MouseEvent, name: string, typeName: string) {
  const path = getMemberPath(name)
  const dataTypeName = getMemberTypeName(typeName)
  const ryoType = appState.getRyoTypeByDataTypeName(typeName)
  const context = appState.createEditorContext(ryoType, {
    itemKey: props.itemKey,
    path,
    dataTypeName,
    isRoot: false,
  })
  const editors = appState.getEditorsByContext(context)
  const resolved = appState.resolveEditorForContext(
      context,
      props.itemKey ? workspaceState.getItemSessionOnceEditorOverrides(props.itemKey) : undefined
  )
  const matchedRules = appState.getMatchedEditorOverrideRulesForContext(context)
  const onceEditorId = props.itemKey
      ? workspaceState.getItemSessionOnceEditorOverrides(props.itemKey)[path]
      : undefined
  const currentEditorId = resolved.editor?.id

  const items: MenuItem[] = [
    {
      name: t("editorCurrentPathLabel", {path}),
      disabled: true,
    },
  ]

  if (props.itemKey && editors.length > 1) {
    items.push({
      name: t("switchEditor"),
      children: editors.map((editor: EditorDescriptor) => ({
        name: editor.id === currentEditorId
            ? `${t(editor.titleKey)} (${t("current")})`
            : t(editor.titleKey),
        disabled: editor.id === currentEditorId,
        action: async () => {
          await applyEditorSelectionWithStrategy({
            appState,
            workspaceState,
            dialogState,
            t,
            itemKey: props.itemKey!,
            context,
            editor,
          })
        },
      })),
    })
  }

  if (matchedRules.length > 0 || !!onceEditorId) {
    items.push({
      name: t("editorAppliedRules"),
      action: () => {
        dialogState.orderSpecial(EditorOverrideManagerDialog, {
          matchedRuleIds: matchedRules.map(rule => rule.id),
          headline: t("editorAppliedRules"),
          description: t("editorAppliedRulesDesc", {path}),
          onceOverride: props.itemKey && onceEditorId
              ? {itemKey: props.itemKey, path, editorId: onceEditorId}
              : undefined,
        })
      },
    })
  }

  showMenu({
    top: event.clientY - 8,
    left: event.clientX,
    items: composeContextMenuItems(
        items,
        props.contextMenuContributions ?? []
    ),
  })
}
</script>

<style scoped>
.field-editor {
  display: flex;
  flex-direction: column;

  overflow-x: auto;
}

.field-holder {
  display: flex;
  flex-direction: column;

  min-width: 100%;
  width: fit-content;

  overflow-x: visible;

  box-shadow: 0 1px var(--ryo-color-outline-varient);
}

.field-list-item {
  min-height: 32px;
  color: white;
  font-size: 14px;
  display: flex;

  background-color: var(--ryo-color-surface-container-high);
}

/*上面是否需要再考虑？*/

.field-list-item.even {
  background-color: var(--ryo-color-surface-container-highest);
}

.item-name {
  min-width: 188px;
  padding-left: 8px;
  padding-top: 7px; /*这俩7是为了凑36的高*/
  padding-bottom: 7px;

  font-size: 14px;
  color: var(--ryo-color-on-surface);

  box-shadow: inset -1px 0 0 0 var(--ryo-color-outline-varient), inset 0 1px 0 0 var(--ryo-color-outline-varient);
}

.item-value-holder {
  flex: 1;
  background-color: var(--ryo-color-surface-container-highest);
  display: flex;

  box-shadow: inset 0 1px 0 0 var(--ryo-color-outline-varient);
}

/*.sub-editor-card {
  border-radius: 12px;
  box-shadow: var(--ryo-elevation-2);

  margin: 6px;

  overflow: hidden;
}*/

.item-value-holder.even {
  background-color: var(--ryo-color-surface-container-high);
}

.full-flex {
  flex: 1;
}
</style>
