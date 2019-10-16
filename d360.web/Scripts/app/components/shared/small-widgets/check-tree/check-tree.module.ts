import { NgModule} from '@angular/core';
import { CommonModule } from '@angular/common';
import { SharedModule } from 'primeng/components/common/shared';

import { CheckTree } from './check-tree.component';
import { UICheckTreeNode } from './check-tree-node.component';

@NgModule({
    imports: [CommonModule],
    exports: [CheckTree, SharedModule],
    declarations: [CheckTree, UICheckTreeNode]
})
export class CheckTreeModule { }