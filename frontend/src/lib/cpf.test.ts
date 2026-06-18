import { describe, it, expect } from 'vitest';
import { isValidCpf, formatCpf } from './cpf';

describe('CPF Utilities', () => {
  it('cpf_ValidateLuhn_CorrectlyValidatesKnownCpfs', () => {
    // A known valid CPF (we can use a generated one that passes Luhn)
    // For example: 000.000.000-00 is usually invalid
    // 52998224725 passes Luhn
    expect(isValidCpf('52998224725')).toBe(true);
    expect(isValidCpf('529.982.247-25')).toBe(true);

    // Known invalid
    expect(isValidCpf('111.111.111-11')).toBe(false);
    expect(isValidCpf('000.000.000-00')).toBe(false);
    expect(isValidCpf('123.456.789-00')).toBe(false);
  });

  it('formatCpf_FormatsCorrectly', () => {
    expect(formatCpf('12345678909')).toBe('123.456.789-09');
    expect(formatCpf('123')).toBe('123');
    expect(formatCpf('123456')).toBe('123.456');
  });

  it('isValidCpf_HandlesNonStrings', () => {
    expect(isValidCpf(null as any)).toBe(false);
    expect(isValidCpf(undefined as any)).toBe(false);
    expect(isValidCpf(123 as any)).toBe(false);
  });
});

