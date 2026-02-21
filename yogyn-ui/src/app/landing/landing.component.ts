import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './landing.component.html',
  styleUrls: ['./landing.component.scss'],
})
export class LandingComponent {
  constructor(private router: Router) {}

  goToStudent() {
    this.router.navigate(['/studios']);
  }

  goToStudio() {
    alert('Studio admin panel coming soon!');
  }
}
