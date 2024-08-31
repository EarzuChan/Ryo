import {createI18n} from "vue-i18n"
import en from "@/locales/en.json"
import zh from "@/locales/zh.json"
import ru from "@/locales/ru.json"

const TAG = "I18n"

const sysLang = navigator.language
const preferLang = sysLang.startsWith('zh') ? 'zh' : sysLang.startsWith('ru') ? 'ru' : 'en'

export const i18n = createI18n({
    legacy: false,
    locale: preferLang,
    fallbackLocale: 'zh',
    messages: {
        en,
        zh,
        ru
    }
})