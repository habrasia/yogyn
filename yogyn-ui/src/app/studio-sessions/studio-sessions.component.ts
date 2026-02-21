import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService, Session } from '../api.service';

@Component({
  selector: 'app-studio-sessions',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './studio-sessions.component.html',
  styleUrl: './studio-sessions.component.scss'
})
export class StudioSessionsComponent implements OnInit {
  studioId: string = '';
  studioName: string = '';
  sessions: Session[] = [];
  loading: boolean = true;
  error: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private api: ApiService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.route.params.subscribe(params => {
      this.studioId = params['studioId'];
      this.loadStudio();
    });
  }

  loadStudio() {
    this.api.getStudio(this.studioId).subscribe({
      next: (studio) => {
        this.studioName = studio.name;
        this.sessions = studio.sessions; // sessions embedded in response!
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error', err);
        this.error = 'Could not load studio.';
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  getSpotsLeft(session: Session): number {
    return session.spotsLeft; // backend calculates this!
  }

  isFull(session: Session): boolean {
    return session.isFull; // backend calculates this!
  }

  isAlmostFull(session: Session): boolean {
    return session.spotsLeft > 0 && session.spotsLeft <= 3;
  }

  formatTime(date: string): string {
    return new Date(date).toLocaleTimeString('en-GB', {
      hour: '2-digit', minute: '2-digit', hour12: true
    });
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('en-GB', {
      weekday: 'long', month: 'short', day: 'numeric'
    });
  }

  bookSession(sessionId: string) {
    this.router.navigate(['/book', sessionId]);
  }
}