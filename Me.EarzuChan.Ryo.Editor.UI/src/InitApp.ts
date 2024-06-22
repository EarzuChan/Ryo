// import './assets/main.css'

import {createApp} from 'vue'
import {createPinia} from 'pinia'
import App from './App.vue'
import './styles/style-default.scss'
import './styles/color-default.scss'
import AppError from "@/views/AppError.vue"

const TAG = "InitApp"
const APP_INFO = {version: "2024.0607", name: "Ryo"}

console.log(TAG, "Start init")

const app = createApp(App)
const pinia = createPinia()

app.config.errorHandler = (err, instance, info) => {
    console.log(`应用爆了`, err)
    app.unmount()

    createApp(AppError).provide('err', err).provide('app_info', APP_INFO).mount('body')
}
app.use(pinia)
app.provide('app_info', APP_INFO)
app.mount('body')

console.log(TAG, "Init over")
