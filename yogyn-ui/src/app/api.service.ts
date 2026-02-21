import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Studio {
  id: string;
  name: string;
  slug: string;
  timezone: string;
}

export interface Session {
  id: string;
  studioId: string;
  title: string;
  startsAt: string;
  durationMinutes: number;
  capacity: number;
  bookedCount: number;
  spotsLeft: number;
  isFull: boolean;
}

export interface StudioDetail {
  id: string;
  name: string;
  slug: string;
  timezone: string;
  requiresApproval: boolean;
  sessions: Session[];
}

export interface CreateBookingRequest {
  sessionId: string;
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private baseUrl = '/api';

  constructor(private http: HttpClient) {}

  getStudios(): Observable<Studio[]> {
    return this.http.get<Studio[]>(`${this.baseUrl}/studios`);
  }

  getStudio(id: string): Observable<StudioDetail> {
    return this.http.get<StudioDetail>(`${this.baseUrl}/studios/${id}`);
  }

  getSession(sessionId: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/sessions/${sessionId}`);
  }

  createBooking(request: CreateBookingRequest): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/bookings`, request);
  }

  getBooking(bookingId: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/bookings/${bookingId}`);
  }
}