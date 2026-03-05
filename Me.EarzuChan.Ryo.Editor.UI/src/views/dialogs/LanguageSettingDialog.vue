<template>
  <DialogBase :ctrl-show="ctrlShow" @opened="opened" @closed="closed">
    <div id="dialog-container">
      <div class="dialog-contents">
        <div id="headline" class="ryo-typography-headline-small">{{ t("settingsLanguage") }}</div>
        <div class="language-items">
          <PreferenceItem
              :rounded="false" :hori-padding="24"
              v-for="option in languageOptions"
              :key="option.code"
              :title="t(option.labelKey)"
              clickable
              @click="draft = option.code"
          >
            <template #tail>
              <RadioButton
                  :checked="draft === option.code"
                  @update:checked="draft = option.code"
              />
            </template>
          </PreferenceItem>
        </div>
      </div>
      <div id="dialog-actions">
        <TextButton @click="closeDialog">{{ t("cancel") }}</TextButton>
        <TextButton @click="confirmAndClose">{{ t("confirm") }}</TextButton>
      </div>
    </div>
  </DialogBase>
</template>

<script setup lang="ts">
import {onMounted, ref} from "vue"
import DialogBase from "@/views/DialogBase.vue"
import TextButton from "@/components/TextButton.vue"
import PreferenceItem from "@/components/PreferenceItem.vue"
import RadioButton from "@/components/RadioButton.vue"
import {useI18n} from "vue-i18n"

// TIPS：本组件没复用CommonDialog，而是独立实现了一个

// 如果未来增加语言，只需在此类型中追加对应的 code
type Language = "zh" | "en" | "ru"

// 定义语言选项的接口
interface LanguageOption {
  code: Language
  labelKey: string
}

const { t } = useI18n()

// 提取数据数组，后续增加语言只需在这里添加对象即可
const languageOptions: LanguageOption[] = [
  { code: 'zh', labelKey: 'languageOptionZh' },
  { code: 'en', labelKey: 'languageOptionEn' },
  { code: 'ru', labelKey: 'languageOptionRu' }
]

const props = defineProps<{
  currentLanguage: Language
  confirm: (language: Language) => void
}>()

const emit = defineEmits(["open", "opened", "close", "closed"])

const ctrlShow = ref(false)
const draft = ref<Language>("zh")

function confirmAndClose() {
  props.confirm(draft.value)
  closeDialog()
}

function closeDialog() {
  emit("close")
  ctrlShow.value = false
}

onMounted(() => {
  emit("open")
  draft.value = props.currentLanguage
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
  min-width: 520px;
  max-width: 520px;
}

.dialog-contents {
  gap: 16px;
  display: flex;
  flex-direction: column;
  padding: 24px 0 0;
}

#headline {
  color: var(--ryo-color-on-surface);
  padding: 0 24px;
}

.language-items {
  display: flex;
  flex-direction: column;
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
