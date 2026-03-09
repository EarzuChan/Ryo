import type {ContextMenuContribution, MenuItem} from "@/models/UIModels"

export function createContextMenuGroup(title: string, targetPath: string, items: MenuItem[]): ContextMenuContribution {
    return {
        title,
        targetPath,
        items,
    }
}

export function composeContextMenuItems(selfItems: MenuItem[], ancestorContributions: ContextMenuContribution[], maxAncestorDepth = 2): MenuItem[] {
    const items: MenuItem[] = [...selfItems]

    ancestorContributions
        .slice(0, maxAncestorDepth)
        .filter((contribution) => contribution.items.length > 0)
        .forEach((contribution) => {
            items.push({
                name: contribution.title,
                children: contribution.items,
            })
        })

    return items
}