import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService } from '../api.service';

@Component({
  selector: 'app-booking-confirmation',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './booking-confirmation.component.html',
  styleUrl: './booking-confirmation.component.scss'
})
export class BookingConfirmationComponent implements OnInit {
  booking: any = null;
  loading = true;
  error: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private api: ApiService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.route.params.subscribe(params => {
      this.api.getBooking(params['bookingId']).subscribe({
        next: (booking) => {
          console.log('Booking:', booking);
          this.booking = booking;
          this.loading = false;
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Error', err);
          this.error = 'Could not load booking.';
          this.loading = false;
          this.cdr.detectChanges();
        }
      });
    });
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('en-GB', {
      weekday: 'long', month: 'long', day: 'numeric', year: 'numeric'
    });
  }

  formatTime(date: string): string {
    return new Date(date).toLocaleTimeString('en-GB', {
      hour: '2-digit', minute: '2-digit', hour12: true
    });
  }

  bookAnother() {
    this.router.navigate(['/studios']);
  }

  goHome() {
    this.router.navigate(['/']);
  }
}