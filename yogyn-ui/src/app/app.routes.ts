import { Routes } from '@angular/router';

export const routes: Routes = [
  // Landing page
  {
    path: '',
    loadComponent: () => import('./landing/landing.component').then(m => m.LandingComponent)
  },

  // Studios list (NEW)
  {
    path: 'studios',
    loadComponent: () => import('./studio-list/studio-list.component').then(m => m.StudioListComponent)
  },

  // STUDENT FLOW
  {
    path: 'studio/:studioId',
    loadComponent: () => import('./studio-sessions/studio-sessions.component')
      .then(m => m.StudioSessionsComponent)
  },
  {
    path: 'book/:sessionId',
    loadComponent: () => import('./booking-form/booking-form.component').then(m => m.BookingFormComponent)
  },
  {
    path: 'confirmation/:bookingId',
    loadComponent: () => import('./booking-confirmation/booking-confirmation.component').then(m => m.BookingConfirmationComponent)
  },

  // 404
  {
    path: '**',
    redirectTo: ''
  }
];