export class Taxonomy {
    ID: number;
    Name: string;
    TaxonomyTypeClass: string;
    Description: string;
    MaximumDepth: number;
}

export class TaxonomyLevel {
    Name: string;
    Description: string;
    Level: number;
    TaxonomyTypeID: number;
}