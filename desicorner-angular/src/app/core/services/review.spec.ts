import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '@env/environment';
import { CreateReviewRequest, PaginatedReviews, Review, ReviewSummary, UpdateReviewRequest } from '../models/review.models';
import { ReviewService } from './review.service';

describe('ReviewService', () => {
  const reviewsUrl = `${environment.gatewayUrl}/api/reviews`;
  let service: ReviewService;
  let httpMock: HttpTestingController;

  const review = { id: 'r1', productId: 'p1', rating: 5 } as Review;
  const page: PaginatedReviews = { reviews: [review], totalCount: 1, page: 1, pageSize: 10, totalPages: 1 };

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(ReviewService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getProductReviews GETs the product reviews with default paging and sort, unwrapping the result', () => {
    let received: PaginatedReviews | undefined;
    service.getProductReviews('p1').subscribe(r => (received = r));

    const req = httpMock.expectOne(r => r.url === `${reviewsUrl}/product/p1`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('10');
    expect(req.request.params.get('sortBy')).toBe('newest');
    req.flush({ isSuccess: true, result: page });

    expect(received).toEqual(page);
  });

  it('getProductReviews passes explicit paging and sort', () => {
    service.getProductReviews('p1', 3, 5, 'highest').subscribe();

    const req = httpMock.expectOne(r => r.url === `${reviewsUrl}/product/p1`);
    expect(req.request.params.toString()).toBe('page=3&pageSize=5&sortBy=highest');
    req.flush({ isSuccess: true, result: page });
  });

  it('getReviewSummary GETs /product/{id}/summary and unwraps the result', () => {
    const summary = { productId: 'p1', averageRating: 4.5, totalReviews: 2 } as ReviewSummary;
    let received: ReviewSummary | undefined;
    service.getReviewSummary('p1').subscribe(r => (received = r));

    const req = httpMock.expectOne(`${reviewsUrl}/product/p1/summary`);
    expect(req.request.method).toBe('GET');
    req.flush({ isSuccess: true, result: summary });

    expect(received).toEqual(summary);
  });

  it('getUserReview GETs /product/{id}/my-review and maps a missing review to null', () => {
    const results: (Review | null)[] = [];
    service.getUserReview('p1').subscribe(r => results.push(r));
    httpMock.expectOne(`${reviewsUrl}/product/p1/my-review`).flush({ isSuccess: true, result: review });

    service.getUserReview('p2').subscribe(r => results.push(r));
    httpMock.expectOne(`${reviewsUrl}/product/p2/my-review`).flush({ isSuccess: true, result: null });

    service.getUserReview('p3').subscribe(r => results.push(r));
    httpMock.expectOne(`${reviewsUrl}/product/p3/my-review`).flush({ isSuccess: true });

    expect(results).toEqual([review, null, null]);
  });

  it('getReview GETs /{reviewId} and unwraps the result', () => {
    let received: Review | undefined;
    service.getReview('r1').subscribe(r => (received = r));

    const req = httpMock.expectOne(`${reviewsUrl}/r1`);
    expect(req.request.method).toBe('GET');
    req.flush({ isSuccess: true, result: review });

    expect(received).toEqual(review);
  });

  it('createReview POSTs the request and returns the created review', () => {
    const request: CreateReviewRequest = { productId: 'p1', rating: 5, title: 'Great', comment: 'Crispy' };
    let received: Review | undefined;
    service.createReview(request).subscribe(r => (received = r));

    const req = httpMock.expectOne(reviewsUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush({ isSuccess: true, result: review });

    expect(received).toEqual(review);
  });

  it('updateReview PUTs the request to /{id}', () => {
    const request: UpdateReviewRequest = { id: 'r1', rating: 4, comment: 'Still good' };
    service.updateReview(request).subscribe();

    const req = httpMock.expectOne(`${reviewsUrl}/r1`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush({ isSuccess: true, result: review });
  });

  it('deleteReview DELETEs /{id} and completes with no value', () => {
    let received: unknown = 'not called';
    let completed = false;
    service.deleteReview('r1').subscribe({ next: r => (received = r), complete: () => (completed = true) });

    const req = httpMock.expectOne(`${reviewsUrl}/r1`);
    expect(req.request.method).toBe('DELETE');
    req.flush({ isSuccess: true });

    expect(received).toBeUndefined();
    expect(completed).toBeTrue();
  });

  it('voteOnReview POSTs the vote to /{reviewId}/vote (ReviewVoteDto shape)', () => {
    service.voteOnReview({ reviewId: 'r1', isHelpful: true }).subscribe();

    const req = httpMock.expectOne(`${reviewsUrl}/r1/vote`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ reviewId: 'r1', isHelpful: true });
    req.flush({ isSuccess: true, result: review });
  });

  it('surfaces HTTP errors to the caller', () => {
    let status: number | undefined;
    service.createReview({ productId: 'p1', rating: 5 }).subscribe({ error: e => (status = e.status) });

    httpMock.expectOne(reviewsUrl).flush({ message: 'Already reviewed' }, { status: 400, statusText: 'Bad Request' });

    expect(status).toBe(400);
  });
});
