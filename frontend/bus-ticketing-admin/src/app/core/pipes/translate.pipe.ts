import { Pipe, PipeTransform, inject } from '@angular/core';
import { TranslateService, Translations } from '../services/translate.service';

@Pipe({ name: 'translate', standalone: true, pure: false })
export class TranslatePipe implements PipeTransform {
  private readonly translateService = inject(TranslateService);
  private currentValue = '';

  transform(key: string, interpolateParams?: Record<string, any>): string {
    if (!key) return '';
    const translations = this.translateService['_translations'].value;
    const current = translations[this.translateService.language] ?? {};
    const text = this.resolve(current, key) ?? key;
    this.currentValue = this.interpolate(text, interpolateParams);
    return this.currentValue;
  }

  private resolve(translations: Translations, key: string): string | undefined {
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
}
