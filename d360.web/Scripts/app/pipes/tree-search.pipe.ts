///<reference path="../es6-shim.d.ts"/>
import { Pipe, PipeTransform, Injectable } from '@angular/core';
import { Http } from '@angular/http';

import { TreeNode } from 'primeng/primeng';
import * as _ from 'lodash';

@Pipe({ name: 'treeSearch' })
export class TreeSearchPipe implements PipeTransform {
    transform(tree: TreeNode[], searchTerm: string, field?: string): any {        
        let newTree: TreeNode[] = [];

        if (!searchTerm || searchTerm.length == 0) {
            return tree;
        }

        var dupTree = _.cloneDeep(tree); // dup tree so we dont mess with original
        
        let search = searchTerm.toLowerCase();
        
        for (let node of dupTree) {
            var nameField = field ? node.data[field] : node.label;

            if (nameField && (nameField.toLowerCase().startsWith(search) || this.findSelectedTreeNode(node.children, search, field))) {
                node = this.removeChildren(node, search, field);

                newTree.push(node);
            }
            
        }

        return newTree;
    }

    private removeChildren(node: TreeNode, search: string, field?: string): TreeNode {        
        if (!node.children) return node;

        for (var i = node.children.length -1; i >= 0; i--) {
            let cNode = node.children[i];
            var nameField = field ? cNode.data[field] : cNode.label;

            if (!nameField) continue;

            if (!nameField.toLowerCase().startsWith(search) && !this.findSelectedTreeNode(cNode.children, search, field)) {
                node.children.splice(i, 1);
            }
            else if (cNode.children) {
                cNode = this.removeChildren(cNode, search,field);
            }
        }

        return node;
    }

    private findSelectedTreeNode(tree: TreeNode[], search: string, field?:string): TreeNode {
        let nodes: TreeNode[] = [];

        if (!tree) return false;
        // add root nodes
        for (let rNode of tree) {
            nodes.push(rNode);
        }

        //do a breadth first search for the given treenode
        if (!nodes || nodes.length == 0) return false;

        let node = nodes[0];

        while (node) {
            var nameField = field ? node.data[field] : node.label;

            if (nameField && nameField.toLowerCase().startsWith(search)) return true;

            //push children
            if (node.children) {
                for (let cNode of node.children) {
                    nodes.push(cNode);
                }
            }

            //remove this node
            nodes.splice(0, 1);

            if (!nodes || nodes.length == 0) return false;
            node = nodes[0];
        }
    }
}