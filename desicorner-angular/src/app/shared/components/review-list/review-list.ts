import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Review, PaginatedReviews } from '@core/models/review.models';
import { ReviewItemComponent } from '../review-item/review-item';

@Component({
  selector: 'app-review-list',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatIconModule,
    MatSelectModule,
    MatFormFieldModule,
    MatProgressSpinnerModule,
    ReviewItemComponent
  ],
  template: `
    <div class="review-list">
      <div class="list-header">
        <h3>Customer Reviews ({{ paginatedReviews?.totalCount || 0 }})</h3>
        
        <mat-form-field appearance="outline" class="sort-select">
          <mat-label>Sort by</mat-label>
          <mat-select [value]="sortBy" (selectionChange)="onSortChange($event.value)">
            <mat-option value="newest">Newest First</mat-option>
            <mat-option value="oldest">Oldest First</mat-option>
            <mat-option value="highest">Highest Rated</mat-option>
            <mat-option value="lowest">Lowest Rated</mat-option>
            <mat-option value="helpful">Most Helpful</mat-option>
          </mat-select>
        </mat-form-field>
      </div>

      @if (loading) {
        <div class="loading">
          <mat-spinner diameter="40"></mat-spinner>
          <span>Loading reviews...</span>
        </div>
      } @else if (paginatedReviews && paginatedReviews.reviews.length > 0) {
        <div class="reviews">
          @for (review of paginatedReviews.reviews; track review.id) {
            <app-review-item
              [review]="review"
              (edit)="onEdit($event)"
              (delete)="onDelete($event)"
              (vote)="onVote($event)">
            </app-review-item>
          }
        </div>

        @if (paginatedReviews.totalPages > 1) {
          <div class="pagination">
            <button 
              mat-button 
              (click)="onPageChange(currentPage - 1)"
              [disabled]="currentPage === 1">
              <mat-icon>chevron_left</mat-icon>
              Previous
            </button>
            
            <span class="page-info">
              Page {{ currentPage }} of {{ paginatedReviews.totalPages }}
            </span>
            
            <button 
              mat-button 
              (click)="onPageChange(currentPage + 1)"
              [disabled]="currentPage === paginatedReviews.totalPages">
              Next
              <mat-icon>chevron_right</mat-icon>
            </button>
          </div>
        }
      } @else {
        <div class="empty-state">
          <mat-icon>rate_review</mat-icon>
          <p>No reviews yet. Be the first to review this product!</p>
        </div>
      }
    </div>
  `,
  styles: [`
    .review-list {
      margin-top: 24px;
    }

    .list-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 16px;
      flex-wrap: wrap;
      gap: 16px;
    }

    .list-header h3 {
      margin: 0;
      color: #333;
    }

    .sort-select {
      width: 180px;
    }

    .loading {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 40px;
      gap: 16px;
      color: #666;
    }

    .reviews {
      background: white;
      border-radius: 8px;
      border: 1px solid #eee;
    }

    .pagination {
      display: flex;
      justify-content: center;
      align-items: center;
      gap: 16px;
      margin-top: 24px;
    }

    .page-info {
      color: #666;
    }

    .empty-state {
      text-align: center;
      padding: 40px;
      color: #666;
    }

    .empty-state mat-icon {
      font-size: 48px;
      width: 48px;
      height: 48px;
      color: #ddd;
      margin-bottom: 16px;
    }
  `]
})
export class ReviewListComponent {
  @Input() paginatedReviews: PaginatedReviews | null = null;
  @Input() loading = false;
  @Input() currentPage = 1;
  @Input() sortBy = 'newest';

  @Output() pageChange = new EventEmitter<number>();
  @Output() sortChange = new EventEmitter<string>();
  @Output() editReview = new EventEmitter<Review>();
  @Output() deleteReview = new EventEmitter<Review>();
  @Output() voteReview = new EventEmitter<{ reviewId: string; isHelpful: boolean }>();

  onPageChange(page: number): void {
    this.pageChange.emit(page);
  }

  onSortChange(sortBy: string): void {
    this.sortChange.emit(sortBy);
  }

  onEdit(review: Review): void {
    this.editReview.emit(review);
  }

  onDelete(review: Review): void {
    this.deleteReview.emit(review);
  }

  onVote(event: { reviewId: string; isHelpful: boolean }): void {
    this.voteReview.emit(event);
  }
}