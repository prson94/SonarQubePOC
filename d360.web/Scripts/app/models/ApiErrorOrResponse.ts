export type ApiErrorOrResponse<T> = {
    type: 'error',
    title: string,
    message: string
} | ({ type: undefined } & T)