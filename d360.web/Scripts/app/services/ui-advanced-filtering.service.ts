import { Injectable } from '@angular/core';
import { AdvancedFilterFieldCondition, ConnectingOperator, FilterBetweenParams, Filters } from '../components/assets-grid/advanced-filtering/advanced-filtering.models';
import { remove } from 'lodash';
import { OperatorString } from '../models/operator.model';

export type FilteredData = any[];

@Injectable({
    providedIn: 'root'
})
export class UiAdvancedFiltering {
    runFiltering(dataToFilter: any, filters: Filters): FilteredData {
        this.removeNotValidFilterOption(filters);
        const connectingOperator = this.findOutTheConnectingOperator(filters);

        if (connectingOperator === ConnectingOperator.Or) {
            return this.filterByOrLogic(dataToFilter, filters);
        } else {
            return this.filterByAndLogic(dataToFilter, filters);
        }
    }

    removeNotValidFilterOption(filters: Filters): void {
        remove(filters.data, (filterOption: AdvancedFilterFieldCondition) => {
            return filterOption.markForDeletion || !filterOption.field;
        });
    }

    // should return advanced filter connectin operator 'or', 'and' or null
    findOutTheConnectingOperator(filters: Filters): string {
        const regexp = /\)\s(\w*)/; // match: ) word
        const match = filters.filter.match(regexp);
        if (match) {
            return match[1];
        }
        return null;
    }

    filterByAndLogic(dataToFilter: ReadonlyArray<any>, filters: Filters): FilteredData {
        let filtredData = [...dataToFilter];
        filters.data.forEach((filterOption: AdvancedFilterFieldCondition) => {
            switch (filterOption.operator) {
                case OperatorString.Contains:
                    filtredData = filtredData.filter((elementToFilter: object) => {
                        return this.isDataValueContainsSearchValue(elementToFilter[filterOption.field], filterOption.value);
                    });
                    break;
                case OperatorString.NotContains:
                    filtredData = filtredData.filter((elementToFilter: object) => {
                        return !this.isDataValueContainsSearchValue(elementToFilter[filterOption.field], filterOption.value);
                    });
                    break;
                case OperatorString.Equals:
                    filtredData = filtredData.filter((elementToFilter: object) => {
                        return this.isDataValueEqualToSearchValue(elementToFilter[filterOption.field], filterOption.value, filterOption.fieldType);
                    });
                    break;
                case OperatorString.NotEquals:
                    filtredData = filtredData.filter((elementToFilter: object) => {
                        return !this.isDataValueEqualToSearchValue(elementToFilter[filterOption.field], filterOption.value, filterOption.fieldType);
                    });
                    break;
                case OperatorString.StartsWith:
                    filtredData = filtredData.filter((elementToFilter: object) => {
                        return this.isDataValueStartsWithSearchValue(elementToFilter[filterOption.field], filterOption.value);
                    });
                    break;
                case OperatorString.EndsWith:
                    filtredData = filtredData.filter((elementToFilter: object) => {
                        return this.isDataValueEndsWithSearchValue(elementToFilter[filterOption.field], filterOption.value);
                    });
                    break;
                case OperatorString.Populated:
                    filtredData = filtredData.filter((elementToFilter: object) => {
                        return this.isDataValuePopulated(elementToFilter[filterOption.field]);
                    });
                    break;
                case OperatorString.NotPopulated:
                    filtredData = filtredData.filter((elementToFilter: object) => {
                        return !this.isDataValuePopulated(elementToFilter[filterOption.field]);
                    });
                    break;
                case OperatorString.LessThan:
                    filtredData = filtredData.filter((elementToFilter: object) => {
                        return this.isGivenValueLessThanSearchValue(elementToFilter[filterOption.field], filterOption.value);
                    });
                    break;
                case OperatorString.GreaterThan:
                    filtredData = filtredData.filter((elementToFilter: object) => {
                        return this.isGivenValueGreaterThanSearchValue(elementToFilter[filterOption.field], filterOption.value);
                    });
                    break;
                case OperatorString.LessThanOrEquals:
                    filtredData = filtredData.filter((elementToFilter: object) => {
                        return this.isGivenValueLessThanOrEqualToSearchValue(elementToFilter[filterOption.field], filterOption.value);
                    });
                    break;
                case OperatorString.GreaterThanOrEquals:
                    filtredData = filtredData.filter((elementToFilter: object) => {
                        return this.isGivenValueGreaterThanOrEqualToSearchValue(elementToFilter[filterOption.field], filterOption.value);
                    });
                    break;
                case OperatorString.Between:
                    filtredData = filtredData.filter((elementToFilter: object) => {
                        const params: FilterBetweenParams = {
                            givenValue: elementToFilter[filterOption.field],
                            searchValue1: filterOption.value,
                            searchValue2: filterOption.value2,
                            valueType: filterOption.fieldType
                        };
                        return this.isGivenValueBetweenSearchValues(params);
                    });
                    break;
                case OperatorString.Before:
                    filtredData = filtredData.filter((elementToFilter: object) => {
                        return this.isGivenDateBeforeSearchDate(elementToFilter[filterOption.field], filterOption.value);
                    });
                    break;
                case OperatorString.After:
                    filtredData = filtredData.filter((elementToFilter: object) => {
                        return !this.isGivenDateBeforeSearchDate(elementToFilter[filterOption.field], filterOption.value);
                    });
                    break;
                default:
                    console.warn(`Unknown filter operator: '${filterOption.operator}'`);
            }
        });
        return filtredData;
    }

    filterByOrLogic(dataToFilter: ReadonlyArray<any>, filters: Filters): FilteredData {
        let filterResult = [];
        let fullData = [...dataToFilter];
        let filteredData = [];

        filters.data.forEach((filterOption: AdvancedFilterFieldCondition) => {
            switch (filterOption.operator) {
                case OperatorString.Contains:
                    filteredData = remove(fullData, (elementToFilter: object) => {
                        return this.isDataValueContainsSearchValue(elementToFilter[filterOption.field], filterOption.value);
                    });
                    filterResult = [...filterResult, ...filteredData];
                    break;
                case OperatorString.NotContains:
                    filteredData = remove(fullData, (elementToFilter: object) => {
                        return !this.isDataValueContainsSearchValue(elementToFilter[filterOption.field], filterOption.value);
                    });
                    filterResult = [...filterResult, ...filteredData];
                    break;
                case OperatorString.Equals:
                    filteredData = remove(fullData, (elementToFilter: object) => {
                        return this.isDataValueEqualToSearchValue(elementToFilter[filterOption.field], filterOption.value, filterOption.fieldType);
                    });
                    filterResult = [...filterResult, ...filteredData];
                    break;
                case OperatorString.NotEquals:
                    filteredData = remove(fullData, (elementToFilter: object) => {
                        return !this.isDataValueEqualToSearchValue(elementToFilter[filterOption.field], filterOption.value, filterOption.fieldType);
                    });
                    filterResult = [...filterResult, ...filteredData];
                    break;
                case OperatorString.StartsWith:
                    filteredData = remove(fullData, (elementToFilter: object) => {
                        return this.isDataValueStartsWithSearchValue(elementToFilter[filterOption.field], filterOption.value);
                    });
                    filterResult = [...filterResult, ...filteredData];
                    break;
                case OperatorString.EndsWith:
                    filteredData = remove(fullData, (elementToFilter: object) => {
                        return this.isDataValueEndsWithSearchValue(elementToFilter[filterOption.field], filterOption.value);
                    });
                    filterResult = [...filterResult, ...filteredData];
                    break;
                case OperatorString.Populated:
                    filteredData = remove(fullData, (elementToFilter: object) => {
                        return this.isDataValuePopulated(elementToFilter[filterOption.field]);
                    });
                    filterResult = [...filterResult, ...filteredData];
                    break;
                case OperatorString.NotPopulated:
                    filteredData = remove(fullData, (elementToFilter: object) => {
                        return !this.isDataValuePopulated(elementToFilter[filterOption.field]);
                    });
                    filterResult = [...filterResult, ...filteredData];
                    break;
                case OperatorString.LessThan:
                    filteredData = remove(fullData, (elementToFilter: object) => {
                        return this.isGivenValueLessThanSearchValue(elementToFilter[filterOption.field], filterOption.value);
                    });
                    filterResult = [...filterResult, ...filteredData];
                    break;
                case OperatorString.GreaterThan:
                    filteredData = remove(fullData, (elementToFilter: object) => {
                        return this.isGivenValueGreaterThanSearchValue(elementToFilter[filterOption.field], filterOption.value);
                    });
                    filterResult = [...filterResult, ...filteredData];
                    break;
                case OperatorString.LessThanOrEquals:
                    filteredData = remove(fullData, (elementToFilter: object) => {
                        return this.isGivenValueLessThanOrEqualToSearchValue(elementToFilter[filterOption.field], filterOption.value);
                    });
                    filterResult = [...filterResult, ...filteredData];
                    break;
                case OperatorString.GreaterThanOrEquals:
                    filteredData = remove(fullData, (elementToFilter: object) => {
                        return this.isGivenValueGreaterThanOrEqualToSearchValue(elementToFilter[filterOption.field], filterOption.value);
                    });
                    filterResult = [...filterResult, ...filteredData];
                    break;
                case OperatorString.Between:
                    filteredData = remove(fullData, (elementToFilter: object) => {
                        const params: FilterBetweenParams = {
                            givenValue: elementToFilter[filterOption.field],
                            searchValue1: filterOption.value,
                            searchValue2: filterOption.value2,
                            valueType: filterOption.fieldType
                        };
                        return this.isGivenValueBetweenSearchValues(params);
                    });
                    filterResult = [...filterResult, ...filteredData];
                    break;
                case OperatorString.Before:
                    filteredData = remove(fullData, (elementToFilter: object) => {
                        return this.isGivenDateBeforeSearchDate(elementToFilter[filterOption.field], filterOption.value);
                    });
                    filterResult = [...filterResult, ...filteredData];
                    break;
                case OperatorString.After:
                    filteredData = remove(fullData, (elementToFilter: object) => {
                        return !this.isGivenDateBeforeSearchDate(elementToFilter[filterOption.field], filterOption.value);
                    });
                    filterResult = [...filterResult, ...filteredData];
                    break;
                default:
                    console.warn(`Unknown filter operator: '${filterOption.operator}'`);
            }
        });
        return filterResult;
    }

    isDataValueContainsSearchValue(dataValue: string, searchValue: string): boolean {
        return Boolean(dataValue.match(new RegExp(searchValue, 'i')));
    }

    isDataValueEqualToSearchValue(dataValue: string, searchValue: string, valueType: string): boolean {
        if (valueType === 'Text') {
            return dataValue.toLowerCase() === searchValue.toLowerCase();
        } else if (valueType === 'Number') {
            return Number(dataValue) === Number(searchValue);
        } else {
            console.warn(`Not recognized FilterFieldType`);
        }
    }

    isDataValueStartsWithSearchValue(dataValue: string, searchValue: string): boolean {
        return dataValue.toLowerCase().startsWith(searchValue.toLowerCase());
    }

    isDataValueEndsWithSearchValue(dataValue: string, searchValue: string): boolean {
        return dataValue.toLowerCase().endsWith(searchValue.toLowerCase());
    }

    isDataValuePopulated(dataValue: string | number): boolean {
        return dataValue !== undefined && dataValue !== null && dataValue !== '';
    }

    isGivenValueLessThanSearchValue(givenValue: string, searchValue: string): boolean {
        return givenValue < searchValue;
    }

    isGivenValueGreaterThanSearchValue(givenValue: string, searchValue: string): boolean {
        return givenValue > searchValue;
    }

    isGivenValueLessThanOrEqualToSearchValue(givenValue: string, searchValue: string): boolean {
        return givenValue <= searchValue;
    }

    isGivenValueGreaterThanOrEqualToSearchValue(givenValue: string, searchValue: string): boolean {
        return givenValue >= searchValue;
    }

    isGivenValueBetweenSearchValues({givenValue, searchValue1, searchValue2, valueType}: FilterBetweenParams): boolean {
        if (valueType === 'DateTime') {
            return new Date(givenValue) > new Date(searchValue1) && new Date(givenValue) < new Date(searchValue2);
        }
        return givenValue > searchValue1 && givenValue < searchValue2;
    }

    isGivenDateBeforeSearchDate(givenDate: string, searchDate: string): boolean {
        return new Date(givenDate) < new Date(searchDate);
    }
}