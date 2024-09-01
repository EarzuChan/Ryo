import {createI18n} from "vue-i18n"
import en from "@/locales/en.json"
import zh from "@/locales/zh.json"
import ru from "@/locales/ru.json"

const TAG = "I18n"

const naviLang = navigator.language
export const sysLang = naviLang.startsWith('zh') ? 'zh' : naviLang.startsWith('ru') ? 'ru' : 'en'

export const i18n = createI18n({
    legacy: false,
    locale: sysLang,
    fallbackLocale: 'zh',
    messages: {
        en,
        zh,
        ru
    }
})

// 添加切换语言的方法
export function setLanguage(lang: string) {
    if (i18n.global.availableLocales.includes(lang)) {
        i18n.global.locale.value = lang
        console.log(TAG, `Language switched to ${lang}`)
        return true
    } else {
        console.warn(TAG, `Language ${lang} is not available`)
        return false
    }
}