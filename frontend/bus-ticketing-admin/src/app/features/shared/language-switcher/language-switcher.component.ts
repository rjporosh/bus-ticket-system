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
        border: 1px solid rgba(255,255,255,0.2);
        color: rgba(255,255,255,0.8);
        padding: 4px 10px;
        border-radius: 4px;
        cursor: pointer;
        font-size: 0.75rem;
        font-weight: 600;
        transition: all 0.2s;
      }
      .lang-btn:hover {
        border-color: rgba(255,255,255,0.5);
        color: white;
      }
      .lang-btn.active {
        background: rgba(255,255,255,0.15);
        border-color: white;
        color: white;
      }
      .lang-divider {
        color: rgba(255,255,255,0.3);
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
