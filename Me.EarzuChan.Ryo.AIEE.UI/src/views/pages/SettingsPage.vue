<template>
  <div id="settings-page">
    <PreferenceLabel>{{ $t("settingsGroupUsage") }}</PreferenceLabel>
    <PreferenceItem icon-key="apps" :title="$t('settingsPreferredEditors')" clickable @click="openEditorOverrideManager"/>

    <PreferenceLabel>{{ $t("settingsGroupInterface") }}</PreferenceLabel>
    <PreferenceItem icon-key="translate" :title="$t('settingsLanguage')" clickable @click="openLanguageDialog">
      <template #tail>{{ currentLanguageLabel }}</template>
    </PreferenceItem>
    <PreferenceItem icon-key="daynight_mode" :title="$t('settingsMode')" :desc="$t('settingsModeDesc')">
      <template #tail>{{ $t("settingsModeFollowSystem") }}</template>
    </PreferenceItem>
    <PreferenceItem icon-key="theme" :title="$t('settingsTheme')">
      <template #tail>{{ $t("settingsThemeDefault") }}</template>
    </PreferenceItem>

    <PreferenceLabel>{{ $t("settingsGroupMore") }}</PreferenceLabel>
    <PreferenceItem icon-key="about" :title="$t('aboutRyo')" :desc="`by ` + appInfo.author" clickable @click="openAboutDialog"/>
    <PreferenceItem icon-key="check_updates" :title="$t('settingsCheckUpdates')" :desc="appInfo.version">
      <template #tail>{{ $t("settingsLatest") }}</template>
    </PreferenceItem>
  </div>
</template>

<script setup lang="ts">
import {computed, inject} from "vue"
import PreferenceLabel from "@/components/PreferenceLabel.vue"
import PreferenceItem from "@/components/PreferenceItem.vue"
import {useAppStateStore} from "@/stores/AppState"
import {useI18n} from "vue-i18n"
import {useDialogStateStore} from "@/stores/DialogState"
import LanguageSettingDialog from "@/views/dialogs/LanguageSettingDialog.vue"
import AboutDialog from "@/views/dialogs/AboutDialog.vue"
import EditorOverrideManagerDialog from "@/views/dialogs/EditorOverrideManagerDialog.vue"

const appState = useAppStateStore()
const {t} = useI18n()
const dialogState = useDialogStateStore()
const appInfo: any = inject("app_info")

const currentLanguageLabel = computed(() => {
  switch (appState.appLanguage) {
    case "zh":
      return t("languageOptionZh")
    case "ru":
      return t("languageOptionRu")
    default:
      return t("languageOptionEn")
  }
})

function openLanguageDialog() {
  dialogState.orderSpecial(LanguageSettingDialog, {
    currentLanguage: appState.appLanguage,
    confirm: (language: "zh" | "en" | "ru") => {
      appState.appLanguage = language
    }
  })
}

function openAboutDialog() {
  dialogState.orderSpecial(AboutDialog)
}

function openEditorOverrideManager() {
  dialogState.orderSpecial(EditorOverrideManagerDialog)
}
</script>

<style scoped>
#settings-page {
  display: flex;
  padding: 0 8px;
  flex-direction: column;
  gap: 0;
}
</style>
