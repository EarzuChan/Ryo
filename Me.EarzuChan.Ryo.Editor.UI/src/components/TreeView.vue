<template>
  <div class="viewport" ref="viewport" @wheel.prevent="onScroll">
    <div class="tree-view" :style="topOffsetTransform">
      <div v-for="node in visibleNodes" :key="node.indexPath.toString()" :style="getNodeStyle(node)"
           @click="handleClickNode(node)" @contextmenu.prevent.stop="e=>handleRightClickNode(e,node)"
           :class="['tree-node-container',{'last-clicked':isEqual(pathOfLastClickedNode,node.indexPath)}]">
        <div class="tree-node">
          <Icon class="tree-node-icon" :filled-icon="node.isStem?false:isEqual(pathOfLastClickedNode,node.indexPath)"
                :class="{'rotate' : node.expanded}" :icon="node.isStem?'chevron':'file'"/>
          <div class="tree-node-label ryo-typography-label-large">{{ node.name }}</div>
          <div class="tree-node-info ryo-typography-label-large ">{{
              node.isStem ? node.childrenCount + " " + $t('items') : $t('item')
            }}
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script lang="ts" setup>
import {isEqual, delayExecution} from "@/utils/UsefulUtils"
import {computed, type PropType, ref, onUnmounted} from 'vue'
import type {TreeNodeModel} from "@/models/UIModels"
import Icon from "./Icon.vue"

const TAG = "TreeView"

const props = defineProps({
  nodes: Array as PropType<TreeNodeModel[]>,
  filterText: String,
  indent: {
    type: Number,
    default: 24
  }
})

interface InternalTreeNode {
  name: string
  level: number
  isStem: boolean
  indexPath: number[]
  expanded: boolean
  childrenCount: number
}

// 所有 非展开节点 的 路径
const nonExpandedNodePaths = ref<number[][]>([])

// 上次被点击节点 的 路径
const pathOfLastClickedNode = ref<number[]>([])

const viewport = ref<HTMLElement>()

// 虚拟空间列表 总高度
const totalVirtualSpaceHeight = computed(() => processedTree.value.length * nodeHeight)
// 虚拟空间滚动 的 顶部位置
const topPositionOfVirtualSpace = ref(0)

// 每个节点元素的高度
const nodeHeight = 36 // 根据实际样式确定

const maxViewportHeight = computed(() => {
  return viewport.value?.clientHeight || 0
})

// 计算可见范围
const visibleRange = computed(() => {
  const start = Math.floor(Math.max(topPositionOfVirtualSpace.value, 0) / nodeHeight) // 从第几个开始
  const end = start + Math.ceil(maxViewportHeight.value / nodeHeight) + 1 // 屏幕中放得下多少 + 起始 + 1

  return [start, end]
})

// 处理成扁平化且一一对应的内部节点数组
function processNodes(treeNodes: TreeNodeModel[] | undefined, level = 0, parentIndexPath: number[] = []): InternalTreeNode[] {
  let result: InternalTreeNode[] = []
  treeNodes?.forEach((node, index) => {
    const isStem = !!(node.children) // 即使子是空数组，子非未定义，就是干
    const currentIndexPath = parentIndexPath.concat(index) // 当前坐标
    const expanded = isStem ? !nonExpandedNodePaths.value.some(ind => isEqual(ind as number[], currentIndexPath)) : false
    const childrenCount = isStem ? node.children!.length : 0

    let takeThis = props.filterText === undefined || node.name.includes(props.filterText) // 直不直拿当前
    // 如果当前干节点有子节点，就递归地处理子节点
    let children: InternalTreeNode[] = isStem && node.children!.length ? processNodes(node.children, level + 1, currentIndexPath) : []
    // 因为这是个干节点，如果子还有剩，就得显示它
    if (isStem && !takeThis) takeThis = !!(children.length)

    // 拿当前节点，就将当前节点添加到结果数组中
    if (takeThis) result.push({
      name: node.name,
      level,
      isStem,
      indexPath: currentIndexPath,
      expanded,
      childrenCount
    })

    // 如果拿当前节点，且是干
    if (takeThis && isStem && expanded) {
      result = result.concat(children)
    }
  })
  return result
}

// props、nonExpandedNodes刷新就自动处理
const processedTree = computed(() => {
  return processNodes(props.nodes)
})

// 最终（可视）渲染的节点们
const visibleNodes = computed(() => {
  return processedTree.value.slice(visibleRange.value[0], visibleRange.value[1])

  // console.log(TAG, "需要渲染", visibleNodes.value)
})

// 新增边界状态类型
type BoundaryState = 'within' | 'over-top' | 'over-bottom'

// 惯性滚动参数配置
const SCROLL_SENSITIVITY = 0.2
const DECELERATION = 0.9
const VELOCITY_THRESHOLD = 0.5
const OVERSCROLL_DECELERATION = 0.7 // 边界外衰减系数
const OVERSCROLL_RESISTANCE = 0.4   // 边界外滚动阻力

// 响应式变量
const scrollVelocity = ref(0)
const isAnimating = ref(false)
let lastScrollTime = 0
let animationFrameId: number

