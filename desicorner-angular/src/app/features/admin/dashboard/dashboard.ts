import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ProductService } from '@core/services/product.service';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule
  ],
  templateUrl: './dashboard.html',
  styleUrls: ['./dashboard.scss']
})
export class AdminDashboardComponent implements OnInit {
  private productService = inject(ProductService);

  stats = {
    totalProducts: 0,
    totalCategories: 0,
    activeProducts: 0
  };

  ngOnInit(): void {
    this.loadStats();
  }

  private loadStats(): void {
    this.productService.loadProducts().subscribe({
      next: (response) => {
        if (response.isSuccess && response.result) {
          this.stats.totalProducts = response.result.length;
          this.stats.activeProducts = response.result.filter(p => p.isAvailable).length;
        }
      }
    });

    this.productService.loadCategories().subscribe({
      next: (response) => {
        if (response.isSuccess && response.result) {
          this.stats.totalCategories = response.result.length;
        }
      }
    });
  }
}