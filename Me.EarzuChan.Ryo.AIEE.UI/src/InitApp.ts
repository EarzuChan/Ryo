import {createApp} from 'vue'
import {createPinia} from 'pinia'
import App from './App.vue'
import AppError from '@/views/AppError.vue'
import './styles/style-default.scss'
import './styles/color-default.scss'
import {i18n} from "@/misc/I18n"

const TAG = "InitApp"
const APP_INFO = {
    version: "ALPHA-2026.1",
    name: "Ryo",
    author: "Earzu Chan",
    repoLink: "https://github.com/EarzuChan/Ryo",
    authorLink: "https://github.com/EarzuChan",
    issueLink: "https://github.com/EarzuChan/Ryo/issues",
}

console.log(TAG, "Start init")

// 用以拦截浏览器默认右键
window.addEventListener('contextmenu', (event) => {
    event.preventDefault()
})

const app = createApp(App)
const pinia = createPinia()

app.config.errorHandler = (err, instance, info) => {
    console.log(TAG, 'App crashed:', err)
    app.unmount()

    createApp(AppError).use(i18n).provide('err', err).provide('app_info', APP_INFO).mount('body')
}
app.use(pinia)
app.use(i18n)
app.provide('app_info', APP_INFO)
app.mount('body')

console.log(TAG, "Init over")
