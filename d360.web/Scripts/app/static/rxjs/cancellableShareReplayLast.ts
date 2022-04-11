import { MonoTypeOperatorFunction, Observable, Subscriber, Subscription } from "rxjs";

/**
 * This is like shareReplay(1)
 * But when all subscribers unsubscribe, we unsubscribe from original subscription
 * This means that we will perform actual http cancelling
 */
 export function cancellableShareReplayLast<T>(): MonoTypeOperatorFunction<T> {
    return (source: Observable<T>) => {
        let innerSubscription: Subscription = null;
        let outerSubscribers: Set<Subscriber<T>> = new Set();
        let lastResult: { resultType: 'value', value: T } | { resultType: 'error', error: any } = null;
        let isCompleted = false;

        const tryCreateInnerSubscription = () => {
            if (innerSubscription != null) {
                return;
            }

            if (isCompleted) {
                return;
            }

            innerSubscription = source.subscribe({
                next: (value) => {
                    for (const subscriber of Array.from(outerSubscribers)) {
                        subscriber.next(value);
                    }

                    lastResult = { resultType: 'value', value };
                },
                error: (error) => {
                    for (const subscriber of Array.from(outerSubscribers)) {
                        subscriber.error(error);
                    }

                    lastResult = { resultType: 'error', error };
                },
                complete: () => {
                    for (const subscriber of Array.from(outerSubscribers)) {
                        subscriber.complete();
                    }

                    isCompleted = true;
                },
            });
        };

        const tryDestroyInnerSubscription = () => {
            if (outerSubscribers.size > 0) {
                return;
            }

            if (innerSubscription == null) {
                return;
            }

            innerSubscription.unsubscribe();
            innerSubscription = null;
        };

        const tryEmitLastValue = (subscriber: Subscriber<T>) => {
            if (lastResult != null && lastResult.resultType === 'value') {
                subscriber.next(lastResult.value);
            }

            if (lastResult != null && lastResult.resultType === 'error') {
                subscriber.error(lastResult.error);
            }

            if (isCompleted) {
                subscriber.complete();
            }
        };


        return new Observable<T>((subscriber) => {
            tryEmitLastValue(subscriber);
            outerSubscribers.add(subscriber);
            tryCreateInnerSubscription();

            return {
                unsubscribe: () => {
                    outerSubscribers.delete(subscriber);
                    tryDestroyInnerSubscription();
                }
            };
        });
    };
}
