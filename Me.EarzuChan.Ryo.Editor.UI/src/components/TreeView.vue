<template>
  <TransitionGroup @leave="handleLeave" name="tree-view-anime" tag="div" class="tree-view" tabindex="0"
                   @focusout="clearLastClicked">
    <div class="tree-node-container" :style="'padding-left:' + (node.level * props.indent) + 'px'"
         v-for="node in processedTree" :key="node.indexPath.toString()"
         @click="handleClickNode(node)" :class="{'last-clicked':isEqual(lastClicked,node.indexPath)}">
      <div class="tree-node">
        <Icon class="tree-node-icon" :filled-icon="node.isStem?false:isEqual(lastClicked,node.indexPath)"
              :class="{'rotate' : node.expanded}" :icon="node.isStem?'chevron':'file'"/>
        <div class="tree-node-label ryo-typography-label-large">{{ node.name }}</div>
        <div class="tree-node-info ryo-typography-label-large ">{{
            node.isStem ? node.childrenCount + ' 项目' : '资源'
          }}
        </div>
      </div>
    </div>
  </TransitionGroup>
</template>

<script lang="ts" setup>
import {isEqual} from "@/utils/UsefulUtils"

const TAG = "TreeView"

interface InternalTreeNode {
  name: string
  level: number
  isStem: boolean
  indexPath: number[]
  expanded: boolean
  childrenCount: number
}

import {type PropType, computed, ref, type TransitionGroupProps} from 'vue'
import type {TreeNodeModel} from "@/models/Models"
import Icon from "./Icon.vue"

const props = defineProps({
  nodes: Array as PropType<TreeNodeModel[]>,
  filterText: String,
  indent: {
    type: Number,
    default: 24
  }
})

function clearLastClicked() {
  console.log("不有焦点了")
  lastClicked.value = []
}

const emit = defineEmits(['nodeClick'])

const nonExpandedNodes = ref<number[][]>([])
const lastClicked = ref<number[]>([])

function handleLeave(el: any) {
  el.style.width = `${el.parentNode.offsetWidth - parseInt(el.style.paddingLeft, 0)}px`
}

// props、nonExpandedNodes刷新就自动处理
const processedTree = computed(() => {
  return processNodes(props.nodes)
})

function processNodes(treeNodes: TreeNodeModel[] | undefined, level = 0, parentIndexPath: number[] = []): InternalTreeNode[] {
  let result: InternalTreeNode[] = []
  treeNodes?.forEach((node, index) => {
    const isStem = !!(node.children) // 即使子是空数组，子非未定义，就是干
    const currentIndexPath = parentIndexPath.concat(index) // 当前坐标
    const expanded = isStem ? !nonExpandedNodes.value.some(ind => isEqual(ind as number[], currentIndexPath)) : false
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

function handleClickNode(node: InternalTreeNode) {
  console.log(TAG, 'Node clicked:', node, node.indexPath)
  lastClicked.value = node.indexPath
  if (node.isStem) {
    // 非展开的处理，响应式自己会追踪并更新
    if (node.expanded) nonExpandedNodes.value.push(node.indexPath)
    else {
      const index = nonExpandedNodes.value.findIndex(iP => isEqual(iP as number[], node.indexPath))
      nonExpandedNodes.value.splice(index, 1)
    }
  } else
    emit('nodeClick', node.indexPath)
}
</script>

<style scoped>
.tree-view * {
  transition: all var(--ryo-motion-standard);
}

.tree-node-container {
  height: 36px;
  display: flex;

  border-radius: 0;
  color: var(--ryo-color-on-surface-variant);
}

.tree-node {
  margin: 0 24px 0 12px;
  display: flex;
  flex: 1;
  align-items: center;
  gap: 12px;
}

.tree-node-label {
  flex: 1;
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

.tree-view-anime-move {
  transition: all var(--ryo-motion-standard);
}

.tree-view-anime-enter-active {
  transition: all var(--ryo-motion-standard-decelerate);
}

.tree-view-anime-leave-active {
  transition: all var(--ryo-motion-standard-accelerate);
}

.tree-view-anime-enter-from,
.tree-view-anime-leave-to {
  opacity: 0;
}

.tree-view-anime-leave-active {
  position: absolute;
}
</style>