/*
   Copyright 2018 Digimarc, Inc

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.

   SPDX-License-Identifier: Apache-2.0
*/

import { Observable, of } from 'rxjs';
import { ServiceError } from './service-error';
import { HttpErrorResponse, HttpResponse } from '@angular/common/http';

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
                svcError.error = (<HttpErrorResponse>error).error;
            } else {
                svcError.resultCode = 0;
                svcError.message = 'An unexpected error occurred.';
            }
            return of(svcError);
        };
    }
}

export function isError<T>(obj: T | ServiceError): obj is ServiceError {
    return obj instanceof ServiceError;
}

export function isHttpString<T>(obj: T | HttpResponse<string>): obj is HttpResponse<string> {
    return obj instanceof HttpResponse;
}
