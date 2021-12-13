import { Injectable } from "@angular/core";

@Injectable({
    providedIn: 'root'
})
export class ObjectIdService {
    private map = new WeakMap<object, number>();
    private lastId = 0;

    public getObjectId(object: any) {
        if (!this.map.has(object)){
            this.map.set(object, this.lastId++);
        }

        return this.map.get(object)!;
    }
}