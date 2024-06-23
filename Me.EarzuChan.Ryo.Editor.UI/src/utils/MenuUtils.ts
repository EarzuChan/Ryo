import type {MenuModel} from "@/models/Models"
import {createApp, h} from "vue"
import Menu from "@/components/Menu.vue"
import {AttachMethod} from "@/models/Models"

const TAG = "MenuUtils"

export function menu(config: MenuModel) {
    if (config.attachToId) {
        const element = document.getElementById(config.attachToId)
        if (element) {
            const rect = element.getBoundingClientRect()

            const {top, left} = config

            switch (config.attachMethod) {
                case AttachMethod.UpLeft:
                    config.top = rect.top
                    config.left = rect.left
                    break
                case AttachMethod.UpRight:
                    config.top = rect.top
                    config.left = rect.left + rect.width
                    break
                case AttachMethod.DownRight:
                    config.top = rect.top + rect.height
                    config.left = rect.left + rect.width
                    break
                default: // AttachMethod.DownLeft
                    config.top = rect.top + rect.height
                    config.left = rect.left
                    break
            }

            if (typeof top === "number") config.top += top
            if (typeof left === "number") config.left += left
        } else throw new Error("Element not found")
    } else if (config.top === undefined || config.left === undefined) {
        throw new Error("If you don't attach to an element, you must provide top and left")
    }

    const div = document.createElement('div')
    document.body.appendChild(div)

    const man = h(Menu, {
        ...config,
        onClose: () => {
            if (config.onClose) {
                config.onClose()
            }
        },
        onCloseOnMenuItem: () => {
            if (config.onCloseOnMenuItem) {
                config.onCloseOnMenuItem()
            }
        },
        onClosed: () => {
            app.unmount()
            document.body.removeChild(div)

            if (config.onClosed) {
                config.onClosed()
            }
        }
    })

    const app = createApp({
        render() {
            return man
        }
    })

    app.mount(div)

    // console.log(TAG, "Menu", man.component)

    return man.component!.exposed
}