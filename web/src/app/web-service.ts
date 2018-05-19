import { Observable, of } from 'rxjs';
import { ServiceError } from './service-error';
import { HttpErrorResponse } from '@angular/common/http';

export class WebService {
    /**
    * Handle Http operation that failed.
    * Let the app continue.
    * @param operation - name of the operation that failed
    * @param result - optional value to return as the observable result
    */
    protected handleError<T>(operation = 'operation', result?: T) {
        return (error: any): Observable<T | ServiceError> => {

            console.error(error);
            const svcError = new ServiceError();
            if ((<HttpErrorResponse>error).status !== undefined) {
                svcError.resultCode = (<HttpErrorResponse>error).status;
                svcError.message = (<HttpErrorResponse>error).message;
            } else {
                svcError.resultCode = 0;
                svcError.message = 'An unexpected error occurred.';
            }
            return of(svcError);
        };
    }
}

export function isError<T>(obj: T | ServiceError): obj is ServiceError {
    return ((<ServiceError>obj).message !== undefined);
}
