import { Injectable } from '@angular/core';
import { AdvancedFilterFieldCondition, ConnectingOperator, FilterBetweenParams, Filters } from '../components/assets-grid/advanced-filtering/advanced-filtering.models';
import { remove } from 'lodash';
import { OperatorString } from '../models/operator.model';
import { MessagesObservableService } from './messages-observable.service';

export type FilteredData = any[];

@Injectable({
    providedIn: 'root'
})
export class UiAdvancedFiltering {
    constructor(private messagesService: MessagesObservableService) {}

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
        let filterOptions = {
            [OperatorString.Contains]: (filterOption: AdvancedFilterFieldCondition) => {
                filtredData = filtredData.filter((elementToFilter: object) => {
                    return this.isDataValueContainsSearchValue(elementToFilter[filterOption.field], filterOption.value);
                });
            },
            [OperatorString.NotContains]: (filterOption: AdvancedFilterFieldCondition) => {
                filtredData = filtredData.filter((elementToFilter: object) => {
                    return !this.isDataValueContainsSearchValue(elementToFilter[filterOption.field], filterOption.value);
                });
            },
            [OperatorString.Equals]: (filterOption: AdvancedFilterFieldCondition) => {
                filtredData = filtredData.filter((elementToFilter: object) => {
                    return this.isDataValueEqualToSearchValue(elementToFilter[filterOption.field], filterOption.value, filterOption.fieldType);
                });
            },
            [OperatorString.NotEquals]: (filterOption: AdvancedFilterFieldCondition) => {
                filtredData = filtredData.filter((elementToFilter: object) => {
                    return !this.isDataValueEqualToSearchValue(elementToFilter[filterOption.field], filterOption.value, filterOption.fieldType);
                });
            },
            [OperatorString.StartsWith]: (filterOption: AdvancedFilterFieldCondition) => {
                filtredData = filtredData.filter((elementToFilter: object) => {
                    return this.isDataValueStartsWithSearchValue(elementToFilter[filterOption.field], filterOption.value);
                });
            },
            [OperatorString.EndsWith]: (filterOption: AdvancedFilterFieldCondition) => {
                filtredData = filtredData.filter((elementToFilter: object) => {
                    return this.isDataValueEndsWithSearchValue(elementToFilter[filterOption.field], filterOption.value);
                });
            },
            [OperatorString.Populated]: (filterOption: AdvancedFilterFieldCondition) => {
                filtredData = filtredData.filter((elementToFilter: object) => {
                    return this.isDataValuePopulated(elementToFilter[filterOption.field]);
                });
            },
            [OperatorString.NotPopulated]: (filterOption: AdvancedFilterFieldCondition) => {
                filtredData = filtredData.filter((elementToFilter: object) => {
                    return !this.isDataValuePopulated(elementToFilter[filterOption.field]);
                });
            },
            [OperatorString.LessThan]: (filterOption: AdvancedFilterFieldCondition) => {
                filtredData = filtredData.filter((elementToFilter: object) => {
                    return this.isGivenValueLessThanSearchValue(elementToFilter[filterOption.field], filterOption.value);
                });
            },
            [OperatorString.GreaterThan]: (filterOption: AdvancedFilterFieldCondition) => {
                filtredData = filtredData.filter((elementToFilter: object) => {
                    return this.isGivenValueGreaterThanSearchValue(elementToFilter[filterOption.field], filterOption.value);
                });
            },
            [OperatorString.LessThanOrEquals]: (filterOption: AdvancedFilterFieldCondition) => {
                filtredData = filtredData.filter((elementToFilter: object) => {
                    return this.isGivenValueLessThanOrEqualToSearchValue(elementToFilter[filterOption.field], filterOption.value);
                });
            },
            [OperatorString.GreaterThanOrEquals]: (filterOption: AdvancedFilterFieldCondition) => {
                filtredData = filtredData.filter((elementToFilter: object) => {
                    return this.isGivenValueGreaterThanOrEqualToSearchValue(elementToFilter[filterOption.field], filterOption.value);
                });
            },
            [OperatorString.Between]: (filterOption: AdvancedFilterFieldCondition) => {
                filtredData = filtredData.filter((elementToFilter: object) => {
                    const params: FilterBetweenParams = {
                        givenValue: elementToFilter[filterOption.field],
                        searchValue1: filterOption.value,
                        searchValue2: filterOption.value2,
                        valueType: filterOption.fieldType
                    };
                    return this.isGivenValueBetweenSearchValues(params);
                });
            },
            [OperatorString.Before]: (filterOption: AdvancedFilterFieldCondition) => {
                filtredData = filtredData.filter((elementToFilter: object) => {
                    return this.isGivenDateBeforeSearchDate(elementToFilter[filterOption.field], filterOption.value);
                });
            },
            [OperatorString.After]: (filterOption: AdvancedFilterFieldCondition) => {
                filtredData = filtredData.filter((elementToFilter: object) => {
                    return !this.isGivenDateBeforeSearchDate(elementToFilter[filterOption.field], filterOption.value);
                });
            },
        }

