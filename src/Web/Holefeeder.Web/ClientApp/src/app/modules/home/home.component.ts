import { Component, OnInit } from '@angular/core';
import { trace } from '@app/core/logger';
import { FooterComponent } from '@app/modules/home/footer/footer.component';
import { HeaderComponent } from '@app/modules/home/header/header.component';
import { ToastViewComponent } from '@app/shared/components';
import { LoadingBarModule } from '@ngx-loading-bar/core';
import { LoadingBarRouterModule } from '@ngx-loading-bar/router';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss'],
  standalone: true,
  imports: [
    LoadingBarRouterModule,
    LoadingBarModule,
    ToastViewComponent,
    HeaderComponent,
    FooterComponent,
  ],
})
export class HomeComponent implements OnInit {
  constructor() {}

  @trace()
  ngOnInit(): void {}
}