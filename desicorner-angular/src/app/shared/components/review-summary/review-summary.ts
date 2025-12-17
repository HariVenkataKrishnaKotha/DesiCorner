import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { ReviewSummary } from '@core/models/review.models';
import { StarRatingComponent } from '../star-rating/star-rating';

@Component({
  selector: 'app-review-summary',
  standalone: true,
  imports: [CommonModule, MatProgressBarModule, StarRatingComponent],
  template: `
    @if (summary) {
      <div class="review-summary">
        <div class="summary-header">
          <div class="average-rating">
            <span class="rating-number">{{ summary.averageRating.toFixed(1) }}</span>
            <app-star-rating 
              [rating]="summary.averageRating" 
              [readonly]="true">
            </app-star-rating>
            <span class="total-reviews">{{ summary.totalReviews }} reviews</span>
          </div>
        </div>

        <div class="rating-breakdown">
          <div class="rating-row">
            <span class="star-label">5 stars</span>
            <mat-progress-bar 
              mode="determinate" 
              [value]="summary.fiveStarPercent"
              color="primary">
            </mat-progress-bar>
            <span class="count">{{ summary.fiveStarCount }}</span>
          </div>

          <div class="rating-row">
            <span class="star-label">4 stars</span>
            <mat-progress-bar 
              mode="determinate" 
              [value]="summary.fourStarPercent"
              color="primary">
            </mat-progress-bar>
            <span class="count">{{ summary.fourStarCount }}</span>
          </div>

          <div class="rating-row">
            <span class="star-label">3 stars</span>
            <mat-progress-bar 
              mode="determinate" 
              [value]="summary.threeStarPercent"
              color="primary">
            </mat-progress-bar>
            <span class="count">{{ summary.threeStarCount }}</span>
          </div>

          <div class="rating-row">
            <span class="star-label">2 stars</span>
            <mat-progress-bar 
              mode="determinate" 
              [value]="summary.twoStarPercent"
              color="primary">
            </mat-progress-bar>
            <span class="count">{{ summary.twoStarCount }}</span>
          </div>

          <div class="rating-row">
            <span class="star-label">1 star</span>
            <mat-progress-bar 
              mode="determinate" 
              [value]="summary.oneStarPercent"
              color="primary">
            </mat-progress-bar>
            <span class="count">{{ summary.oneStarCount }}</span>
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    .review-summary {
      padding: 16px;
      background: #f9f9f9;
      border-radius: 8px;
    }

    .summary-header {
      margin-bottom: 16px;
      text-align: center;
    }

    .average-rating {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 8px;
    }

    .rating-number {
      font-size: 3rem;
      font-weight: 700;
      color: #333;
      line-height: 1;
    }

    .total-reviews {
      color: #666;
      font-size: 0.9rem;
    }

    .rating-breakdown {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .rating-row {
      display: flex;
      align-items: center;
      gap: 12px;
    }

    .star-label {
      width: 60px;
      font-size: 0.85rem;
      color: #666;
    }

    mat-progress-bar {
      flex: 1;
      height: 8px;
      border-radius: 4px;
    }

    .count {
      width: 30px;
      text-align: right;
      font-size: 0.85rem;
      color: #666;
    }
  `]
})
export class ReviewSummaryComponent {
  @Input() summary: ReviewSummary | null = null;
}