<template>
  <div id="item-page">
    <div class="info-group">
      <div class="ryo-typography-label-large">项目元数据</div>
      <div class="horizontal-layout">
        <div class="info ryo-typography-body-large">ID：{{
            data.id
          }}<br>名称：{{ data.name ? data.name : "（无名内联项目）" }}<br>类型：{{
            data.type ? data.type.typeName : "（未知类型）"
          }}
        </div>
        <div class="info ryo-typography-body-large">解析状态：{{
            boolToText(data.parseSuccess)
          }}<br>编辑器：{{ arrayToText(supportedEditors) }}<br>导入导出：{{ arrayToText(inOutMethods) }}
        </div>
      </div>
    </div>
    <EditorHolder card-surrounded :type="data.type" v-model="data.data" :prefer-editor="preferEditor">
      <div id="editor-holder-action-bar">
        <IconButton button-style="filled" id="reload-editor-button" icon="reload"/>
        <IconButton button-style="filled" id="discard-unsaved-changes-button" icon="discard"/>
        <ComboBox id="action-bar-text" :items="supportedEditors" v-model:selected="preferEditor"/>
        <TextButton button-style="filled" id="save-button">保存</TextButton>
      </div>
    </EditorHolder>
  </div>
</template>

<script setup lang="ts">
import {computed, type PropType, ref} from "vue";
import type {ItemModel} from "@/models/Models"
import {arrayToText, boolToText, getSfcName} from "@/utils/UsefulUtils"
import EditorHolder from "@/components/EditorHolder.vue"
import FieldEditor from "@/components/Editors/FieldEditor.vue"
import IconButton from "@/components/IconButton.vue"
import EditableLabel from "@/components/EditableLabel.vue"
import TextButton from "@/components/TextButton.vue"
import {useAppStateStore} from "@/stores/AppState"
import ComboBox from "@/components/ComboBox.vue";

const appState = useAppStateStore()

const props = defineProps({
  data: {
    type: Object as PropType<ItemModel>,
    default: {}
  }
})

const supportedEditors = computed(() => {
  const type = props.data.type
  if (type) {
    return appState.getEditorsByRyoType(type).map(et => getSfcName(et))
  }

  return ["TODO"]
})

const inOutMethods = computed(() => {
  const typeName = props.data.type

  return ["TODO"]
})

const preferEditor = ref(0)
</script>

<style scoped>
#item-page {
  display: flex;
  flex-direction: column;
  gap: 24px;

  min-height: 100%;
}

.info-group {
  display: flex;
  flex-direction: column;
  gap: 8px;

  color: white;
}

.horizontal-layout {
  display: flex;
}

.info {
  flex: 1;
}

#editor-holder-action-bar {
  padding: 16px;
  gap: 16px;
  display: flex;

  border-top: 1px solid var(--ryo-color-outline-varient);
}

#action-bar-text {
  flex: 1;
  margin: -2px;
  max-height: unset;
}

#reload-editor-button {
  --ryo-color-primary: var(--ryo-color-secondary-container);
  --ryo-color-on-primary: var(--ryo-color-on-secondary-container);
}

#discard-unsaved-changes-button {
  --ryo-color-primary: var(--ryo-color-primary-container);
  --ryo-color-on-primary: var(--ryo-color-on-primary-container);
}

#save-button {
  --ryo-color-primary: var(--ryo-color-tertiary-container);
  --ryo-color-on-primary: var(--ryo-color-on-tertiary-container);
}
</style>