        filters.data.forEach((filterOption: AdvancedFilterFieldCondition) => {
            if(filterOptions.hasOwnProperty(filterOption.operator)){
                filterOptions[filterOption.operator](filterOption);
            } else {
                this.showFilterNotFoundMessage(filterOption);
            }
        });
        return filtredData;
    }

    filterByOrLogic(dataToFilter: ReadonlyArray<any>, filters: Filters): FilteredData {
        let filterResult = [];
        let fullData = [...dataToFilter];
        let filteredData = [];

        let filterOptions = {
            [OperatorString.Contains]: (filterOption: AdvancedFilterFieldCondition) => {
                filteredData = remove(fullData, (elementToFilter: object) => {
                    return this.isDataValueContainsSearchValue(elementToFilter[filterOption.field], filterOption.value);
                });
                filterResult = [...filterResult, ...filteredData];
            },
            [OperatorString.NotContains]: (filterOption: AdvancedFilterFieldCondition) => {
                filteredData = remove(fullData, (elementToFilter: object) => {
                    return !this.isDataValueContainsSearchValue(elementToFilter[filterOption.field], filterOption.value);
                });
                filterResult = [...filterResult, ...filteredData];
            },
            [OperatorString.Equals]: (filterOption: AdvancedFilterFieldCondition) => {
                filteredData = remove(fullData, (elementToFilter: object) => {
                    return this.isDataValueEqualToSearchValue(elementToFilter[filterOption.field], filterOption.value, filterOption.fieldType);
                });
                filterResult = [...filterResult, ...filteredData];
            },
            [OperatorString.NotEquals]: (filterOption: AdvancedFilterFieldCondition) => {
                filteredData = remove(fullData, (elementToFilter: object) => {
                    return !this.isDataValueEqualToSearchValue(elementToFilter[filterOption.field], filterOption.value, filterOption.fieldType);
                });
                filterResult = [...filterResult, ...filteredData];
            },
            [OperatorString.StartsWith]: (filterOption: AdvancedFilterFieldCondition) => {
                filteredData = remove(fullData, (elementToFilter: object) => {
                    return this.isDataValueStartsWithSearchValue(elementToFilter[filterOption.field], filterOption.value);
                });
                filterResult = [...filterResult, ...filteredData];
            },
            [OperatorString.EndsWith]: (filterOption: AdvancedFilterFieldCondition) => {
                filteredData = remove(fullData, (elementToFilter: object) => {
                    return this.isDataValueEndsWithSearchValue(elementToFilter[filterOption.field], filterOption.value);
                });
                filterResult = [...filterResult, ...filteredData];
            },
            [OperatorString.Populated]: (filterOption: AdvancedFilterFieldCondition) => {
                filteredData = remove(fullData, (elementToFilter: object) => {
                    return this.isDataValuePopulated(elementToFilter[filterOption.field]);
                });
                filterResult = [...filterResult, ...filteredData];
            },
            [OperatorString.NotPopulated]: (filterOption: AdvancedFilterFieldCondition) => {
                filteredData = remove(fullData, (elementToFilter: object) => {
                    return !this.isDataValuePopulated(elementToFilter[filterOption.field]);
                });
                filterResult = [...filterResult, ...filteredData];
            },
            [OperatorString.LessThan]: (filterOption: AdvancedFilterFieldCondition) => {
                filteredData = remove(fullData, (elementToFilter: object) => {
                    return this.isGivenValueLessThanSearchValue(elementToFilter[filterOption.field], filterOption.value);
                });
                filterResult = [...filterResult, ...filteredData];
            },
            [OperatorString.GreaterThan]: (filterOption: AdvancedFilterFieldCondition) => {
                filteredData = remove(fullData, (elementToFilter: object) => {
                    return this.isGivenValueGreaterThanSearchValue(elementToFilter[filterOption.field], filterOption.value);
                });
                filterResult = [...filterResult, ...filteredData];
            },
            [OperatorString.LessThanOrEquals]: (filterOption: AdvancedFilterFieldCondition) => {
                filteredData = remove(fullData, (elementToFilter: object) => {
                    return this.isGivenValueLessThanOrEqualToSearchValue(elementToFilter[filterOption.field], filterOption.value);
                });
                filterResult = [...filterResult, ...filteredData];
            },
            [OperatorString.GreaterThanOrEquals]: (filterOption: AdvancedFilterFieldCondition) => {
                filteredData = remove(fullData, (elementToFilter: object) => {
                    return this.isGivenValueGreaterThanOrEqualToSearchValue(elementToFilter[filterOption.field], filterOption.value);
                });
                filterResult = [...filterResult, ...filteredData];
            },
            [OperatorString.Between]: (filterOption: AdvancedFilterFieldCondition) => {
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
            },
            [OperatorString.Before]: (filterOption: AdvancedFilterFieldCondition) => {
                filteredData = remove(fullData, (elementToFilter: object) => {
                    return this.isGivenDateBeforeSearchDate(elementToFilter[filterOption.field], filterOption.value);
                });
                filterResult = [...filterResult, ...filteredData];
            },
            [OperatorString.After]: (filterOption: AdvancedFilterFieldCondition) => {
                filteredData = remove(fullData, (elementToFilter: object) => {
                    return !this.isGivenDateBeforeSearchDate(elementToFilter[filterOption.field], filterOption.value);
                });
                filterResult = [...filterResult, ...filteredData];
            },
        }

        filters.data.forEach((filterOption: AdvancedFilterFieldCondition) => {
            if(filterOptions.hasOwnProperty(filterOption.operator)){
                filterOptions[filterOption.operator](filterOption);
            } else {
                this.showFilterNotFoundMessage(filterOption);
            }
        });
        return filterResult;
    }

    showFilterNotFoundMessage(filterOption: AdvancedFilterFieldCondition): void {
        this.messagesService.showInfoMessage(`Unknown filter`, `Unknown Advanced Filter '${filterOption.operator}'`);
    }

    isDataValueContainsSearchValue(dataValue: string, searchValue: string): boolean {
        // RegExp need to be build dynamically
        /* eslint-disable-next-line detect-non-literal-regexp */
        return Boolean(dataValue.match(new RegExp(searchValue, 'i')));
    }

    isDataValueEqualToSearchValue(dataValue: string, searchValue: string, valueType: string): boolean {
        if (valueType === 'Text') {
            return dataValue.toLowerCase() === searchValue.toLowerCase();
        } else if (valueType === 'Number') {
            return Number(dataValue) === Number(searchValue);
        } else {
            this.messagesService.showInfoMessage(`Unknown FilterFieldType`, `Not recognized FilterFieldType`);
        }
    }

    isDataValueStartsWithSearchValue(dataValue: string, searchValue: string): boolean {
        return dataValue.toLowerCase().startsWith(searchValue.toLowerCase());
    }

    isDataValueEndsWithSearchValue(dataValue: string, searchValue: string): boolean {
        return dataValue.toLowerCase().endsWith(searchValue.toLowerCase());
    }

    isDataValuePopulated(dataValue: string | number): boolean {
        return typeof dataValue !== 'undefined' && dataValue !== null && dataValue !== '';
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
