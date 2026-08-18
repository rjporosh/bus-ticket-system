import { Injectable, signal, effect } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { catchError, map, shareReplay, distinctUntilChanged } from 'rxjs/operators';

export type Language = 'en' | 'bn';

const DEFAULT_LANGUAGE: Language = 'en';
const STORAGE_KEY = 'app-language';

export interface Translations {
  [key: string]: string | Translations;
}

@Injectable({ providedIn: 'root' })
export class TranslateService {
  private readonly _currentLanguage = signal<Language>(this.getStoredLanguage());
  private readonly _translations = new BehaviorSubject<Record<Language, Translations>>({} as Record<Language, Translations>);
  private readonly _loading = signal(false);

  readonly currentLanguage = this._currentLanguage.asReadonly();
  readonly loading = this._loading.asReadonly();

  constructor(private readonly http: HttpClient) {
    effect(() => {
      const lang = this._currentLanguage();
      this.loadLanguage(lang).subscribe();
    });
  }

  get language(): Language {
    return this._currentLanguage();
  }

  set language(lang: Language) {
    this._currentLanguage.set(lang);
    localStorage.setItem(STORAGE_KEY, lang);
  }

  get(key: string, interpolateParams?: Record<string, any>): Observable<string> {
    return this._translations.pipe(
      // map() will automatically fire every time _translations gets updated!
      map(translations => {
        const current = translations[this._currentLanguage()] ?? {};
        const text = this.resolveKey(current, key) ?? key;
        return this.interpolate(text, interpolateParams);
      }),
      // Prevent the UI from flickering/updating if the translation hasn't changed
      distinctUntilChanged() 
    );
  }

  getSync(key: string, interpolateParams?: Record<string, any>): string {
    const translations = this._translations.value;
    const current = translations[this._currentLanguage()] ?? {};
    const text = this.resolveKey(current, key) ?? key;
    return this.interpolate(text, interpolateParams);
  }

  private loadLanguage(lang: Language): Observable<Translations> {
    if (this._translations.value[lang]) {
      return of(this._translations.value[lang]);
    }

    this._loading.set(true);
    return this.http.get<Translations>(`assets/i18n/${lang}.json`).pipe(
      map(translations => {
        this._translations.next({ ...this._translations.value, [lang]: translations });
        this._loading.set(false);
        return translations;
      }),
      catchError((error) => {
        console.error(`[TranslateService] Failed to load translation file for language "${lang}":`, error);
        this._loading.set(false);
        return of({});
      }),
      shareReplay(1)
    );
  }

  private resolveKey(translations: Translations, key: string): string | undefined {
    const parts = key.split('.');
    let current: Translations | string = translations;
    for (const part of parts) {
      if (typeof current === 'string' || !current) return undefined;
      current = current[part];
    }
    return typeof current === 'string' ? current : undefined;
  }

  private interpolate(text: string, params?: Record<string, any>): string {
    if (!params) return text;
    return text.replace(/\{\{(\w+)\}\}/g, (_, match) => params[match] ?? match);
  }

  private getStoredLanguage(): Language {
    const stored = localStorage.getItem(STORAGE_KEY);
    return stored === 'bn' ? 'bn' : DEFAULT_LANGUAGE;
  }
}