const animate = () => {
  console.log(TAG, '动画', scrollVelocity.value)

  const currentPosition = topPositionOfVirtualSpace.value
  const maxScroll = Math.max(0, totalVirtualSpaceHeight.value - maxViewportHeight.value)

  // 边界状态检测
  const boundaryState = getBoundaryState(currentPosition, maxScroll)

  // 在边界外时的特殊处理
  if (boundaryState !== 'within') {
    console.log(TAG, '边界外', boundaryState)
    // 边界外速度衰减更快
    scrollVelocity.value *= OVERSCROLL_DECELERATION

    // 创建弹性效果
    const overshoot = boundaryState === 'over-top' ? currentPosition : currentPosition - maxScroll
    scrollVelocity.value -= overshoot * 0.1
  } else {
    // 正常区域衰减
    scrollVelocity.value *= DECELERATION
  }

  // 应用速度（边界外也允许更新位置）
  topPositionOfVirtualSpace.value += scrollVelocity.value

  // 停止条件判断
  if (Math.abs(scrollVelocity.value) < VELOCITY_THRESHOLD) {
    // 自动回弹到边界内
    if (boundaryState !== 'within') {
      const targetPosition = boundaryState === 'over-top' ? 0 : maxScroll
      smoothScrollTo(targetPosition)
      return
    }
    isAnimating.value = false
    return
  }

  animationFrameId = requestAnimationFrame(animate)
}

// 新增边界状态判断
const getBoundaryState = (position: number, max: number): BoundaryState => {
  if (position < 0) return 'over-top'
  if (position > max) return 'over-bottom'
  return 'within'
}

// 新增平滑滚动方法（用于回弹）
const smoothScrollTo = (target: number) => {
  const startPosition = topPositionOfVirtualSpace.value
  const duration = 300
  const startTime = Date.now()

  const tick = () => {
    console.log(TAG, '平滑滚动')

    const now = Date.now()
    const progress = Math.min(1, (now - startTime) / duration)
    const eased = 0.5 * (1 - Math.cos(progress * Math.PI))
    topPositionOfVirtualSpace.value = startPosition + (target - startPosition) * eased

    if (progress < 1) {
      animationFrameId = requestAnimationFrame(tick)
    } else {
      isAnimating.value = false
    }
  }

  animationFrameId = requestAnimationFrame(tick)
}

// 修改后的滚轮事件处理器
const onScroll = (event: WheelEvent) => {
  event.preventDefault()

  // console.log(TAG, '滚动', event)

  const now = Date.now()
  const delta = event.deltaMode === 0 ? event.deltaY : event.deltaY * 40
  const timeDiff = now - lastScrollTime

  // 速度累积逻辑优化
  const velocityBoost = Math.min(2, 1 + (1 / (timeDiff || 1))) // 防除零
  scrollVelocity.value += delta * SCROLL_SENSITIVITY * velocityBoost

  // 新增边界外滚动阻力应用
  const currentPosition = topPositionOfVirtualSpace.value
  const maxScroll = Math.max(0, totalVirtualSpaceHeight.value - maxViewportHeight.value)
  const boundaryState = getBoundaryState(currentPosition, maxScroll)

  if (boundaryState !== 'within') {
    // 边界外滚动时施加额外阻力
    scrollVelocity.value *= OVERSCROLL_RESISTANCE
  }

  // 移除直接的位置更新，仅在动画未启动时触发
  if (!isAnimating.value) {
    isAnimating.value = true
    lastScrollTime = now
    animationFrameId = requestAnimationFrame(animate)
  }
}

// 组件卸载时清理
onUnmounted(() => {
  if (animationFrameId) cancelAnimationFrame(animationFrameId)
})

const topOffsetTransform = computed(() => ({
  transform: `translateY(${-(topPositionOfVirtualSpace.value % nodeHeight)}px)`
}))

const getNodeStyle = (node: InternalTreeNode) => ({
  paddingLeft: `${node.level * props.indent}px`,
  height: `${nodeHeight}px`
})

function clearLastClicked() {
  console.log(TAG, "清除最后点击节点项")
  pathOfLastClickedNode.value = []
}

const emit = defineEmits(['nodeClick', 'nodeRightClick'])

function handleClickNode(node: InternalTreeNode) {
  console.log(TAG, '节点被点击', node, node.indexPath)

  pathOfLastClickedNode.value = node.indexPath

  if (node.isStem) {
    // 非展开的处理，响应式自己会追踪并更新
    if (node.expanded) nonExpandedNodePaths.value.push(node.indexPath)
    else {
      const index = nonExpandedNodePaths.value.findIndex(iP => isEqual(iP as number[], node.indexPath))
      nonExpandedNodePaths.value.splice(index, 1)
    }
  } else emit('nodeClick', node.indexPath)
}

function handleRightClickNode(e: MouseEvent, node: InternalTreeNode) {
  console.log(TAG, '节点被右击', node, node.indexPath)
  emit('nodeRightClick', node.indexPath, e)
}

defineExpose({clearLastClicked})
</script>

<style scoped>
.viewport {
  flex: 1;
  overflow: hidden;

  position: relative;
}

.tree-view * {
  transition: all var(--ryo-motion-standard);
}

.tree-node-container {
  display: flex;

  border-radius: 0;
  color: var(--ryo-color-on-surface-variant);
}

.tree-node {
  margin: 0 24px 0 12px;
  display: flex;
  flex: 1;
  overflow: hidden;
  align-items: center;
  gap: 12px;
}

.tree-node-label {
  flex: 1;
  white-space: nowrap;
  text-overflow: ellipsis;
  overflow: hidden;
}

.tree-node-icon {
  --ryo-color-primary: var(--ryo-color-on-surface-variant);
}

.tree-node-container.last-clicked .tree-node-icon {
  --ryo-color-primary: var(--ryo-color-on-primary-container);
}

.tree-node-icon.rotate {
  transform: rotate(90deg);
}

.tree-node-container.last-clicked {
  color: var(--ryo-color-on-primary-container);
  background-color: var(--ryo-color-primary-container);
  border-radius: 18px;
}

.tree-node-container:hover:not(.last-clicked) {
  background-color: rgba(var(--ryo-color-state-layers-on-primary-container), var(--ryo-opacity-state-layers-008));
  border-radius: 18px;
}
</style>