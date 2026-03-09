import {ref, computed, onUnmounted, type Ref} from 'vue'

export interface VirtualScrollOptions {
    // 每个条目的高度
    itemHeight: number
    // 视口实时高度
    viewportHeight: Ref<number>
    // 总条目数量
    totalItems: Ref<number>
    // 滚动灵敏度
    scrollSensitivity?: number
    // 速度衰减系数
    deceleration?: number
    // 速度阈值
    velocityThreshold?: number
}

export function useVirtualScroll(options: VirtualScrollOptions) {
    const {
        itemHeight,
        viewportHeight,
        totalItems,
        scrollSensitivity = 0.1,
        deceleration = 0.9,
        velocityThreshold = 0.5
    } = options

    const topPositionOfVirtualSpace = ref(0)
    const scrollVelocity = ref(0)
    const isAnimating = ref(false)

    let lastScrollTime = 0
    let animationFrameId: number

    const totalVirtualSpaceHeight = computed(() => totalItems.value * itemHeight)

    // 优化2: 将最大滚动距离也定义为 computed 属性
    // 这避免了在 animate 循环（每秒60次）中重复计算，提升性能。
    const maxScroll = computed(() =>
        Math.max(0, totalVirtualSpaceHeight.value - viewportHeight.value)
    )

    const visibleRange = computed(() => {
        // 使用 Math.max 确保即使在负数位置（理论上不应发生）也不会导致负数索引
        const start = Math.floor(Math.max(0, topPositionOfVirtualSpace.value) / itemHeight)
        // 多渲染一个元素作为缓冲区，防止快速滚动时出现空白
        const end = start + Math.ceil(viewportHeight.value / itemHeight) + 1
        return [start, end] as const
    })

    const topOffsetTransform = computed(() => ({
        transform: `translateY(${-(topPositionOfVirtualSpace.value % itemHeight)}px)`
    }))

    const smoothScrollTo = (target: number) => {
        const startPosition = topPositionOfVirtualSpace.value
        const duration = 300
        const startTime = Date.now()

        const tick = () => {
            const now = Date.now()
            const progress = Math.min(1, (now - startTime) / duration)
            const eased = 0.5 * (1 - Math.cos(progress * Math.PI)) // Ease-in-out easing

            // 确保平滑滚动也不会超出边界
            const newPosition = startPosition + (target - startPosition) * eased
            topPositionOfVirtualSpace.value = Math.max(0, Math.min(newPosition, maxScroll.value))

            if (progress < 1) animationFrameId = requestAnimationFrame(tick)
            else isAnimating.value = false
        }

        isAnimating.value = true
        cancelAnimationFrame(animationFrameId) // 取消之前的动画
        animationFrameId = requestAnimationFrame(tick)
    }

    // 动画循环 (核心优化)
    const animate = () => {
        scrollVelocity.value *= deceleration

        const nextPosition = topPositionOfVirtualSpace.value + scrollVelocity.value

        // 优化3: 使用 Math.max 和 Math.min 进行边界限制（Clamping）
        // 这比 if/else 结构更简洁、高效，意图也更清晰。
        topPositionOfVirtualSpace.value = Math.max(0, Math.min(nextPosition, maxScroll.value))

        // 优化4: 当滚动位置被强制限制在边界时，应立即停止速度，防止“粘”在边缘
        const isAtBoundary = topPositionOfVirtualSpace.value === 0 || topPositionOfVirtualSpace.value === maxScroll.value
        if (isAtBoundary) scrollVelocity.value = 0

        // 停止条件判断
        if (Math.abs(scrollVelocity.value) < velocityThreshold) {
            isAnimating.value = false
            return
        }

        animationFrameId = requestAnimationFrame(animate)
    }

    // 滚轮事件处理
    const onScroll = (event: WheelEvent) => {
        event.preventDefault()

        const now = Date.now()
        const delta = event.deltaMode === 0 ? event.deltaY : event.deltaY * 40
        const timeDiff = now - lastScrollTime

        // 优化5: 每次滚动都更新 lastScrollTime
        // 原始逻辑只在动画开始时更新，这会导致连续滚动时 timeDiff 过大，速度计算不准确。
        // 现在，即使在动画期间，后续滚动事件也能基于最近一次事件来计算速度增量。
        lastScrollTime = now

        // 注释: 速度增强逻辑，用于处理连续快速滚动事件，使其感觉更灵敏
        const velocityBoost = Math.min(2, 1 + (1 / (timeDiff || 1)))
        scrollVelocity.value += delta * scrollSensitivity * velocityBoost

        if (!isAnimating.value) {
            isAnimating.value = true
            // 在启动动画时，确保 lastScrollTime 是最新的
            animationFrameId = requestAnimationFrame(animate)
        }
    }

    // 重置滚动位置
    const resetScroll = () => {
        // 优化6: 确保在重置时取消任何正在进行的动画
        cancelAnimationFrame(animationFrameId)
        topPositionOfVirtualSpace.value = 0
        scrollVelocity.value = 0
        isAnimating.value = false
    }

    // 组件卸载时清理
    onUnmounted(() => {
        cancelAnimationFrame(animationFrameId)
    })

    // API 保持不变
    return {
        // 计算属性
        visibleRange,
        topOffsetTransform,

        // 方法
        onScroll,
        resetScroll,
        smoothScrollTo
    }
}