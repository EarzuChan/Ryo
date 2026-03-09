// CHECK：还没实装，注意检查功能和兼容性

import { onMounted, onUnmounted } from 'vue'

export function useKeyboardInterceptor() {
    // F5 刷新拦截
    const handleBeforeUnload = (e: BeforeUnloadEvent) => {
        // 这里可以加条件判断，比如检查是否有未保存数据
        const hasUnsavedData = checkUnsavedData()

        if (hasUnsavedData) {
            e.preventDefault()
            e.returnValue = '' // Chrome 需要设置 returnValue
            return '' // 某些浏览器需要返回字符串
        }
    }

    // 键盘事件拦截
    const handleKeyDown = (e: KeyboardEvent) => {
        // F5 刷新
        if (e.key === 'F5') {
            e.preventDefault()
            console.warn('F5 刷新已被禁用')
            // 可选：显示提示消息
            showWarning('请使用保存功能后再刷新页面')
            return false
        }

        // F12 开发者工具
        if (e.key === 'F12') {
            e.preventDefault()
            console.warn('F12 开发者工具已被禁用')
            return false
        }

        // Ctrl + 加号（放大）
        if ((e.ctrlKey || e.metaKey) && (e.key === '+' || e.key === '=')) {
            e.preventDefault()
            console.warn('页面缩放已被禁用')
            return false
        }

        // Ctrl + 减号（缩小）
        if ((e.ctrlKey || e.metaKey) && (e.key === '-' || e.key === '_')) {
            e.preventDefault()
            console.warn('页面缩放已被禁用')
            return false
        }

        // Ctrl + 0（重置缩放）
        if ((e.ctrlKey || e.metaKey) && e.key === '0') {
            e.preventDefault()
            console.warn('页面缩放已被禁用')
            return false
        }

        // Ctrl + Shift + I（开发者工具）
        if ((e.ctrlKey || e.metaKey) && e.shiftKey && e.key === 'I') {
            e.preventDefault()
            return false
        }

        // Ctrl + Shift + J（控制台）
        if ((e.ctrlKey || e.metaKey) && e.shiftKey && e.key === 'J') {
            e.preventDefault()
            return false
        }

        // Ctrl + U（查看源代码）
        if ((e.ctrlKey || e.metaKey) && e.key === 'u') {
            e.preventDefault()
            return false
        }
    }

    // 鼠标滚轮缩放拦截
    const handleWheel = (e: WheelEvent) => {
        if (e.ctrlKey || e.metaKey) {
            e.preventDefault()
            console.warn('鼠标滚轮缩放已被禁用')
        }
    }

    // 右键菜单拦截（全局默认禁用）
    const handleContextMenu = (e: MouseEvent) => {
        const target = e.target as HTMLElement

        // 检查目标元素或其父元素是否有 data-context-menu 属性
        if (target.closest('[data-context-menu="allowed"]')) {
            // 允许自定义右键菜单的元素
            return true
        }

        // 禁用默认右键菜单
        e.preventDefault()
        return false
    }

    // 检查是否有未保存数据的函数（需要根据实际业务实现）
    function checkUnsavedData(): boolean {
        // 示例：从 sessionStorage 或 Vuex/Pinia 检查
        // return store.hasUnsavedChanges
        return true // 默认假设有未保存数据
    }

    // 显示警告消息（可选，需要配合 UI 组件）
    function showWarning(message: string) {
        // 可以使用 Element Plus、Ant Design Vue 等 UI 库的消息提示
        console.warn(message)
        // 示例：ElMessage.warning(message)
    }

    // 注册事件监听器
    const register = () => {
        window.addEventListener('beforeunload', handleBeforeUnload)
        window.addEventListener('keydown', handleKeyDown)
        window.addEventListener('wheel', handleWheel, { passive: false })
        window.addEventListener('contextmenu', handleContextMenu)
    }

    // 移除事件监听器
    const unregister = () => {
        window.removeEventListener('beforeunload', handleBeforeUnload)
        window.removeEventListener('keydown', handleKeyDown)
        window.removeEventListener('wheel', handleWheel)
        window.removeEventListener('contextmenu', handleContextMenu)
    }

    // Vue 组件挂载时注册，卸载时移除
    onMounted(register)
    onUnmounted(unregister)

    return {
        register,
        unregister,
    }
}


// App.vue 使用示例
/*
<script setup lang="ts">
import { useKeyboardInterceptor } from '@/composables/useKeyboardInterceptor';

// 在根组件中启用全局拦截
useKeyboardInterceptor();
</script>

<template>
  <div id="app">
    <!-- 普通元素，右键被禁用 -->
    <div>这里右键无效</div>
    
    <!-- 需要显示自定义右键菜单的元素，添加 data-context-menu="allowed" -->
    <div 
      data-context-menu="allowed"
      @contextmenu="showCustomMenu"
    >
      这里可以显示自定义右键菜单
    </div>
  </div>
</template>
*/


// 自定义右键菜单组件示例
/*
<template>
  <div 
    data-context-menu="allowed"
    @contextmenu.prevent="handleContextMenu"
    class="custom-context-area"
  >
    <p>在这里右键可以看到自定义菜单</p>
    
    <Teleport to="body">
      <div
        v-if="menuVisible"
        :style="{ top: menuY + 'px', left: menuX + 'px' }"
        class="context-menu"
        @click="menuVisible = false"
      >
        <div @click="handleCopy">复制</div>
        <div @click="handlePaste">粘贴</div>
        <div @click="handleDelete">删除</div>
      </div>
    </Teleport>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';

const menuVisible = ref(false);
const menuX = ref(0);
const menuY = ref(0);

const handleContextMenu = (e: MouseEvent) => {
  menuX.value = e.clientX;
  menuY.value = e.clientY;
  menuVisible.value = true;
};

const handleCopy = () => {
  console.log('复制');
};

const handlePaste = () => {
  console.log('粘贴');
};

const handleDelete = () => {
  console.log('删除');
};
</script>

<style scoped>
.context-menu {
  position: fixed;
  background: white;
  border: 1px solid #ccc;
  box-shadow: 0 2px 8px rgba(0,0,0,0.15);
  border-radius: 4px;
  padding: 4px 0;
  z-index: 9999;
}

.context-menu div {
  padding: 8px 16px;
  cursor: pointer;
}

.context-menu div:hover {
  background: #f5f5f5;
}
</style>
*/