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
import {isEqual} from "@/utils/UsefulUtils"
import {computed, type PropType, ref} from 'vue'
import type {TreeNodeModel} from "@/models/UIModels"
import Icon from "./Icon.vue"
import {useVirtualScroll} from "@/composables/VirtualScroll"

const TAG = "TreeView"

// TODO、FIXME：窗口大小改变，会导致虚拟滚动的个数没刷新

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

// 每个节点元素的高度
const NODE_HEIGHT = 36

// 处理成扁平化且一一对应的内部节点数组
function processNodes(treeNodes: TreeNodeModel[] | undefined, level = 0, parentIndexPath: number[] = []): InternalTreeNode[] {
  let result: InternalTreeNode[] = []
  treeNodes?.forEach((node, index) => {
    const isStem = !!(node.children)
    const currentIndexPath = parentIndexPath.concat(index)
    const expanded = isStem ? !nonExpandedNodePaths.value.some(ind => isEqual(ind as number[], currentIndexPath)) : false
    const childrenCount = isStem ? node.children!.length : 0

    let takeThis = props.filterText === undefined || node.name.includes(props.filterText)
    let children: InternalTreeNode[] = isStem && node.children!.length ? processNodes(node.children, level + 1, currentIndexPath) : []

    if (isStem && !takeThis) takeThis = !!(children.length)

    if (takeThis) result.push({
      name: node.name,
      level,
      isStem,
      indexPath: currentIndexPath,
      expanded,
      childrenCount
    })

    if (takeThis && isStem && expanded) {
      result = result.concat(children)
    }
  })
  return result
}

// 处理后的树
const processedTree = computed(() => processNodes(props.nodes))

// 视口高度
const maxViewportHeight = computed(() => viewport.value?.clientHeight || 0)

// 使用虚拟滚动 hook
const {
  visibleRange,
  topOffsetTransform,
  onScroll,
} = useVirtualScroll({
  itemHeight: NODE_HEIGHT,
  viewportHeight: maxViewportHeight,
  totalItems: computed(() => processedTree.value.length)
})

// 最终（可视）渲染的节点们
const visibleNodes = computed(() => {
  return processedTree.value.slice(visibleRange.value[0], visibleRange.value[1])
})

// 节点样式 - 合并基础样式和缩进
const getNodeStyle = (node: InternalTreeNode) => ({
  height: `${NODE_HEIGHT}px`,
  paddingLeft: `${node.level * props.indent}px`
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