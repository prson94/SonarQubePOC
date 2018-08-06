export class ShoppingCartType {
    ID: number;
    Name: string;
}

export class ShoppingCart {
    ID: number;
    ShoppingCartTypeID: number;
    ResourceID: number;
    CreatedOn: string;
    RequestedOn: Date;
    Request: string;
    Requestor: string;
}

export class ShoppingCartItem {
    ShoppingCartID: number;
    Object: string;
    ObjectID: number;
    AddedOn: string;
}

export class ShoppingCartListItem {
    ObjectID: number;
    Name: string;
    Object: string;
    ObjectTypeName: string;
    Url: string;
}

export class CartModel {
    Cart: ShoppingCart;
    Items: ShoppingCartListItem[] = [];
}