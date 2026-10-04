import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { PolicyDetail, PolicySummary, UploadResult } from './policy.models';

@Injectable({ providedIn: 'root' })
export class PolicyApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/policies';

  list(): Observable<PolicySummary[]> {
    return this.http.get<PolicySummary[]>(this.baseUrl);
  }

  get(id: string): Observable<PolicyDetail> {
    return this.http.get<PolicyDetail>(`${this.baseUrl}/${id}`);
  }

  upload(name: string, file: File): Observable<UploadResult> {
    const form = new FormData();
    form.append('name', name);
    form.append('file', file, file.name);
    return this.http.post<UploadResult>(this.baseUrl, form);
  }
}
