import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService, Session } from '../api.service';

@Component({
  selector: 'app-booking-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './booking-form.component.html',
  styleUrl: './booking-form.component.scss'
})
export class BookingFormComponent implements OnInit {
  bookingForm: FormGroup;
  session: any = null;
  loading = true;
  submitting = false;
  error: string | null = null;

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private api: ApiService,
    private cdr: ChangeDetectorRef
  ) {
    this.bookingForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', [Validators.pattern(/^[+]?[(]?[0-9]{3}[)]?[-\s.]?[0-9]{3}[-\s.]?[0-9]{4,6}$/)]],
      agreedToPolicy: [false, [Validators.requiredTrue]]
    });
  }

  ngOnInit() {
    this.route.params.subscribe(params => {
      this.api.getSession(params['sessionId']).subscribe({
        next: (session) => {
          this.session = session;
          this.loading = false;
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Session load error', err);
          this.error = 'Could not load session details.';
          this.loading = false;
          this.cdr.detectChanges();
        }
      });
    });
  }

  get firstName() { return this.bookingForm.get('firstName'); }
  get lastName() { return this.bookingForm.get('lastName'); }
  get email() { return this.bookingForm.get('email'); }
  get phone() { return this.bookingForm.get('phone'); }
  get agreedToPolicy() { return this.bookingForm.get('agreedToPolicy'); }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('en-GB', {
      weekday: 'long', month: 'short', day: 'numeric', year: 'numeric'
    });
  }

  formatTime(date: string): string {
    return new Date(date).toLocaleTimeString('en-GB', {
      hour: '2-digit', minute: '2-digit', hour12: true
    });
  }

  onSubmit() {
    if (this.bookingForm.invalid) {
      Object.keys(this.bookingForm.controls).forEach(key => {
        this.bookingForm.get(key)?.markAsTouched();
      });
      return;
    }

    this.submitting = true;
    this.error = null;

    const { firstName, lastName, email, phone } = this.bookingForm.value;

    this.api.createBooking({
      sessionId: this.session.id,
      firstName,
      lastName,
      email,
      phone: phone || undefined
    }).subscribe({
      next: (booking) => {
        this.router.navigate(['/confirmation', booking.id]);
      },
      error: (err) => {
        console.error('Booking error', err);
        this.error = err.error?.error || 'Booking failed. Please try again.';
        this.submitting = false;
        this.cdr.detectChanges();
      }
    });
  }

  goBack() {
    history.back();
  }
}