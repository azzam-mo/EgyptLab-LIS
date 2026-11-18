import { describe, expect, it } from 'vitest';
import { apiUrl } from './api';

describe('apiUrl', () => {
  it('joins the configured base URL and path without duplicate slashes', () => {
    expect(apiUrl('/health', 'http://localhost:5080/')).toBe(
      'http://localhost:5080/health',
    );
  });

  it('returns a root-relative path when no base URL is configured', () => {
    expect(apiUrl('health', '')).toBe('/health');
  });
});
