import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { ApiService, Studio } from '../api.service';

@Component({
  selector: 'app-studio-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './studio-list.component.html',
  styleUrl: './studio-list.component.scss'
})
export class StudioListComponent implements OnInit {
  studios: Studio[] = [];
  loading = true;
  error: string | null = null;

  constructor(
    private api: ApiService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.api.getStudios().subscribe({
      next: (studios) => {
        this.studios = studios;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error:', err);
        this.error = 'Could not load studios.';
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  goToStudio(id: string) {
    this.router.navigate(['/studio', id]);
  }
}