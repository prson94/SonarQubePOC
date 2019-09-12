import * as go from 'gojs';
/**
 * A custom {@link TreeLayout} that requires a "Split" node and a "Merge" node, by category.
 * The "Split" node should be the root of a tree-like structure if one excludes links to the "Merge" node.
 * This will position the "Merge" node to line up with the "Split" node.
 *
 * Assume there is a pair of nodes that "Split" and "Merge",
 * along with any number of nodes extending in a tree-structure from the "Split" node.
 * You can set all of the TreeLayout properties that you like,
 * except that for simplicity this code just works for angle === 0 or angle === 90.
 *
 * If you want to experiment with this extension, try the <a href="../../extensionsTS/Parallel.html">Parallel Layout</a> sample.
 * @category Layout Extension
 */
export class AssetBrowserLayout extends go.Layout {
    /**
      * Copies properties to a cloned Layout.
      */
    public cloneProtected(copy: this): void {
        super.cloneProtected(copy);
        //copy._radius = this._radius;
        //copy._spacing = this._spacing;
        //copy._clockwise = this._clockwise;
    }

    private recurse(p: go.LayoutVertex, lastX: number, layer: number): void {
        if (p.node.data.layer == layer) {
            const currentX: number = lastX + 200;
            if (p.destinationVertexes !== null) {
                const it = p.destinationVertexes.iterator;
                while (it.next()) {
                    const c = it.value;
                    if (c.node.data.layer === layer) {
                        c.x = currentX;
                        this.recurse(c, currentX, layer);
                    }
                }
            }
        }
    }

    /**
   * This method actually positions all of the Nodes, assuming that the ordering of the nodes
   * is given by a single link from one node to the next.
   * This respects the {@link #spacing} property to affect the layout.
   * @param {Diagram|Group|Iterable.<Part>} coll A {@link Diagram} or a {@link Group} or a collection of {@link Part}s.
   */
    public doLayout(coll: go.Diagram | go.Group | go.Iterable<go.Part>): void {

        if (this.network === null) {
            this.network = this.makeNetwork(coll);
        }

        let minLayer = 0;
        let maxLayer = 0;

        //this.arrangementOrigin = this.initialOrigin(this.arrangementOrigin);
        //const originx = this.arrangementOrigin.x;
        //const originy = this.arrangementOrigin.y;

        let it = this.network.vertexes.iterator;

        // Find min/max layer number.
        while (it.next()) {
            let v = it.value;
            let data = v.node.diagram.model.findNodeDataForKey(v.node.key); 
            v.node.data = data;
            if (v.node.data.layer > maxLayer) {
                maxLayer = v.node.data.layer;
            }
            if (v.node.data.layer  < minLayer) {
                minLayer = v.node.data.layer;
            }
        }

        // Lay out layer 0, the transformation layer, and set the order
        let currentX: number = 0;
        it = this.network.vertexes.iterator;
        while (it.next()) {
            const v = it.value;
            if (v.sourceEdges.count === 0 && v.node.data.layer === 0) {
                v.x = currentX;
                this.recurse(v, currentX, 0);
            }
        }

        // Figure out height
        let ratio: number = 0;
        for (var i = minLayer; i <= maxLayer; i++)
        {
            it = this.network.vertexes.iterator;
            while (it.next()) {
                let v = it.value;
                if (v.node.data.layer === i) {
                    v.y = ratio * 150;
                }
            }

            ratio++;
        }


        //const space = this.spacing;
        //const cw = (this.clockwise ? 1 : -1);
        //let rad = this.radius;
        //if (rad <= 0 || isNaN(rad) || !isFinite(rad)) rad = this.diameter(root) / 4;

        // treat the root node specially: it goes in the center
        //let angle = cw * Math.PI;
        //root.centerX = originx;
        //root.centerY = originy;

        //let edge = root.destinationEdges.first();
        //// if (edge === null || edge.link === null) return;
        //const link = (edge !== null ? edge.link : null);
        //if (link !== null) link.curviness = cw * rad;

        //// now locate each of the following nodes, in order, along a spiral
        //let vert = (edge !== null ? edge.toVertex : null);
        //while (vert !== null) {
        //    // involute spiral
        //    const cos = Math.cos(angle);
        //    const sin = Math.sin(angle);
        //    let x = rad * (cos + angle * sin);
        //    let y = rad * (sin - angle * cos);
        //    // the link might connect to a member node of a group
        //    if (link !== null && vert.node instanceof go.Group && link.toNode !== null && link.toNode !== vert.node) {
        //        const offset = link.toNode.location.copy().subtract(vert.node.location);
        //        x -= offset.x;
        //        y -= offset.y;
        //    }
        //    vert.centerX = x + originx;
        //    vert.centerY = y + originy;

        //    const nextedge = vert.destinationEdges.first();
        //    const nextvert = (nextedge !== null ? nextedge.toVertex : null);
        //    if (nextvert !== null) {
        //        // clockwise curves want positive Link.curviness
        //        if (this.isRouting && nextedge !== null && nextedge.link !== null) {
        //            if (!isNaN(nextedge.link.curviness)) {
        //                const c = nextedge.link.curviness;
        //                nextedge.link.curviness = cw * Math.abs(c);
        //            }
        //        }

        //        // determine next node's angle
        //        //const dia = this.diameter(vert) / 2 + this.diameter(nextvert) / 2;
        //        //angle += cw * Math.atan((dia + space) / Math.sqrt(x * x + y * y));
        //    }
        //    edge = nextedge;
        //    vert = nextvert;
        //}

        this.updateParts();
        this.network = null;
    }
}