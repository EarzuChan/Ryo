import {createApp, ref} from 'vue'
import {createPinia} from 'pinia'
import {createI18n} from 'vue-i18n'
import App from './App.vue'
import AppError from '@/views/AppError.vue'
import './styles/style-default.scss'
import './styles/color-default.scss'
import en from './locales/en.json'
import zh from './locales/zh.json'

const TAG = "InitApp"
const APP_INFO = {
    version: "2024.0831",
    name: "Ryo",
    author: "Earzu Chan",
    repoLink: "https://github.com/EarzuChan/Ryo",
    authorLink: "https://github.com/EarzuChan",
    issueLink: "https://github.com/EarzuChan/Ryo/issues",
}

console.log(TAG, "Start init")

const userLang = navigator.language.startsWith('zh') ? 'zh' : 'en'

const app = createApp(App)
const pinia = createPinia()
const i18n = createI18n({
    locale: userLang,
    fallbackLocale: 'zh',
    messages: {
        en,
        zh
    }
})

app.config.errorHandler = (err, instance, info) => {
    console.log('App crashed:', err, info)
    app.unmount()

    createApp(AppError).provide('err', err).provide('info', info).provide('app_info', APP_INFO).mount('body')
}
app.use(pinia)
app.use(i18n)
app.provide('app_info', APP_INFO)
app.mount('body')

console.log(TAG, "Init over")
