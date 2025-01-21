import {TODO} from "@/utils/UsefulUtils"

const TAG = "Stub"

type LogEntry = {
    type: '取值' | '调用'
    name: string | symbol
    args?: any[]
}

export function createStub(stubName: string = '未命名存根实例') {
    const logs: LogEntry[] = []

    const handler: ProxyHandler<{}> = {
        get(target, prop, receiver) {
            // 处理获取日志的方法
            if (prop === 'getLogs') {
                return () => [...logs]
            } else if (prop === 'setFieldValue') {
                return TODO("还没做设置预设字段值")
            } else if (prop === 'setReturnValue') {
                return TODO("还没做设置预设返回值")
            }

            // 记录属性访问
            let getRecord: LogEntry = {type: '取值', name: prop}

            logs.push(getRecord)
            console.log(TAG, stubName, getRecord)

            // 返回一个函数，用于处理方法调用
            return (...args: any[]) => {
                // 记录方法调用

                let invokeRecord: LogEntry = {type: '调用', name: prop, args}

                logs.push(invokeRecord)
                console.log(TAG, stubName, invokeRecord)

                // TODO：根据需要返回模拟数据

                return undefined
            }
        },
    }

    // 创建并返回代理对象
    return new Proxy({}, handler) as {
        [key: string]: any
        getLogs: () => LogEntry[]
        setFieldValue: (fieldName: string, value: any) => void
        setReturnValue: (methodName: string, value: any) => void
    }
}