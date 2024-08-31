import {createI18n} from "vue-i18n"
import en from "@/locales/en.json"
import zh from "@/locales/zh.json"

const userLang = navigator.language.startsWith('zh') ? 'zh' : 'en'

export const i18n = createI18n({
    legacy: false,
    locale: userLang,
    fallbackLocale: 'zh',
    messages: {
        en,
        zh
    }
})