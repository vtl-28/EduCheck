import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ToastService } from './shared/services/toast';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class AppComponent {
  protected readonly title = signal('EduCheck.UI');
  constructor(public toastService: ToastService) {}
}

