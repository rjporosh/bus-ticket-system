import { Component, inject } from '@angular/core';
import { TranslateService, Language } from '../../../core/services/translate.service';

@Component({
  selector: 'app-language-switcher',
  standalone: true,
  imports: [],
  template: `
    <div class="language-switcher">
      <button class="lang-btn" [class.active]="currentLanguage() === 'en'" (click)="setLanguage('en')" title="English">
        EN
      </button>
      <span class="lang-divider">|</span>
      <button class="lang-btn" [class.active]="currentLanguage() === 'bn'" (click)="setLanguage('bn')" title="বাংলা">
        BN
      </button>
    </div>
  `,
  styles: [
    `
      .language-switcher {
        display: flex;
        align-items: center;
        gap: 6px;
      }
      .lang-btn {
        background: transparent;
        border: 1px solid rgba(0,0,0,0.15);
        color: rgba(0,0,0,0.7);
        padding: 4px 10px;
        border-radius: 4px;
        cursor: pointer;
        font-size: 0.75rem;
        font-weight: 600;
        transition: all 0.2s;
      }
      .lang-btn:hover {
        border-color: rgba(0,0,0,0.4);
        color: rgba(0,0,0,0.9);
      }
      .lang-btn.active {
        background: rgba(0,0,0,0.08);
        border-color: rgba(0,0,0,0.6);
        color: rgba(0,0,0,0.95);
      }
      .lang-divider {
        color: rgba(0,0,0,0.25);
        font-size: 0.75rem;
      }
    `,
  ],
})
export class LanguageSwitcherComponent {
  private readonly translateService = inject(TranslateService);

  protected readonly currentLanguage = this.translateService.currentLanguage;

  setLanguage(lang: Language): void {
    this.translateService.language = lang;
  }
}
