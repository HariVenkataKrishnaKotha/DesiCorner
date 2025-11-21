import { Injectable } from '@angular/core';
import { v4 as uuidv4 } from 'uuid';

@Injectable({
  providedIn: 'root'
})
export class GuestSessionService {
  private readonly SESSION_KEY = 'desicorner_guest_session';

  getSessionId(): string {
    let sessionId = localStorage.getItem(this.SESSION_KEY);
    
    if (!sessionId) {
      sessionId = uuidv4();
      localStorage.setItem(this.SESSION_KEY, sessionId);
    }
    
    return sessionId;
  }

  clearSession(): void {
    localStorage.removeItem(this.SESSION_KEY);
  }

  hasSession(): boolean {
    return !!localStorage.getItem(this.SESSION_KEY);
  }
}